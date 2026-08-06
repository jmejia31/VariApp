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

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var translated = TranslateException(ex);
                if (translated != null)
                {
                    throw translated;
                }
                throw;
            }
        });
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
