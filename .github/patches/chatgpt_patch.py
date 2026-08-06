from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, content: str) -> None:
    (ROOT / path).write_text(content, encoding="utf-8")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: se esperaba 1 coincidencia y se encontraron {count}")
    return text.replace(old, new, 1)


def replace_between(text: str, start: str, end: str, replacement: str, label: str) -> str:
    i = text.find(start)
    if i < 0:
        raise RuntimeError(f"{label}: marcador inicial no encontrado")
    j = text.find(end, i)
    if j < 0:
        raise RuntimeError(f"{label}: marcador final no encontrado")
    return text[:i] + replacement + text[j:]


# 1. Contrato tipado: exponer la demanda consolidada realmente bloqueada.
interface_path = "backend/src/Application/Interfaces/IInventarioConcurrencyService.cs"
interface_text = '''using InventoryApp.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryApp.Application.Interfaces;

public sealed record InventarioDemanda(
    int ProductoId,
    int? ProductoVarianteId,
    int Cantidad);

public sealed class InventarioLockSet
{
    public InventarioLockSet(
        IReadOnlyDictionary<int, Producto> productos,
        IReadOnlyDictionary<int, ProductoVariante> variantes,
        IReadOnlyList<InventarioDemanda>? demandas = null)
    {
        Productos = productos;
        Variantes = variantes;
        Demandas = demandas ?? Array.Empty<InventarioDemanda>();
    }

    public IReadOnlyDictionary<int, Producto> Productos { get; }
    public IReadOnlyDictionary<int, ProductoVariante> Variantes { get; }
    public IReadOnlyList<InventarioDemanda> Demandas { get; }
}

public interface IInventarioConcurrencyService
{
    Task<InventarioLockSet> BloquearYValidarInventarioAsync(
        IEnumerable<InventarioDemanda> demandMap,
        bool esDeduccion = true);

    Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva);
}
'''
write(interface_path, interface_text)

# 2. Coordinador: validar también el agregado del producto y devolver demandas consolidadas.
coord_path = "backend/src/Infrastructure/Services/InventarioConcurrencyService.cs"
coord = read(coord_path)
coord = replace_once(
    coord,
    '''            return new InventarioLockSet(
                new Dictionary<int, InventoryApp.Domain.Entities.Producto>(),
                new Dictionary<int, InventoryApp.Domain.Entities.ProductoVariante>());''',
    '''            return new InventarioLockSet(
                new Dictionary<int, InventoryApp.Domain.Entities.Producto>(),
                new Dictionary<int, InventoryApp.Domain.Entities.ProductoVariante>(),
                Array.Empty<InventarioDemanda>());''',
    "lockset vacío")
coord = replace_once(
    coord,
    '''        foreach (var item in consolidada)
        {
            if (!productosMap.TryGetValue(item.ProductoId, out var producto))
                throw new BusinessRuleException($"El producto ID '{item.ProductoId}' no existe o fue eliminado.");

            if (item.ProductoVarianteId.HasValue)''',
    '''        foreach (var productoGrupo in consolidada.GroupBy(x => x.ProductoId))
        {
            if (!productosMap.TryGetValue(productoGrupo.Key, out var producto))
                throw new BusinessRuleException($"El producto ID '{productoGrupo.Key}' no existe o fue eliminado.");

            var cantidadTotalProducto = productoGrupo.Sum(x => x.Cantidad);
            if (esDeduccion && producto.Cantidad < cantidadTotalProducto)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {cantidadTotalProducto}.");
            }
        }

        foreach (var item in consolidada)
        {
            var producto = productosMap[item.ProductoId];

            if (item.ProductoVarianteId.HasValue)''',
    "validación agregada de producto")
coord = replace_once(
    coord,
    '''            else if (esDeduccion && producto.Cantidad < item.Cantidad)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {item.Cantidad}.");
            }
        }

        return new InventarioLockSet(productosMap, variantesMap);''',
    '''        }

        return new InventarioLockSet(productosMap, variantesMap, consolidada);''',
    "retorno consolidado")
write(coord_path, coord)

# 3. Consultas internas completas para anulación conservadora de compras.
mov_interface_path = "backend/src/Application/Interfaces/IMovimientoInventarioRepository.cs"
write(mov_interface_path, '''using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IMovimientoInventarioRepository
{
    Task AddAsync(MovimientoInventario movimiento);
    Task<List<MovimientoInventario>> GetByProductoAsync(int productoId);
    Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
    Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId);
    Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds);
}
''')

mov_repo_path = "backend/src/Infrastructure/Repositories/MovimientoInventarioRepository.cs"
mov_repo = read(mov_repo_path)
mov_repo = replace_once(
    mov_repo,
    '''    public async Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(ConIncludes(), alcance);
        if (productoId.HasValue) query = query.Where(m => m.ProductoId == productoId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.Tipo.ToString() == tipo);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).Take(200).ToListAsync();
    }
}''',
    '''    public async Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(ConIncludes(), alcance);
        if (productoId.HasValue) query = query.Where(m => m.ProductoId == productoId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.Tipo.ToString() == tipo);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).Take(200).ToListAsync();
    }

    public async Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId) =>
        await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m => m.ReferenciaTipo == "Compra" && m.ReferenciaId == compraId)
            .MaxAsync(m => (int?)m.Id);

    public async Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds)
    {
        var ids = productoIds.Distinct().ToArray();
        if (ids.Length == 0) return false;

        return await _context.MovimientosInventario
            .AsNoTracking()
            .AnyAsync(m => m.Id > ultimoMovimientoOriginalId && ids.Contains(m.ProductoId));
    }
}''',
    "consultas internas de movimientos")
write(mov_repo_path, mov_repo)

# 4. Venta: consolidar confirmación y anulación sin tocar detalles financieros.
venta_path = "backend/src/Application/Services/VentaService.cs"
venta = read(venta_path)
confirmar_start = '''            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();'''
confirmar_end = '''            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero'''
confirmar_replacement = '''            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: true);

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad -= productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = venta.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var precioUnitarioMovimiento = detallesClave.Sum(d => d.Subtotal) / item.Cantidad;
                var costoUnitarioMovimiento = detallesClave.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad) / item.Cantidad;

                var stockAnteriorMovimiento = producto.Cantidad + item.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    if (!variante.Activo)
                        throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva y no puede venderse.");

                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad -= item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Salida,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    PrecioUnitario = precioUnitarioMovimiento,
                    CostoUnitario = costoUnitarioMovimiento,
                    ReferenciaTipo = "Venta",
                    ReferenciaId = venta.Id,
                    Descripcion = $"Salida por venta {venta.NumeroVenta}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

'''
venta = replace_between(venta, confirmar_start, confirmar_end, confirmar_replacement, "venta confirmar consolidada")

anular_method_start = venta.index('    public async Task<VentaDto?> AnularAsync')
anular_region = venta[anular_method_start:]
anular_start = confirmar_start
anular_end = confirmar_end
anular_replacement = '''            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: false);

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad += productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = venta.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var precioUnitarioMovimiento = detallesClave.Sum(d => d.Subtotal) / item.Cantidad;
                var costoUnitarioMovimiento = detallesClave.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad) / item.Cantidad;

                var stockAnteriorMovimiento = producto.Cantidad - item.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad += item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    PrecioUnitario = precioUnitarioMovimiento,
                    CostoUnitario = costoUnitarioMovimiento,
                    ReferenciaTipo = "VentaAnulada",
                    ReferenciaId = venta.Id,
                    Descripcion = $"Entrada por anulación de venta {venta.NumeroVenta}. Motivo: {motivo}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

'''
anular_region = replace_between(anular_region, anular_start, anular_end, anular_replacement, "venta anular consolidada")
venta = venta[:anular_method_start] + anular_region
write(venta_path, venta)

# 5. Compra: costo ponderado exacto, una modificación por clave y anulación conservadora completa.
compra_path = "backend/src/Application/Services/CompraService.cs"
compra = read(compra_path)
compra_confirm_start = '''            var demanda = compra.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();'''
compra_fin_end = '''            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero'''
compra_confirm_replacement = '''            var demanda = compra.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: false);

            var stocksProductoAnteriores = inventario.Productos.ToDictionary(x => x.Key, x => x.Value.Cantidad);
            var costosProductoAnteriores = inventario.Productos.ToDictionary(x => x.Key, x => x.Value.Costo);

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = compra.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var valorEntrada = detallesClave.Sum(d => d.CostoUnitario * d.Cantidad);
                var costoEntradaPonderado = valorEntrada / item.Cantidad;

                var stockAnteriorMovimiento = stocksProductoAnteriores[item.ProductoId];
                var stockNuevoMovimiento = stockAnteriorMovimiento + item.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    var valorAnteriorVariante = variante.Costo * variante.Cantidad;
                    variante.Cantidad += item.Cantidad;
                    variante.Costo = Math.Round(
                        (valorAnteriorVariante + valorEntrada) / variante.Cantidad,
                        2,
                        MidpointRounding.AwayFromZero);
                    variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                    variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                    variante.FechaActualizacion = DateTime.UtcNow;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    CostoUnitario = costoEntradaPonderado,
                    ReferenciaTipo = "Compra",
                    ReferenciaId = compra.Id,
                    Descripcion = $"Entrada por compra {compra.NumeroCompra}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                var cantidadEntrada = productoGrupo.Sum(x => x.Cantidad);
                var valorEntrada = compra.Detalles
                    .Where(d => d.ProductoId == producto.Id)
                    .Sum(d => d.CostoUnitario * d.Cantidad);
                var stockAnterior = stocksProductoAnteriores[producto.Id];
                var costoAnterior = costosProductoAnteriores[producto.Id];
                var stockNuevo = stockAnterior + cantidadEntrada;

                producto.Cantidad = stockNuevo;
                producto.Costo = stockNuevo > 0
                    ? Math.Round(
                        ((costoAnterior * stockAnterior) + valorEntrada) / stockNuevo,
                        2,
                        MidpointRounding.AwayFromZero)
                    : 0m;
                _productoRepository.Update(producto);
            }

'''
compra = replace_between(compra, compra_confirm_start, compra_fin_end, compra_confirm_replacement, "compra confirmar consolidada")

compra_anular_method = compra.index('    public async Task<CompraDto?> AnularAsync')
compra_anular_region = compra[compra_anular_method:]
compra_anular_replacement = '''            var demanda = compra.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: true);

            var productoIds = inventario.Productos.Keys.OrderBy(x => x).ToArray();
            var ultimoMovimientoOriginalId = await _movimientoInventarioRepository
                .GetUltimoMovimientoOriginalCompraIdAsync(compra.Id)
                ?? throw new BusinessRuleException(
                    "No se encontraron los movimientos originales de la compra; la anulación no puede ejecutarse de forma segura.");

            if (await _movimientoInventarioRepository.ExisteMovimientoPosteriorAsync(
                    ultimoMovimientoOriginalId,
                    productoIds))
            {
                throw new BusinessRuleException(
                    "No se puede anular la compra porque existen movimientos posteriores de inventario sobre sus productos o variantes.");
            }

            var stocksProductoAnteriores = inventario.Productos.ToDictionary(x => x.Key, x => x.Value.Cantidad);

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = compra.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var costoUnitarioMovimiento = detallesClave.Sum(d => d.CostoUnitario * d.Cantidad) / item.Cantidad;

                var stockAnteriorMovimiento = stocksProductoAnteriores[item.ProductoId];
                var stockNuevoMovimiento = stockAnteriorMovimiento - item.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad -= item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Salida,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    CostoUnitario = costoUnitarioMovimiento,
                    ReferenciaTipo = "CompraAnulada",
                    ReferenciaId = compra.Id,
                    Descripcion = $"Salida por anulación de compra {compra.NumeroCompra}. Motivo: {motivo}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad = stocksProductoAnteriores[producto.Id] - productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

'''
compra_anular_region = replace_between(
    compra_anular_region,
    compra_confirm_start,
    compra_fin_end,
    compra_anular_replacement,
    "compra anular consolidada")
compra = compra[:compra_anular_method] + compra_anular_region
write(compra_path, compra)

# 6. Ajustar mocks de compra para las consultas internas nuevas.
test_path = "backend/tests/InventoryApp.Tests/CompraServiceTests.cs"
test = read(test_path)
test = replace_once(
    test,
    '''        _movInvRepoMock
            .Setup(r => r.GetFilteredAsync(null, null, null, null))
            .ReturnsAsync(new List<MovimientoInventario>());''',
    '''        _movInvRepoMock
            .Setup(r => r.GetUltimoMovimientoOriginalCompraIdAsync(compra.Id))
            .ReturnsAsync(1);
        _movInvRepoMock
            .Setup(r => r.ExisteMovimientoPosteriorAsync(
                It.IsAny<int>(),
                It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(false);''',
    "mocks de movimientos de compra")
write(test_path, test)

print("Parche de consolidación y costos ponderados aplicado correctamente.")
