using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class EmpresaConfiguracionConfiguration : IEntityTypeConfiguration<EmpresaConfiguracion>
{
    public void Configure(EntityTypeBuilder<EmpresaConfiguracion> builder)
    {
        builder.ToTable("EmpresaConfiguraciones");
        builder.Property(e => e.NombreComercial).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Eslogan).HasMaxLength(200);
    }
}
