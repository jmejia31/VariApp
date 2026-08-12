using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }

    public TipoMovimientoInventario Tipo { get; set; }
    public CausaMovimientoInventario Causa { get; set; } = CausaMovimientoInventario.NoEspecificada;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public decimal? CostoUnitario { get; set; }
    public decimal? PrecioUnitario { get; set; }

    // Autoridad relacional de origen incorporada físicamente por ERP-N0.6 C2/C3.
    // Las columnas legacy permanecen temporalmente como snapshot de compatibilidad.
    public int? CompraId { get; set; }
    public int? VentaId { get; set; }
    public int? ConsumoInsumoId { get; set; }

    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }
    public string? Descripcion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
