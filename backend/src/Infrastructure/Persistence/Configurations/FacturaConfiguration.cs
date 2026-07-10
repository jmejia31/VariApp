using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas");
        builder.Property(f => f.NumeroFactura).IsRequired().HasMaxLength(20);
        builder.HasIndex(f => f.NumeroFactura).IsUnique();
        builder.Property(f => f.Estado).HasConversion<string>().HasMaxLength(20);

        builder.Property(f => f.EmpresaNombre).IsRequired().HasMaxLength(200);
        builder.Property(f => f.ClienteNombre).IsRequired().HasMaxLength(200);
        builder.Property(f => f.VendedorNombreUsuario).IsRequired().HasMaxLength(150);
        builder.Property(f => f.MotivoAnulacion).HasMaxLength(500);
        builder.Property(f => f.Observaciones).HasMaxLength(500);

        builder.Property(f => f.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(f => f.Descuento).HasColumnType("decimal(18,2)");
        builder.Property(f => f.Impuesto).HasColumnType("decimal(18,2)");
        builder.Property(f => f.Total).HasColumnType("decimal(18,2)");

        builder.HasMany(f => f.Detalles)
            .WithOne(d => d.Factura)
            .HasForeignKey(d => d.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
