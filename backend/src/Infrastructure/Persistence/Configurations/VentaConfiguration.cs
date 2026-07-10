using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");
        builder.Property(v => v.NumeroVenta).IsRequired().HasMaxLength(20);
        builder.HasIndex(v => v.NumeroVenta).IsUnique();
        builder.Property(v => v.ClienteNombre).IsRequired().HasMaxLength(200);
        builder.Property(v => v.ClienteTelefono).HasMaxLength(30);
        builder.Property(v => v.ClienteIdentidadORTN).HasMaxLength(50);
        builder.Property(v => v.ClienteCorreo).HasMaxLength(150);
        builder.Property(v => v.ClienteDireccion).HasMaxLength(300);
        builder.Property(v => v.Notas).HasMaxLength(1000);
        builder.Property(v => v.MotivoAnulacion).HasMaxLength(500);

        builder.Property(v => v.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Descuento).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Impuesto).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Total).HasColumnType("decimal(18,2)");
        builder.Property(v => v.CostoTotal).HasColumnType("decimal(18,2)");
        builder.Property(v => v.UtilidadBruta).HasColumnType("decimal(18,2)");

        builder.Property(v => v.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.EstadoPago).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.MetodoPago).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(v => v.Detalles)
            .WithOne(d => d.Venta)
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Factura)
            .WithOne(f => f.Venta)
            .HasForeignKey<Factura>(f => f.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
