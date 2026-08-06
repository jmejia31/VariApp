using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Exceptions;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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

    private static bool IsTransientError(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mysqlException && (mysqlException.Number == 1205 || mysqlException.Number == 1213))
            {
                return true;
            }
        }
        return false;
    }

    private Exception? TranslateException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mysqlException &&
                mysqlException.Number == 1062 &&
                mysqlException.Message.Contains("IX_TipoClientes_EsPredeterminadoUnico", StringComparison.Ordinal))
            {
                return new UniqueConstraintViolationException(
                    "TipoClientePredeterminadoUnico",
                    "Conflicto de concurrencia: Ya existe otro tipo de cliente marcado como predeterminado único. Inténtalo de nuevo.",
                    exception);
            }
        }
        return null;
    }
}
