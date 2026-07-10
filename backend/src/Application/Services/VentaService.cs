using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IFacturaRepository _facturaRepository;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly IEmpresaConfiguracionService _empresaConfiguracionService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public VentaService(
        IVentaRepository ventaRepository,
        IProductoRepository productoRepository,
        IFacturaRepository facturaRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        IEmpresaConfiguracionService empresaConfiguracionService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _ventaRepository = ventaRepository;
        _productoRepository = productoRepository;
        _facturaRepository = facturaRepository;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _empresaConfiguracionService = empresaConfiguracionService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<VentaDto?> GetByIdAsync(int id)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        return venta is null ? null : ToDto(venta);
    }

    public async Task<PagedResult<VentaDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, total) = await _ventaRepository.GetPagedAsync(request);
        return new PagedResult<VentaDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<VentaDto> CreateAsync(CreateVentaDto dto)
    {
        var venta = new Venta
        {
            NumeroVenta = await GenerarNumeroVentaAsync(),
            ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre,
            ClienteTelefono = dto.ClienteTelefono,
            ClienteIdentidadORTN = dto.ClienteIdentidadORTN,
            ClienteCorreo = dto.ClienteCorreo,
            ClienteDireccion = dto.ClienteDireccion,
            MetodoPago = ParseEnum(dto.MetodoPago, MetodoPago.Efectivo),
            EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente),
            Estado = EstadoDocumento.Borrador,
            Descuento = dto.Descuento,
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await ArmarDetallesAsync(venta, dto.Detalles, validarStock: false);
        CalcularTotales(venta, dto.Impuesto);

        await _ventaRepository.AddAsync(venta);
        await _ventaRepository.SaveChangesAsync();

        return ToDto(venta);
    }

    public async Task<VentaDto?> UpdateAsync(int id, UpdateVentaDto dto)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return null;

        if (venta.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden editar ventas en estado Borrador.");

        venta.ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre;
        venta.ClienteTelefono = dto.ClienteTelefono;
        venta.ClienteIdentidadORTN = dto.ClienteIdentidadORTN;
        venta.ClienteCorreo = dto.ClienteCorreo;
        venta.ClienteDireccion = dto.ClienteDireccion;
        venta.MetodoPago = ParseEnum(dto.MetodoPago, MetodoPago.Efectivo);
        venta.EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente);
        venta.Descuento = dto.Descuento;
        venta.Notas = dto.Notas;
        venta.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        venta.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        venta.FechaActualizacion = DateTime.UtcNow;

        venta.Detalles.Clear();
        await ArmarDetallesAsync(venta, dto.Detalles, validarStock: false);
        CalcularTotales(venta, dto.Impuesto);

        _ventaRepository.Update(venta);
        await _ventaRepository.SaveChangesAsync();

        return ToDto(venta);
    }

    public async Task<VentaDto?> ConfirmarAsync(int id)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return null;

        if (venta.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden confirmar ventas en estado Borrador.");
        if (venta.Detalles.Count == 0)
            throw new BusinessRuleException("La venta debe tener al menos un producto para confirmarse.");

        // Validar stock suficiente ANTES de tocar nada.
        foreach (var detalle in venta.Detalles)
        {
            var producto = await _productoRepository.GetByIdAsync(detalle.ProductoId)
                ?? throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

            if (producto.Cantidad < detalle.Cantidad)
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {detalle.Cantidad}.");
        }

        var empresa = await _empresaConfiguracionService.GetActivaEntidadAsync();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detalle in venta.Detalles)
            {
                var producto = await _productoRepository.GetByIdAsync(detalle.ProductoId)
                    ?? throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

                var stockAnterior = producto.Cantidad;
                producto.Cantidad -= detalle.Cantidad;
                _productoRepository.Update(producto);

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    Tipo = TipoMovimientoInventario.Salida,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    CostoUnitario = detalle.CostoUnitarioSnapshot,
                    ReferenciaTipo = "Venta",
                    ReferenciaId = venta.Id,
                    Descripcion = $"Salida por venta {venta.NumeroVenta}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Venta,
                Concepto = $"Venta {venta.NumeroVenta} - {venta.ClienteNombre}",
                Monto = venta.Total,
                Estado = venta.EstadoPago == EstadoPago.Pagado
                    ? EstadoMovimientoFinanciero.Pagado
                    : EstadoMovimientoFinanciero.Pendiente,
                MetodoPago = venta.MetodoPago,
                EsAutomatico = true,
                ModuloOrigen = "Venta",
                ReferenciaId = venta.Id,
                VentaId = venta.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            venta.Estado = EstadoDocumento.Confirmada;
            venta.ConfirmadoPorUsuarioId = _currentUser.UsuarioId;
            venta.ConfirmadoPorNombreUsuario = _currentUser.NombreUsuario;
            venta.FechaConfirmacion = DateTime.UtcNow;
            _ventaRepository.Update(venta);

            // Generar factura automáticamente
            var factura = new Factura
            {
                VentaId = venta.Id,
                NumeroFactura = await GenerarNumeroFacturaAsync(),
                Estado = EstadoFactura.Emitida,
                EmpresaNombre = empresa.NombreComercial,
                EmpresaRTN = empresa.RTN,
                EmpresaTelefono = empresa.Telefono,
                EmpresaCorreo = empresa.Correo,
                EmpresaDireccion = empresa.Direccion,
                ClienteNombre = venta.ClienteNombre,
                ClienteTelefono = venta.ClienteTelefono,
                ClienteIdentidadORTN = venta.ClienteIdentidadORTN,
                ClienteCorreo = venta.ClienteCorreo,
                ClienteDireccion = venta.ClienteDireccion,
                VendedorUsuarioId = _currentUser.UsuarioId ?? 0,
                VendedorNombreUsuario = _currentUser.NombreCompleto ?? _currentUser.NombreUsuario ?? "—",
                GeneradaPorUsuarioId = _currentUser.UsuarioId,
                GeneradaPorNombreUsuario = _currentUser.NombreCompleto ?? _currentUser.NombreUsuario,
                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Impuesto = venta.Impuesto,
                Total = venta.Total,
                Detalles = venta.Detalles.Select(d => new FacturaDetalle
                {
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.ProductoNombreSnapshot,
                    ProductoMarca = d.ProductoMarcaSnapshot,
                    ProductoModelo = d.ProductoModeloSnapshot,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList()
            };

            await _facturaRepository.AddAsync(factura);
            await _ventaRepository.SaveChangesAsync();
        });

        var actualizada = await _ventaRepository.GetByIdAsync(id);
        return ToDto(actualizada!);
    }

    public async Task<VentaDto?> AnularAsync(int id, string motivo)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return null;

        if (venta.Estado != EstadoDocumento.Confirmada)
            throw new BusinessRuleException("Solo se pueden anular ventas confirmadas.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detalle in venta.Detalles)
            {
                var producto = await _productoRepository.GetByIdAsync(detalle.ProductoId)
                    ?? throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

                var stockAnterior = producto.Cantidad;
                producto.Cantidad += detalle.Cantidad;
                _productoRepository.Update(producto);

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    Tipo = TipoMovimientoInventario.Reversion,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    ReferenciaTipo = "Venta",
                    ReferenciaId = venta.Id,
                    Descripcion = $"Reversión por anulación de venta {venta.NumeroVenta}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.Reversion,
                Concepto = $"Reversión de venta anulada {venta.NumeroVenta}",
                Monto = venta.Total,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = true,
                ModuloOrigen = "Reversion",
                ReferenciaId = venta.Id,
                VentaId = venta.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            venta.Estado = EstadoDocumento.Anulada;
            venta.AnuladoPorUsuarioId = _currentUser.UsuarioId;
            venta.AnuladoPorNombreUsuario = _currentUser.NombreUsuario;
            venta.FechaAnulacion = DateTime.UtcNow;
            venta.MotivoAnulacion = motivo;
            _ventaRepository.Update(venta);

            var factura = await _facturaRepository.GetByVentaIdAsync(venta.Id);
            if (factura is not null)
            {
                factura.Estado = EstadoFactura.Anulada;
                factura.FechaAnulacion = DateTime.UtcNow;
                factura.AnuladaPorUsuarioId = _currentUser.UsuarioId;
                factura.AnuladaPorNombreUsuario = _currentUser.NombreUsuario;
                factura.MotivoAnulacion = motivo;
                _facturaRepository.Update(factura);
            }

            await _ventaRepository.SaveChangesAsync();
        });

        var actualizada = await _ventaRepository.GetByIdAsync(id);
        return ToDto(actualizada!);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return false;

        if (venta.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden eliminar ventas en estado Borrador.");

        venta.Detalles.Clear();
        _ventaRepository.Update(venta);
        return await _ventaRepository.SaveChangesAsync();
    }

    private async Task ArmarDetallesAsync(Venta venta, List<VentaDetalleInputDto> detallesInput, bool validarStock)
    {
        if (detallesInput.Count == 0)
            throw new BusinessRuleException("La venta debe tener al menos un producto.");

        foreach (var input in detallesInput)
        {
            if (input.Cantidad <= 0)
                throw new BusinessRuleException("La cantidad de cada producto debe ser mayor a 0.");
            if (input.PrecioUnitario <= 0)
                throw new BusinessRuleException("El precio unitario de cada producto debe ser mayor a 0.");

            var producto = await _productoRepository.GetByIdAsync(input.ProductoId)
                ?? throw new BusinessRuleException($"El producto con id {input.ProductoId} no existe.");

            if (validarStock && producto.Cantidad < input.Cantidad)
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {input.Cantidad}.");

            var subtotal = input.Cantidad * input.PrecioUnitario;
            var costoTotal = input.Cantidad * producto.Costo;

            venta.Detalles.Add(new VentaDetalle
            {
                ProductoId = producto.Id,
                Cantidad = input.Cantidad,
                PrecioUnitario = input.PrecioUnitario,
                CostoUnitarioSnapshot = producto.Costo,
                Subtotal = subtotal,
                UtilidadBruta = subtotal - costoTotal,
                ProductoNombreSnapshot = producto.Nombre,
                ProductoMarcaSnapshot = producto.Marca,
                ProductoModeloSnapshot = producto.Modelo
            });
        }
    }

    private static void CalcularTotales(Venta venta, decimal impuesto)
    {
        venta.Subtotal = venta.Detalles.Sum(d => d.Subtotal);
        venta.Impuesto = impuesto;
        venta.Total = venta.Subtotal - venta.Descuento + venta.Impuesto;
        venta.CostoTotal = venta.Detalles.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad);
        venta.UtilidadBruta = venta.Detalles.Sum(d => d.UtilidadBruta) - venta.Descuento;

        if (venta.Total < 0)
            throw new BusinessRuleException("El total de la venta no puede ser negativo (revisa el descuento).");
    }

    private async Task<string> GenerarNumeroVentaAsync()
    {
        var total = await _ventaRepository.ContarTodasAsync();
        return $"VEN-{(total + 1):D6}";
    }

    private async Task<string> GenerarNumeroFacturaAsync()
    {
        var total = await _facturaRepository.ContarTodasAsync();
        return $"FAC-{(total + 1):D6}";
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum valorPorDefecto) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var resultado) ? resultado : valorPorDefecto;

    private static VentaDto ToDto(Venta v) => new()
    {
        Id = v.Id,
        NumeroVenta = v.NumeroVenta,
        Fecha = v.Fecha,
        ClienteNombre = v.ClienteNombre,
        ClienteTelefono = v.ClienteTelefono,
        ClienteIdentidadORTN = v.ClienteIdentidadORTN,
        ClienteCorreo = v.ClienteCorreo,
        ClienteDireccion = v.ClienteDireccion,
        Estado = v.Estado.ToString(),
        EstadoPago = v.EstadoPago.ToString(),
        MetodoPago = v.MetodoPago.ToString(),
        Subtotal = v.Subtotal,
        Descuento = v.Descuento,
        Impuesto = v.Impuesto,
        Total = v.Total,
        CostoTotal = v.CostoTotal,
        UtilidadBruta = v.UtilidadBruta,
        Notas = v.Notas,
        Detalles = v.Detalles.Select(d => new VentaDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoNombre = d.ProductoNombreSnapshot,
            ProductoMarca = d.ProductoMarcaSnapshot,
            ProductoModelo = d.ProductoModeloSnapshot,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Subtotal,
            UtilidadBruta = d.UtilidadBruta
        }).ToList(),
        FacturaId = v.Factura?.Id,
        NumeroFactura = v.Factura?.NumeroFactura,
        CreadoPorNombreUsuario = v.CreadoPorNombreUsuario,
        FechaCreacion = v.FechaCreacion,
        ConfirmadoPorNombreUsuario = v.ConfirmadoPorNombreUsuario,
        FechaConfirmacion = v.FechaConfirmacion,
        AnuladoPorNombreUsuario = v.AnuladoPorNombreUsuario,
        FechaAnulacion = v.FechaAnulacion,
        MotivoAnulacion = v.MotivoAnulacion
    };
}
