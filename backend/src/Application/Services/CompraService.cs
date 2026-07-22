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
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await VincularProveedorAsync(compra, dto);
        await ArmarDetallesAsync(compra, dto.Detalles);
        await CalcularTotalesAsync(compra);

        await _compraRepository.AddAsync(compra);
        await _compraRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Crear,
            $"Compra creada: {compra.NumeroCompra}.",
            compra.Id,
            entidad: "Compra",
            valoresNuevos: new
            {
                compra.NumeroCompra,
                compra.ProveedorNombre,
                compra.Subtotal,
                compra.Impuesto,
                compra.Total,
                Detalles = compra.Detalles.Count
            });

        return ToDto(compra);
    }

    public async Task<CompraDto?> UpdateAsync(int id, UpdateCompraDto dto)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return null;
        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden editar compras en estado Borrador.");

        var valoresAnteriores = new
        {
            compra.ProveedorNombre,
            compra.DocumentoReferencia,
            compra.MetodoPago,
            compra.EstadoPago,
            compra.Subtotal,
            compra.Impuesto,
            compra.Total,
            Detalles = compra.Detalles.Count
        };

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

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Editar,
            $"Compra actualizada: {compra.NumeroCompra}.",
            compra.Id,
            entidad: "Compra",
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: new
            {
                compra.ProveedorNombre,
                compra.DocumentoReferencia,
                compra.MetodoPago,
                compra.EstadoPago,
                compra.Subtotal,
                compra.Impuesto,
                compra.Total,
                Detalles = compra.Detalles.Count
            });

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
                var producto = await ObtenerProductoActivoAsync(detalle.ProductoId);
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

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Confirmar,
            $"Compra confirmada: {compra.NumeroCompra}.",
            compra.Id,
            entidad: "Compra",
            valoresNuevos: new { compra.Estado, compra.Total, compra.FechaConfirmacion });

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
                        $"No se puede anular: el producto '{producto.Nombre}' ya no tiene suficientes unidades para revertir la compra.");

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
            compra.MotivoAnulacion = motivo.Trim();
            _compraRepository.Update(compra);
            await _compraRepository.SaveChangesAsync();
        });

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Anular,
            $"Compra anulada: {compra.NumeroCompra}.",
            compra.Id,
            entidad: "Compra",
            motivo: motivo);

        return ToDto(compra);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return false;
        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException(
                "Solo se pueden eliminar lógicamente compras en estado Borrador.");

        compra.Eliminado = true;
        compra.FechaEliminacion = DateTime.UtcNow;
        compra.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        compra.FechaActualizacion = DateTime.UtcNow;

        _compraRepository.Update(compra);
        var eliminado = await _compraRepository.SaveChangesAsync();
        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Compras,
                AccionPermiso.EliminarLogico,
                $"Borrador de compra eliminado lógicamente: {compra.NumeroCompra}.",
                compra.Id,
                entidad: "Compra",
                valoresAnteriores: new
                {
                    compra.NumeroCompra,
                    compra.ProveedorNombre,
                    compra.Total,
                    Detalles = compra.Detalles.Count
                },
                valoresNuevos: new
                {
                    compra.Eliminado,
                    compra.FechaEliminacion
                });
        }

        return eliminado;
    }

    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularCompraRequest request)
    {
        if (request.Detalles.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto.");

        var entradas = new List<DetalleCalculoInput>();
        foreach (var detalle in request.Detalles)
        {
            if (detalle.Cantidad <= 0 || detalle.PrecioUnitario <= 0)
                throw new BusinessRuleException("La cantidad y el costo unitario deben ser mayores a cero.");

            var producto = await ObtenerProductoActivoAsync(detalle.ProductoId);
            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            });
        }

        return await _calculoService.CalcularCompraAsync(entradas, request.ProveedorId);
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

            var producto = await ObtenerProductoActivoAsync(input.ProductoId);
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

    private async Task CalcularTotalesAsync(Compra compra)
    {
        var entradas = new List<DetalleCalculoInput>();
        foreach (var detalle in compra.Detalles)
        {
            var producto = await ObtenerProductoActivoAsync(detalle.ProductoId);
            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.CostoUnitario
            });
        }

        var resultado = await _calculoService.CalcularCompraAsync(entradas, compra.ProveedorId);
        compra.Subtotal = resultado.Subtotal;
        compra.Descuento = resultado.TotalDescuento;
        compra.Impuesto = resultado.TotalImpuesto;
        compra.Total = resultado.Total;
        compra.ImpuestosAplicados = resultado.ImpuestosAplicados.Select(impuesto => new CompraImpuesto
        {
            ImpuestoId = impuesto.ImpuestoId,
            ImpuestoNombreSnapshot = impuesto.Nombre,
            ImpuestoCodigoSnapshot = impuesto.Codigo,
            TasaSnapshot = impuesto.Tasa,
            BaseImponible = impuesto.BaseImponible,
            MontoAplicado = impuesto.Monto,
            IncluidoEnPrecioSnapshot = impuesto.IncluidoEnPrecio
        }).ToList();

        if (compra.Total < 0)
            throw new BusinessRuleException("El total de la compra no puede ser negativo.");
    }

    private async Task<Producto> ObtenerProductoActivoAsync(int productoId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId)
            ?? throw new BusinessRuleException($"El producto con id {productoId} no existe.");
        if (!producto.Activo)
            throw new BusinessRuleException(
                $"El producto '{producto.Nombre}' está inactivo. Actívalo antes de utilizarlo en una compra.");
        return producto;
    }

    private async Task<string> GenerarNumeroAsync()
    {
        var total = await _compraRepository.ContarTodasAsync();
        return $"COM-{(total + 1):D6}";
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum valorPorDefecto) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var resultado) ? resultado : valorPorDefecto;

    private static CompraDto ToDto(Compra compra) => new()
    {
        Id = compra.Id,
        NumeroCompra = compra.NumeroCompra,
        Fecha = compra.Fecha,
        ProveedorId = compra.ProveedorId,
        ProveedorNombre = compra.ProveedorNombre,
        ProveedorTelefono = compra.ProveedorTelefono,
        ProveedorDocumento = compra.ProveedorDocumento,
        DocumentoReferencia = compra.DocumentoReferencia,
        Estado = compra.Estado.ToString(),
        EstadoPago = compra.EstadoPago.ToString(),
        MetodoPago = compra.MetodoPago.ToString(),
        Subtotal = compra.Subtotal,
        Descuento = compra.Descuento,
        Impuesto = compra.Impuesto,
        Total = compra.Total,
        Notas = compra.Notas,
        Detalles = compra.Detalles.Select(detalle => new CompraDetalleDto
        {
            Id = detalle.Id,
            ProductoId = detalle.ProductoId,
            ProductoNombre = detalle.ProductoNombreSnapshot,
            ProductoMarca = detalle.ProductoMarcaSnapshot,
            ProductoModelo = detalle.ProductoModeloSnapshot,
            Cantidad = detalle.Cantidad,
            CostoUnitario = detalle.CostoUnitario,
            Subtotal = detalle.Subtotal
        }).ToList(),
        ImpuestosAplicados = compra.ImpuestosAplicados.Select(impuesto => new ImpuestoAplicadoDto
        {
            ImpuestoId = impuesto.ImpuestoId,
            Nombre = impuesto.ImpuestoNombreSnapshot,
            Codigo = impuesto.ImpuestoCodigoSnapshot,
            Tasa = impuesto.TasaSnapshot,
            BaseImponible = impuesto.BaseImponible,
            Monto = impuesto.MontoAplicado,
            IncluidoEnPrecio = impuesto.IncluidoEnPrecioSnapshot
        }).ToList(),
        CreadoPorNombreUsuario = compra.CreadoPorNombreUsuario,
        FechaCreacion = compra.FechaCreacion,
        ConfirmadoPorNombreUsuario = compra.ConfirmadoPorNombreUsuario,
        FechaConfirmacion = compra.FechaConfirmacion,
        AnuladoPorNombreUsuario = compra.AnuladoPorNombreUsuario,
        FechaAnulacion = compra.FechaAnulacion,
        MotivoAnulacion = compra.MotivoAnulacion
    };
}
