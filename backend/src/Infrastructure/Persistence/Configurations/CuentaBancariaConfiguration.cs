using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class CuentaBancariaConfiguration : IEntityTypeConfiguration<CuentaBancaria>
{
    public void Configure(EntityTypeBuilder<CuentaBancaria> builder)
    {
        builder.ToTable("CuentasBancarias");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BancoId).IsRequired();
        builder.HasOne<Banco>()
            .WithMany()
            .HasForeignKey(x => x.BancoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.NumeroCuenta)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Moneda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.SaldoInicial)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.BancoId)
            .HasDatabaseName("IX_CuentasBancarias_BancoId");
        builder.HasIndex(x => new { x.BancoId, x.NumeroCuenta })
            .IsUnique()
            .HasDatabaseName("UX_CuentasBancarias_BancoId_NumeroCuenta");
        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_CuentasBancarias_Estado");
    }
}
