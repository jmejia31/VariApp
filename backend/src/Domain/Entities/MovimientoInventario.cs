using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }

    public TipoMovimientoInventario Tipo { get; set; }
    public CausaMovimientoInventario Causa { get; set; } = CausaMovimientoInventario.NoEspecificada;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public decimal? CostoUnitario { get; set; }
    public decimal? PrecioUnitario { get; set; }

    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }
    public string? Descripcion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
