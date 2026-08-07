using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class MovimientoFinancieroRepository : IMovimientoFinancieroRepository
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public MovimientoFinancieroRepository(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    private static IQueryable<MovimientoFinanciero> AplicarAlcance(
        IQueryable<MovimientoFinanciero> query,
        UsuarioScopeActual? alcance)
    {
        if (alcance is null)
            return query.Where(_ => false);

        return alcance.EsAdministrador
            ? query
            : query.Where(m => m.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task AddAsync(MovimientoFinanciero movimiento)
    {
        if (EsReversionAutomaticaDeCompra(movimiento))
        {
            var original = await _context.MovimientosFinancieros
                .Where(m =>
                    m.CompraId == movimiento.CompraId &&
                    m.EsAutomatico &&
                    m.ModuloOrigen == "Compra" &&
                    m.Tipo == TipoMovimientoFinanciero.Egreso)
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync()
                ?? throw new BusinessRuleException(
                    "No se encontró el movimiento financiero original de la compra; la anulación no puede conciliarse.");

            if (original.Estado == EstadoMovimientoFinanciero.Pendiente)
            {
                original.Estado = EstadoMovimientoFinanciero.Anulado;
                original.AnuladoPorUsuarioId = movimiento.CreadoPorUsuarioId;
                original.AnuladoPorNombreUsuario = movimiento.CreadoPorNombreUsuario;
                original.FechaAnulacion = DateTime.UtcNow;
                original.MotivoAnulacion = movimiento.Descripcion ?? movimiento.Concepto;
                _context.MovimientosFinancieros.Update(original);
                return;
            }

            if (original.Estado == EstadoMovimientoFinanciero.Pagado)
            {
                movimiento.Estado = EstadoMovimientoFinanciero.Pendiente;
                movimiento.MetodoPago = original.MetodoPago;
                movimiento.ReferenciaId = original.Id;
                await _context.MovimientosFinancieros.AddAsync(movimiento);
                return;
            }

            throw new BusinessRuleException(
                "El movimiento financiero original de la compra ya está anulado y no admite otra reversión.");
        }

        await _context.MovimientosFinancieros.AddAsync(movimiento);
    }

    public async Task<MovimientoFinanciero?> GetByIdAsync(int id)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosFinancieros, alcance)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public void Update(MovimientoFinanciero movimiento) =>
        _context.MovimientosFinancieros.Update(movimiento);

    public async Task<MovimientoFinanciero?> GetByCompraIdAsync(int compraId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosFinancieros, alcance)
            .Where(m =>
                m.CompraId == compraId &&
                m.EsAutomatico &&
                m.ModuloOrigen == "Compra")
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<MovimientoFinanciero?> GetByVentaIdAsync(int ventaId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosFinancieros, alcance)
            .FirstOrDefaultAsync(m => m.VentaId == ventaId && m.EsAutomatico);
    }

    public async Task<List<MovimientoFinanciero>> GetFilteredAsync(DateTime? desde, DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(_context.MovimientosFinancieros.AsQueryable(), alcance);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private static bool EsReversionAutomaticaDeCompra(MovimientoFinanciero movimiento) =>
        movimiento.EsAutomatico &&
        movimiento.CompraId.HasValue &&
        movimiento.ModuloOrigen == "Reversion" &&
        movimiento.Tipo == TipoMovimientoFinanciero.Ingreso &&
        movimiento.Categoria == CategoriaMovimientoFinanciero.Reversion;
}
