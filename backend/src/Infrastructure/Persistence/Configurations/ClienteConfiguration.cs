using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => c.Nombre).IsUnique();
        builder.Property(c => c.Telefono).HasMaxLength(30);
        builder.Property(c => c.IdentidadORTN).HasMaxLength(50);
        builder.Property(c => c.Correo).HasMaxLength(150);
        builder.Property(c => c.Direccion).HasMaxLength(300);

        builder.HasMany(c => c.Ventas)
            .WithOne(v => v.Cliente)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
