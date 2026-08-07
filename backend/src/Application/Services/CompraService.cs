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
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IInventarioConcurrencyService _inventarioConcurrency;
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
        IProductoVarianteRepository productoVarianteRepository,
        IInventarioConcurrencyService inventarioConcurrency,
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
        _productoVarianteRepository = productoVarianteRepository;
        _inventarioConcurrency = inventarioConcurrency;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _calculoService = calculoService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<CompraDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, total) = await _compraRepository.GetPagedAsync(request);
        return new PagedResult<CompraDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<CompraDto?> GetByIdAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        return compra is null ? null : ToDto(compra);
    }

    public async Task<ResultadoCalculoDto> CalcularAsync(CalcularCompraDto dto)
    {
        var inputs = await ArmarInputsCalculoAsync(dto.Detalles);
        return await _calculoService.CalcularCompraAsync(inputs, dto.ProveedorId);
    }

    public async Task<CompraDto> CreateAsync(CreateCompraDto dto)
    {
        var compra = new Compra
        {
            NumeroCompra = await _compraRepository.GenerarNumeroAsync(),
            Fecha = DateTime.UtcNow,
            ProveedorNombre = dto.ProveedorNombre.Trim(),
            ProveedorTelefono = dto.ProveedorTelefono,
            ProveedorDocumento = dto.ProveedorDocumento,
            DocumentoReferencia = dto.DocumentoReferencia,
            MetodoPago = ParseMetodoPago(dto.MetodoPago),
            EstadoPago = ParseEstadoPago(dto.EstadoPago),
            Estado = EstadoDocumento.Borrador,
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario,
            FechaCreacion = DateTime.UtcNow
        };

        await VincularProveedorAsync(compra, dto);
        await ArmarDetallesAsync(compra, dto.Detalles);
        await CalcularTotalesAsync(compra);

        await _compraRepository.AddAsync(compra);
        await _compraRepository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Crear, $"Compra creada: {compra.NumeroCompra}.", compra.Id);
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

        compra.ProveedorNombre = dto.ProveedorNombre.Trim();
        compra.ProveedorTelefono = dto.ProveedorTelefono;
        compra.ProveedorDocumento = dto.ProveedorDocumento;
        compra.DocumentoReferencia = dto.DocumentoReferencia;
        compra.MetodoPago = ParseMetodoPago(dto.MetodoPago);
        compra.EstadoPago = ParseEstadoPago(dto.EstadoPago);
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
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var compra = await _compraRepository.GetByIdForUpdateAsync(id);
            if (compra is null)
                throw new KeyNotFoundException($"Compra ID '{id}' no encontrada.");

            if (compra.Estado != EstadoDocumento.Borrador)
                throw new BusinessRuleException("Solo se pueden confirmar compras en estado Borrador.");
            if (compra.Detalles.Count == 0)
                throw new BusinessRuleException("La compra debe tener al menos un producto para confirmarse.");

            var demanda = compra.Detalles
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

        var actualizada = await _compraRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Confirmar, $"Compra confirmada: {actualizada!.NumeroCompra}.", id);
        return ToDto(actualizada);
    }

    public async Task<CompraDto?> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var compra = await _compraRepository.GetByIdForUpdateAsync(id);
            if (compra is null)
                throw new KeyNotFoundException($"Compra ID '{id}' no encontrada.");

            if (compra.Estado != EstadoDocumento.Confirmada)
                throw new BusinessRuleException("Solo se pueden anular compras confirmadas.");

            var demanda = compra.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: true);

            var ultimoMovimientoOriginalId = await _movimientoInventarioRepository
                .GetUltimoMovimientoOriginalCompraIdAsync(compra.Id)
                ?? throw new BusinessRuleException(
                    "No se encontraron los movimientos originales de la compra; la anulación no puede ejecutarse de forma segura.");

            if (await _movimientoInventarioRepository.ExisteMovimientoPosteriorAsync(
                    ultimoMovimientoOriginalId,
                    inventario.Demandas))
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

        var actualizada = await _compraRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Anular,
            $"Compra anulada: {actualizada!.NumeroCompra}.",
            id,
            entidad: "Compra",
            motivo: motivo);

        return ToDto(actualizada);
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
            await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Eliminar, $"Borrador de compra eliminado: {compra.NumeroCompra}.", compra.Id);
        return eliminado;
    }

    private async Task VincularProveedorAsync(Compra compra, CompraBaseDto dto)
    {
        if (dto.ProveedorId.HasValue)
        {
            var proveedor = await _proveedorRepository.GetByIdAsync(dto.ProveedorId.Value)
                ?? throw new BusinessRuleException("El proveedor seleccionado no existe.");
            compra.ProveedorId = proveedor.Id;
            compra.ProveedorNombre = proveedor.Nombre;
            compra.ProveedorTelefono = proveedor.Telefono;
            compra.ProveedorDocumento = proveedor.Documento;
            return;
        }

        compra.ProveedorId = null;
    }

    private async Task ArmarDetallesAsync(Compra compra, IEnumerable<CompraDetalleInputDto> detallesDto)
    {
        foreach (var dto in detallesDto)
        {
            var producto = await _productoRepository.GetByIdAsync(dto.ProductoId)
                ?? throw new BusinessRuleException($"Producto ID {dto.ProductoId} no existe.");
            if (!producto.Activo)
                throw new BusinessRuleException($"Producto '{producto.Nombre}' está inactivo.");

            ProductoVariante? variante = null;
            if (dto.ProductoVarianteId.HasValue)
            {
                variante = await _productoVarianteRepository.GetByIdAsync(dto.ProductoVarianteId.Value)
                    ?? throw new BusinessRuleException("La variante seleccionada no existe o fue eliminada.");
                if (variante.ProductoId != producto.Id)
                    throw new BusinessRuleException("La variante no pertenece al producto seleccionado.");
                if (!variante.Activo)
                    throw new BusinessRuleException("La variante seleccionada está inactiva.");
            }
            else
            {
                var variantes = await _productoVarianteRepository.GetByProductoIdAsync(producto.Id, incluirInactivas: true);
                variante = variantes.SingleOrDefault(v => v.EsTecnica && !v.Eliminado)
                    ?? throw new BusinessRuleException(
                        $"El producto '{producto.Nombre}' no tiene una variante técnica disponible. Complete la migración de variante antes de comprarlo.");
            }

            compra.Detalles.Add(new CompraDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante.Id,
                Cantidad = dto.Cantidad,
                CostoUnitario = dto.CostoUnitario,
                Subtotal = dto.Cantidad * dto.CostoUnitario,
                ProductoNombreSnapshot = producto.Nombre,
                ProductoMarcaSnapshot = producto.Marca,
                ProductoModeloSnapshot = producto.Modelo,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoSkuSnapshot = variante.Sku
            });
        }
    }

    private async Task CalcularTotalesAsync(Compra compra)
    {
        var inputs = await ArmarInputsCalculoAsync(compra.Detalles.Select(d => new CompraDetalleInputDto
        {
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            Cantidad = d.Cantidad,
            CostoUnitario = d.CostoUnitario
        }));
        var resultado = await _calculoService.CalcularCompraAsync(inputs, compra.ProveedorId);

        compra.Subtotal = resultado.Subtotal;
        compra.Descuento = resultado.TotalDescuento;
        compra.Impuesto = resultado.TotalImpuesto;
        compra.Total = resultado.Total;
        compra.ImpuestosAplicados = resultado.ImpuestosAplicados.Select(i => new CompraImpuesto
        {
            ImpuestoId = i.ImpuestoId,
            ImpuestoNombreSnapshot = i.Nombre,
            ImpuestoCodigoSnapshot = i.Codigo,
            TasaSnapshot = i.Tasa,
            BaseImponible = i.BaseImponible,
            MontoAplicado = i.Monto,
            IncluidoEnPrecioSnapshot = i.IncluidoEnPrecio
        }).ToList();
    }

    private async Task<List<DetalleCalculoInput>> ArmarInputsCalculoAsync(IEnumerable<CompraDetalleInputDto> detallesDto)
    {
        var inputs = new List<DetalleCalculoInput>();
        foreach (var d in detallesDto)
        {
            var producto = await _productoRepository.GetByIdAsync(d.ProductoId)
                ?? throw new BusinessRuleException($"Producto ID {d.ProductoId} no existe.");
            inputs.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.CostoUnitario
            });
        }
        return inputs;
    }

    private static EstadoPago ParseEstadoPago(string value) =>
        Enum.TryParse<EstadoPago>(value, true, out var estado) ? estado : EstadoPago.Pendiente;

    private static MetodoPago ParseMetodoPago(string value) =>
        Enum.TryParse<MetodoPago>(value, true, out var metodo) ? metodo : MetodoPago.Efectivo;

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
        MetodoPago = c.MetodoPago.ToString(),
        EstadoPago = c.EstadoPago.ToString(),
        Estado = c.Estado.ToString(),
        Subtotal = c.Subtotal,
        Descuento = c.Descuento,
        Impuesto = c.Impuesto,
        Total = c.Total,
        Notas = c.Notas,
        Detalles = c.Detalles.Select(d => new CompraDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            ProductoNombre = d.ProductoNombreSnapshot,
            ProductoMarca = d.ProductoMarcaSnapshot,
            ProductoModelo = d.ProductoModeloSnapshot,
            ProductoColor = d.ProductoColorSnapshot,
            ProductoSku = d.ProductoSkuSnapshot,
            ProductoImagenPrincipalUrl = ProductoImagenSelector.ObtenerPrincipal(d.Producto),
            Cantidad = d.Cantidad,
            CostoUnitario = d.CostoUnitario,
            Subtotal = d.Subtotal
        }).ToList(),
        ImpuestosAplicados = c.ImpuestosAplicados.Select(i => new ImpuestoAplicadoDto
        {
            ImpuestoId = i.ImpuestoId,
            Nombre = i.ImpuestoNombreSnapshot,
            Codigo = i.ImpuestoCodigoSnapshot,
            Tasa = i.TasaSnapshot,
            BaseImponible = i.BaseImponible,
            Monto = i.MontoAplicado,
            IncluidoEnPrecio = i.IncluidoEnPrecioSnapshot
        }).ToList(),
        CreadoPorUsuarioId = c.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion,
        ConfirmadoPorNombreUsuario = c.ConfirmadoPorNombreUsuario,
        FechaConfirmacion = c.FechaConfirmacion,
        AnuladoPorNombreUsuario = c.AnuladoPorNombreUsuario,
        FechaAnulacion = c.FechaAnulacion,
        MotivoAnulacion = c.MotivoAnulacion
    };
}
