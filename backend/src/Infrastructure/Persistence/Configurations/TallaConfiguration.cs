using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class TallaConfiguration : IEntityTypeConfiguration<Talla>
{
    public void Configure(EntityTypeBuilder<Talla> builder)
    {
        builder.ToTable("Tallas");
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("UX_Tallas_Nombre");
        builder.HasIndex(x => new { x.Activo, x.Eliminado }).HasDatabaseName("IX_Tallas_Estado");
    }
}
