using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N16KardexTransferenciaReadTests
{
    [Fact]
    public async Task GetPaged_ExponeTransferenciaComoOrigenTipadoAutoritativo()
    {
        var repository = new Mock<IMovimientoInventarioRepository>();
        var movimiento = new MovimientoInventario
        {
            Id = 501,
            ProductoId = 10,
            Tipo = TipoMovimientoInventario.Salida,
            Causa = CausaMovimientoInventario.TransferenciaDespacho,
            Cantidad = 4,
            StockAnterior = 12,
            StockNuevo = 8,
            CorrelationId = "transferencia:42:despachar",
            ReferenciaTipo = "TransferenciaInventario",
            ReferenciaId = 42,
            Fecha = new DateTime(2026, 8, 16, 9, 0, 0, DateTimeKind.Utc)
        };
        var filtro = new MovimientoInventarioQueryDto { Page = 1, PageSize = 20 };

        repository
            .Setup(x => x.GetPagedAsync(filtro))
            .ReturnsAsync((new List<MovimientoInventario> { movimiento }, 1));
        repository
            .Setup(x => x.GetOrigenesTipadosAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new Dictionary<int, MovimientoInventarioOrigenPersistido>
            {
                [501] = new(501, null, null, null, null, 42)
            });

        var service = new MovimientoInventarioService(repository.Object);

        var result = await service.GetPagedAsync(filtro);

        var dto = Assert.Single(result.Items);
        Assert.Equal("TransferenciaInventario", dto.OrigenTipo);
        Assert.Equal(42, dto.OrigenId);
        Assert.Equal(42, dto.TransferenciaInventarioId);
        Assert.Null(dto.CompraId);
        Assert.Null(dto.VentaId);
        Assert.Null(dto.ConsumoInsumoId);
        Assert.Null(dto.AjusteInventarioId);
    }
}
