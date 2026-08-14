using System.ComponentModel.DataAnnotations.Schema;
using InventoryApp.Domain.Common;
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

    // Snapshots legacy de compatibilidad/correlación. No son la autoridad relacional.
    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }

    // Contrato de dominio N0.8.B. Las columnas ya existen parcialmente en el esquema
    // por N0.6/N0.7, pero el mapeo EF se incorpora únicamente en N0.8.C.
    [NotMapped]
    public int? CompraId { get; set; }

    [NotMapped]
    public int? VentaId { get; set; }

    [NotMapped]
    public int? ConsumoInsumoId { get; set; }

    [NotMapped]
    public int? AjusteInventarioId { get; set; }

    [NotMapped]
    public OrigenMovimientoInventario? OrigenTipado
    {
        get
        {
            var cantidadOrigenes =
                (CompraId.HasValue ? 1 : 0) +
                (VentaId.HasValue ? 1 : 0) +
                (ConsumoInsumoId.HasValue ? 1 : 0) +
                (AjusteInventarioId.HasValue ? 1 : 0);

            if (cantidadOrigenes == 0)
                return null;

            return OrigenMovimientoInventario.DesdeIds(
                CompraId,
                VentaId,
                ConsumoInsumoId,
                AjusteInventarioId);
        }
    }

    public string? Descripcion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
