using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ModeloConfiguration : IEntityTypeConfiguration<Modelo>
{
    public void Configure(EntityTypeBuilder<Modelo> builder)
    {
        builder.ToTable("Modelos");
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex(x => new { x.MarcaId, x.Nombre }).IsUnique().HasDatabaseName("UX_Modelos_Marca_Nombre");
        builder.HasIndex(x => new { x.MarcaId, x.Activo, x.Eliminado }).HasDatabaseName("IX_Modelos_Marca_Estado");
        builder.HasOne(x => x.Marca)
            .WithMany(x => x.Modelos)
            .HasForeignKey(x => x.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
