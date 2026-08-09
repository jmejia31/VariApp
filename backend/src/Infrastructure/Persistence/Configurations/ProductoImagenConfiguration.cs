using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ProductoImagenConfiguration : IEntityTypeConfiguration<ProductoImagen>
{
    public void Configure(EntityTypeBuilder<ProductoImagen> builder)
    {
        builder.ToTable("ProductoImagenes");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(1000);
        builder.Property(i => i.PublicId).IsRequired().HasMaxLength(500);
        builder.Property(i => i.CreadoPorNombreUsuario).HasMaxLength(100);

        builder.HasOne(i => i.Producto)
            .WithMany(p => p.Imagenes)
            .HasForeignKey(i => i.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ProductoVariante)
            .WithMany(v => v.Imagenes)
            .HasForeignKey(i => i.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<string>("PrincipalAmbitoKey")
            .HasMaxLength(80)
            .HasComputedColumnSql("IF(EsPrincipal = 1, CONCAT(ProductoId, ':', IFNULL(ProductoVarianteId, 0)), NULL)", stored: true);

        builder.HasIndex("PrincipalAmbitoKey")
            .IsUnique()
            .HasDatabaseName("UX_ProductoImagenes_Principal_Ambito");
        builder.HasIndex(i => new { i.ProductoId, i.ProductoVarianteId, i.Orden })
            .HasDatabaseName("IX_ProductoImagenes_Producto_Variante_Orden");
    }
}
