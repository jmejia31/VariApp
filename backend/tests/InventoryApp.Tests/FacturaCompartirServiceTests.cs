using System.Security.Cryptography;
using System.Text;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class FacturaCompartirServiceTests
{
    private readonly Mock<IFacturaCompartirRepository> _repository = new();
    private readonly Mock<IFacturaService> _facturaService = new();
    private readonly Mock<IFacturaPdfService> _pdfService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly IConfiguration _configuration;
    private readonly FacturaCompartirService _service;

    public FacturaCompartirServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:BackendPublicUrl"] = "https://api.varistorehn.test",
                ["AppSettings:EnlacePublicoFacturaHorasValidez"] = "24",
                ["AppSettings:EnlacePublicoFacturaMaximoAccesos"] = "3"
            })
            .Build();

        _currentUser.Setup(c => c.UsuarioId).Returns(7);
        _currentUser.Setup(c => c.NombreUsuario).Returns("admin");
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        _service = new FacturaCompartirService(
            _repository.Object,
            _facturaService.Object,
            _pdfService.Object,
            _emailService.Object,
            _auditoria.Object,
            _currentUser.Object,
            _configuration,
            new HttpContextAccessor());
    }

    [Fact]
    public async Task PrepararCompartirAsync_Persiste_Solo_Hash_Del_Token()
    {
        var factura = CrearFactura();
        EnlacePublicoFactura? guardado = null;

        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _repository.Setup(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>())).ReturnsAsync(2);
        _repository.Setup(r => r.AddEnlaceAsync(It.IsAny<EnlacePublicoFactura>()))
            .Callback<EnlacePublicoFactura>(e => guardado = e)
            .Returns(Task.CompletedTask);

        var resultado = await _service.PrepararCompartirAsync(factura.Id);

        Assert.NotNull(guardado);
        Assert.Equal(64, guardado!.Token.Length);
        Assert.Matches("^[0-9A-F]{64}$", guardado.Token);
        Assert.DoesNotContain(guardado.Token, resultado.UrlPdfPublica, StringComparison.Ordinal);

        var tokenPublico = resultado.UrlPdfPublica
            .Split("/facturas/publico/", StringSplitOptions.None)[1]
            .Split("/pdf", StringSplitOptions.None)[0];
        var hashEsperado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPublico)));

        Assert.Equal(hashEsperado, guardado.Token);
        Assert.Equal(7, guardado.CreadoPorUsuarioId);
        Assert.True(guardado.FechaExpiracion > guardado.FechaCreacion);
        _repository.Verify(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerPdfPorTokenAsync_Enlace_Expirado_No_Genera_Pdf()
    {
        const string token = "token-publico-seguro-con-mas-de-32-caracteres-123";
        _repository.Setup(r => r.GetPorTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(new EnlacePublicoFactura
            {
                Id = 1,
                FacturaId = 15,
                Token = "HASH",
                FechaExpiracion = DateTime.UtcNow.AddMinutes(-1)
            });

        var resultado = await _service.ObtenerPdfPorTokenAsync(token);

        Assert.Null(resultado);
        _facturaService.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _pdfService.Verify(s => s.GenerarPdfAsync(It.IsAny<FacturaDto>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerPdfPorTokenAsync_Respeta_Limite_De_Accesos()
    {
        const string token = "token-publico-seguro-con-mas-de-32-caracteres-456";
        _repository.Setup(r => r.GetPorTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(new EnlacePublicoFactura
            {
                Id = 2,
                FacturaId = 16,
                Token = "HASH",
                FechaExpiracion = DateTime.UtcNow.AddHours(1),
                VecesAccedido = 3
            });

        var resultado = await _service.ObtenerPdfPorTokenAsync(token);

        Assert.Null(resultado);
        _pdfService.Verify(s => s.GenerarPdfAsync(It.IsAny<FacturaDto>()), Times.Never);
    }

    [Fact]
    public async Task RevocarEnlacesAsync_Adelanta_Expiracion_Y_Conserva_Historial()
    {
        var factura = CrearFactura();
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _repository.Setup(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>())).ReturnsAsync(3);

        var revocados = await _service.RevocarEnlacesAsync(factura.Id);

        Assert.Equal(3, revocados);
        _repository.Verify(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _repository.Verify(r => r.AddEnlaceAsync(It.IsAny<EnlacePublicoFactura>()), Times.Never);
    }

    private static FacturaDto CrearFactura() => new()
    {
        Id = 15,
        NumeroFactura = "FAC-000015",
        Estado = "Emitida",
        ClienteNombre = "Cliente prueba",
        ClienteTelefono = "99999999",
        EmpresaNombre = "VariStorehn",
        Total = 500m,
        FechaEmision = DateTime.UtcNow
    };
}
