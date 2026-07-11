using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.Nombre).IsUnique();
        builder.Property(p => p.Telefono).HasMaxLength(30);
        builder.Property(p => p.Documento).HasMaxLength(50);
        builder.Property(p => p.Correo).HasMaxLength(150);
        builder.Property(p => p.Direccion).HasMaxLength(300);

        builder.HasMany(p => p.Compras)
            .WithOne(c => c.Proveedor)
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
