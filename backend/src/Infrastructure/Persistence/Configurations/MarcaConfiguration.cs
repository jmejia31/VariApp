using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> builder)
    {
        builder.ToTable("Marcas");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.Property(x => x.NombreActivoUnico)
            .HasMaxLength(120)
            .HasComputedColumnSql("IF(Eliminado = 0, LOWER(TRIM(Nombre)), NULL)", stored: true);
        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex(x => x.NombreActivoUnico).IsUnique().HasDatabaseName("UX_Marcas_Nombre_Activo");
        builder.HasIndex(x => new { x.Activo, x.Eliminado }).HasDatabaseName("IX_Marcas_Estado");
    }
}
