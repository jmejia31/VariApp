using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public sealed class KardexMovimientoWriter : IKardexMovimientoWriter
{
    private readonly IMovimientoInventarioRepository _repository;

    public KardexMovimientoWriter(IMovimientoInventarioRepository repository)
    {
        _repository = repository;
    }

    public Task RegistrarCorrelacionadoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        string correlationId) =>
        _repository.AddConOrigenTipadoCorrelacionadoAsync(movimiento, origen, correlationId);

    public Task RegistrarFisicoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        ContextoFisicoMovimientoInventario contexto) =>
        _repository.AddConOrigenTipadoAsync(movimiento, origen, contexto);
}
