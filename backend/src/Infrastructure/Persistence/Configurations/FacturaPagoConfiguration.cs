using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class FacturaPagoConfiguration : IEntityTypeConfiguration<FacturaPago>
{
    public void Configure(EntityTypeBuilder<FacturaPago> builder)
    {
        builder.ToTable("FacturaPagos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Monto).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Referencia).HasMaxLength(120);
        builder.Property(x => x.Observaciones).HasMaxLength(500);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.HasIndex(x => new { x.FacturaId, x.FechaPago });
        builder.HasOne(x => x.Factura)
            .WithMany(x => x.Pagos)
            .HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
