using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class ImpuestoConfiguration : IEntityTypeConfiguration<Impuesto>
{
    public void Configure(EntityTypeBuilder<Impuesto> builder)
    {
        builder.ToTable("Impuestos");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(i => i.Codigo).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Tasa).HasPrecision(9, 4);
        builder.Property(i => i.MontoFijo).HasPrecision(18, 4);

        builder.HasIndex(i => i.Codigo).IsUnique();

        builder.HasMany(i => i.Productos).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Categorias).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Operaciones).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.ClientesExentos).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.ProveedoresExentos).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Historial).WithOne(h => h.Impuesto).HasForeignKey(h => h.ImpuestoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VentaImpuestoConfiguration : IEntityTypeConfiguration<VentaImpuesto>
{
    public void Configure(EntityTypeBuilder<VentaImpuesto> builder)
    {
        builder.ToTable("VentaImpuestos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImpuestoNombreSnapshot).IsRequired();
        builder.Property(x => x.ImpuestoCodigoSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.Property(x => x.BaseImponible).HasPrecision(18, 4);
        builder.Property(x => x.TasaSnapshot).HasPrecision(9, 4);
        builder.Property(x => x.IncluidoEnPrecioSnapshot).HasDefaultValue(false);
    }
}

public class CompraImpuestoConfiguration : IEntityTypeConfiguration<CompraImpuesto>
{
    public void Configure(EntityTypeBuilder<CompraImpuesto> builder)
    {
        builder.ToTable("CompraImpuestos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImpuestoNombreSnapshot).IsRequired();
        builder.Property(x => x.ImpuestoCodigoSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.Property(x => x.BaseImponible).HasPrecision(18, 4);
        builder.Property(x => x.TasaSnapshot).HasPrecision(9, 4);
        builder.Property(x => x.IncluidoEnPrecioSnapshot).HasDefaultValue(false);
    }
}
