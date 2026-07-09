using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.ReferenciaTipo).IsRequired().HasMaxLength(30);
        builder.Property(m => m.Descripcion).HasMaxLength(300);
        builder.Property(m => m.CostoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(m => m.PrecioUnitario).HasColumnType("decimal(18,2)");

        builder.HasOne(m => m.Producto)
            .WithMany()
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ReferenciaTipo, m.ReferenciaId });
    }
}
