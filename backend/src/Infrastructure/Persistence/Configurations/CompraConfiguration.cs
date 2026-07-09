using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras");
        builder.Property(c => c.NumeroCompra).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.NumeroCompra).IsUnique();
        builder.Property(c => c.ProveedorNombre).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ProveedorTelefono).HasMaxLength(30);
        builder.Property(c => c.ProveedorDocumento).HasMaxLength(50);
        builder.Property(c => c.DocumentoReferencia).HasMaxLength(100);
        builder.Property(c => c.Notas).HasMaxLength(1000);
        builder.Property(c => c.MotivoAnulacion).HasMaxLength(500);

        builder.Property(c => c.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Descuento).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Impuesto).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Total).HasColumnType("decimal(18,2)");

        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.EstadoPago).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.MetodoPago).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(c => c.Detalles)
            .WithOne(d => d.Compra)
            .HasForeignKey(d => d.CompraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
