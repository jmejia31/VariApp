using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de sucursales. N6.2.C establece la relación tenant-aware
/// sin inventar un backfill: EmpresaId conserva nullabilidad temporal hasta que N6.2.D
/// haga que todos los flujos de aplicación resuelvan una Empresa de forma segura.
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

        builder.Property(x => x.EmpresaId);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

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
