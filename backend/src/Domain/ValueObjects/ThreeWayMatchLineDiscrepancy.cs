using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.ValueObjects;

public sealed record ThreeWayMatchLineDiscrepancy(
    int OrdenCompraDetalleId,
    ThreeWayMatchDiscrepancyType Tipo,
    decimal EsperadoOrdenado,
    decimal ValorRecepcion,
    decimal ValorFacturado,
    string Mensaje,
    string? EsperadoTexto = null,
    string? ValorFacturadoTexto = null);
