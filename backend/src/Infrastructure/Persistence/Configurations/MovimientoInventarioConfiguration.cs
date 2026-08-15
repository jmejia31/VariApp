using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario", table =>
            table.HasCheckConstraint(
                "CK_MovimientosInventario_Ubicacion_RequiereAlmacen",
                "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL"));
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Causa)
            .HasConversion<int>()
            .HasDefaultValue(CausaMovimientoInventario.NoEspecificada);
        builder.Property(m => m.ReferenciaTipo).IsRequired().HasMaxLength(30);
        builder.Property(m => m.Descripcion).HasMaxLength(300);
        builder.Property(m => m.ProductoMarcaSnapshot).HasMaxLength(100);
        builder.Property(m => m.ProductoModeloSnapshot).HasMaxLength(100);
        builder.Property(m => m.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(m => m.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(m => m.ProductoSkuSnapshot).HasMaxLength(80);
        builder.Property(m => m.CorrelationId)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);
        builder.Property(m => m.CostoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(m => m.PrecioUnitario).HasColumnType("decimal(18,2)");

        builder.HasOne(m => m.Producto)
            .WithMany()
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ProductoVarianteId);
        builder.HasOne(m => m.ProductoVariante)
            .WithMany()
            .HasForeignKey(m => m.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.AlmacenId, m.UbicacionAlmacenId })
            .HasDatabaseName("IX_MovimientosInventario_Almacen_Ubicacion");
        builder.HasOne(m => m.Almacen)
            .WithMany()
            .HasForeignKey(m => m.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_Almacenes_AlmacenId_N14");
        builder.HasOne(m => m.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(m => new { m.AlmacenId, m.UbicacionAlmacenId })
            .HasPrincipalKey(u => new { u.AlmacenId, u.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_Ubicacion_MismoAlmacen_N14");

        builder.HasIndex(m => m.CompraId)
            .HasDatabaseName("IX_MovimientosInventario_CompraId");
        builder.HasIndex(m => m.VentaId)
            .HasDatabaseName("IX_MovimientosInventario_VentaId");
        builder.HasIndex(m => m.ConsumoInsumoId)
            .HasDatabaseName("IX_MovimientosInventario_ConsumoInsumoId");
        builder.HasIndex(m => m.AjusteInventarioId)
            .HasDatabaseName("IX_MovimientosInventario_AjusteInventarioId");

        builder.HasOne<Compra>()
            .WithMany()
            .HasForeignKey(m => m.CompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_Compras_CompraId_N06");

        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(m => m.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_Ventas_VentaId_N06");

        builder.HasOne<ConsumoInsumo>()
            .WithMany()
            .HasForeignKey(m => m.ConsumoInsumoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_ConsumosInsumos_ConsumoInsumoId_N06");

        builder.HasOne<AjusteInventario>()
            .WithMany()
            .HasForeignKey(m => m.AjusteInventarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovInv_AjusteInventarioId_N07");

        builder.HasIndex(m => new { m.ReferenciaTipo, m.ReferenciaId });
        builder.HasIndex(m => new { m.Causa, m.Fecha });
        builder.HasIndex(m => new { m.CorrelationId, m.Fecha })
            .HasDatabaseName("IX_MovimientosInventario_CorrelationId_Fecha");
    }
}
