using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de sucursales para ERP-N1. EmpresaId permanece como
/// compatibilidad futura y deliberadamente no crea una FK/tenant antes de ERP-N6.
/// </summary>
public sealed class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        builder.ToTable("Sucursales");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Direccion).HasMaxLength(500);
        builder.Property(x => x.Telefono).HasMaxLength(50);
        builder.Property(x => x.Correo).HasMaxLength(254);
        builder.Property(x => x.ZonaHoraria)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("America/Tegucigalpa");
        builder.Property(x => x.Activa).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<string>("CodigoActivoUnico")
            .HasMaxLength(40)
            .HasComputedColumnSql(
                "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)",
                stored: true);

        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex("CodigoActivoUnico")
            .IsUnique()
            .HasDatabaseName("UX_Sucursales_Codigo_Activo");
        builder.HasIndex(x => x.EmpresaId)
            .HasDatabaseName("IX_Sucursales_EmpresaId");
        builder.HasIndex(x => new { x.Activa, x.Eliminado })
            .HasDatabaseName("IX_Sucursales_Estado");
    }
}
