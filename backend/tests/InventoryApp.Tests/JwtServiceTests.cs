using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InventoryApp.Tests;

public class JwtServiceTests
{
    [Fact]
    public void GenerarToken_NoPermiteExpiracionFijaMenorAlMargenDeRenovacion()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "Test-Only-Secret-With-More-Than-32-Characters-2026",
                ["Jwt:Issuer"] = "VariApp.Tests",
                ["Jwt:Audience"] = "VariApp.Tests.Frontend",
                ["Jwt:ExpiraMinutos"] = "30"
            })
            .Build();
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "javier",
            NombreCompleto = "Javier Mejía",
            Rol = RolUsuario.Administrador,
            RolId = 1,
            RolEntidad = new Rol
            {
                Id = 1,
                Nombre = "Administrador",
                EsAdministrador = true,
                Activo = true
            }
        };

        var inicio = DateTime.UtcNow;
        var (_, expiraEn) = new JwtService(configuration).GenerarToken(usuario);

        Assert.True(expiraEn >= inicio.AddMinutes(34).AddSeconds(50));
        Assert.True(expiraEn <= inicio.AddMinutes(36));
    }
}
