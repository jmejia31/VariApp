using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class MovimientoFinancieroConfiguration : IEntityTypeConfiguration<MovimientoFinanciero>
{
    public void Configure(EntityTypeBuilder<MovimientoFinanciero> builder)
    {
        builder.ToTable("MovimientosFinancieros");
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Categoria).HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.MetodoPago).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Concepto).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Descripcion).HasMaxLength(500);
        builder.Property(m => m.ModuloOrigen).IsRequired().HasMaxLength(30);
        builder.Property(m => m.MotivoAnulacion).HasMaxLength(500);
        builder.Property(m => m.Monto).HasColumnType("decimal(18,2)");
    }
}
