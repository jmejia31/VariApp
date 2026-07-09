using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> builder)
    {
        builder.ToTable("CompraDetalles");
        builder.Property(d => d.CostoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(d => d.ProductoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarcaSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).IsRequired().HasMaxLength(100);

        // No cascada hacia Producto: si el producto se elimina, el detalle histórico se conserva
        // (el snapshot ya guarda nombre/marca/modelo). Se restringe el borrado del producto en su lugar.
        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
