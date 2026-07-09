using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _compraRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CompraService(
        ICompraRepository compraRepository,
        IProductoRepository productoRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _compraRepository = compraRepository;
        _productoRepository = productoRepository;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompraDto?> GetByIdAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        return compra is null ? null : ToDto(compra);
    }

    public async Task<PagedResult<CompraDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, total) = await _compraRepository.GetPagedAsync(request);
        return new PagedResult<CompraDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<CompraDto> CreateAsync(CreateCompraDto dto)
    {
        var compra = new Compra
        {
            NumeroCompra = await GenerarNumeroAsync(),
            ProveedorNombre = dto.ProveedorNombre,
            ProveedorTelefono = dto.ProveedorTelefono,
            ProveedorDocumento = dto.ProveedorDocumento,
            DocumentoReferencia = dto.DocumentoReferencia,
            MetodoPago = ParseEnum(dto.MetodoPago, MetodoPago.Efectivo),
            EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente),
            Estado = EstadoDocumento.Borrador,
            Descuento = dto.Descuento,
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await ArmarDetallesAsync(compra, dto.Detalles);
        CalcularTotales(compra, dto.Impuesto);

        await _compraRepository.AddAsync(compra);
        await _compraRepository.SaveChangesAsync();

        return ToDto(compra);
    }

    public async Task<CompraDto?> UpdateAsync(int id, UpdateCompraDto dto)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return null;

        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden editar compras en estado Borrador.");

        compra.ProveedorNombre = dto.ProveedorNombre;
        compra.ProveedorTelefono = dto.ProveedorTelefono;
        compra.ProveedorDocumento = dto.ProveedorDocumento;
        compra.DocumentoReferencia = dto.DocumentoReferencia;
        compra.MetodoPago = ParseEnum(dto.MetodoPago, MetodoPago.Efectivo);
        compra.EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente);
        compra.Descuento = dto.Descuento;
        compra.Notas = dto.Notas;
        compra.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        compra.FechaActualizacion = DateTime.UtcNow;

        compra.Detalles.Clear();
        await ArmarDetallesAsync(compra, dto.Detalles);
        CalcularTotales(compra, dto.Impuesto);

        _compraRepository.Update(compra);
        await _compraRepository.SaveChangesAsync();

        return ToDto(compra);
    }

    public async Task<CompraDto?> ConfirmarAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return null;

        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden confirmar compras en estado Borrador.");
        if (compra.Detalles.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto para confirmarse.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detalle in compra.Detalles)
            {
                var producto = await _productoRepository.GetByIdAsync(detalle.ProductoId)
                    ?? throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

                var stockAnterior = producto.Cantidad;
                producto.Cantidad += detalle.Cantidad;
                _productoRepository.Update(producto);

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    ReferenciaTipo = "Compra",
                    ReferenciaId = compra.Id,
                    Descripcion = $"Entrada por compra {compra.NumeroCompra}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.Compra,
                Concepto = $"Compra {compra.NumeroCompra} - {compra.ProveedorNombre}",
                Monto = compra.Total,
                Estado = compra.EstadoPago == EstadoPago.Pagado
                    ? EstadoMovimientoFinanciero.Pagado
                    : EstadoMovimientoFinanciero.Pendiente,
                MetodoPago = compra.MetodoPago,
                EsAutomatico = true,
                ModuloOrigen = "Compra",
                ReferenciaId = compra.Id,
                CompraId = compra.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            compra.Estado = EstadoDocumento.Confirmada;
            compra.ConfirmadoPorUsuarioId = _currentUser.UsuarioId;
            compra.ConfirmadoPorNombreUsuario = _currentUser.NombreUsuario;
            compra.FechaConfirmacion = DateTime.UtcNow;
            _compraRepository.Update(compra);

            await _compraRepository.SaveChangesAsync();
        });

        return ToDto(compra);
    }

    public async Task<CompraDto?> AnularAsync(int id, string motivo)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return null;

        if (compra.Estado != EstadoDocumento.Confirmada)
            throw new BusinessRuleException("Solo se pueden anular compras confirmadas.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detalle in compra.Detalles)
            {
                var producto = await _productoRepository.GetByIdAsync(detalle.ProductoId)
                    ?? throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

                if (producto.Cantidad < detalle.Cantidad)
                    throw new BusinessRuleException(
                        $"No se puede anular: el producto '{producto.Nombre}' ya no tiene suficientes unidades en stock para revertir esta compra (posiblemente ya se vendieron).");

                var stockAnterior = producto.Cantidad;
                producto.Cantidad -= detalle.Cantidad;
                _productoRepository.Update(producto);

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    Tipo = TipoMovimientoInventario.Reversion,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    ReferenciaTipo = "Compra",
                    ReferenciaId = compra.Id,
                    Descripcion = $"Reversión por anulación de compra {compra.NumeroCompra}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Reversion,
                Concepto = $"Reversión de compra anulada {compra.NumeroCompra}",
                Monto = compra.Total,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = true,
                ModuloOrigen = "Reversion",
                ReferenciaId = compra.Id,
                CompraId = compra.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            compra.Estado = EstadoDocumento.Anulada;
            compra.AnuladoPorUsuarioId = _currentUser.UsuarioId;
            compra.AnuladoPorNombreUsuario = _currentUser.NombreUsuario;
            compra.FechaAnulacion = DateTime.UtcNow;
            compra.MotivoAnulacion = motivo;
            _compraRepository.Update(compra);

            await _compraRepository.SaveChangesAsync();
        });

        return ToDto(compra);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return false;

        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden eliminar compras en estado Borrador. Las confirmadas o anuladas se conservan por auditoría.");

        // Nota: se permite eliminación física únicamente en Borrador (no afecta stock/finanzas).
        compra.Detalles.Clear();
        _compraRepository.Update(compra);
        return await _compraRepository.SaveChangesAsync();
    }

    private async Task ArmarDetallesAsync(Compra compra, List<CompraDetalleInputDto> detallesInput)
    {
        if (detallesInput.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto.");

        foreach (var input in detallesInput)
        {
            if (input.Cantidad <= 0)
                throw new BusinessRuleException("La cantidad de cada producto debe ser mayor a 0.");
            if (input.CostoUnitario <= 0)
                throw new BusinessRuleException("El costo unitario de cada producto debe ser mayor a 0.");

            var producto = await _productoRepository.GetByIdAsync(input.ProductoId)
                ?? throw new BusinessRuleException($"El producto con id {input.ProductoId} no existe.");

            compra.Detalles.Add(new CompraDetalle
            {
                ProductoId = producto.Id,
                Cantidad = input.Cantidad,
                CostoUnitario = input.CostoUnitario,
                Subtotal = input.Cantidad * input.CostoUnitario,
                ProductoNombreSnapshot = producto.Nombre,
                ProductoMarcaSnapshot = producto.Marca,
                ProductoModeloSnapshot = producto.Modelo
            });
        }
    }

    private static void CalcularTotales(Compra compra, decimal impuesto)
    {
        compra.Subtotal = compra.Detalles.Sum(d => d.Subtotal);
        compra.Impuesto = impuesto;
        compra.Total = compra.Subtotal - compra.Descuento + compra.Impuesto;

        if (compra.Total < 0)
            throw new BusinessRuleException("El total de la compra no puede ser negativo (revisa el descuento).");
    }

    private async Task<string> GenerarNumeroAsync()
    {
        var total = await _compraRepository.ContarTodasAsync();
        return $"COM-{(total + 1):D6}";
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum valorPorDefecto) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var resultado) ? resultado : valorPorDefecto;

    private static CompraDto ToDto(Compra c) => new()
    {
        Id = c.Id,
        NumeroCompra = c.NumeroCompra,
        Fecha = c.Fecha,
        ProveedorNombre = c.ProveedorNombre,
        ProveedorTelefono = c.ProveedorTelefono,
        ProveedorDocumento = c.ProveedorDocumento,
        DocumentoReferencia = c.DocumentoReferencia,
        Estado = c.Estado.ToString(),
        EstadoPago = c.EstadoPago.ToString(),
        MetodoPago = c.MetodoPago.ToString(),
        Subtotal = c.Subtotal,
        Descuento = c.Descuento,
        Impuesto = c.Impuesto,
        Total = c.Total,
        Notas = c.Notas,
        Detalles = c.Detalles.Select(d => new CompraDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoNombre = d.ProductoNombreSnapshot,
            ProductoMarca = d.ProductoMarcaSnapshot,
            ProductoModelo = d.ProductoModeloSnapshot,
            Cantidad = d.Cantidad,
            CostoUnitario = d.CostoUnitario,
            Subtotal = d.Subtotal
        }).ToList(),
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion,
        ConfirmadoPorNombreUsuario = c.ConfirmadoPorNombreUsuario,
        FechaConfirmacion = c.FechaConfirmacion,
        AnuladoPorNombreUsuario = c.AnuladoPorNombreUsuario,
        FechaAnulacion = c.FechaAnulacion,
        MotivoAnulacion = c.MotivoAnulacion
    };
}
