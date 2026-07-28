using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> builder)
    {
        builder.ToTable("CompraDetalles");
        builder.Property(d => d.CostoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(d => d.ProductoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarcaSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoSkuSnapshot).HasMaxLength(80);

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ProductoVarianteId);
        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
