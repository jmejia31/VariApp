using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Exceptions;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace InventoryApp.Infrastructure.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private const int MaxRetryCount = 3;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        int attempt = 0;
        while (true)
        {
            attempt++;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
                break;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                if (attempt < MaxRetryCount && IsTransientError(ex))
                {
                    await Task.Delay(100 * attempt);
                    continue;
                }

                var translated = TranslateException(ex);
                if (translated != null)
                {
                    throw translated;
                }
                throw;
            }
        }
    }

    private static bool IsTransientError(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            var msg = current.Message;
            if (msg.Contains("1205") || msg.Contains("1213") ||
                msg.Contains("Lock wait timeout") || msg.Contains("Deadlock"))
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }

    private Exception? TranslateException(Exception ex)
    {
        if (ex is DbUpdateException dbUpdateEx)
        {
            var inner = dbUpdateEx.InnerException;
            while (inner != null)
            {
                if (inner.Message.Contains("IX_TipoClientes_EsPredeterminadoUnico"))
                {
                    return new UniqueConstraintViolationException(
                        "TipoClientePredeterminadoUnico",
                        "Conflicto de concurrencia: Ya existe otro tipo de cliente marcado como predeterminado único. Inténtalo de nuevo.",
                        ex);
                }
                inner = inner.InnerException;
            }
        }
        return null;
    }
}
