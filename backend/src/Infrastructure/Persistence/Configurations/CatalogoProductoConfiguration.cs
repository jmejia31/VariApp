using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class CatalogoProductoConfiguration : IEntityTypeConfiguration<CatalogoProducto>
{
    public void Configure(EntityTypeBuilder<CatalogoProducto> builder)
    {
        builder.ToTable("CatalogosProducto");
        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Descripcion).HasMaxLength(500);
        builder.Property(c => c.CodigoVisual).HasMaxLength(30);
        builder.Property(c => c.Activo).HasDefaultValue(true);
        builder.Property(c => c.Eliminado).HasDefaultValue(false);

        builder.HasQueryFilter(c => !c.Eliminado);
        builder.HasIndex(c => new { c.Tipo, c.Nombre, c.CatalogoPadreId })
            .HasDatabaseName("IX_CatalogosProducto_Tipo_Nombre_Padre");
        builder.HasIndex(c => new { c.Tipo, c.Activo, c.Eliminado })
            .HasDatabaseName("IX_CatalogosProducto_Estado");

        builder.HasOne(c => c.CatalogoPadre)
            .WithMany(c => c.ElementosHijos)
            .HasForeignKey(c => c.CatalogoPadreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.ProductosComoColor)
            .WithOne(p => p.Color)
            .HasForeignKey(p => p.ColorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.ProductosComoTalla)
            .WithOne(p => p.Talla)
            .HasForeignKey(p => p.TallaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.ProductosComoMarca)
            .WithOne(p => p.MarcaCatalogo)
            .HasForeignKey(p => p.MarcaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.ProductosComoModelo)
            .WithOne(p => p.ModeloCatalogo)
            .HasForeignKey(p => p.ModeloId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
