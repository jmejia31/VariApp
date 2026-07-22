using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Marca).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Modelo).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Descripcion).HasMaxLength(1000);
        builder.Property(p => p.Costo).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Precio).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Activo).HasDefaultValue(true);
        builder.Property(p => p.Eliminado).HasDefaultValue(false);

        // Toda consulta ordinaria excluye registros eliminados. El historial de
        // ventas/compras conserva sus snapshots y relaciones existentes.
        builder.HasQueryFilter(p => !p.Eliminado);

        builder.Ignore(p => p.ImagenPrincipal);
        builder.Ignore(p => p.TieneStockBajo);

        builder.HasIndex(p => p.Nombre).HasDatabaseName("IX_Productos_Nombre");
        builder.HasIndex(p => p.Marca).HasDatabaseName("IX_Productos_Marca");
        builder.HasIndex(p => p.Modelo).HasDatabaseName("IX_Productos_Modelo");
        builder.HasIndex(p => new { p.Eliminado, p.Activo }).HasDatabaseName("IX_Productos_Estado");
    }
}
