using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class CalculoServiceTests
{
    private readonly Mock<IDescuentoRepository> _descuentoRepository = new();
    private readonly Mock<IImpuestoRepository> _impuestoRepository = new();
    private readonly CalculoService _service;

    public CalculoServiceTests()
    {
        _descuentoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Descuento>());

        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), It.IsAny<OperacionImpuesto>()))
            .ReturnsAsync(new List<Impuesto>());

        _service = new CalculoService(_descuentoRepository.Object, _impuestoRepository.Object);
    }

    [Fact]
    public async Task CalcularVentaAsync_ImpuestoIncluido_No_Se_Suma_Dos_Veces()
    {
        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = 1,
                    Nombre = "ISV incluido",
                    Codigo = "ISV15-I",
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = 15m,
                    IncluidoEnPrecio = true,
                    Acumulativo = true,
                    Activo = true
                }
            });

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 115m }
            },
            clienteId: null,
            rolIdUsuario: null,
            codigoPromocional: null);

        Assert.Equal(115m, resultado.ImporteBruto);
        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(15m, resultado.ImpuestoIncluido);
        Assert.Equal(0m, resultado.ImpuestoAdicional);
        Assert.Equal(115m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_ImpuestoAdicional_Se_Suma_Al_Total()
    {
        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = 2,
                    Nombre = "ISV adicional",
                    Codigo = "ISV15-A",
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = 15m,
                    IncluidoEnPrecio = false,
                    Acumulativo = true,
                    Activo = true
                }
            });

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m }
            },
            clienteId: null,
            rolIdUsuario: null,
            codigoPromocional: null);

        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(0m, resultado.ImpuestoIncluido);
        Assert.Equal(15m, resultado.ImpuestoAdicional);
        Assert.Equal(115m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_Descuento_Se_Aplica_Antes_Del_Impuesto()
    {
        _descuentoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Descuento>
            {
                new()
                {
                    Id = 1,
                    Nombre = "Descuento general 10%",
                    Tipo = TipoDescuento.Porcentaje,
                    Valor = 10m,
                    Prioridad = 1,
                    Acumulable = false,
                    Activo = true
                }
            });

        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = 3,
                    Nombre = "ISV 15% después de descuento",
                    Codigo = "ISV15-D",
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = 15m,
                    IncluidoEnPrecio = false,
                    SeCalculaAntesDescuento = false,
                    Acumulativo = true,
                    Activo = true
                }
            });

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m }
            },
            clienteId: null,
            rolIdUsuario: null,
            codigoPromocional: null);

        Assert.Equal(10m, resultado.TotalDescuento);
        Assert.Equal(90m, resultado.Subtotal);
        Assert.Equal(13.50m, resultado.ImpuestoAdicional);
        Assert.Equal(103.50m, resultado.Total);
    }

    [Fact]
    public async Task CalcularCompraAsync_ImpuestoIncluido_Extrae_Base_Sin_Modificar_Total_Proveedor()
    {
        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Compra))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = 4,
                    Nombre = "Impuesto de compra incluido",
                    Codigo = "COMPRA15-I",
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = 15m,
                    IncluidoEnPrecio = true,
                    Acumulativo = true,
                    Activo = true
                }
            });

        var resultado = await _service.CalcularCompraAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 1000m }
            },
            proveedorId: 10);

        Assert.Equal(1000m, resultado.ImporteBruto);
        Assert.Equal(869.57m, resultado.Subtotal);
        Assert.Equal(130.43m, resultado.ImpuestoIncluido);
        Assert.Equal(1000m, resultado.Total);
    }
}
