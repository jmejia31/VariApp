using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolPermisos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Rol).IsRequired();
        builder.Property(p => p.Modulo).IsRequired();
        builder.Property(p => p.Accion).IsRequired();
        builder.Property(p => p.Permitido).IsRequired();

        builder.HasIndex(p => new { p.Rol, p.Modulo, p.Accion }).IsUnique();
    }
}
