using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(80);
        builder.Property(r => r.NombreNormalizado).IsRequired().HasMaxLength(80);
        builder.Property(r => r.Descripcion).HasMaxLength(300);

        builder.HasIndex(r => r.NombreNormalizado).IsUnique();

        builder.HasMany(r => r.Usuarios)
            .WithOne(u => u.RolEntidad)
            .HasForeignKey(u => u.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Permisos)
            .WithOne()
            .HasForeignKey(p => p.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
