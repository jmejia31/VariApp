namespace InventoryApp.Domain.Enums;

/// <summary>
/// N3.7.B — lifecycle mínimo del documento de nota de crédito de cliente.
/// No implica por sí mismo aplicación contable, caja, stock, Kardex ni devolución física.
/// </summary>
public enum EstadoNotaCreditoCliente
{
    Borrador = 1,
    Emitida = 2,
    Anulada = 3
}
