using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.NombreUsuario).IsUnique();
        builder.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(300);
        builder.Property(u => u.Rol).HasConversion<string>().HasMaxLength(30);
        builder.Property(u => u.MotivoBloqueo).HasMaxLength(300);
        builder.HasIndex(u => u.Eliminado);

        // Seed: usuario admin con password "Admin123!" (SOLO PARA DESARROLLO)
        // IMPORTANTE: cambia esta contraseña antes de subir a producción (ver README - seccion seguridad).
        builder.HasData(new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            NombreCompleto = "Administrador",
            PasswordHash = "$2b$11$unl.Q/ZCV7KaW8i7BbocyemHNX9hdpAOqatkmKk2.b3PLzDjKAMuy", // hash real de "Admin123!"
            Rol = RolUsuario.Administrador,
            Activo = true,
            FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
