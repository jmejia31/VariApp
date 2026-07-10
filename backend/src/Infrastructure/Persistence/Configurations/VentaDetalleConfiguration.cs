using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class VentaDetalleConfiguration : IEntityTypeConfiguration<VentaDetalle>
{
    public void Configure(EntityTypeBuilder<VentaDetalle> builder)
    {
        builder.ToTable("VentaDetalles");
        builder.Property(d => d.PrecioUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoUnitarioSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(d => d.UtilidadBruta).HasColumnType("decimal(18,2)");
        builder.Property(d => d.ProductoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarcaSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).IsRequired().HasMaxLength(100);

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
