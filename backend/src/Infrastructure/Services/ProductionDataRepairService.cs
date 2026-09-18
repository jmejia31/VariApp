using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public sealed record ProductionDataRepairResult(
    int EmpresasTotales,
    int MembresiasLegacyCreadas);

public class ProductionDataRepairService
{
    private readonly AppDbContext _context;

    public ProductionDataRepairService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductionDataRepairResult> RepairAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("""
            UPDATE `Usuarios`
            SET `Eliminado` = FALSE
            WHERE `Eliminado` = TRUE
              AND `FechaEliminacion` IS NULL
              AND `EliminadoPorUsuarioId` IS NULL;
            """);

        var empresasTotales = await _context.Set<Empresa>()
            .AsNoTracking()
            .CountAsync();

        if (empresasTotales != 1)
            return new ProductionDataRepairResult(empresasTotales, 0);

        var empresa = await _context.Set<Empresa>()
            .AsNoTracking()
            .SingleAsync();

        if (!empresa.Activa)
            return new ProductionDataRepairResult(empresasTotales, 0);

        var candidatos = await _context.Usuarios
            .AsNoTracking()
            .Where(u =>
                !u.Eliminado &&
                u.Activo &&
                !u.Bloqueado &&
                u.RolId > 0 &&
                u.RolEntidad.Activo &&
                !u.RolEntidad.Eliminado &&
                !_context.UsuarioEmpresas.Any(m => m.UsuarioId == u.Id))
            .Select(u => new
            {
                u.Id,
                u.RolId
            })
            .ToListAsync();

        foreach (var candidato in candidatos)
        {
            _context.UsuarioEmpresas.Add(new UsuarioEmpresa(
                candidato.Id,
                empresa.Id,
                candidato.RolId)
            {
                CreadoPorNombreUsuario = "SYSTEM:N64_LEGACY_SINGLE_TENANT_REPAIR"
            });
        }

        if (candidatos.Count > 0)
            await _context.SaveChangesAsync();

        return new ProductionDataRepairResult(empresasTotales, candidatos.Count);
    }
}
