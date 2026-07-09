using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ProductoImagenConfiguration : IEntityTypeConfiguration<ProductoImagen>
{
    public void Configure(EntityTypeBuilder<ProductoImagen> builder)
    {
        builder.ToTable("ProductoImagenes");
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.PublicId).IsRequired().HasMaxLength(300);

        builder.HasOne(i => i.Producto)
            .WithMany(p => p.Imagenes)
            .HasForeignKey(i => i.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
