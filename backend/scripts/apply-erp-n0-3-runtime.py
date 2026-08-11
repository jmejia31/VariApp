#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def write(rel, text):
    path = ROOT / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def replace_once(rel, old, new):
    text = read(rel)
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{rel}: se esperaba 1 coincidencia y se encontraron {count}: {old[:90]!r}")
    write(rel, text.replace(old, new, 1))


def regex_once(rel, pattern, replacement, flags=re.S):
    text = read(rel)
    text2, count = re.subn(pattern, replacement, text, count=1, flags=flags)
    if count != 1:
        raise RuntimeError(f"{rel}: patrón no único/no encontrado: {pattern[:100]!r} ({count})")
    write(rel, text2)


# 1) ProductoMapper: lecturas 100% derivadas de ProductoVariante.
write("backend/src/Application/Mappings/ProductoMapper.cs", r'''using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Mappings;

public static class ProductoMapper
{
    public static ProductoDto ToDto(Producto p)
    {
        var variantes = p.Variantes
            .Where(v => !v.Eliminado)
            .OrderBy(v => v.Marca?.Nombre)
            .ThenBy(v => v.Modelo?.Nombre)
            .ThenBy(v => v.Color?.Nombre)
            .ThenBy(v => v.Talla?.Nombre)
            .ThenBy(v => v.Sku)
            .ToList();
        var activas = variantes.Where(v => v.Activo).ToList();
        var operativas = activas.Count > 0 ? activas : variantes;
        var stockTotal = variantes.Sum(v => v.Cantidad);
        var costo = CalcularCosto(variantes);
        var precios = operativas.Where(v => v.Precio.HasValue).Select(v => v.Precio!.Value).ToList();
        var precio = precios.Count > 0 ? precios.Min() : 0m;
        var precioMaximo = precios.Count > 0 ? precios.Max() : 0m;
        var umbral = variantes.Sum(v => v.UmbralStockBajo);
        var agotado = p.Activo && !p.Eliminado && (activas.Count == 0 || activas.All(v => v.Cantidad <= 0));
        var stockBajo = p.Activo && !p.Eliminado && !agotado && activas.Any(v => v.TieneStockBajo);

        var marcaId = ValorComun(variantes, v => v.MarcaId);
        var modeloId = ValorComun(variantes, v => v.ModeloId);
        var colorId = ValorComun(variantes, v => v.ColorId);
        var tallaId = ValorComun(variantes, v => v.TallaId);

        var marcaNombres = variantes.Select(v => v.Marca?.Nombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var modeloNombres = variantes.Select(v => v.Modelo?.Nombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var marca = string.Join(" / ", marcaNombres!);
        var modelo = string.Join(" / ", modeloNombres!);

        var color = colorId.HasValue ? variantes.FirstOrDefault(v => v.ColorId == colorId)?.Color : null;
        var talla = tallaId.HasValue ? variantes.FirstOrDefault(v => v.TallaId == tallaId)?.Talla : null;
        var marcaEntidad = marcaId.HasValue ? variantes.FirstOrDefault(v => v.MarcaId == marcaId)?.Marca : null;
        var modeloEntidad = modeloId.HasValue ? variantes.FirstOrDefault(v => v.ModeloId == modeloId)?.Modelo : null;

        var imagenesGenerales = p.Imagenes
            .Where(i => i.ProductoVarianteId == null)
            .OrderBy(i => i.Orden)
            .ToList();

        return new ProductoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Marca = marca,
            Modelo = modelo,
            Descripcion = p.Descripcion,
            TipoInventario = p.TipoInventario,
            Cantidad = stockTotal,
            Costo = costo,
            Precio = precio,
            PrecioMinimo = precio,
            PrecioMaximo = precioMaximo,
            UmbralStockBajo = umbral,
            TieneStockBajo = stockBajo,
            EstaAgotado = agotado,
            EstadoInventario = agotado ? "Agotado" : stockBajo ? "Stock bajo" : "Disponible",
            Activo = p.Activo,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre,
            ColorId = colorId,
            ColorNombre = color?.Nombre,
            ColorCodigoVisual = color?.CodigoVisual,
            TallaId = tallaId,
            TallaNombre = talla?.Nombre,
            MarcaId = marcaId,
            MarcaNombre = marcaEntidad?.Nombre,
            ModeloId = modeloId,
            ModeloNombre = modeloEntidad?.Nombre,
            ImagenPrincipalUrl = p.ImagenPrincipal?.Url,
            TotalImagenes = imagenesGenerales.Count,
            Imagenes = imagenesGenerales.Select(i => new ProductoImagenDto
            {
                Id = i.Id,
                Url = i.Url,
                Orden = i.Orden,
                EsPrincipal = i.EsPrincipal,
                ProductoVarianteId = null
            }).ToList(),
            Variantes = variantes.Select(v => new ProductoVarianteDto
            {
                Id = v.Id,
                ProductoId = v.ProductoId,
                ProductoNombre = p.Nombre,
                MarcaId = v.MarcaId,
                MarcaNombre = v.Marca?.Nombre,
                ModeloId = v.ModeloId,
                ModeloNombre = v.Modelo?.Nombre,
                ColorId = v.ColorId,
                ColorNombre = v.Color?.Nombre,
                ColorCodigoVisual = v.Color?.CodigoVisual,
                TallaId = v.TallaId,
                TallaNombre = v.Talla?.Nombre,
                Etiqueta = ConstruirEtiqueta(v),
                Sku = v.Sku ?? string.Empty,
                CodigoBarras = v.CodigoBarras,
                Cantidad = v.Cantidad,
                UmbralStockBajo = v.UmbralStockBajo,
                Costo = v.Costo ?? 0m,
                Precio = v.Precio ?? 0m,
                Activo = v.Activo,
                EsTecnica = v.EsTecnica,
                TieneStockBajo = v.TieneStockBajo,
                EstaAgotada = v.EstaAgotada,
                EstadoInventario = v.EstaAgotada ? "Agotada" : v.TieneStockBajo ? "Stock bajo" : "Disponible",
                FechaCreacion = v.FechaCreacion,
                FechaActualizacion = v.FechaActualizacion
            }).ToList(),
            TotalVariantes = variantes.Count,
            CreadoPorUsuarioId = p.CreadoPorUsuarioId,
            CreadoPorNombreUsuario = p.CreadoPorNombreUsuario,
            ActualizadoPorUsuarioId = p.ActualizadoPorUsuarioId,
            ActualizadoPorNombreUsuario = p.ActualizadoPorNombreUsuario,
            FechaCreacion = p.FechaCreacion,
            FechaActualizacion = p.FechaActualizacion
        };
    }

    private static decimal CalcularCosto(IReadOnlyCollection<ProductoVariante> variantes)
    {
        if (variantes.Count == 0) return 0m;
        var total = variantes.Sum(v => v.Cantidad);
        if (total > 0)
            return Math.Round(variantes.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / total, 2, MidpointRounding.AwayFromZero);
        var costos = variantes.Where(v => v.Costo.HasValue).Select(v => v.Costo!.Value).ToList();
        return costos.Count > 0 ? Math.Round(costos.Average(), 2, MidpointRounding.AwayFromZero) : 0m;
    }

    private static int? ValorComun(IReadOnlyCollection<ProductoVariante> variantes, Func<ProductoVariante, int?> selector)
    {
        if (variantes.Count == 0) return null;
        var valores = variantes.Select(selector).Distinct().Take(2).ToList();
        return valores.Count == 1 ? valores[0] : null;
    }

    private static bool NoVacio(string? valor) => !string.IsNullOrWhiteSpace(valor);

    private static string ConstruirEtiqueta(ProductoVariante variante)
    {
        var partes = new[] { variante.Marca?.Nombre, variante.Modelo?.Nombre, variante.Color?.Nombre, variante.Talla?.Nombre, variante.Sku };
        return string.Join(" · ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
    }
}
''')

# 2) ProductoRepository: filtros, búsqueda, orden y valorización solo por variantes.
write("backend/src/Infrastructure/Repositories/ProductoRepository.cs", r'''using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly AppDbContext _context;

    public ProductoRepository(AppDbContext context) => _context = context;

    private IQueryable<Producto> ConIncludes() =>
        _context.Productos
            .Include(p => p.Imagenes)
            .Include(p => p.Categoria)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Marca)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Modelo)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Color)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Talla)
            .AsSplitQuery();

    public Task<Producto?> GetByIdAsync(int id) => ConIncludes().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Producto?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");
        return await _context.Productos
            .FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {id} AND p.Eliminado = 0 FOR UPDATE")
            .AsTracking().FirstOrDefaultAsync();
    }

    public async Task<List<Producto>> GetByIdsForUpdateAsync(IEnumerable<int> ids)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdsForUpdateAsync requiere una transacción activa.");
        var result = new List<Producto>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var p = await GetByIdForUpdateAsync(id);
            if (p is not null) result.Add(p);
        }
        return result;
    }

    public async Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = ConIncludes().AsNoTracking().AsQueryable();
        if (request is ProductoPagedRequest filters)
        {
            if (filters.CategoriaId.HasValue) query = query.Where(p => p.CategoriaId == filters.CategoriaId.Value);
            if (filters.ColorId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ColorId == filters.ColorId.Value));
            if (filters.TallaId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.TallaId == filters.TallaId.Value));
            if (filters.MarcaId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.MarcaId == filters.MarcaId.Value));
            if (filters.ModeloId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ModeloId == filters.ModeloId.Value));
            if (filters.Activo.HasValue) query = query.Where(p => p.Activo == filters.Activo.Value);
            if (filters.Agotado.HasValue)
                query = filters.Agotado.Value
                    ? query.Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0))
                    : query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(search) ||
                (p.Descripcion != null && p.Descripcion.ToLower().Contains(search)) ||
                (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(search)) ||
                p.Variantes.Any(v => !v.Eliminado &&
                    ((v.Sku != null && v.Sku.ToLower().Contains(search)) ||
                     (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(search)) ||
                     (v.Marca != null && v.Marca.Nombre.ToLower().Contains(search)) ||
                     (v.Modelo != null && v.Modelo.Nombre.ToLower().Contains(search)) ||
                     (v.Color != null && v.Color.Nombre.ToLower().Contains(search)) ||
                     (v.Talla != null && v.Talla.Nombre.ToLower().Contains(search)))));
        }

        var totalCount = await query.CountAsync();
        var desc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.ToLower() switch
        {
            "marca" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault()),
            "modelo" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault()),
            "color" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Color != null ? v.Color.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Color != null ? v.Color.Nombre : string.Empty).FirstOrDefault()),
            "talla" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Talla != null ? v.Talla.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Talla != null ? v.Talla.Nombre : string.Empty).FirstOrDefault()),
            "cantidad" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Sum(v => v.Cantidad)) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Sum(v => v.Cantidad)),
            "costo" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Costo ?? 0m).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Costo ?? 0m).FirstOrDefault()),
            "precio" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Precio ?? 0m).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Precio ?? 0m).FirstOrDefault()),
            _ => desc ? query.OrderByDescending(p => p.Nombre) : query.OrderBy(p => p.Nombre)
        };

        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
        return (items, totalCount);
    }

    public Task<List<Producto>> GetStockBajoAsync() => ConIncludes().AsNoTracking()
        .Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > v.UmbralStockBajo))
        .OrderBy(p => p.Nombre).ToListAsync();

    public Task<List<Producto>> GetUltimosAgregadosAsync(int cantidad = 5) => ConIncludes().AsNoTracking()
        .OrderByDescending(p => p.FechaCreacion).Take(cantidad).ToListAsync();

    public Task<int> GetTotalProductosAsync() => _context.Productos.CountAsync();
    public async Task<int> GetTotalUnidadesAsync() => await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (int?)v.Cantidad) ?? 0;
    public async Task<decimal> GetValorTotalCostoAsync() => await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (decimal?)((v.Costo ?? 0m) * v.Cantidad)) ?? 0m;
    public async Task<decimal> GetValorTotalPrecioAsync() => await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (decimal?)((v.Precio ?? 0m) * v.Cantidad)) ?? 0m;
    public Task<int> GetTotalProductosPorTipoAsync(TipoInventario tipoInventario) => _context.Productos.CountAsync(p => p.TipoInventario == tipoInventario);
    public async Task<int> GetTotalUnidadesPorTipoAsync(TipoInventario tipoInventario) => await _context.ProductoVariantes.Where(v => !v.Eliminado && v.Producto.TipoInventario == tipoInventario).SumAsync(v => (int?)v.Cantidad) ?? 0;
    public async Task<decimal> GetValorTotalCostoPorTipoAsync(TipoInventario tipoInventario) => await _context.ProductoVariantes.Where(v => !v.Eliminado && v.Producto.TipoInventario == tipoInventario).SumAsync(v => (decimal?)((v.Costo ?? 0m) * v.Cantidad)) ?? 0m;
    public async Task<decimal> GetValorTotalPrecioPorTipoAsync(TipoInventario tipoInventario) => await _context.ProductoVariantes.Where(v => !v.Eliminado && v.Producto.TipoInventario == tipoInventario).SumAsync(v => (decimal?)((v.Precio ?? 0m) * v.Cantidad)) ?? 0m;

    public Task AddAsync(Producto producto) => _context.Productos.AddAsync(producto).AsTask();
    public void Update(Producto producto) => _context.Productos.Update(producto);
    public void Remove(Producto producto) => _context.Productos.Remove(producto);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
''')

# 3) Escáner: sin fallback económico/dimensional a Producto.
replace_once("backend/src/Application/Services/ProductoEscanerService.cs",
'''        Marca = variante.Marca?.Nombre ?? variante.Producto.Marca, Modelo = variante.Modelo?.Nombre ?? variante.Producto.Modelo,''',
'''        Marca = variante.Marca?.Nombre ?? string.Empty, Modelo = variante.Modelo?.Nombre ?? string.Empty,''')
replace_once("backend/src/Application/Services/ProductoEscanerService.cs",
'''        CodigoBarras = variante.CodigoBarras, CantidadDisponible = variante.Cantidad, Precio = variante.Precio ?? variante.Producto.Precio,''',
'''        CodigoBarras = variante.CodigoBarras, CantidadDisponible = variante.Cantidad, Precio = variante.Precio ?? 0m,''')
replace_once("backend/src/Application/Services/ProductoEscanerService.cs",
'''        Marca = variante.Marca?.Nombre ?? variante.Producto.Marca, Modelo = variante.Modelo?.Nombre ?? variante.Producto.Modelo,''',
'''        Marca = variante.Marca?.Nombre ?? string.Empty, Modelo = variante.Modelo?.Nombre ?? string.Empty,''')
replace_once("backend/src/Application/Services/ProductoEscanerService.cs",
'''        CodigoBarras = variante.CodigoBarras, CantidadDisponible = variante.Cantidad, Costo = variante.Costo ?? variante.Producto.Costo,
        Precio = variante.Precio ?? variante.Producto.Precio, ImagenMiniaturaUrl = ObtenerImagenMiniatura(variante.Producto)''',
'''        CodigoBarras = variante.CodigoBarras, CantidadDisponible = variante.Cantidad, Costo = variante.Costo ?? 0m,
        Precio = variante.Precio ?? 0m, ImagenMiniaturaUrl = ObtenerImagenMiniatura(variante.Producto)''')

# 4) DTO: deja explícito que campos superiores son entrada de compatibilidad, no persistencia autoritativa.
replace_once("backend/src/Application/DTOs/CreateProductoDto.cs",
'''/// Variante capturada dentro del formulario principal de productos.
/// El stock consolidado del producto se calcula como la suma de estas filas.''',
'''/// Unidad exacta de inventario capturada dentro del formulario principal.
/// ProductoVariante es la única autoridad de SKU, barcode, stock, costo, precio, umbral y dimensiones.''')
replace_once("backend/src/Application/DTOs/CreateProductoDto.cs",
'''    // Compatibilidad temporal con clientes anteriores. Cuando se envían los IDs,
    // el backend obtiene Marca/Modelo desde sus mantenimientos.''',
'''    // Compatibilidad de entrada para clientes anteriores. Estos campos NO son fuente de verdad:
    // el controlador los traduce a ProductoVariante y Producto no los usa operativamente.''')

# 5) ProductoService: solo familia/común; no valida ni escribe atributos/economía/inventario de variante.
replace_once("backend/src/Application/Services/ProductoService.cs",
'''        await ValidarCategoriaAsync(dto.CategoriaId, exigirActiva: true);
        await ValidarCatalogosAsync(dto.ColorId, dto.TallaId, dto.MarcaId, dto.ModeloId);
        var (marcaNombre, modeloNombre) = await ResolverMarcaModeloAsync(
            dto.MarcaId,
            dto.ModeloId,
            dto.Marca,
            dto.Modelo);
''',
'''        await ValidarCategoriaAsync(dto.CategoriaId, exigirActiva: true);
''')
replace_once("backend/src/Application/Services/ProductoService.cs",
'''            Nombre = dto.Nombre.Trim(),
            Marca = marcaNombre,
            Modelo = modeloNombre,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            TipoInventario = dto.TipoInventario,
            Cantidad = dto.Cantidad,
            Costo = dto.Costo,
            Precio = dto.Precio,
            UmbralStockBajo = dto.UmbralStockBajo,
            CategoriaId = dto.CategoriaId,
            ColorId = dto.ColorId,
            TallaId = dto.TallaId,
            MarcaId = dto.MarcaId,
            ModeloId = dto.ModeloId,''',
'''            Nombre = dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            TipoInventario = dto.TipoInventario,
            CategoriaId = dto.CategoriaId,''')
regex_once("backend/src/Application/Services/ProductoService.cs",
r'''            valoresNuevos: new\n            \{\n                producto\.Nombre,\n                producto\.TipoInventario,\n                producto\.MarcaId,\n                producto\.ModeloId,\n                producto\.ColorId,\n                producto\.TallaId,\n                producto\.Cantidad,\n                producto\.Costo,\n                producto\.Precio,\n                ImagenesGenerales = producto\.Imagenes\.Count\(i => i\.ProductoVarianteId == null\)\n            \}\);''',
r'''            valoresNuevos: new
            {
                producto.Nombre,
                producto.TipoInventario,
                producto.CategoriaId,
                ImagenesGenerales = producto.Imagenes.Count(i => i.ProductoVarianteId == null)
            });''')
regex_once("backend/src/Application/Services/ProductoService.cs",
r'''        var valoresAnteriores = new\n        \{.*?            ImagenPrincipalId = producto\.ImagenPrincipal\?\.Id\n        \};\n\n        await ValidarCategoriaAsync\(dto\.CategoriaId, exigirActiva: false\);\n        await ValidarCatalogosAsync\(dto\.ColorId, dto\.TallaId, dto\.MarcaId, dto\.ModeloId\);\n        var \(marcaNombre, modeloNombre\) = await ResolverMarcaModeloAsync\(\n            dto\.MarcaId,\n            dto\.ModeloId,\n            dto\.Marca,\n            dto\.Modelo\);\n\n        if \(dto\.Cantidad != producto\.Cantidad\)\n        \{\n            throw new BusinessRuleException\(\n                "El stock no puede modificarse desde el mantenimiento general\. Utiliza la operación Ajustar inventario\."\);\n        \}\n''',
r'''        var valoresAnteriores = new
        {
            producto.Nombre,
            producto.TipoInventario,
            producto.Descripcion,
            producto.CategoriaId,
            Imagenes = imagenesGenerales.Count,
            ImagenPrincipalId = producto.ImagenPrincipal?.Id
        };

        await ValidarCategoriaAsync(dto.CategoriaId, exigirActiva: false);
''')
replace_once("backend/src/Application/Services/ProductoService.cs",
'''        producto.Nombre = dto.Nombre.Trim();
        producto.Marca = marcaNombre;
        producto.Modelo = modeloNombre;
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.Costo = dto.Costo;
        producto.Precio = dto.Precio;
        producto.UmbralStockBajo = dto.UmbralStockBajo;
        producto.CategoriaId = dto.CategoriaId;
        producto.ColorId = dto.ColorId;
        producto.TallaId = dto.TallaId;
        producto.MarcaId = dto.MarcaId;
        producto.ModeloId = dto.ModeloId;''',
'''        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.CategoriaId = dto.CategoriaId;''')
regex_once("backend/src/Application/Services/ProductoService.cs",
r'''            valoresNuevos: new\n            \{\n                producto\.Nombre,\n                producto\.TipoInventario,\n                producto\.MarcaId,\n                producto\.ModeloId,\n                producto\.ColorId,\n                producto\.TallaId,\n                producto\.Descripcion,\n                producto\.Cantidad,\n                producto\.Costo,\n                producto\.Precio,\n                producto\.UmbralStockBajo,\n                producto\.CategoriaId,''',
r'''            valoresNuevos: new
            {
                producto.Nombre,
                producto.TipoInventario,
                producto.Descripcion,
                producto.CategoriaId,''')

# 6) API de variantes: sincronización técnica recibe el payload exacto.
replace_once("backend/src/Application/Interfaces/IProductoVarianteService.cs",
'''    Task<ProductoVarianteDto> AsegurarTecnicaAsync(int productoId);
    Task RetirarTecnicaParaConversionAsync(int productoId);''',
'''    Task<ProductoVarianteDto> AsegurarTecnicaAsync(int productoId);
    Task<ProductoVarianteDto> SincronizarTecnicaAsync(int productoId, ProductoVarianteFormularioDto dto);
    Task RetirarTecnicaParaConversionAsync(int productoId);''')

# 7) ProductoVarianteService: elimina recalculo espejo y hace técnica autoritativa.
text = read("backend/src/Application/Services/ProductoVarianteService.cs")
text = text.replace("await RecalcularProductoAsync(producto);", "await MarcarProductoActualizadoAsync(producto);")
text = text.replace("            producto.Cantidad = 0;\n            producto.FechaActualizacion = DateTime.UtcNow;\n            await _productoRepository.SaveChangesAsync();\n", "            await MarcarProductoActualizadoAsync(producto);\n")
old_sync = '''    public async Task SincronizarTecnicaConProductoAsync(int productoId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;
            var vigentes = await _repository.GetByProductoIdAsync(productoId, true);
            var comerciales = vigentes.Where(v => !v.EsTecnica).ToList();
            var tecnica = vigentes.SingleOrDefault(v => v.EsTecnica);
            if (comerciales.Count > 0)
            {
                if (tecnica is not null)
                    throw new BusinessRuleException("El producto no puede conservar simultáneamente una variante técnica y variantes comerciales.");
                return;
            }
            await AsegurarTecnicaBajoLockAsync(producto);
        });
    }
'''
new_sync = '''    public async Task<ProductoVarianteDto> SincronizarTecnicaAsync(int productoId, ProductoVarianteFormularioDto dto)
    {
        ProductoVariante? tecnica = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
                ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");
            if (!string.IsNullOrWhiteSpace(dto.Sku) || !string.IsNullOrWhiteSpace(dto.CodigoBarras))
                throw new BusinessRuleException("La variante técnica utiliza un SKU interno y no admite código de barras manual.");
            if (dto.ModeloId.HasValue && !dto.MarcaId.HasValue)
                throw new BusinessRuleException("Todo modelo debe indicar su marca.");
            if (dto.Cantidad < 0 || dto.UmbralStockBajo < 0 || dto.Costo < 0 || dto.Precio <= 0)
                throw new BusinessRuleException("La variante técnica requiere stock/umbral no negativos, costo no negativo y precio mayor que cero.");
            await _catalogoService.ValidarSeleccionProductoAsync(dto.ColorId, dto.TallaId, dto.MarcaId, dto.ModeloId);

            var vigentes = await _repository.GetByProductoIdAsync(productoId, true);
            if (vigentes.Any(v => !v.EsTecnica))
                throw new BusinessRuleException("No se puede sincronizar una variante técnica mientras existan variantes comerciales.");

            tecnica = await _repository.GetTecnicaByProductoIdAsync(productoId, true);
            var esNueva = tecnica is null || tecnica.Eliminado;
            if (tecnica is null)
            {
                tecnica = new ProductoVariante
                {
                    ProductoId = productoId,
                    Sku = CrearSkuTecnico(productoId),
                    EsTecnica = true,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                await _repository.AddAsync(tecnica);
            }
            else if (!esNueva && tecnica.Cantidad != dto.Cantidad)
            {
                throw new BusinessRuleException("El stock del producto simple debe cambiarse mediante Ajustar inventario para conservar trazabilidad.");
            }

            tecnica.EsTecnica = true;
            tecnica.MarcaId = dto.MarcaId;
            tecnica.ModeloId = dto.ModeloId;
            tecnica.ColorId = dto.ColorId;
            tecnica.TallaId = dto.TallaId;
            tecnica.CodigoBarras = null;
            if (esNueva) tecnica.Cantidad = dto.Cantidad;
            tecnica.UmbralStockBajo = dto.UmbralStockBajo;
            tecnica.Costo = dto.Costo;
            tecnica.Precio = dto.Precio;
            tecnica.Activo = producto.Activo;
            tecnica.Eliminado = false;
            tecnica.FechaEliminacion = null;
            tecnica.EliminadoPorUsuarioId = null;
            MarcarActualizacion(tecnica);
            await _repository.SaveChangesAsync();
            await MarcarProductoActualizadoAsync(producto);
        });
        return ToDto((await _repository.GetByIdAsync(tecnica!.Id))!);
    }

    public async Task SincronizarTecnicaConProductoAsync(int productoId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var producto = await _productoRepository.GetByIdForUpdateAsync(productoId);
            if (producto is null) return;
            var vigentes = await _repository.GetByProductoIdAsync(productoId, true);
            var comerciales = vigentes.Where(v => !v.EsTecnica).ToList();
            var tecnica = vigentes.SingleOrDefault(v => v.EsTecnica);
            if (comerciales.Count > 0)
            {
                if (tecnica is not null)
                    throw new BusinessRuleException("El producto no puede conservar simultáneamente una variante técnica y variantes comerciales.");
                return;
            }
            tecnica ??= await AsegurarTecnicaBajoLockAsync(producto);
            tecnica.Activo = producto.Activo;
            MarcarActualizacion(tecnica);
            await _repository.SaveChangesAsync();
        });
    }
'''
if old_sync not in text:
    raise RuntimeError("ProductoVarianteService: no se encontró SincronizarTecnicaConProductoAsync esperado")
text = text.replace(old_sync, new_sync, 1)
old_aseg = '''        tecnica.EsTecnica = true;
        tecnica.MarcaId = null;
        tecnica.ModeloId = null;
        tecnica.ColorId = null;
        tecnica.TallaId = null;
        tecnica.CodigoBarras = null;
        tecnica.Cantidad = producto.Cantidad;
        tecnica.UmbralStockBajo = producto.UmbralStockBajo;
        tecnica.Costo = producto.Costo;
        tecnica.Precio = producto.Precio;
        tecnica.Activo = producto.Activo;'''
new_aseg = '''        tecnica.EsTecnica = true;
        tecnica.CodigoBarras = null;
        tecnica.Activo = producto.Activo;'''
if old_aseg not in text:
    raise RuntimeError("ProductoVarianteService: no se encontró copia legacy a técnica")
text = text.replace(old_aseg, new_aseg, 1)
text, n = re.subn(r'''    private async Task RecalcularProductoAsync\(Producto producto\)\n    \{.*?    \}\n\n    private static int\? ValorComun\(IEnumerable<int\?> valores\)\n    \{.*?    \}\n''', '''    private async Task MarcarProductoActualizadoAsync(Producto producto)
    {
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;
        await _productoRepository.SaveChangesAsync();
    }

''', text, count=1, flags=re.S)
if n != 1:
    raise RuntimeError("ProductoVarianteService: no se pudo retirar RecalcularProductoAsync")
text = text.replace('''        if (!dto.MarcaId.HasValue && !dto.ModeloId.HasValue && !dto.ColorId.HasValue && !dto.TallaId.HasValue)
            throw new BusinessRuleException("Una variante comercial debe definir al menos una dimensión: marca, modelo, color o talla.");''', '''        if (!dto.MarcaId.HasValue && !dto.ModeloId.HasValue && !dto.ColorId.HasValue && !dto.TallaId.HasValue)
            throw new BusinessRuleException("Una variante comercial debe definir al menos una dimensión: marca, modelo, color o talla.");
        if (dto.ModeloId.HasValue && !dto.MarcaId.HasValue)
            throw new BusinessRuleException("Todo modelo de variante debe indicar su marca.");''', 1)
write("backend/src/Application/Services/ProductoVarianteService.cs", text)

# 8) ProductosController: compatibilidad se traduce a variante; se elimina proyección inversa.
replace_once("backend/src/API/Controllers/ProductosController.cs",
'''        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            AplicarProyeccionLegacy(dto);
            creado = await _productoService.CreateAsync(dto);
            await SincronizarVariantesAsync(creado.Id, dto.Variantes, Array.Empty<ProductoVarianteDto>());
        });''',
'''        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (variantes, forzarTecnica) = ResolverVariantesSolicitud(dto);
            creado = await _productoService.CreateAsync(dto);
            await SincronizarVariantesAsync(creado.Id, variantes, Array.Empty<ProductoVarianteDto>(), forzarTecnica);
        });''')
replace_once("backend/src/API/Controllers/ProductosController.cs",
'''        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            AplicarProyeccionLegacy(dto);
            var existentes = await _varianteService.GetByProductoIdAsync(id, incluirInactivas: true);
            actualizado = await _productoService.UpdateAsync(id, dto);
            if (actualizado is not null)
            {
                await SincronizarVariantesAsync(id, dto.Variantes, existentes);
                await _varianteService.SincronizarTecnicaConProductoAsync(id);
            }
        });''',
'''        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (variantes, forzarTecnica) = ResolverVariantesSolicitud(dto);
            var existentes = await _varianteService.GetByProductoIdAsync(id, incluirInactivas: true);
            actualizado = await _productoService.UpdateAsync(id, dto);
            if (actualizado is not null)
            {
                await SincronizarVariantesAsync(id, variantes, existentes, forzarTecnica);
                await _varianteService.SincronizarTecnicaConProductoAsync(id);
            }
        });''')
replace_once("backend/src/API/Controllers/ProductosController.cs",
'''        IReadOnlyCollection<ProductoVarianteFormularioDto> solicitadas,
        IReadOnlyCollection<ProductoVarianteDto> existentes)
    {''',
'''        IReadOnlyCollection<ProductoVarianteFormularioDto> solicitadas,
        IReadOnlyCollection<ProductoVarianteDto> existentes,
        bool forzarTecnica = false)
    {''')
replace_once("backend/src/API/Controllers/ProductosController.cs",
'''        if (EsSolicitudTecnica(solicitadas))
        {''',
'''        if (forzarTecnica || EsSolicitudTecnica(solicitadas))
        {''')
replace_once("backend/src/API/Controllers/ProductosController.cs",
'''            await _varianteService.SincronizarTecnicaConProductoAsync(productoId);
            return;''',
'''            await _varianteService.SincronizarTecnicaAsync(productoId, solicitadas.Single());
            return;''')
# sustituye bloque de proyección legacy por resolutores de compatibilidad.
regex_once("backend/src/API/Controllers/ProductosController.cs",
r'''    private static void AplicarProyeccionLegacy\(CreateProductoDto dto\)\n    \{.*?    private static bool EsSolicitudTecnica''',
r'''    private static (IReadOnlyCollection<ProductoVarianteFormularioDto> Variantes, bool ForzarTecnica)
        ResolverVariantesSolicitud(CreateProductoDto dto)
    {
        if (dto.Variantes.Count > 0) return (dto.Variantes, false);
        return (new[] { DesdeCompatibilidad(dto.MarcaId, dto.ModeloId, dto.ColorId, dto.TallaId, dto.Cantidad, dto.UmbralStockBajo, dto.Costo, dto.Precio) }, true);
    }

    private static (IReadOnlyCollection<ProductoVarianteFormularioDto> Variantes, bool ForzarTecnica)
        ResolverVariantesSolicitud(UpdateProductoDto dto)
    {
        if (dto.Variantes.Count > 0) return (dto.Variantes, false);
        return (new[] { DesdeCompatibilidad(dto.MarcaId, dto.ModeloId, dto.ColorId, dto.TallaId, dto.Cantidad, dto.UmbralStockBajo, dto.Costo, dto.Precio) }, true);
    }

    private static ProductoVarianteFormularioDto DesdeCompatibilidad(
        int? marcaId, int? modeloId, int? colorId, int? tallaId,
        int cantidad, int umbral, decimal costo, decimal precio) => new()
    {
        MarcaId = marcaId,
        ModeloId = modeloId,
        ColorId = colorId,
        TallaId = tallaId,
        Cantidad = cantidad,
        UmbralStockBajo = umbral,
        Costo = costo,
        Precio = precio,
        Activo = true
    };

    private static bool EsSolicitudTecnica''')
write("backend/src/API/Controllers/ProductosController.cs", read("backend/src/API/Controllers/ProductosController.cs"))

# 9) InventarioConcurrencyService: toda demanda se resuelve a una variante exacta; Producto jamás muta stock.
write("backend/src/Infrastructure/Services/InventarioConcurrencyService.cs", r'''using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public class InventarioConcurrencyService : IInventarioConcurrencyService
{
    private readonly AppDbContext _context;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public InventarioConcurrencyService(AppDbContext context, IProductoRepository productoRepository, IProductoVarianteRepository productoVarianteRepository)
    {
        _context = context;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
    }

    public Task<InventarioLockSet> BloquearYValidarInventarioAsync(IEnumerable<InventarioDemanda> demandMap, bool esDeduccion = true) =>
        BloquearYValidarCoreAsync(demandMap, esDeduccion, incluirEliminados: false);

    public Task<InventarioLockSet> BloquearInventarioParaReversionAsync(IEnumerable<InventarioDemanda> demandMap) =>
        BloquearYValidarCoreAsync(demandMap, esDeduccion: false, incluirEliminados: true);

    private async Task<InventarioLockSet> BloquearYValidarCoreAsync(IEnumerable<InventarioDemanda> demandMap, bool esDeduccion, bool incluirEliminados)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de inventario requiere una transacción activa.");

        var original = demandMap.Select(x => x ?? throw new ArgumentException("La demanda contiene un elemento nulo.", nameof(demandMap))).ToList();
        if (original.Any(x => x.ProductoId <= 0 || x.Cantidad <= 0))
            throw new BusinessRuleException("Cada demanda debe indicar un producto válido y una cantidad mayor a cero.");
        if (original.Count == 0)
            return new InventarioLockSet(new Dictionary<int, Producto>(), new Dictionary<int, ProductoVariante>(), Array.Empty<InventarioDemanda>());

        var productoIds = original.Select(x => x.ProductoId).Distinct().OrderBy(x => x).ToList();
        var productos = incluirEliminados ? await BloquearProductosIncluyendoEliminadosAsync(productoIds) : await _productoRepository.GetByIdsForUpdateAsync(productoIds);
        var productosMap = productos.ToDictionary(x => x.Id);
        foreach (var id in productoIds)
            if (!productosMap.ContainsKey(id)) throw new BusinessRuleException($"El producto ID '{id}' no existe físicamente.");

        var resueltas = new List<InventarioDemanda>(original.Count);
        foreach (var item in original)
        {
            var varianteId = item.ProductoVarianteId;
            if (!varianteId.HasValue)
            {
                var tecnica = await _productoVarianteRepository.GetTecnicaByProductoIdAsync(item.ProductoId, incluirEliminados);
                if (tecnica is null)
                    throw new BusinessRuleException("La operación requiere una variante exacta. El producto no posee una variante técnica resoluble.");
                varianteId = tecnica.Id;
            }
            resueltas.Add(new InventarioDemanda(item.ProductoId, varianteId, item.Cantidad));
        }

        var consolidada = resueltas
            .GroupBy(x => (x.ProductoId, x.ProductoVarianteId))
            .Select(g => new InventarioDemanda(g.Key.ProductoId, g.Key.ProductoVarianteId, g.Sum(x => x.Cantidad)))
            .OrderBy(x => x.ProductoId).ThenBy(x => x.ProductoVarianteId).ToList();
        var varianteIds = consolidada.Select(x => x.ProductoVarianteId!.Value).Distinct().OrderBy(x => x).ToList();
        var variantes = incluirEliminados ? await BloquearVariantesIncluyendoEliminadasAsync(varianteIds) : await _productoVarianteRepository.GetByIdsForUpdateAsync(varianteIds);
        var variantesMap = variantes.ToDictionary(x => x.Id);

        foreach (var item in consolidada)
        {
            if (!variantesMap.TryGetValue(item.ProductoVarianteId!.Value, out var variante))
                throw new BusinessRuleException($"La variante ID '{item.ProductoVarianteId.Value}' no existe físicamente.");
            if (variante.ProductoId != item.ProductoId)
                throw new BusinessRuleException($"La variante ID '{variante.Id}' no pertenece al producto ID '{item.ProductoId}'.");
            if (esDeduccion && variante.Cantidad < item.Cantidad)
                throw new BusinessRuleException($"Stock insuficiente para la variante '{variante.Sku}': disponible {variante.Cantidad}, solicitado {item.Cantidad}.");
        }

        return new InventarioLockSet(productosMap, variantesMap, consolidada);
    }

    private async Task<List<Producto>> BloquearProductosIncluyendoEliminadosAsync(IEnumerable<int> ids)
    {
        var resultado = new List<Producto>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var producto = await _context.Productos.FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {id} FOR UPDATE")
                .IgnoreQueryFilters().AsTracking().FirstOrDefaultAsync();
            if (producto is not null) resultado.Add(producto);
        }
        return resultado;
    }

    private async Task<List<ProductoVariante>> BloquearVariantesIncluyendoEliminadasAsync(IEnumerable<int> ids)
    {
        var resultado = new List<ProductoVariante>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var variante = await _context.ProductoVariantes.FromSqlInterpolated($"SELECT pv.* FROM ProductoVariantes pv WHERE pv.Id = {id} FOR UPDATE")
                .IgnoreQueryFilters().AsTracking().FirstOrDefaultAsync();
            if (variante is not null) resultado.Add(variante);
        }
        return resultado;
    }

    public async Task AjustarStockPesimistaAsync(int productoId, int? productoVarianteId, int cantidadActualEsperada, int cantidadNueva)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("AjustarStockPesimistaAsync requiere una transacción activa.");
        if (cantidadActualEsperada < 0 || cantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");

        _ = await _productoRepository.GetByIdForUpdateAsync(productoId)
            ?? throw new BusinessRuleException($"El producto ID '{productoId}' no existe.");
        if (!productoVarianteId.HasValue)
        {
            var tecnica = await _productoVarianteRepository.GetTecnicaByProductoIdAsync(productoId)
                ?? throw new BusinessRuleException("El producto no posee una variante técnica; ajusta la variante exacta.");
            productoVarianteId = tecnica.Id;
        }

        var variante = await _productoVarianteRepository.GetByIdForUpdateAsync(productoVarianteId.Value)
            ?? throw new BusinessRuleException($"La variante ID '{productoVarianteId.Value}' no existe.");
        if (variante.ProductoId != productoId)
            throw new BusinessRuleException("La variante indicada no pertenece al producto solicitado.");
        if (variante.Cantidad != cantidadActualEsperada)
            throw new BusinessRuleException("El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");

        variante.Cantidad = cantidadNueva;
        _productoVarianteRepository.Update(variante);
    }
}
''')

# 10) Venta: siempre resuelve variante exacta y nunca muta stock/costo de Producto.
for block in [
'''            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad -= productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

''',
'''            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad += productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

''']:
    replace_once("backend/src/Application/Services/VentaService.cs", block, "")
replace_once("backend/src/Application/Services/VentaService.cs",
'''                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)''',
'''                    .Where(d => d.ProductoId == item.ProductoId && (d.ProductoVarianteId == item.ProductoVarianteId || !d.ProductoVarianteId.HasValue))''')
replace_once("backend/src/Application/Services/VentaService.cs",
'''                var stockAnteriorMovimiento = producto.Cantidad + item.Cantidad;
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
                }''',
'''                var variante = inventario.Variantes[item.ProductoVarianteId!.Value];
                if (!variante.Activo)
                    throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva y no puede venderse.");
                var stockAnteriorMovimiento = variante.Cantidad;
                variante.Cantidad -= item.Cantidad;
                var stockNuevoMovimiento = variante.Cantidad;
                _productoVarianteRepository.Update(variante);''')
replace_once("backend/src/Application/Services/VentaService.cs",
'''                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)''',
'''                    .Where(d => d.ProductoId == item.ProductoId && (d.ProductoVarianteId == item.ProductoVarianteId || !d.ProductoVarianteId.HasValue))''')
replace_once("backend/src/Application/Services/VentaService.cs",
'''                var stockAnteriorMovimiento = producto.Cantidad - item.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad += item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }''',
'''                var variante = inventario.Variantes[item.ProductoVarianteId!.Value];
                var stockAnteriorMovimiento = variante.Cantidad;
                variante.Cantidad += item.Cantidad;
                var stockNuevoMovimiento = variante.Cantidad;
                _productoVarianteRepository.Update(variante);''')
replace_once("backend/src/Application/Services/VentaService.cs",
'''            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }

            var costoUnitario = variante?.Costo ?? producto.Costo;
            var precioUnitario = variante?.Precio ?? input.PrecioUnitario;''',
'''            var variante = input.ProductoVarianteId.HasValue
                ? await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true)
                : await ObtenerTecnicaAsync(producto.Id, exigirActiva: true);
            if (validarStock && variante.Cantidad < input.Cantidad)
                throw new BusinessRuleException($"Stock insuficiente para '{producto.Nombre}': disponible {variante.Cantidad}, solicitado {input.Cantidad}.");

            var costoUnitario = variante.Costo ?? 0m;
            var precioUnitario = variante.Precio ?? 0m;''')
# elimina check product stock previo (si existe)
text = read("backend/src/Application/Services/VentaService.cs")
text = re.sub(r'''\n            if \(validarStock && producto\.Cantidad < input\.Cantidad\)\n                throw new BusinessRuleException\(\n                    \$"Stock insuficiente para '\{producto\.Nombre\}': disponible \{producto\.Cantidad\}, solicitado \{input\.Cantidad\}\."\);''', '', text, count=1)
text = text.replace('''                ProductoVarianteId = variante?.Id,''', '''                ProductoVarianteId = variante.Id,''')
text = text.replace('''                ProductoMarcaSnapshot = variante?.Marca?.Nombre ?? producto.Marca,
                ProductoModeloSnapshot = variante?.Modelo?.Nombre ?? producto.Modelo,
                ProductoColorSnapshot = variante?.Color?.Nombre,
                ProductoTallaSnapshot = variante?.Talla?.Nombre,
                ProductoSkuSnapshot = variante?.Sku''', '''                ProductoMarcaSnapshot = variante.Marca?.Nombre,
                ProductoModeloSnapshot = variante.Modelo?.Nombre,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                ProductoSkuSnapshot = variante.Sku''', 1)
# preview: resolver técnica y precio solo variante
old_preview = '''            ProductoVariante? variante = null;
            if (d.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(d.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }

            if (variante is null && d.PrecioUnitario <= 0)
                throw new BusinessRuleException("El precio unitario de cada producto debe ser mayor a 0.");

            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = d.Cantidad,
                PrecioUnitario = variante?.Precio ?? d.PrecioUnitario
            });'''
new_preview = '''            var variante = d.ProductoVarianteId.HasValue
                ? await ObtenerVarianteAsync(d.ProductoVarianteId.Value, producto.Id, exigirActiva: true)
                : await ObtenerTecnicaAsync(producto.Id, exigirActiva: true);
            var precio = variante.Precio ?? 0m;
            if (precio <= 0)
                throw new BusinessRuleException("La variante seleccionada no posee un precio operativo válido.");

            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = d.Cantidad,
                PrecioUnitario = precio
            });'''
if old_preview not in text:
    raise RuntimeError("VentaService: no se encontró bloque preview")
text = text.replace(old_preview, new_preview, 1)
# helper técnica antes de ObtenerVariante
marker = '''    private async Task<ProductoVariante> ObtenerVarianteAsync(int varianteId, int productoId, bool exigirActiva)
'''
helper = '''    private async Task<ProductoVariante> ObtenerTecnicaAsync(int productoId, bool exigirActiva)
    {
        var variante = await _productoVarianteRepository.GetTecnicaByProductoIdAsync(productoId)
            ?? throw new BusinessRuleException("El producto simple no posee una variante técnica operativa.");
        if (exigirActiva && !variante.Activo)
            throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva.");
        return variante;
    }

'''
if marker not in text:
    raise RuntimeError("VentaService: marcador helper no encontrado")
text = text.replace(marker, helper + marker, 1)
write("backend/src/Application/Services/VentaService.cs", text)

# 11) Compra: exact variant always; cost/stock only variant.
text = read("backend/src/Application/Services/CompraService.cs")
text = re.sub(r'''\n            var stocksProductoAnteriores = inventario\.Productos\.ToDictionary\(x => x\.Key, x => x\.Value\.Cantidad\);\n            var costosProductoAnteriores = inventario\.Productos\.ToDictionary\(x => x\.Key, x => x\.Value\.Costo\);\n''', '\n', text, count=1)
text = text.replace('''                var stockAnteriorMovimiento = stocksProductoAnteriores[item.ProductoId];
                var stockNuevoMovimiento = stockAnteriorMovimiento + item.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    var valorAnteriorVariante = (variante.Costo ?? 0m) * variante.Cantidad;
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
                }''', '''                var variante = inventario.Variantes[item.ProductoVarianteId!.Value];
                var stockAnteriorMovimiento = variante.Cantidad;
                var valorAnteriorVariante = (variante.Costo ?? 0m) * variante.Cantidad;
                variante.Cantidad += item.Cantidad;
                variante.Costo = Math.Round((valorAnteriorVariante + valorEntrada) / variante.Cantidad, 2, MidpointRounding.AwayFromZero);
                variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                variante.FechaActualizacion = DateTime.UtcNow;
                var stockNuevoMovimiento = variante.Cantidad;
                _productoVarianteRepository.Update(variante);''', 1)
text, n = re.subn(r'''\n            foreach \(var productoGrupo in inventario\.Demandas\.GroupBy\(x => x\.ProductoId\)\)\n            \{\n                var producto = inventario\.Productos\[productoGrupo\.Key\];\n                var cantidadEntrada = productoGrupo\.Sum\(x => x\.Cantidad\);.*?                _productoRepository\.Update\(producto\);\n            \}\n''', '\n', text, count=1, flags=re.S)
if n != 1:
    raise RuntimeError("CompraService: no se retiró recálculo de Producto al confirmar")
text = re.sub(r'''\n            var stocksProductoAnteriores = inventario\.Productos\.ToDictionary\(x => x\.Key, x => x\.Value\.Cantidad\);\n''', '\n', text, count=1)
text = text.replace('''                var stockAnteriorMovimiento = stocksProductoAnteriores[item.ProductoId];
                var stockNuevoMovimiento = stockAnteriorMovimiento - item.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad -= item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }''', '''                var variante = inventario.Variantes[item.ProductoVarianteId!.Value];
                var stockAnteriorMovimiento = variante.Cantidad;
                variante.Cantidad -= item.Cantidad;
                var stockNuevoMovimiento = variante.Cantidad;
                _productoVarianteRepository.Update(variante);''', 1)
text, n = re.subn(r'''\n            foreach \(var productoGrupo in inventario\.Demandas\.GroupBy\(x => x\.ProductoId\)\)\n            \{\n                var producto = inventario\.Productos\[productoGrupo\.Key\];\n                producto\.Cantidad = stocksProductoAnteriores\[producto\.Id\] - productoGrupo\.Sum\(x => x\.Cantidad\);\n                _productoRepository\.Update\(producto\);\n            \}\n''', '\n', text, count=1)
if n != 1:
    raise RuntimeError("CompraService: no se retiró recálculo de Producto al anular")
old_armar = '''            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }

            compra.Detalles.Add(new CompraDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante?.Id,'''
new_armar = '''            var variante = input.ProductoVarianteId.HasValue
                ? await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true)
                : await ObtenerTecnicaAsync(producto.Id, exigirActiva: true);

            compra.Detalles.Add(new CompraDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante.Id,'''
if old_armar not in text:
    raise RuntimeError("CompraService: bloque ArmarDetalles no encontrado")
text = text.replace(old_armar, new_armar, 1)
text = text.replace('''                ProductoMarcaSnapshot = variante?.Marca?.Nombre ?? producto.Marca,
                ProductoModeloSnapshot = variante?.Modelo?.Nombre ?? producto.Modelo,
                ProductoColorSnapshot = variante?.Color?.Nombre,
                ProductoTallaSnapshot = variante?.Talla?.Nombre,
                ProductoSkuSnapshot = variante?.Sku''', '''                ProductoMarcaSnapshot = variante.Marca?.Nombre,
                ProductoModeloSnapshot = variante.Modelo?.Nombre,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                ProductoSkuSnapshot = variante.Sku''', 1)
marker = '''    private async Task<ProductoVariante> ObtenerVarianteAsync(int varianteId, int productoId, bool exigirActiva)
'''
helper = '''    private async Task<ProductoVariante> ObtenerTecnicaAsync(int productoId, bool exigirActiva)
    {
        var variante = await _productoVarianteRepository.GetTecnicaByProductoIdAsync(productoId)
            ?? throw new BusinessRuleException("El producto simple no posee una variante técnica operativa.");
        if (exigirActiva && !variante.Activo)
            throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva.");
        return variante;
    }

'''
if marker not in text:
    raise RuntimeError("CompraService: marcador helper no encontrado")
text = text.replace(marker, helper + marker, 1)
# matching de detalles históricos nulos tras resolver demanda a técnica
text = text.replace('''.Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)''', '''.Where(d => d.ProductoId == item.ProductoId && (d.ProductoVarianteId == item.ProductoVarianteId || !d.ProductoVarianteId.HasValue))''')
write("backend/src/Application/Services/CompraService.cs", text)

# 12) Carga masiva: Producto importa familia + técnica; variantes no recalculan Producto.
text = read("backend/src/Infrastructure/Services/CargaMasivaService.cs")
# Validación de productos carga variantes para detectar entidad por dimensiones, no campos legacy.
text = text.replace('''        var productos = await _db.Productos.AsNoTracking().ToListAsync(ct);''', '''        var productos = await _db.Productos.AsNoTracking()
            .Include(x => x.Variantes.Where(v => !v.Eliminado))
            .ToListAsync(ct);''', 1)
text = text.replace('''            fila.Accion = productos.Any(x => ClaveProducto(x.Nombre, x.Marca, x.Modelo) == clave) ? "Actualizar" : "Crear";''', '''            fila.Accion = productos.Any(x =>
                NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Nombre")) &&
                x.Variantes.Any(v => v.MarcaId == marca?.Id && v.ModeloId == modelo?.Id)) ? "Actualizar" : "Crear";''', 1)
new_aplicar_productos = r'''    private async Task<(int Creados, int Actualizados)> AplicarProductosAsync(List<CargaMasivaFilaDto> filas, CancellationToken ct)
    {
        var marcas = await _db.Marcas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var modelos = await _db.Modelos.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var tallas = await _db.Tallas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);
        var categorias = await _db.Categorias.Where(x => !x.Eliminada).ToListAsync(ct);
        var existentes = await _db.Productos
            .Include(x => x.Variantes.Where(v => !v.Eliminado))
            .ToListAsync(ct);
        var creados = 0;
        var actualizados = 0;

        foreach (var fila in filas)
        {
            var marca = marcas.First(x => NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Marca")));
            var modelo = modelos.First(x => x.MarcaId == marca.Id && NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Modelo")));
            var categoriaNombre = V(fila, "Categoria");
            var tallaNombre = V(fila, "Talla");
            var categoria = string.IsNullOrWhiteSpace(categoriaNombre) ? null : categorias.First(x => x.Activa && NormalizarClave(x.Nombre) == NormalizarClave(categoriaNombre));
            var talla = string.IsNullOrWhiteSpace(tallaNombre) ? null : tallas.First(x => NormalizarClave(x.Nombre) == NormalizarClave(tallaNombre));
            var nombre = V(fila, "Nombre")!;

            var candidatos = existentes.Where(x => NormalizarClave(x.Nombre) == NormalizarClave(nombre)).ToList();
            var producto = candidatos.FirstOrDefault(x => x.Variantes.Any(v => v.MarcaId == marca.Id && v.ModeloId == modelo.Id));
            if (producto is null && candidatos.Count == 1 && candidatos[0].Variantes.Count <= 1)
                producto = candidatos[0];

            if (producto is null)
            {
                producto = new Producto
                {
                    Nombre = nombre,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.Productos.Add(producto);
                existentes.Add(producto);
                creados++;
                await _db.SaveChangesAsync(ct);
            }
            else actualizados++;

            producto.Nombre = nombre;
            producto.CategoriaId = categoria?.Id;
            producto.Descripcion = NuloSiVacio(V(fila, "Descripcion"));
            producto.Activo = Booleano(fila, "Activo");
            producto.Eliminado = false;
            producto.FechaEliminacion = null;
            producto.EliminadoPorUsuarioId = null;
            MarcarActualizacion(producto);

            var tecnica = producto.Variantes.SingleOrDefault(v => v.EsTecnica && !v.Eliminado);
            if (tecnica is null)
            {
                if (producto.Variantes.Any(v => !v.EsTecnica && !v.Eliminado))
                    throw new BusinessRuleException("La importación de Productos no puede modificar economía de una familia con múltiples variantes. Usa VariantesInventario.");
                tecnica = new ProductoVariante
                {
                    ProductoId = producto.Id,
                    Sku = $"TEC-{producto.Id:D10}",
                    EsTecnica = true,
                    Cantidad = 0,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.ProductoVariantes.Add(tecnica);
                producto.Variantes.Add(tecnica);
            }
            tecnica.MarcaId = marca.Id;
            tecnica.ModeloId = modelo.Id;
            tecnica.TallaId = talla?.Id;
            tecnica.ColorId = null;
            tecnica.CodigoBarras = null;
            tecnica.Costo = Decimal(fila, "Costo");
            tecnica.Precio = Decimal(fila, "Precio");
            tecnica.UmbralStockBajo = Entero(fila, "UmbralStockBajo");
            tecnica.Activo = producto.Activo;
            tecnica.Eliminado = false;
            tecnica.FechaEliminacion = null;
            tecnica.EliminadoPorUsuarioId = null;
            MarcarActualizacion(tecnica);
        }
        return (creados, actualizados);
    }
'''
text, n = re.subn(r'''    private async Task<\(int Creados, int Actualizados\)> AplicarProductosAsync\(List<CargaMasivaFilaDto> filas, CancellationToken ct\)\n    \{.*?\n    \}\n\n    private async Task<\(int Creados, int Actualizados\)> AplicarVariantesAsync''', new_aplicar_productos + '\n    private async Task<(int Creados, int Actualizados)> AplicarVariantesAsync', text, count=1, flags=re.S)
if n != 1:
    raise RuntimeError("CargaMasivaService: no se reemplazó AplicarProductosAsync")
text = text.replace('''        var productosAfectados = new HashSet<int>();
''', '')
text = text.replace('''            productosAfectados.Add(producto.Id);
''', '')
text, n = re.subn(r'''\n        foreach \(var producto in productos\.Values\.Where\(x => productosAfectados\.Contains\(x\.Id\)\)\)\n        \{.*?\n        \}\n''', '\n', text, count=1, flags=re.S)
if n != 1:
    raise RuntimeError("CargaMasivaService: no se retiró recálculo de Producto en variantes")
# Al importar una comercial, exige/retira técnica sin stock para evitar mezcla.
needle = '''        var movimientos = new List<(Producto Producto, ProductoVariante Variante, CargaMasivaFilaDto Fila, int Anterior, int Nueva)>();
        var creados = 0;'''
replacement = '''        var tecnicas = await _db.ProductoVariantes
            .Where(v => productoIds.Contains(v.ProductoId) && v.EsTecnica && !v.Eliminado)
            .ToListAsync(ct);
        foreach (var tecnica in tecnicas)
        {
            if (tecnica.Cantidad != 0)
                throw new BusinessRuleException("No se puede convertir a variantes comerciales mientras la variante técnica tenga existencias.");
            tecnica.Activo = false;
            tecnica.Eliminado = true;
            tecnica.FechaEliminacion = DateTime.UtcNow;
            tecnica.EliminadoPorUsuarioId = _currentUser.UsuarioId;
            MarcarActualizacion(tecnica);
        }

        var movimientos = new List<(Producto Producto, ProductoVariante Variante, CargaMasivaFilaDto Fila, int Anterior, int Nueva)>();
        var creados = 0;'''
if needle not in text:
    raise RuntimeError("CargaMasivaService: punto de inserción tecnicas no encontrado")
text = text.replace(needle, replacement, 1)
write("backend/src/Infrastructure/Services/CargaMasivaService.cs", text)

# 13) Migración N0.3: backfill, normalización y constraints fuertes adicionales.
write("backend/src/Infrastructure/Migrations/20260811032000_N0_3_ConsolidarProductoVariante.cs", r'''using System;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260811032000_N0_3_ConsolidarProductoVariante")]
public sealed class N0_3_ConsolidarProductoVariante : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __PreflightN03;
            CREATE TEMPORARY TABLE __PreflightN03
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones INT NOT NULL,
                CONSTRAINT CK_PreflightN03_Cero CHECK (Violaciones = 0)
            );
            INSERT INTO __PreflightN03 (Id, Violaciones)
            SELECT 1,
                (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND (Cantidad < 0 OR UmbralStockBajo < 0 OR (Costo IS NOT NULL AND Costo < 0) OR (Precio IS NOT NULL AND Precio < 0)))
              + (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND ModeloId IS NOT NULL AND MarcaId IS NULL)
              + (SELECT COUNT(*) FROM ProductoVariantes pv JOIN Modelos m ON m.Id = pv.ModeloId WHERE pv.Eliminado = 0 AND pv.ModeloId IS NOT NULL AND pv.MarcaId <> m.MarcaId)
              + (SELECT COUNT(*) FROM ProductoImagenes pi JOIN ProductoVariantes pv ON pv.Id = pi.ProductoVarianteId WHERE pi.ProductoVarianteId IS NOT NULL AND pi.ProductoId <> pv.ProductoId)
              + (SELECT COUNT(*) FROM (SELECT UPPER(TRIM(Sku)) k FROM ProductoVariantes WHERE Sku IS NOT NULL AND TRIM(Sku) <> '' GROUP BY UPPER(TRIM(Sku)) HAVING COUNT(*) > 1) x)
              + (SELECT COUNT(*) FROM (SELECT TRIM(CodigoBarras) k FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) <> '' GROUP BY TRIM(CodigoBarras) HAVING COUNT(*) > 1) x)
              + (SELECT COUNT(*) FROM (SELECT ProductoId FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId HAVING SUM(EsTecnica = 1) > 0 AND SUM(EsTecnica = 0) > 0) x)
              + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND p.ModeloId IS NOT NULL AND (p.MarcaId IS NULL OR NOT EXISTS (SELECT 1 FROM Modelos m WHERE m.Id = p.ModeloId AND m.MarcaId = p.MarcaId)))
              + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0) AND EXISTS (SELECT 1 FROM ProductoVariantes z WHERE UPPER(TRIM(z.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))));
            DROP TEMPORARY TABLE __PreflightN03;
            """);

        migrationBuilder.Sql("""
            UPDATE ProductoVariantes
               SET Sku = UPPER(TRIM(Sku)),
                   CodigoBarras = NULLIF(TRIM(CodigoBarras), '')
             WHERE Sku IS NOT NULL OR CodigoBarras IS NOT NULL;

            INSERT INTO ProductoVariantes
                (ProductoId, MarcaId, ModeloId, ColorId, TallaId, Sku, CodigoBarras,
                 Cantidad, UmbralStockBajo, Costo, Precio, EsTecnica, Activo, Eliminado,
                 FechaCreacion, FechaActualizacion, CreadoPorNombreUsuario)
            SELECT p.Id, p.MarcaId, p.ModeloId, p.ColorId, p.TallaId,
                   CONCAT('TEC-', LPAD(p.Id, 10, '0')), NULL,
                   p.Cantidad, p.UmbralStockBajo, p.Costo, p.Precio, 1, p.Activo, 0,
                   UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 'ERP-N0.3 backfill'
              FROM Productos p
             WHERE p.Eliminado = 0
               AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0);

            UPDATE ProductoVariantes pv
            JOIN Productos p ON p.Id = pv.ProductoId
               SET pv.MarcaId = COALESCE(pv.MarcaId, p.MarcaId),
                   pv.ModeloId = COALESCE(pv.ModeloId, p.ModeloId),
                   pv.ColorId = COALESCE(pv.ColorId, p.ColorId),
                   pv.TallaId = COALESCE(pv.TallaId, p.TallaId),
                   pv.Costo = COALESCE(pv.Costo, p.Costo),
                   pv.Precio = COALESCE(pv.Precio, p.Precio),
                   pv.FechaActualizacion = UTC_TIMESTAMP(6)
             WHERE pv.Eliminado = 0 AND pv.EsTecnica = 1;
            """);

        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Sku CHECK (Sku IS NOT NULL AND TRIM(Sku) <> '');");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Barcode CHECK (CodigoBarras IS NULL OR TRIM(CodigoBarras) <> '');");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Stock CHECK (Cantidad >= 0 AND UmbralStockBajo >= 0);");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_Importes CHECK ((Costo IS NULL OR Costo >= 0) AND (Precio IS NULL OR Precio >= 0));");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_ModeloMarca CHECK (ModeloId IS NULL OR MarcaId IS NOT NULL);");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT CK_ProductoVariantes_N03_TecnicaBarcode CHECK (EsTecnica = 0 OR CodigoBarras IS NULL);");

        migrationBuilder.Sql("CREATE UNIQUE INDEX UX_Modelos_Id_MarcaId_N03 ON Modelos (Id, MarcaId);");
        migrationBuilder.Sql("ALTER TABLE ProductoVariantes ADD CONSTRAINT FK_ProductoVariantes_Modelos_ModeloMarca_N03 FOREIGN KEY (ModeloId, MarcaId) REFERENCES Modelos (Id, MarcaId) ON DELETE RESTRICT;");
        migrationBuilder.Sql("CREATE UNIQUE INDEX UX_ProductoVariantes_Id_ProductoId_N03 ON ProductoVariantes (Id, ProductoId);");
        migrationBuilder.Sql("ALTER TABLE ProductoImagenes ADD CONSTRAINT FK_ProductoImagenes_VarianteProducto_N03 FOREIGN KEY (ProductoVarianteId, ProductoId) REFERENCES ProductoVariantes (Id, ProductoId) ON DELETE RESTRICT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("ERP-N0.3 es forward-only. Para revertir constraints o backfill debe restaurarse el respaldo/preflight anterior a N0.3.");
}
''')

# 14) SQL de preflight y postdeploy independientes.
write("backend/scripts/preflight-erp-n0-3-producto-variante.sql", r'''SET @violaciones :=
    (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND (Cantidad < 0 OR UmbralStockBajo < 0 OR (Costo IS NOT NULL AND Costo < 0) OR (Precio IS NOT NULL AND Precio < 0)))
  + (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND ModeloId IS NOT NULL AND MarcaId IS NULL)
  + (SELECT COUNT(*) FROM ProductoVariantes pv JOIN Modelos m ON m.Id = pv.ModeloId WHERE pv.Eliminado = 0 AND pv.ModeloId IS NOT NULL AND pv.MarcaId <> m.MarcaId)
  + (SELECT COUNT(*) FROM ProductoImagenes pi JOIN ProductoVariantes pv ON pv.Id = pi.ProductoVarianteId WHERE pi.ProductoVarianteId IS NOT NULL AND pi.ProductoId <> pv.ProductoId)
  + (SELECT COUNT(*) FROM (SELECT UPPER(TRIM(Sku)) k FROM ProductoVariantes WHERE Sku IS NOT NULL AND TRIM(Sku) <> '' GROUP BY UPPER(TRIM(Sku)) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM (SELECT TRIM(CodigoBarras) k FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) <> '' GROUP BY TRIM(CodigoBarras) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM (SELECT ProductoId FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId HAVING SUM(EsTecnica = 1) > 0 AND SUM(EsTecnica = 0) > 0) x)
  + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND p.ModeloId IS NOT NULL AND (p.MarcaId IS NULL OR NOT EXISTS (SELECT 1 FROM Modelos m WHERE m.Id = p.ModeloId AND m.MarcaId = p.MarcaId)))
  + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0) AND EXISTS (SELECT 1 FROM ProductoVariantes z WHERE UPPER(TRIM(z.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))));
SELECT @violaciones AS BloqueosN03;
''')
write("backend/scripts/postdeploy-erp-n0-3-producto-variante.sql", r'''SET @errores :=
    (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0))
  + (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND (Sku IS NULL OR TRIM(Sku) = '' OR Cantidad < 0 OR UmbralStockBajo < 0 OR (Costo IS NOT NULL AND Costo < 0) OR (Precio IS NOT NULL AND Precio < 0)))
  + (SELECT COUNT(*) FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) = '')
  + (SELECT COUNT(*) FROM ProductoVariantes pv JOIN Modelos m ON m.Id = pv.ModeloId WHERE pv.ModeloId IS NOT NULL AND (pv.MarcaId IS NULL OR pv.MarcaId <> m.MarcaId))
  + (SELECT COUNT(*) FROM ProductoImagenes pi JOIN ProductoVariantes pv ON pv.Id = pi.ProductoVarianteId WHERE pi.ProductoVarianteId IS NOT NULL AND pi.ProductoId <> pv.ProductoId)
  + (SELECT COUNT(*) FROM (SELECT UPPER(TRIM(Sku)) k FROM ProductoVariantes GROUP BY UPPER(TRIM(Sku)) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM (SELECT TRIM(CodigoBarras) k FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL GROUP BY TRIM(CodigoBarras) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_schema = DATABASE() AND table_name = 'ProductoVariantes' AND constraint_name IN ('CK_ProductoVariantes_N03_Sku','CK_ProductoVariantes_N03_Barcode','CK_ProductoVariantes_N03_Stock','CK_ProductoVariantes_N03_Importes','CK_ProductoVariantes_N03_ModeloMarca','CK_ProductoVariantes_N03_TecnicaBarcode') HAVING COUNT(*) <> 6)
  + (SELECT IF(COUNT(*) = 2, 0, 1) FROM information_schema.referential_constraints WHERE constraint_schema = DATABASE() AND constraint_name IN ('FK_ProductoVariantes_Modelos_ModeloMarca_N03','FK_ProductoImagenes_VarianteProducto_N03'));
SELECT @errores AS ErroresN03;
''')

# 15) Prueba de mapper: los campos legacy deliberadamente contradictorios no influyen.
write("backend/tests/InventoryApp.Tests/ProductoVarianteAuthorityTests.cs", r'''using InventoryApp.Application.Mappings;
using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public sealed class ProductoVarianteAuthorityTests
{
    [Fact]
    public void ProductoMapper_UsaExclusivamenteVarianteParaInventarioEconomiaYDimensiones()
    {
        var marca = new Marca { Id = 11, Nombre = "Marca Variante" };
        var modelo = new Modelo { Id = 12, MarcaId = 11, Nombre = "Modelo Variante" };
        var color = new Color { Id = 13, Nombre = "Negro" };
        var talla = new Talla { Id = 14, Nombre = "M" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Producto",
            Marca = "LEGACY INCORRECTA",
            Modelo = "LEGACY INCORRECTO",
            MarcaId = 91,
            ModeloId = 92,
            ColorId = 93,
            TallaId = 94,
            Cantidad = 999,
            Costo = 999m,
            Precio = 999m,
            UmbralStockBajo = 999,
            Activo = true
        };
        producto.Variantes.Add(new ProductoVariante
        {
            Id = 2,
            ProductoId = 1,
            MarcaId = 11,
            Marca = marca,
            ModeloId = 12,
            Modelo = modelo,
            ColorId = 13,
            Color = color,
            TallaId = 14,
            Talla = talla,
            Sku = "SKU-N03",
            CodigoBarras = "123456789",
            Cantidad = 7,
            Costo = 10m,
            Precio = 20m,
            UmbralStockBajo = 2,
            Activo = true
        });

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(7, dto.Cantidad);
        Assert.Equal(10m, dto.Costo);
        Assert.Equal(20m, dto.Precio);
        Assert.Equal(2, dto.UmbralStockBajo);
        Assert.Equal(11, dto.MarcaId);
        Assert.Equal(12, dto.ModeloId);
        Assert.Equal(13, dto.ColorId);
        Assert.Equal(14, dto.TallaId);
        Assert.Equal("Marca Variante", dto.Marca);
        Assert.Equal("Modelo Variante", dto.Modelo);
    }
}
''')

# 16) Guardia estática permanente.
write("backend/scripts/check-erp-n0-3-runtime.py", r'''#!/usr/bin/env python3
from pathlib import Path
import re, sys
root = Path(__file__).resolve().parents[2]
checks = {
    "backend/src/Application/Mappings/ProductoMapper.cs": [r"\bp\.(Cantidad|Costo|Precio|UmbralStockBajo|MarcaId|ModeloId|ColorId|TallaId|MarcaCatalogo|ModeloCatalogo|ColorCatalogo|TallaCatalogo)\b"],
    "backend/src/Infrastructure/Repositories/ProductoRepository.cs": [r"\bp\.(Cantidad|Costo|Precio|MarcaCatalogo|ModeloCatalogo|ColorCatalogo|TallaCatalogo)\b", r"v\.Producto\.(Costo|Precio)\b"],
    "backend/src/Application/Services/ProductoEscanerService.cs": [r"variante\.Producto\.(Cantidad|Costo|Precio|Marca|Modelo)\b"],
    "backend/src/Application/Services/ProductoVarianteService.cs": [r"producto\.(Cantidad|Costo|Precio|UmbralStockBajo|MarcaId|ModeloId|ColorId|TallaId)\s*="],
    "backend/src/Infrastructure/Services/InventarioConcurrencyService.cs": [r"producto\.Cantidad\b"],
    "backend/src/Application/Services/VentaService.cs": [r"producto\.(Cantidad|Costo)\b"],
    "backend/src/Application/Services/CompraService.cs": [r"producto\.(Cantidad|Costo)\b"],
    "backend/src/Infrastructure/Services/CargaMasivaService.cs": [r"producto\.(Cantidad|Costo|Precio|UmbralStockBajo|MarcaId|ModeloId|ColorId|TallaId)\s*="],
}
errors=[]
for rel, patterns in checks.items():
    text=(root/rel).read_text(encoding='utf-8')
    for pattern in patterns:
        for m in re.finditer(pattern,text):
            line=text.count('\n',0,m.start())+1
            errors.append(f"{rel}:{line}: {m.group(0)}")
if errors:
    print("N0.3 FAIL: dependencias operativas legacy encontradas:\n"+"\n".join(errors), file=sys.stderr)
    sys.exit(1)
print("N0.3 runtime guard: ProductoVariante es autoridad operativa en rutas críticas.")
''')

# 17) Workflow permanente de certificación N0.3.
write(".github/workflows/erp-n0-3-ci.yml", r'''name: ERP-N0.3 - Certificación ProductoVariante autoridad única

on:
  push:
    branches: [Desarrollo]
  pull_request:
    branches: [main]
    paths:
      - 'backend/src/**'
      - 'backend/tests/**'
      - 'backend/scripts/*erp-n0-3*'
      - '.github/workflows/erp-n0-3-ci.yml'
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: erp-n0-3-ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  autoridad-variante:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    services:
      mysql:
        image: mysql:8.4
        env:
          MYSQL_ROOT_PASSWORD: root
          MYSQL_DATABASE: inventoryapp_n03_ci
        ports: ['3306:3306']
        options: >-
          --health-cmd="mysqladmin ping -h localhost -proot"
          --health-interval=10s --health-timeout=5s --health-retries=12
    env:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: Server=127.0.0.1;Port=3306;Database=inventoryapp_n03_ci;User=root;Password=root;SslMode=None;AllowPublicKeyRetrieval=True;
      Database__ServerVersion: 8.4.0
      Database__ApplyMigrationsOnStartup: 'false'
      Jwt__Secret: ERP-N0-3-CI-Only-Secret-With-More-Than-32-Characters-2026
      Jwt__Issuer: VariApp.CI
      Jwt__Audience: VariApp.CI.Frontend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - name: Guardas runtime N0.3
        run: python3 backend/scripts/check-erp-n0-3-runtime.py
      - name: Restaurar, compilar y probar backend
        working-directory: backend
        run: |
          dotnet restore InventoryApp.sln
          dotnet build InventoryApp.sln -c Release --no-restore -warnaserror
          dotnet test InventoryApp.sln -c Release --no-build
      - name: Instalar EF
        run: dotnet tool install --global dotnet-ef --version 8.0.8
      - name: Crear esquema N0.2
        working-directory: backend
        run: |
          dotnet ef database update 20260811013917_N0_2_RetirarCatalogoProductoLegacy \
            --project src/Infrastructure/InventoryApp.Infrastructure.csproj \
            --startup-project src/API/InventoryApp.API.csproj --context AppDbContext
      - name: Sembrar caso representativo pre-N0.3
        shell: bash
        run: |
          set -euo pipefail
          c="${{ job.services.mysql.id }}"
          docker exec "$c" mysql -uroot -proot inventoryapp_n03_ci -e "
            INSERT INTO Marcas (Id,Nombre,Orden,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES (9301,'N03 Marca',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
            INSERT INTO Modelos (Id,MarcaId,Nombre,Orden,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES (9302,9301,'N03 Modelo',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
            INSERT INTO Colores (Id,Nombre,CodigoVisual,Orden,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES (9303,'N03 Color','#112233',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
            INSERT INTO Tallas (Id,Nombre,Orden,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES (9304,'N03 Talla',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
            INSERT INTO Productos (Id,Nombre,Marca,Modelo,Cantidad,Costo,Precio,UmbralStockBajo,MarcaId,ModeloId,ColorId,TallaId,Activo,Eliminado,FechaCreacion,FechaActualizacion)
              VALUES (9300,'N03 Producto','legacy marca','legacy modelo',7,10.00,20.00,2,9301,9302,9303,9304,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
            INSERT INTO ProductoVariantes (Id,ProductoId,Sku,Cantidad,UmbralStockBajo,Costo,Precio,EsTecnica,Activo,Eliminado,FechaCreacion,FechaActualizacion)
              VALUES (9350,9300,'  tec-n03-9300  ',7,2,10.00,20.00,1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"
      - name: Preflight N0.3
        shell: bash
        run: |
          c="${{ job.services.mysql.id }}"
          test "$(docker exec -i "$c" mysql -uroot -proot inventoryapp_n03_ci --batch --skip-column-names < backend/scripts/preflight-erp-n0-3-producto-variante.sql | tail -n 1)" = '0'
      - name: Aplicar N0.3
        working-directory: backend
        run: |
          dotnet ef database update --project src/Infrastructure/InventoryApp.Infrastructure.csproj \
            --startup-project src/API/InventoryApp.API.csproj --context AppDbContext
      - name: Postcheck N0.3 y datos preservados
        shell: bash
        run: |
          set -euo pipefail
          c="${{ job.services.mysql.id }}"
          test "$(docker exec -i "$c" mysql -uroot -proot inventoryapp_n03_ci --batch --skip-column-names < backend/scripts/postdeploy-erp-n0-3-producto-variante.sql | tail -n 1)" = '0'
          test "$(docker exec "$c" mysql -uroot -proot inventoryapp_n03_ci -Nse "SELECT COUNT(*) FROM ProductoVariantes WHERE Id=9350 AND Sku='TEC-N03-9300' AND MarcaId=9301 AND ModeloId=9302 AND ColorId=9303 AND TallaId=9304 AND Cantidad=7 AND Costo=10.00 AND Precio=20.00 AND UmbralStockBajo=2;")" = '1'
          if docker exec "$c" mysql -uroot -proot inventoryapp_n03_ci -e "UPDATE ProductoVariantes SET Sku=' ' WHERE Id=9350;"; then echo 'CHECK SKU vacío no bloqueó' >&2; exit 1; fi
          if docker exec "$c" mysql -uroot -proot inventoryapp_n03_ci -e "UPDATE ProductoVariantes SET ModeloId=9302, MarcaId=NULL WHERE Id=9350;"; then echo 'CHECK Modelo/Marca no bloqueó' >&2; exit 1; fi
      - name: Snapshot EF consistente
        working-directory: backend
        run: |
          dotnet ef migrations has-pending-model-changes --project src/Infrastructure/InventoryApp.Infrastructure.csproj \
            --startup-project src/API/InventoryApp.API.csproj --context AppDbContext
''')

print("ERP-N0.3: parches runtime, migración, pruebas y gate generados correctamente.")
