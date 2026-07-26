using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class CatalogoProductoService : ICatalogoProductoService
{
    private readonly ICatalogoProductoRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public CatalogoProductoService(
        ICatalogoProductoRepository repository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<List<CatalogoProductoDto>> GetAllAsync(
        TipoCatalogoProducto tipo,
        string? buscar = null,
        int? catalogoPadreId = null)
    {
        if (tipo != TipoCatalogoProducto.Modelo && catalogoPadreId.HasValue)
            throw new BusinessRuleException("Solo los modelos pueden filtrarse por marca.");

        var elementos = await _repository.GetAllAsync(tipo, buscar, catalogoPadreId);
        return elementos.Select(ToDto).ToList();
    }

    public async Task<List<CatalogoProductoDto>> GetActivosAsync(
        TipoCatalogoProducto tipo,
        int? catalogoPadreId = null)
    {
        ValidarPadrePorTipo(tipo, catalogoPadreId);
        var elementos = await _repository.GetActivosAsync(tipo, catalogoPadreId);
        return elementos.Select(ToDto).ToList();
    }

    public async Task<CatalogoProductoDto?> GetByIdAsync(TipoCatalogoProducto tipo, int id)
    {
        var elemento = await _repository.GetByIdConRelacionesAsync(id);
        return elemento is null || elemento.Tipo != tipo ? null : ToDto(elemento);
    }

    public async Task<CatalogoProductoDto> CreateAsync(
        TipoCatalogoProducto tipo,
        CreateCatalogoProductoDto dto)
    {
        var nombre = ValidarNombre(dto.Nombre);
        var padre = await ValidarYObtenerPadreAsync(tipo, dto.CatalogoPadreId);
        var padreId = padre?.Id;

        if (await _repository.ExisteNombreAsync(tipo, nombre, padreId))
            throw new BusinessRuleException($"Ya existe {Articulo(tipo)} {NombreTipo(tipo).ToLower()} con el nombre '{nombre}'.");

        var elemento = new CatalogoProducto
        {
            Tipo = tipo,
            Nombre = nombre,
            Descripcion = NormalizarOpcional(dto.Descripcion),
            CodigoVisual = ValidarCodigoVisual(tipo, dto.CodigoVisual),
            Orden = Math.Max(dto.Orden, 0),
            Activo = true,
            Eliminado = false,
            CatalogoPadreId = padreId,
            CatalogoPadre = padre,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(elemento);
        await _repository.SaveChangesAsync();
        await RegistrarAuditoriaAsync(tipo, AccionPermiso.Crear, $"{NombreTipo(tipo)} creado: {elemento.Nombre}", elemento.Id);

        return ToDto(elemento);
    }

    public async Task<CatalogoProductoDto?> UpdateAsync(
        TipoCatalogoProducto tipo,
        int id,
        UpdateCatalogoProductoDto dto)
    {
        var elemento = await _repository.GetByIdAsync(id);
        if (elemento is null || elemento.Tipo != tipo) return null;

        var nombre = ValidarNombre(dto.Nombre);
        var padre = await ValidarYObtenerPadreAsync(tipo, dto.CatalogoPadreId);
        var padreId = padre?.Id;

        if (await _repository.ExisteNombreAsync(tipo, nombre, padreId, id))
            throw new BusinessRuleException($"Ya existe {Articulo(tipo)} {NombreTipo(tipo).ToLower()} con el nombre '{nombre}'.");

        elemento.Nombre = nombre;
        elemento.Descripcion = NormalizarOpcional(dto.Descripcion);
        elemento.CodigoVisual = ValidarCodigoVisual(tipo, dto.CodigoVisual);
        elemento.Orden = Math.Max(dto.Orden, 0);
        elemento.CatalogoPadreId = padreId;
        elemento.CatalogoPadre = padre;
        elemento.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        elemento.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        elemento.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(elemento);
        await _repository.SaveChangesAsync();
        await RegistrarAuditoriaAsync(tipo, AccionPermiso.Editar, $"{NombreTipo(tipo)} actualizado: {elemento.Nombre}", elemento.Id);

        return ToDto(elemento);
    }

    public async Task<CatalogoProductoDto?> CambiarEstadoAsync(
        TipoCatalogoProducto tipo,
        int id,
        bool activo)
    {
        var elemento = await _repository.GetByIdConRelacionesAsync(id);
        if (elemento is null || elemento.Tipo != tipo) return null;

        if (activo && tipo == TipoCatalogoProducto.Modelo && elemento.CatalogoPadre is { Activo: false })
            throw new BusinessRuleException("No se puede activar un modelo cuya marca está inactiva.");

        if (!activo && tipo == TipoCatalogoProducto.Marca && elemento.ElementosHijos.Any(h => h.Activo))
            throw new BusinessRuleException("Desactiva primero los modelos activos asociados a la marca.");

        if (elemento.Activo == activo) return ToDto(elemento);

        elemento.Activo = activo;
        elemento.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        elemento.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        elemento.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(elemento);
        await _repository.SaveChangesAsync();
        await RegistrarAuditoriaAsync(
            tipo,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"{NombreTipo(tipo)} {(activo ? "activado" : "desactivado")}: {elemento.Nombre}",
            elemento.Id);

        return ToDto(elemento);
    }

    public async Task<bool> DeleteAsync(TipoCatalogoProducto tipo, int id)
    {
        var elemento = await _repository.GetByIdConRelacionesAsync(id);
        if (elemento is null || elemento.Tipo != tipo) return false;

        if (tipo == TipoCatalogoProducto.Marca && elemento.ElementosHijos.Any())
            throw new BusinessRuleException("No se puede eliminar una marca mientras tenga modelos asociados. Elimina primero sus modelos.");

        elemento.Activo = false;
        elemento.Eliminado = true;
        elemento.FechaEliminacion = DateTime.UtcNow;
        elemento.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        elemento.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        elemento.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        elemento.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(elemento);
        var guardado = await _repository.SaveChangesAsync();
        if (guardado)
            await RegistrarAuditoriaAsync(tipo, AccionPermiso.EliminarLogico, $"{NombreTipo(tipo)} eliminado lógicamente: {elemento.Nombre}", elemento.Id);

        return guardado;
    }

    public async Task ValidarSeleccionProductoAsync(int? colorId, int? tallaId, int? marcaId, int? modeloId)
    {
        await ValidarSeleccionAsync(colorId, TipoCatalogoProducto.Color, "color");
        await ValidarSeleccionAsync(tallaId, TipoCatalogoProducto.Talla, "talla");
        var marca = await ValidarSeleccionAsync(marcaId, TipoCatalogoProducto.Marca, "marca");
        var modelo = await ValidarSeleccionAsync(modeloId, TipoCatalogoProducto.Modelo, "modelo");

        if (modelo is not null)
        {
            if (marca is null)
                throw new BusinessRuleException("Selecciona la marca correspondiente al modelo.");
            if (modelo.CatalogoPadreId != marca.Id)
                throw new BusinessRuleException("El modelo seleccionado no pertenece a la marca indicada.");
        }
    }

    private async Task<CatalogoProducto?> ValidarSeleccionAsync(int? id, TipoCatalogoProducto tipo, string etiqueta)
    {
        if (!id.HasValue) return null;
        var elemento = await _repository.GetByIdAsync(id.Value);
        if (elemento is null || elemento.Tipo != tipo)
            throw new BusinessRuleException($"El {etiqueta} seleccionado no existe.");
        if (!elemento.Activo)
            throw new BusinessRuleException($"El {etiqueta} seleccionado está inactivo.");
        return elemento;
    }

    private async Task<CatalogoProducto?> ValidarYObtenerPadreAsync(
        TipoCatalogoProducto tipo,
        int? catalogoPadreId)
    {
        ValidarPadrePorTipo(tipo, catalogoPadreId);
        if (tipo != TipoCatalogoProducto.Modelo) return null;

        var marca = await _repository.GetByIdAsync(catalogoPadreId!.Value);
        if (marca is null || marca.Tipo != TipoCatalogoProducto.Marca)
            throw new BusinessRuleException("La marca seleccionada no existe.");
        if (!marca.Activo)
            throw new BusinessRuleException("La marca seleccionada está inactiva.");
        return marca;
    }

    private static void ValidarPadrePorTipo(TipoCatalogoProducto tipo, int? catalogoPadreId)
    {
        if (tipo == TipoCatalogoProducto.Modelo && !catalogoPadreId.HasValue)
            throw new BusinessRuleException("Todo modelo debe pertenecer a una marca.");
        if (tipo != TipoCatalogoProducto.Modelo && catalogoPadreId.HasValue)
            throw new BusinessRuleException("Solo los modelos pueden relacionarse con una marca.");
    }

    private static string ValidarNombre(string? nombre)
    {
        var normalizado = nombre?.Trim() ?? string.Empty;
        if (normalizado.Length < 2 || normalizado.Length > 120)
            throw new BusinessRuleException("El nombre debe contener entre 2 y 120 caracteres.");
        return normalizado;
    }

    private static string? ValidarCodigoVisual(TipoCatalogoProducto tipo, string? codigo)
    {
        var valor = NormalizarOpcional(codigo);
        if (tipo != TipoCatalogoProducto.Color) return valor;
        if (valor is null) return null;
        if (!System.Text.RegularExpressions.Regex.IsMatch(valor, "^#[0-9A-Fa-f]{6}$"))
            throw new BusinessRuleException("El código visual del color debe tener formato hexadecimal, por ejemplo #1D4ED8.");
        return valor.ToUpperInvariant();
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task RegistrarAuditoriaAsync(
        TipoCatalogoProducto tipo,
        AccionPermiso accion,
        string descripcion,
        int id) =>
        await _auditoria.RegistrarAsync(ObtenerModulo(tipo), accion, descripcion, id, entidad: "CatalogoProducto");

    private static ModuloSistema ObtenerModulo(TipoCatalogoProducto tipo) => tipo switch
    {
        TipoCatalogoProducto.Color => ModuloSistema.Colores,
        TipoCatalogoProducto.Talla => ModuloSistema.Tallas,
        TipoCatalogoProducto.Marca => ModuloSistema.Marcas,
        TipoCatalogoProducto.Modelo => ModuloSistema.Modelos,
        _ => ModuloSistema.Productos
    };

    private static string NombreTipo(TipoCatalogoProducto tipo) => tipo switch
    {
        TipoCatalogoProducto.Color => "Color",
        TipoCatalogoProducto.Talla => "Talla",
        TipoCatalogoProducto.Marca => "Marca",
        TipoCatalogoProducto.Modelo => "Modelo",
        _ => "Elemento"
    };

    private static string Articulo(TipoCatalogoProducto tipo) =>
        tipo is TipoCatalogoProducto.Marca or TipoCatalogoProducto.Talla ? "una" : "un";

    private static CatalogoProductoDto ToDto(CatalogoProducto c) => new()
    {
        Id = c.Id,
        Tipo = c.Tipo.ToString(),
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        CodigoVisual = c.CodigoVisual,
        Orden = c.Orden,
        Activo = c.Activo,
        CatalogoPadreId = c.CatalogoPadreId,
        CatalogoPadreNombre = c.CatalogoPadre?.Nombre,
        TotalProductos = c.Tipo switch
        {
            TipoCatalogoProducto.Color => c.ProductosComoColor.Count,
            TipoCatalogoProducto.Talla => c.ProductosComoTalla.Count,
            TipoCatalogoProducto.Marca => c.ProductosComoMarca.Count,
            TipoCatalogoProducto.Modelo => c.ProductosComoModelo.Count,
            _ => 0
        },
        TotalModelos = c.ElementosHijos.Count,
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = c.ActualizadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion,
        FechaActualizacion = c.FechaActualizacion
    };
}
