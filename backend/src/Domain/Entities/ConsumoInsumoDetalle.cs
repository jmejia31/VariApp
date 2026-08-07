using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class ConsumoInsumoDetalle : BaseEntity
{
    public int ConsumoInsumoId { get; set; }
    public ConsumoInsumo ConsumoInsumo { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public int Cantidad { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal CostoTotalSnapshot { get; set; }
    public string NombreSnapshot { get; set; } = string.Empty;
    public string? SkuSnapshot { get; set; }
    public string? ColorSnapshot { get; set; }
}
