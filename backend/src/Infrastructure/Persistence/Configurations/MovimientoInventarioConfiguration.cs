using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");
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

        // C2/C3 ya crearon físicamente estas columnas, índices y FKs en MySQL.
        // D2A únicamente las incorpora al modelo EF para que D2B pueda escribirlas.
        builder.HasIndex(m => m.CompraId)
            .HasDatabaseName("IX_MovimientosInventario_CompraId");
        builder.HasOne<Compra>()
            .WithMany()
            .HasForeignKey(m => m.CompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_Compras_CompraId_N06");

        builder.HasIndex(m => m.VentaId)
            .HasDatabaseName("IX_MovimientosInventario_VentaId");
        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(m => m.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_Ventas_VentaId_N06");

        builder.HasIndex(m => m.ConsumoInsumoId)
            .HasDatabaseName("IX_MovimientosInventario_ConsumoInsumoId");
        builder.HasOne<ConsumoInsumo>()
            .WithMany()
            .HasForeignKey(m => m.ConsumoInsumoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MovimientosInventario_ConsumosInsumos_ConsumoInsumoId_N06");

        builder.HasIndex(m => new { m.ReferenciaTipo, m.ReferenciaId });
        builder.HasIndex(m => new { m.Causa, m.Fecha });
    }
}
