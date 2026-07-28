using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class ProductoVarianteService : IProductoVarianteService
{
    private readonly IProductoVarianteRepository _repository;
    private readonly IProductoRepository _productoRepository;
    private readonly ICatalogoProductoService _catalogoService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public ProductoVarianteService(
        IProductoVarianteRepository repository,
        IProductoRepository productoRepository,
        ICatalogoProductoService catalogoService,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _productoRepository = productoRepository;
        _catalogoService = catalogoService;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<List<ProductoVarianteDto>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true)
    {
        await ObtenerProductoAsync(productoId);
        var variantes = await _repository.GetByProductoIdAsync(productoId, incluirInactivas);
        return variantes.Select(ToDto).ToList();
    }

    public async Task<ProductoVarianteDto?> GetByIdAsync(int productoId, int id)
    {
        var variante = await _repository.GetByIdAsync(id);
        return variante is null || variante.ProductoId != productoId ? null : ToDto(variante);
    }

    public async Task<ProductoVarianteDto> CreateAsync(int productoId, CreateProductoVarianteDto dto)
    {
        var producto = await ObtenerProductoAsync(productoId);
        var color = await ValidarAsync(productoId, null, dto);

        var variante = new ProductoVariante
        {
            ProductoId = productoId,
            ColorId = color.Id,
            Sku = NormalizarSku(dto.Sku),
            CodigoBarras = NormalizarOpcional(dto.CodigoBarras),
            Cantidad = dto.Cantidad,
            UmbralStockBajo = dto.UmbralStockBajo,
            Costo = dto.Costo,
            Precio = dto.Precio,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(variante);
        await _repository.SaveChangesAsync();
        await RecalcularProductoAsync(producto);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Crear,
            $"Variante creada para {producto.Nombre}: {variante.Sku}.",
            variante.Id,
            entidad: "ProductoVariante",
            valoresNuevos: new { variante.ProductoId, variante.ColorId, variante.Sku, variante.CodigoBarras, variante.Cantidad, variante.Costo, variante.Precio });

        return ToDto((await _repository.GetByIdAsync(variante.Id))!);
    }

    public async Task<ProductoVarianteDto?> UpdateAsync(int productoId, int id, UpdateProductoVarianteDto dto)
    {
        var producto = await ObtenerProductoAsync(productoId);
        var variante = await _repository.GetByIdAsync(id);
        if (variante is null || variante.ProductoId != productoId) return null;

        var anteriores = new { variante.ColorId, variante.Sku, variante.CodigoBarras, variante.Cantidad, variante.UmbralStockBajo, variante.Costo, variante.Precio };
        var color = await ValidarAsync(productoId, id, dto);

        variante.ColorId = color.Id;
        variante.Sku = NormalizarSku(dto.Sku);
        variante.CodigoBarras = NormalizarOpcional(dto.CodigoBarras);
        variante.Cantidad = dto.Cantidad;
        variante.UmbralStockBajo = dto.UmbralStockBajo;
        variante.Costo = dto.Costo;
        variante.Precio = dto.Precio;
        variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        variante.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(variante);
        await _repository.SaveChangesAsync();
        await RecalcularProductoAsync(producto);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            $"Variante actualizada para {producto.Nombre}: {variante.Sku}.",
            variante.Id,
            entidad: "ProductoVariante",
            valoresAnteriores: anteriores,
            valoresNuevos: new { variante.ColorId, variante.Sku, variante.CodigoBarras, variante.Cantidad, variante.UmbralStockBajo, variante.Costo, variante.Precio });

        return ToDto((await _repository.GetByIdAsync(variante.Id))!);
    }

    public async Task<ProductoVarianteDto?> CambiarEstadoAsync(int productoId, int id, bool activo)
    {
        var producto = await ObtenerProductoAsync(productoId);
        var variante = await _repository.GetByIdAsync(id);
        if (variante is null || variante.ProductoId != productoId) return null;
        if (variante.Activo == activo) return ToDto(variante);

        variante.Activo = activo;
        variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        variante.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(variante);
        await _repository.SaveChangesAsync();
        await RecalcularProductoAsync(producto);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Variante {(activo ? "activada" : "desactivada")}: {variante.Sku}.",
            variante.Id,
            entidad: "ProductoVariante",
            valoresNuevos: new { variante.Activo });

        return ToDto((await _repository.GetByIdAsync(variante.Id))!);
    }

    public async Task<bool> DeleteAsync(int productoId, int id)
    {
        var producto = await ObtenerProductoAsync(productoId);
        var variante = await _repository.GetByIdAsync(id);
        if (variante is null || variante.ProductoId != productoId) return false;
        if (variante.Cantidad != 0)
            throw new BusinessRuleException("Solo se puede eliminar una variante cuando su stock sea cero. Puedes desactivarla para conservar existencias e historial.");

        variante.Activo = false;
        variante.Eliminado = true;
        variante.FechaEliminacion = DateTime.UtcNow;
        variante.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        variante.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(variante);
        var guardado = await _repository.SaveChangesAsync();
        await RecalcularProductoAsync(producto);

        if (guardado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Productos,
                AccionPermiso.EliminarLogico,
                $"Variante eliminada lógicamente: {variante.Sku}.",
                variante.Id,
                entidad: "ProductoVariante",
                valoresNuevos: new { variante.Eliminado, variante.FechaEliminacion });
        }

        return guardado;
    }

    private async Task<CatalogoProductoDto> ValidarAsync(int productoId, int? varianteId, CreateProductoVarianteDto dto)
    {
        if (dto.ColorId <= 0) throw new BusinessRuleException("El color de la variante es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Sku)) throw new BusinessRuleException("El SKU de la variante es obligatorio.");
        if (dto.Cantidad < 0) throw new BusinessRuleException("El stock de la variante no puede ser negativo.");
        if (dto.UmbralStockBajo < 0) throw new BusinessRuleException("El umbral de stock bajo no puede ser negativo.");
        if (dto.Costo < 0 || dto.Precio <= 0) throw new BusinessRuleException("El costo no puede ser negativo y el precio debe ser mayor que cero.");

        var color = await _catalogoService.GetByIdAsync(TipoCatalogoProducto.Color, dto.ColorId)
            ?? throw new BusinessRuleException("El color seleccionado no existe.");
        if (!color.Activo) throw new BusinessRuleException("El color seleccionado está inactivo.");

        var porColor = await _repository.GetByProductoColorAsync(productoId, dto.ColorId);
        if (porColor is not null && porColor.Id != varianteId)
            throw new BusinessRuleException("Este producto ya tiene una variante para el color seleccionado.");

        var sku = NormalizarSku(dto.Sku);
        var porSku = await _repository.GetBySkuAsync(sku);
        if (porSku is not null && porSku.Id != varianteId)
            throw new BusinessRuleException($"El SKU '{sku}' ya está utilizado por otra variante.");

        var codigo = NormalizarOpcional(dto.CodigoBarras);
        if (codigo is not null)
        {
            var porCodigo = await _repository.GetByCodigoBarrasAsync(codigo);
            if (porCodigo is not null && porCodigo.Id != varianteId)
                throw new BusinessRuleException("El código de barras ya está utilizado por otra variante.");
        }

        return color;
    }

    private async Task<Producto> ObtenerProductoAsync(int productoId) =>
        await _productoRepository.GetByIdAsync(productoId)
        ?? throw new BusinessRuleException("El producto no existe o fue eliminado.");

    private async Task RecalcularProductoAsync(Producto producto)
    {
        var variantes = await _repository.GetByProductoIdAsync(producto.Id, incluirInactivas: false);
        var total = variantes.Sum(v => v.Cantidad);
        producto.Cantidad = total;
        if (variantes.Count > 0)
        {
            producto.Costo = total > 0
                ? Math.Round(variantes.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / total, 2, MidpointRounding.AwayFromZero)
                : variantes.Average(v => v.Costo ?? 0m);
            producto.Precio = variantes.Min(v => v.Precio ?? producto.Precio);
            producto.ColorId = variantes.Count == 1 ? variantes[0].ColorId : null;
        }
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;
        _productoRepository.Update(producto);
        await _productoRepository.SaveChangesAsync();
    }

    private static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();
    private static string? NormalizarOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ProductoVarianteDto ToDto(ProductoVariante v) => new()
    {
        Id = v.Id,
        ProductoId = v.ProductoId,
        ProductoNombre = v.Producto?.Nombre ?? string.Empty,
        ColorId = v.ColorId ?? 0,
        ColorNombre = v.Color?.Nombre ?? "Sin color",
        ColorCodigoVisual = v.Color?.CodigoVisual,
        Sku = v.Sku ?? string.Empty,
        CodigoBarras = v.CodigoBarras,
        Cantidad = v.Cantidad,
        UmbralStockBajo = v.UmbralStockBajo,
        Costo = v.Costo ?? 0m,
        Precio = v.Precio ?? 0m,
        Activo = v.Activo,
        TieneStockBajo = v.TieneStockBajo,
        EstaAgotada = v.EstaAgotada,
        EstadoInventario = v.EstaAgotada ? "Agotada" : v.TieneStockBajo ? "Stock bajo" : "Disponible",
        FechaCreacion = v.FechaCreacion,
        FechaActualizacion = v.FechaActualizacion
    };
}
