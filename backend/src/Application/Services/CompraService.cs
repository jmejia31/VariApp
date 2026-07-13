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
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly ICalculoService _calculoService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public CompraService(
        ICompraRepository compraRepository,
        IProveedorRepository proveedorRepository,
        IProductoRepository productoRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        ICalculoService calculoService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _compraRepository = compraRepository;
        _proveedorRepository = proveedorRepository;
        _productoRepository = productoRepository;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _calculoService = calculoService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
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
            // Descuento/Impuesto NO se toman de dto: se recalculan abajo (sección 13).
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await VincularProveedorAsync(compra, dto);
        await ArmarDetallesAsync(compra, dto.Detalles);
        await CalcularTotalesAsync(compra);

        await _compraRepository.AddAsync(compra);
        await _compraRepository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Crear, $"Compra creada: {compra.NumeroCompra}", compra.Id);

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
        compra.Notas = dto.Notas;
        compra.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        compra.FechaActualizacion = DateTime.UtcNow;

        await VincularProveedorAsync(compra, dto);
        compra.Detalles.Clear();
        compra.ImpuestosAplicados.Clear();
        await ArmarDetallesAsync(compra, dto.Detalles);
        await CalcularTotalesAsync(compra);

        _compraRepository.Update(compra);
        await _compraRepository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Editar, $"Compra actualizada: {compra.NumeroCompra}", compra.Id);

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

            await _calculoService.RegistrarUsoCompraAsync(compra.Id, compra.ImpuestosAplicados.ToList());

            await _compraRepository.SaveChangesAsync();
        });

        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Confirmar, $"Compra confirmada: {compra.NumeroCompra}", compra.Id);
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

        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Anular, $"Compra anulada: {compra.NumeroCompra}. Motivo: {motivo}", compra.Id);
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
        var eliminado = await _compraRepository.SaveChangesAsync();
        if (eliminado)
            await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Eliminar, $"Borrador de compra eliminado: {compra.NumeroCompra}", compra.Id);
        return eliminado;
    }

    private async Task VincularProveedorAsync(Compra compra, CreateCompraDto dto)
    {
        Proveedor? proveedor = null;

        if (dto.ProveedorId.HasValue)
        {
            proveedor = await _proveedorRepository.GetByIdAsync(dto.ProveedorId.Value)
                ?? throw new BusinessRuleException("El proveedor seleccionado no existe.");

            if (!proveedor.Activo)
                throw new BusinessRuleException("El proveedor seleccionado está inactivo.");
        }
        else if (DebeGestionarProveedor(dto.ProveedorNombre, dto.ProveedorDocumento, dto.ProveedorTelefono))
        {
            proveedor = await _proveedorRepository.BuscarCoincidenciaActivaAsync(
                dto.ProveedorDocumento,
                null,
                dto.ProveedorTelefono,
                dto.ProveedorNombre);

            if (proveedor is null)
            {
                proveedor = new Proveedor
                {
                    Nombre = dto.ProveedorNombre.Trim(),
                    Telefono = dto.ProveedorTelefono,
                    Documento = dto.ProveedorDocumento,
                    Activo = true,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                await _proveedorRepository.AddAsync(proveedor);
            }
        }

        if (proveedor is null)
        {
            compra.ProveedorId = null;
            compra.Proveedor = null;
            return;
        }

        compra.Proveedor = proveedor;
        compra.ProveedorId = proveedor.Id == 0 ? null : proveedor.Id;
        compra.ProveedorNombre = proveedor.Nombre;
        compra.ProveedorTelefono = proveedor.Telefono;
        compra.ProveedorDocumento = proveedor.Documento;
    }

    private static bool DebeGestionarProveedor(string? nombre, string? documento, string? telefono) =>
        !string.IsNullOrWhiteSpace(documento)
        || !string.IsNullOrWhiteSpace(telefono)
        || !string.IsNullOrWhiteSpace(nombre);

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

    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularCompraRequest request)
    {
        var entradas = new List<DetalleCalculoInput>();
        foreach (var d in request.Detalles)
        {
            var producto = await _productoRepository.GetByIdAsync(d.ProductoId);
            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = d.ProductoId,
                CategoriaId = producto?.CategoriaId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario
            });
        }

        return await _calculoService.CalcularCompraAsync(entradas, request.ProveedorId);
    }

    /// LIMITACIÓN DOCUMENTADA: a diferencia de Ventas, aquí solo se aplican
    /// Impuestos reales vía el motor (CalculoService.CalcularCompraAsync). El
    /// modelo de Descuento está diseñado con alcance Cliente/Rol (pensado para
    /// ventas); Compra.Descuento queda en 0 por ahora — extender Descuento con
    /// alcance Proveedor es la vía natural para habilitarlo aquí, no se hizo
    /// por límite de tiempo de esta fase.
    private async Task CalcularTotalesAsync(Compra compra)
    {
        var entradas = compra.Detalles.Select(d => new DetalleCalculoInput
        {
            ProductoId = d.ProductoId,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.CostoUnitario
        }).ToList();

        foreach (var entrada in entradas)
        {
            var producto = await _productoRepository.GetByIdAsync(entrada.ProductoId);
            entrada.CategoriaId = producto?.CategoriaId;
        }

        var resultado = await _calculoService.CalcularCompraAsync(entradas, compra.ProveedorId);

        compra.Subtotal = resultado.Subtotal;
        compra.Descuento = 0; // ver limitación documentada arriba
        compra.Impuesto = resultado.TotalImpuesto;
        compra.Total = resultado.Total;

        compra.ImpuestosAplicados = resultado.ImpuestosAplicados.Select(i => new CompraImpuesto
        {
            ImpuestoId = i.ImpuestoId,
            ImpuestoNombreSnapshot = i.Nombre,
            TasaSnapshot = i.Tasa,
            BaseImponible = i.BaseImponible,
            MontoAplicado = i.Monto
        }).ToList();

        if (compra.Total < 0)
            throw new BusinessRuleException("El total de la compra no puede ser negativo.");
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
        ProveedorId = c.ProveedorId,
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
        ImpuestosAplicados = c.ImpuestosAplicados.Select(i => new ImpuestoAplicadoDto
        {
            ImpuestoId = i.ImpuestoId,
            Nombre = i.ImpuestoNombreSnapshot,
            Tasa = i.TasaSnapshot,
            BaseImponible = i.BaseImponible,
            Monto = i.MontoAplicado
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
