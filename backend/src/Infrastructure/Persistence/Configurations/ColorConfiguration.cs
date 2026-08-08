using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("Colores");
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.CodigoVisual).HasMaxLength(30);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("UX_Colores_Nombre");
        builder.HasIndex(x => new { x.Activo, x.Eliminado }).HasDatabaseName("IX_Colores_Estado");
    }
}
