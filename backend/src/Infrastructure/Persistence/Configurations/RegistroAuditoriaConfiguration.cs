using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("RegistrosAuditoria");
        builder.Property(r => r.NombreUsuario).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Modulo).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Accion).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Descripcion).IsRequired().HasMaxLength(500);

        builder.HasIndex(r => r.Fecha);
        builder.HasIndex(r => new { r.Modulo, r.ReferenciaId });
    }
}
