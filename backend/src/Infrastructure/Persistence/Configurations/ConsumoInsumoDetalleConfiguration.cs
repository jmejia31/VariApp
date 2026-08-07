using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ConsumoInsumoDetalleConfiguration : IEntityTypeConfiguration<ConsumoInsumoDetalle>
{
    public void Configure(EntityTypeBuilder<ConsumoInsumoDetalle> builder)
    {
        builder.ToTable("ConsumoInsumoDetalles");
        builder.Property(d => d.CostoUnitarioSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoTotalSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.NombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.SkuSnapshot).HasMaxLength(80);
        builder.Property(d => d.ColorSnapshot).HasMaxLength(100);
        builder.HasIndex(d => d.ConsumoInsumoId);
        builder.HasIndex(d => new { d.ProductoId, d.ProductoVarianteId });

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
