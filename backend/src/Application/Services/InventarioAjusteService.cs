using InventoryApp.Application.DTOs;
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
        if (request.CantidadActualEsperada == request.CantidadNueva)
            throw new BusinessRuleException("La nueva cantidad debe ser diferente del stock actual.");

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
