using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class FacturaDetalleConfiguration : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> builder)
    {
        builder.ToTable("FacturaDetalles");
        builder.Property(d => d.ProductoNombre).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarca).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModelo).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoCodigo).HasMaxLength(50);
        builder.Property(d => d.PrecioUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Descuento).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
    }
}
