using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class DescuentoConfiguration : IEntityTypeConfiguration<Descuento>
{
    public void Configure(EntityTypeBuilder<Descuento> builder)
    {
        builder.ToTable("Descuentos");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(d => d.CodigoPromocional).HasMaxLength(50);
        builder.Property(d => d.CodigoPromocionalNormalizado).HasMaxLength(50);
        builder.Property(d => d.Valor).HasPrecision(18, 4);
        builder.Property(d => d.MontoMinimo).HasPrecision(18, 4);
        builder.Property(d => d.MontoMaximoDescuento).HasPrecision(18, 4);

        builder.HasIndex(d => d.CodigoPromocionalNormalizado).IsUnique().HasFilter("`CodigoPromocionalNormalizado` IS NOT NULL");

        builder.HasMany(d => d.Productos).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Categorias).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Clientes).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Roles).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Historial).WithOne(h => h.Descuento).HasForeignKey(h => h.DescuentoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VentaDescuentoConfiguration : IEntityTypeConfiguration<VentaDescuento>
{
    public void Configure(EntityTypeBuilder<VentaDescuento> builder)
    {
        builder.ToTable("VentaDescuentos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.Property(x => x.ValorSnapshot).HasPrecision(18, 4);
    }
}
