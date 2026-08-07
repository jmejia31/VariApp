namespace InventoryApp.Domain.Enums;

public enum CausaMovimientoInventario
{
    NoEspecificada = 0,
    Compra = 1,
    Venta = 2,
    ConsumoAdministrativo = 3,
    AjusteManual = 4,
    AnulacionCompra = 5,
    AnulacionVenta = 6,
    ReversionConsumo = 7
}
