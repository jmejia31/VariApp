using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public class ProductionDataRepairService
{
    private readonly AppDbContext _context;

    public ProductionDataRepairService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RepairAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("""
            UPDATE `Usuarios`
            SET `Eliminado` = FALSE
            WHERE `Eliminado` = TRUE
              AND `FechaEliminacion` IS NULL
              AND `EliminadoPorUsuarioId` IS NULL;
            """);
    }
}
