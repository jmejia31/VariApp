using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

/// <summary>
/// Persistencia de los mantenimientos Marca/Modelo/Color/Talla sobre tablas
/// normalizadas. CatalogosProducto se conserva temporalmente como espejo de
/// compatibilidad porque Productos y ProductoVariantes todavía referencian sus
/// IDs; M2 retirará esa dependencia.
/// </summary>
public class CatalogoProductoRepository : ICatalogoProductoRepository
{
    private readonly AppDbContext _context;

    public CatalogoProductoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogoProducto>> GetAllAsync(
        TipoCatalogoProducto tipo,
        string? buscar = null,
        int? catalogoPadreId = null)
    {
        var resultado = await ConsultarNormalizadosAsync(tipo, buscar, catalogoPadreId, soloActivos: false);
        await CargarUsosProductoAsync(tipo, resultado);
        return resultado;
    }

    public async Task<List<CatalogoProducto>> GetActivosAsync(
        TipoCatalogoProducto tipo,
        int? catalogoPadreId = null)
    {
        var resultado = await ConsultarNormalizadosAsync(tipo, null, catalogoPadreId, soloActivos: true);
        await CargarUsosProductoAsync(tipo, resultado);
        return resultado;
    }

    public async Task<CatalogoProducto?> GetByIdAsync(int id)
    {
        var marca = await _context.Marcas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (marca is not null) return MapMarca(marca, incluirModelos: false);

        var modelo = await _context.Modelos.AsNoTracking().Include(x => x.Marca).FirstOrDefaultAsync(x => x.Id == id);
        if (modelo is not null) return MapModelo(modelo);

        var color = await _context.Colores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (color is not null) return MapColor(color);

        var talla = await _context.Tallas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return talla is null ? null : MapTalla(talla);
    }

    public async Task<CatalogoProducto?> GetByIdConRelacionesAsync(int id)
    {
        CatalogoProducto? resultado;
        TipoCatalogoProducto tipo;

        var marca = await _context.Marcas.AsNoTracking().Include(x => x.Modelos).FirstOrDefaultAsync(x => x.Id == id);
        if (marca is not null)
        {
            resultado = MapMarca(marca, incluirModelos: true);
            tipo = TipoCatalogoProducto.Marca;
        }
        else
        {
            var modelo = await _context.Modelos.AsNoTracking().Include(x => x.Marca).FirstOrDefaultAsync(x => x.Id == id);
            if (modelo is not null)
            {
                resultado = MapModelo(modelo);
                tipo = TipoCatalogoProducto.Modelo;
            }
            else
            {
                var color = await _context.Colores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (color is not null)
                {
                    resultado = MapColor(color);
                    tipo = TipoCatalogoProducto.Color;
                }
                else
                {
                    var talla = await _context.Tallas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (talla is null) return null;
                    resultado = MapTalla(talla);
                    tipo = TipoCatalogoProducto.Talla;
                }
            }
        }

        await CargarUsosProductoAsync(tipo, new List<CatalogoProducto> { resultado });
        return resultado;
    }

    public Task<bool> ExisteNombreAsync(
        TipoCatalogoProducto tipo,
        string nombre,
        int? catalogoPadreId,
        int? excluirId = null)
    {
        var normalizado = nombre.Trim().ToLower();
        return tipo switch
        {
            TipoCatalogoProducto.Marca => _context.Marcas.AnyAsync(x =>
                x.Nombre.ToLower() == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value)),
            TipoCatalogoProducto.Modelo => _context.Modelos.AnyAsync(x =>
                x.MarcaId == catalogoPadreId && x.Nombre.ToLower() == normalizado &&
                (!excluirId.HasValue || x.Id != excluirId.Value)),
            TipoCatalogoProducto.Color => _context.Colores.AnyAsync(x =>
                x.Nombre.ToLower() == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value)),
            TipoCatalogoProducto.Talla => _context.Tallas.AnyAsync(x =>
                x.Nombre.ToLower() == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value)),
            _ => Task.FromResult(false)
        };
    }

    public async Task AddAsync(CatalogoProducto catalogo)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            // Registro global temporal: conserva IDs compatibles con las FKs
            // legacy de Producto/ProductoVariante hasta que M2 las reoriente.
            var espejo = CrearEspejoLegacy(catalogo);
            await _context.CatalogosProducto.AddAsync(espejo);
            await _context.SaveChangesAsync();

            catalogo.Id = espejo.Id;
            await AgregarNormalizadoAsync(catalogo);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public void Update(CatalogoProducto catalogo)
    {
        _context.CatalogosProducto.Update(CrearEspejoLegacy(catalogo));

        switch (catalogo.Tipo)
        {
            case TipoCatalogoProducto.Marca:
                _context.Marcas.Update(CrearMarca(catalogo));
                break;
            case TipoCatalogoProducto.Modelo:
                _context.Modelos.Update(CrearModelo(catalogo));
                break;
            case TipoCatalogoProducto.Color:
                _context.Colores.Update(CrearColor(catalogo));
                break;
            case TipoCatalogoProducto.Talla:
                _context.Tallas.Update(CrearTalla(catalogo));
                break;
            default:
                throw new InvalidOperationException($"Tipo de catálogo no soportado: {catalogo.Tipo}.");
        }
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private async Task<List<CatalogoProducto>> ConsultarNormalizadosAsync(
        TipoCatalogoProducto tipo,
        string? buscar,
        int? catalogoPadreId,
        bool soloActivos)
    {
        var termino = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        switch (tipo)
        {
            case TipoCatalogoProducto.Marca:
            {
                var query = _context.Marcas.AsNoTracking().Include(x => x.Modelos).AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(x => MapMarca(x, incluirModelos: true)).ToList();
            }
            case TipoCatalogoProducto.Modelo:
            {
                var query = _context.Modelos.AsNoTracking().Include(x => x.Marca).AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (catalogoPadreId.HasValue) query = query.Where(x => x.MarcaId == catalogoPadreId.Value);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(MapModelo).ToList();
            }
            case TipoCatalogoProducto.Color:
            {
                var query = _context.Colores.AsNoTracking().AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(MapColor).ToList();
            }
            case TipoCatalogoProducto.Talla:
            {
                var query = _context.Tallas.AsNoTracking().AsQueryable();
                if (soloActivos) query = query.Where(x => x.Activo);
                if (termino is not null)
                    query = query.Where(x => x.Nombre.Contains(termino) || (x.Descripcion != null && x.Descripcion.Contains(termino)));
                var rows = await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync();
                return rows.Select(MapTalla).ToList();
            }
            default:
                return new List<CatalogoProducto>();
        }
    }

    private async Task CargarUsosProductoAsync(TipoCatalogoProducto tipo, List<CatalogoProducto> catalogos)
    {
        if (catalogos.Count == 0) return;
        var ids = catalogos.Select(x => x.Id).ToHashSet();

        List<Producto> productos = tipo switch
        {
            TipoCatalogoProducto.Marca => await _context.Productos.AsNoTracking().Where(p => p.MarcaId.HasValue && ids.Contains(p.MarcaId.Value)).ToListAsync(),
            TipoCatalogoProducto.Modelo => await _context.Productos.AsNoTracking().Where(p => p.ModeloId.HasValue && ids.Contains(p.ModeloId.Value)).ToListAsync(),
            TipoCatalogoProducto.Color => await _context.Productos.AsNoTracking().Where(p => p.ColorId.HasValue && ids.Contains(p.ColorId.Value)).ToListAsync(),
            TipoCatalogoProducto.Talla => await _context.Productos.AsNoTracking().Where(p => p.TallaId.HasValue && ids.Contains(p.TallaId.Value)).ToListAsync(),
            _ => new List<Producto>()
        };

        foreach (var catalogo in catalogos)
        {
            switch (tipo)
            {
                case TipoCatalogoProducto.Marca:
                    catalogo.ProductosComoMarca = productos.Where(p => p.MarcaId == catalogo.Id).ToList();
                    break;
                case TipoCatalogoProducto.Modelo:
                    catalogo.ProductosComoModelo = productos.Where(p => p.ModeloId == catalogo.Id).ToList();
                    break;
                case TipoCatalogoProducto.Color:
                    catalogo.ProductosComoColor = productos.Where(p => p.ColorId == catalogo.Id).ToList();
                    break;
                case TipoCatalogoProducto.Talla:
                    catalogo.ProductosComoTalla = productos.Where(p => p.TallaId == catalogo.Id).ToList();
                    break;
            }
        }
    }

    private async Task AgregarNormalizadoAsync(CatalogoProducto catalogo)
    {
        switch (catalogo.Tipo)
        {
            case TipoCatalogoProducto.Marca:
                await _context.Marcas.AddAsync(CrearMarca(catalogo));
                break;
            case TipoCatalogoProducto.Modelo:
                await _context.Modelos.AddAsync(CrearModelo(catalogo));
                break;
            case TipoCatalogoProducto.Color:
                await _context.Colores.AddAsync(CrearColor(catalogo));
                break;
            case TipoCatalogoProducto.Talla:
                await _context.Tallas.AddAsync(CrearTalla(catalogo));
                break;
            default:
                throw new InvalidOperationException($"Tipo de catálogo no soportado: {catalogo.Tipo}.");
        }
    }

    private static CatalogoProducto CrearEspejoLegacy(CatalogoProducto x) => new()
    {
        Id = x.Id,
        Tipo = x.Tipo,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        CodigoVisual = x.CodigoVisual,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CatalogoPadreId = x.CatalogoPadreId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Marca CrearMarca(CatalogoProducto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Modelo CrearModelo(CatalogoProducto x) => new()
    {
        Id = x.Id,
        MarcaId = x.CatalogoPadreId ?? throw new InvalidOperationException("Todo modelo normalizado requiere MarcaId."),
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Color CrearColor(CatalogoProducto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        CodigoVisual = x.CodigoVisual,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static Talla CrearTalla(CatalogoProducto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static CatalogoProducto MapMarca(Marca x, bool incluirModelos) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Marca,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion,
        ElementosHijos = incluirModelos
            ? x.Modelos.Select(MapModeloSinMarca).ToList()
            : new List<CatalogoProducto>()
    };

    private static CatalogoProducto MapModelo(Modelo x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Modelo,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CatalogoPadreId = x.MarcaId,
        CatalogoPadre = x.Marca is null ? null : MapMarca(x.Marca, incluirModelos: false),
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static CatalogoProducto MapModeloSinMarca(Modelo x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Modelo,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        CatalogoPadreId = x.MarcaId,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static CatalogoProducto MapColor(Color x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Color,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        CodigoVisual = x.CodigoVisual,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };

    private static CatalogoProducto MapTalla(Talla x) => new()
    {
        Id = x.Id,
        Tipo = TipoCatalogoProducto.Talla,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Orden = x.Orden,
        Activo = x.Activo,
        Eliminado = x.Eliminado,
        FechaEliminacion = x.FechaEliminacion,
        EliminadoPorUsuarioId = x.EliminadoPorUsuarioId,
        CreadoPorUsuarioId = x.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = x.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = x.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = x.ActualizadoPorNombreUsuario,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };
}
