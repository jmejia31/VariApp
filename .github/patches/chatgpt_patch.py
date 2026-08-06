from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, content: str) -> None:
    destination = ROOT / path
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(content, encoding="utf-8")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: se esperaba 1 coincidencia y se encontraron {count}")
    return text.replace(old, new, 1)


write(
    "backend/src/Application/DTOs/AjusteStockDto.cs",
    '''namespace InventoryApp.Application.DTOs;

public sealed class AjusteStockRequest
{
    public int CantidadActualEsperada { get; set; }
    public int CantidadNueva { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class AjusteStockResultadoDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int CantidadAnterior { get; set; }
    public int CantidadNueva { get; set; }
    public int Diferencia { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
''')

write(
    "backend/src/Application/Interfaces/IInventarioAjusteService.cs",
    '''using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IInventarioAjusteService
{
    Task<AjusteStockResultadoDto> AjustarProductoAsync(
        int productoId,
        AjusteStockRequest request);

    Task<AjusteStockResultadoDto> AjustarVarianteAsync(
        int productoId,
        int varianteId,
        AjusteStockRequest request);
}
''')

write(
    "backend/src/Application/Services/InventarioAjusteService.cs",
    '''using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class InventarioAjusteService : IInventarioAjusteService
{
    private readonly IInventarioConcurrencyService _concurrency;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IProductoRepository _productos;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public InventarioAjusteService(
        IInventarioConcurrencyService concurrency,
        IMovimientoInventarioRepository movimientos,
        IProductoRepository productos,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _concurrency = concurrency;
        _movimientos = movimientos;
        _productos = productos;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
    }

    public Task<AjusteStockResultadoDto> AjustarProductoAsync(
        int productoId,
        AjusteStockRequest request) =>
        AjustarAsync(productoId, null, request);

    public Task<AjusteStockResultadoDto> AjustarVarianteAsync(
        int productoId,
        int varianteId,
        AjusteStockRequest request) =>
        AjustarAsync(productoId, varianteId, request);

    private async Task<AjusteStockResultadoDto> AjustarAsync(
        int productoId,
        int? varianteId,
        AjusteStockRequest request)
    {
        if (productoId <= 0 || varianteId <= 0)
            throw new BusinessRuleException("El producto o la variante indicada no es válida.");
        if (request.CantidadActualEsperada < 0 || request.CantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");
        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new BusinessRuleException("El motivo del ajuste de inventario es obligatorio.");

        var motivo = request.Motivo.Trim();
        var diferencia = request.CantidadNueva - request.CantidadActualEsperada;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _concurrency.AjustarStockPesimistaAsync(
                productoId,
                varianteId,
                request.CantidadActualEsperada,
                request.CantidadNueva);

            await _movimientos.AddAsync(new MovimientoInventario
            {
                ProductoId = productoId,
                ProductoVarianteId = varianteId,
                Tipo = TipoMovimientoInventario.Ajuste,
                Cantidad = Math.Abs(diferencia),
                StockAnterior = request.CantidadActualEsperada,
                StockNuevo = request.CantidadNueva,
                ReferenciaTipo = varianteId.HasValue
                    ? "AjusteProductoVariante"
                    : "AjusteProducto",
                ReferenciaId = varianteId ?? productoId,
                Descripcion = $"Ajuste formal de inventario. Motivo: {motivo}",
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            await _productos.SaveChangesAsync();
        });

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            varianteId.HasValue
                ? $"Stock de variante ajustado. Producto {productoId}, variante {varianteId}."
                : $"Stock de producto ajustado. Producto {productoId}.",
            varianteId ?? productoId,
            entidad: varianteId.HasValue ? "ProductoVariante" : "Producto",
            valoresAnteriores: new { Cantidad = request.CantidadActualEsperada },
            valoresNuevos: new
            {
                Cantidad = request.CantidadNueva,
                Diferencia = diferencia,
                Motivo = motivo
            },
            motivo: motivo);

        return new AjusteStockResultadoDto
        {
            ProductoId = productoId,
            ProductoVarianteId = varianteId,
            CantidadAnterior = request.CantidadActualEsperada,
            CantidadNueva = request.CantidadNueva,
            Diferencia = diferencia,
            Motivo = motivo
        };
    }
}
''')

write(
    "backend/src/API/Controllers/InventarioAjustesController.cs",
    '''using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("productos")]
public sealed class InventarioAjustesController : ControllerBase
{
    private readonly IInventarioAjusteService _service;

    public InventarioAjustesController(IInventarioAjusteService service)
    {
        _service = service;
    }

    [HttpPost("{productoId:int}/ajustes-stock")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> AjustarProducto(
        int productoId,
        [FromBody] AjusteStockRequest request)
    {
        var resultado = await _service.AjustarProductoAsync(productoId, request);
        return Ok(ApiResponse<AjusteStockResultadoDto>.Ok(
            resultado,
            "Inventario del producto ajustado correctamente."));
    }

    [HttpPost("{productoId:int}/variantes/{varianteId:int}/ajustes-stock")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> AjustarVariante(
        int productoId,
        int varianteId,
        [FromBody] AjusteStockRequest request)
    {
        var resultado = await _service.AjustarVarianteAsync(
            productoId,
            varianteId,
            request);
        return Ok(ApiResponse<AjusteStockResultadoDto>.Ok(
            resultado,
            "Inventario de la variante ajustado correctamente."));
    }
}
''')

program_path = "backend/src/API/Program.cs"
program = read(program_path)
program = replace_once(
    program,
    "builder.Services.AddScoped<IInventarioConcurrencyService, InventarioConcurrencyService>();",
    "builder.Services.AddScoped<IInventarioConcurrencyService, InventarioConcurrencyService>();\nbuilder.Services.AddScoped<IInventarioAjusteService, InventarioAjusteService>();",
    "registro DI de ajuste")
write(program_path, program)

producto_service_path = "backend/src/Application/Services/ProductoService.cs"
producto_service = read(producto_service_path)
producto_service = replace_once(
    producto_service,
    '''        producto.Nombre = dto.Nombre.Trim();
        producto.Marca = marcaNombre;''',
    '''        if (dto.Cantidad != producto.Cantidad)
        {
            throw new BusinessRuleException(
                "El stock no puede modificarse desde el mantenimiento general. Utiliza la operación Ajustar inventario.");
        }

        producto.Nombre = dto.Nombre.Trim();
        producto.Marca = marcaNombre;''',
    "bloqueo de stock en producto")
producto_service = replace_once(
    producto_service,
    "        producto.Cantidad = dto.Cantidad;\n",
    "",
    "eliminar asignación directa producto")
write(producto_service_path, producto_service)

variante_service_path = "backend/src/Application/Services/ProductoVarianteService.cs"
variante_service = read(variante_service_path)
variante_service = replace_once(
    variante_service,
    '''        var anteriores = new { variante.ColorId, variante.Sku, variante.CodigoBarras, variante.Cantidad, variante.UmbralStockBajo, variante.Costo, variante.Precio };
        var color = await ValidarAsync(productoId, id, dto);

        variante.ColorId = color.Id;''',
    '''        var anteriores = new { variante.ColorId, variante.Sku, variante.CodigoBarras, variante.Cantidad, variante.UmbralStockBajo, variante.Costo, variante.Precio };
        var color = await ValidarAsync(productoId, id, dto);

        if (dto.Cantidad != variante.Cantidad)
        {
            throw new BusinessRuleException(
                "El stock de la variante no puede modificarse desde el mantenimiento general. Utiliza la operación Ajustar inventario.");
        }

        variante.ColorId = color.Id;''',
    "bloqueo de stock en variante")
variante_service = replace_once(
    variante_service,
    "        variante.Cantidad = dto.Cantidad;\n",
    "",
    "eliminar asignación directa variante")
write(variante_service_path, variante_service)

coordinator_path = "backend/src/Infrastructure/Services/InventarioConcurrencyService.cs"
coordinator = read(coordinator_path)
coordinator = replace_once(
    coordinator,
    '''        else
        {
            if (producto.Cantidad != cantidadActualEsperada)''',
    '''        else
        {
            var variantesExistentes = await _productoVarianteRepository
                .GetByProductoIdAsync(productoId, incluirInactivas: true);
            if (variantesExistentes.Count > 0)
            {
                throw new BusinessRuleException(
                    "El producto tiene variantes. Ajusta el inventario de cada variante; el stock total se recalcula automáticamente.");
            }

            if (producto.Cantidad != cantidadActualEsperada)''',
    "prohibir ajuste agregado con variantes")
write(coordinator_path, coordinator)

write(
    "backend/tests/InventoryApp.Tests/InventarioAjusteServiceTests.cs",
    '''using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class InventarioAjusteServiceTests
{
    private readonly Mock<IInventarioConcurrencyService> _concurrency = new();
    private readonly Mock<IMovimientoInventarioRepository> _movimientos = new();
    private readonly Mock<IProductoRepository> _productos = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly InventarioAjusteService _service;

    public InventarioAjusteServiceTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("inventario-admin");
        _productos.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        _service = new InventarioAjusteService(
            _concurrency.Object,
            _movimientos.Object,
            _productos.Object,
            _currentUser.Object,
            new FakeUnitOfWork(),
            _auditoria.Object);
    }

    [Fact]
    public async Task AjustarProductoAsync_RegistraMovimientoAjuste()
    {
        MovimientoInventario? movimiento = null;
        _movimientos.Setup(x => x.AddAsync(It.IsAny<MovimientoInventario>()))
            .Callback<MovimientoInventario>(x => movimiento = x)
            .Returns(Task.CompletedTask);

        var resultado = await _service.AjustarProductoAsync(10, new AjusteStockRequest
        {
            CantidadActualEsperada = 8,
            CantidadNueva = 5,
            Motivo = "Conteo físico"
        });

        Assert.Equal(-3, resultado.Diferencia);
        Assert.NotNull(movimiento);
        Assert.Equal(TipoMovimientoInventario.Ajuste, movimiento!.Tipo);
        Assert.Equal(3, movimiento.Cantidad);
        Assert.Equal(8, movimiento.StockAnterior);
        Assert.Equal(5, movimiento.StockNuevo);
        Assert.Equal("AjusteProducto", movimiento.ReferenciaTipo);
        _concurrency.Verify(x => x.AjustarStockPesimistaAsync(10, null, 8, 5), Times.Once);
    }

    [Fact]
    public async Task AjustarVarianteAsync_PropagaConflictoDeStockObsoleto()
    {
        _concurrency.Setup(x => x.AjustarStockPesimistaAsync(10, 4, 8, 5))
            .ThrowsAsync(new BusinessRuleException(
                "El inventario cambió desde que se cargó el formulario."));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AjustarVarianteAsync(10, 4, new AjusteStockRequest
            {
                CantidadActualEsperada = 8,
                CantidadNueva = 5,
                Motivo = "Conteo"
            }));

        _movimientos.Verify(x => x.AddAsync(It.IsAny<MovimientoInventario>()), Times.Never);
    }

    [Theory]
    [InlineData(-1, 0, "motivo")]
    [InlineData(0, -1, "motivo")]
    [InlineData(0, 0, "")]
    public async Task AjustarProductoAsync_ValidaSolicitud(
        int esperada,
        int nueva,
        string motivo)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AjustarProductoAsync(10, new AjusteStockRequest
            {
                CantidadActualEsperada = esperada,
                CantidadNueva = nueva,
                Motivo = motivo
            }));
    }
}
''')

print("Ajustes formales de inventario conectados.")
