using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class CostoEnvioConfiguration : IEntityTypeConfiguration<CostoEnvio>
{
    public void Configure(EntityTypeBuilder<CostoEnvio> builder)
    {
        builder.ToTable("CostosEnvio");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Monto).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PredeterminadoActivoUnico)
            .HasMaxLength(10)
            .HasComputedColumnSql("IF(EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0, 'DEFAULT', NULL)", stored: true);
        builder.HasIndex(x => x.Nombre);
        builder.HasIndex(x => new { x.Activo, x.EsPredeterminado });
        builder.HasIndex(x => x.PredeterminadoActivoUnico)
            .IsUnique()
            .HasDatabaseName("UX_CostosEnvio_PredeterminadoActivo");
    }
}
