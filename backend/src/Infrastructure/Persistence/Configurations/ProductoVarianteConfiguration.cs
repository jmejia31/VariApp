using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ProductoVarianteConfiguration : IEntityTypeConfiguration<ProductoVariante>
{
    public void Configure(EntityTypeBuilder<ProductoVariante> builder)
    {
        builder.ToTable("ProductoVariantes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(80);
        builder.Property(x => x.CodigoBarras).HasMaxLength(120);
        builder.Property(x => x.Costo).HasPrecision(18, 2);
        builder.Property(x => x.Precio).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.ProductoId, x.ColorId }).IsUnique();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.CodigoBarras).IsUnique();
        builder.HasOne(x => x.Producto)
            .WithMany(x => x.Variantes)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Color)
            .WithMany()
            .HasForeignKey(x => x.ColorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
