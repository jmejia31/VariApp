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
    private readonly IClienteRepository _clienteRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IInventarioConcurrencyService _inventarioConcurrency;
    private readonly IFacturaRepository _facturaRepository;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly IEmpresaConfiguracionService _empresaConfiguracionService;
    private readonly ICalculoService _calculoService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;
    private readonly ITipoClientePredeterminadoResolver _predeterminadoResolver;

    public VentaService(
        IVentaRepository ventaRepository,
        IClienteRepository clienteRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IInventarioConcurrencyService inventarioConcurrency,
        IFacturaRepository facturaRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        IEmpresaConfiguracionService empresaConfiguracionService,
        ICalculoService calculoService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        ITipoClientePredeterminadoResolver predeterminadoResolver)
    {
        _ventaRepository = ventaRepository;
        _clienteRepository = clienteRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _inventarioConcurrency = inventarioConcurrency;
        _facturaRepository = facturaRepository;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _empresaConfiguracionService = empresaConfiguracionService;
        _calculoService = calculoService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
        _predeterminadoResolver = predeterminadoResolver;
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
            NumeroVenta = CrearNumeroTemporal("VEN"),
            ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre,
            ClienteTelefono = dto.ClienteTelefono,
            ClienteIdentidadORTN = dto.ClienteIdentidadORTN,
            ClienteCorreo = dto.ClienteCorreo,
            ClienteDireccion = dto.ClienteDireccion,
            MetodoPago = ParseEnum(dto.MetodoPago, MetodoPago.Efectivo),
            EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente),
            Estado = EstadoDocumento.Borrador,
            // Descuento/Impuesto NO se toman de dto: se recalculan abajo (sección 13).
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await VincularClienteAsync(venta, dto);
        await ArmarDetallesAsync(venta, dto.Detalles, validarStock: false);
        await CalcularTotalesAsync(venta, dto.CodigoPromocional, dto.CostoEnvioId, dto.EnvioExonerado, dto.MotivoExoneracionEnvio);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _ventaRepository.AddAsync(venta);
            await _ventaRepository.SaveChangesAsync();

            // El identificador autoincremental es la única fuente de numeración.
            // El número temporal evita colisiones mientras MySQL asigna el Id.
            venta.NumeroVenta = $"VEN-{venta.Id:D6}";
            _ventaRepository.Update(venta);
            await _ventaRepository.SaveChangesAsync();
        });

        await _auditoria.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Crear,
            $"Venta creada: {venta.NumeroVenta}",
            venta.Id,
            entidad: "Venta",
            valoresNuevos: new
            {
                venta.NumeroVenta,
                venta.ImporteBruto,
                venta.Subtotal,
                venta.Impuesto,
                venta.Descuento,
                venta.CostoEnvio,
                venta.EnvioExonerado,
                venta.MotivoExoneracionEnvio,
                venta.Total
            });

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
        venta.Notas = dto.Notas;
        venta.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        venta.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        venta.FechaActualizacion = DateTime.UtcNow;

        await VincularClienteAsync(venta, dto);
        venta.Detalles.Clear();
        venta.DescuentosAplicados.Clear();
        venta.ImpuestosAplicados.Clear();
        await ArmarDetallesAsync(venta, dto.Detalles, validarStock: false);
        await CalcularTotalesAsync(venta, dto.CodigoPromocional, dto.CostoEnvioId, dto.EnvioExonerado, dto.MotivoExoneracionEnvio);

        _ventaRepository.Update(venta);
        await _ventaRepository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Editar, $"Venta actualizada: {venta.NumeroVenta}", venta.Id);

        return ToDto(venta);
    }

    public async Task<VentaDto?> ConfirmarAsync(int id)
    {
        var empresa = await _empresaConfiguracionService.GetActivaEntidadAsync();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var venta = await _ventaRepository.GetByIdForUpdateAsync(id);
            if (venta is null)
                throw new KeyNotFoundException($"Venta ID '{id}' no encontrada.");

            if (venta.Estado != EstadoDocumento.Borrador)
                throw new BusinessRuleException("Solo se pueden confirmar ventas en estado Borrador.");
            if (venta.Detalles.Count == 0)
                throw new BusinessRuleException("La venta debe tener al menos un producto para confirmarse.");

            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: true);
            var productosMap = inventario.Productos;
            var variantesMap = inventario.Variantes;

            foreach (var detalle in venta.Detalles)
            {
                if (!productosMap.TryGetValue(detalle.ProductoId, out var producto))
                    throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

                var stockAnteriorMovimiento = producto.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad - detalle.Cantidad;

                if (detalle.ProductoVarianteId.HasValue)
                {
                    if (!variantesMap.TryGetValue(detalle.ProductoVarianteId.Value, out var variante))
                        throw new BusinessRuleException($"La variante de '{producto.Nombre}' ya no existe.");

                    if (!variante.Activo)
                        throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva y no puede venderse.");

                    if (variante.Cantidad < detalle.Cantidad)
                        throw new BusinessRuleException($"Stock insuficiente para la variante '{variante.Sku}': disponible {variante.Cantidad}, solicitado {detalle.Cantidad}.");

                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad -= detalle.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);

                    producto.Cantidad -= detalle.Cantidad;
                    _productoRepository.Update(producto);
                }
                else
                {
                    if (producto.Cantidad < detalle.Cantidad)
                        throw new BusinessRuleException($"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {detalle.Cantidad}.");

                    producto.Cantidad -= detalle.Cantidad;
                    _productoRepository.Update(producto);
                }

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = detalle.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Salida,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
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

            var factura = new Factura
            {
                VentaId = venta.Id,
                NumeroFactura = CrearNumeroTemporal("FAC"),
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
                ImporteBruto = venta.ImporteBruto,
                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Impuesto = venta.Impuesto,
                CostoEnvioId = venta.CostoEnvioId,
                CostoEnvioNombreSnapshot = venta.CostoEnvioNombreSnapshot,
                CostoEnvioMontoSnapshot = venta.CostoEnvioMontoSnapshot,
                CostoEnvio = venta.CostoEnvio,
                EnvioExonerado = venta.EnvioExonerado,
                MotivoExoneracionEnvio = venta.MotivoExoneracionEnvio,
                Total = venta.Total,
                SaldoPendiente = venta.Total,
                Detalles = venta.Detalles.Select(d => new FacturaDetalle
                {
                    ProductoId = d.ProductoId,
                    ProductoVarianteId = d.ProductoVarianteId,
                    ProductoNombre = d.ProductoNombreSnapshot,
                    ProductoMarca = d.ProductoMarcaSnapshot,
                    ProductoModelo = d.ProductoModeloSnapshot,
                    VarianteColor = d.ProductoColorSnapshot,
                    VarianteSku = d.ProductoSkuSnapshot,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList()
            };

            await _facturaRepository.AddAsync(factura);
            await _facturaRepository.SaveChangesAsync();

            factura.NumeroFactura = $"FAC-{factura.Id:D6}";
            _facturaRepository.Update(factura);

            await _calculoService.RegistrarUsoVentaAsync(
                venta.Id, venta.ClienteId,
                venta.DescuentosAplicados.ToList(),
                venta.ImpuestosAplicados.ToList());

            await _ventaRepository.SaveChangesAsync();
        });

        var actualizada = await _ventaRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Confirmar, $"Venta confirmada: {actualizada!.NumeroVenta}", id);
        return ToDto(actualizada);
    }

    public async Task<VentaDto?> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var venta = await _ventaRepository.GetByIdForUpdateAsync(id);
            if (venta is null)
                throw new KeyNotFoundException($"Venta ID '{id}' no encontrada.");

            if (venta.Estado != EstadoDocumento.Confirmada)
                throw new BusinessRuleException("Solo se pueden anular ventas confirmadas.");

            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: false);
            var productosMap = inventario.Productos;
            var variantesMap = inventario.Variantes;

            foreach (var detalle in venta.Detalles)
            {
                if (!productosMap.TryGetValue(detalle.ProductoId, out var producto))
                    throw new BusinessRuleException($"El producto '{detalle.ProductoNombreSnapshot}' ya no existe.");

                var stockAnteriorMovimiento = producto.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad + detalle.Cantidad;

                if (detalle.ProductoVarianteId.HasValue)
                {
                    if (!variantesMap.TryGetValue(detalle.ProductoVarianteId.Value, out var variante))
                        throw new BusinessRuleException($"La variante de '{producto.Nombre}' ya no existe.");

                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad += detalle.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);

                    producto.Cantidad += detalle.Cantidad;
                    _productoRepository.Update(producto);
                }
                else
                {
                    producto.Cantidad += detalle.Cantidad;
                    _productoRepository.Update(producto);
                }

                await _movimientoInventarioRepository.AddAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = detalle.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    PrecioUnitario = detalle.PrecioUnitario,
                    CostoUnitario = detalle.CostoUnitarioSnapshot,
                    ReferenciaTipo = "VentaAnulada",
                    ReferenciaId = venta.Id,
                    Descripcion = $"Entrada por anulación de venta {venta.NumeroVenta}. Motivo: {motivo}",
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
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Anular,
            $"Venta anulada: {actualizada!.NumeroVenta}.", id, entidad: "Venta", motivo: motivo);
        return ToDto(actualizada);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return false;

        if (venta.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden eliminar ventas en estado Borrador.");

        venta.Detalles.Clear();
        _ventaRepository.Update(venta);
        var eliminado = await _ventaRepository.SaveChangesAsync();
        if (eliminado)
            await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Eliminar, $"Borrador de venta eliminado: {venta.NumeroVenta}", venta.Id);
        return eliminado;
    }

    private async Task VincularClienteAsync(Venta venta, CreateVentaDto dto)
    {
        Cliente? cliente = null;

        if (dto.ClienteId.HasValue)
        {
            cliente = await _clienteRepository.GetByIdAsync(dto.ClienteId.Value)
                ?? throw new BusinessRuleException("El cliente seleccionado no existe.");

            if (!cliente.Activo)
                throw new BusinessRuleException("El cliente seleccionado está inactivo.");
        }
        else if (DebeGestionarCliente(dto.ClienteNombre, dto.ClienteIdentidadORTN, dto.ClienteCorreo, dto.ClienteTelefono))
        {
            cliente = await _clienteRepository.BuscarCoincidenciaActivaAsync(
                dto.ClienteIdentidadORTN,
                dto.ClienteCorreo,
                dto.ClienteTelefono,
                dto.ClienteNombre);

            if (cliente is null)
            {
                int tipoClienteId = await _predeterminadoResolver.ResolverIdPredeterminadoAsync();

                cliente = new Cliente
                {
                    Nombre = dto.ClienteNombre.Trim(),
                    Telefono = dto.ClienteTelefono,
                    IdentidadORTN = dto.ClienteIdentidadORTN,
                    Correo = dto.ClienteCorreo,
                    Direccion = dto.ClienteDireccion,
                    Activo = true,
                    TipoClienteId = tipoClienteId,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                await _clienteRepository.AddAsync(cliente);
            }
        }

        if (cliente is null)
        {
            venta.ClienteId = null;
            venta.Cliente = null;
            venta.ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre.Trim();
            return;
        }

        venta.Cliente = cliente;
        venta.ClienteId = cliente.Id == 0 ? null : cliente.Id;
        venta.ClienteNombre = cliente.Nombre;
        venta.ClienteTelefono = cliente.Telefono;
        venta.ClienteIdentidadORTN = cliente.IdentidadORTN;
        venta.ClienteCorreo = cliente.Correo;
        venta.ClienteDireccion = cliente.Direccion;
    }

    private static bool DebeGestionarCliente(string? nombre, string? identidad, string? correo, string? telefono)
    {
        var nombreNormalizado = nombre?.Trim().ToLower() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(identidad)
            || !string.IsNullOrWhiteSpace(correo)
            || !string.IsNullOrWhiteSpace(telefono)
            || (!string.IsNullOrWhiteSpace(nombreNormalizado) && nombreNormalizado != "cliente final");
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

            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }

            var costoUnitario = variante?.Costo ?? producto.Costo;
            var subtotal = input.Cantidad * input.PrecioUnitario;
            var costoTotal = input.Cantidad * costoUnitario;

            venta.Detalles.Add(new VentaDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante?.Id,
                Cantidad = input.Cantidad,
                PrecioUnitario = input.PrecioUnitario,
                CostoUnitarioSnapshot = costoUnitario,
                Subtotal = subtotal,
                UtilidadBruta = subtotal - costoTotal,
                ProductoNombreSnapshot = producto.Nombre,
                ProductoMarcaSnapshot = producto.Marca,
                ProductoModeloSnapshot = producto.Modelo,
                ProductoColorSnapshot = variante?.Color?.Nombre,
                ProductoSkuSnapshot = variante?.Sku
            });
        }
    }

    private async Task<ProductoVariante> ObtenerVarianteAsync(int varianteId, int productoId, bool exigirActiva)
    {
        var variante = await _productoVarianteRepository.GetByIdAsync(varianteId)
            ?? throw new BusinessRuleException("La variante seleccionada no existe.");
        if (variante.ProductoId != productoId)
            throw new BusinessRuleException("La variante seleccionada no pertenece al producto indicado.");
        if (exigirActiva && !variante.Activo)
            throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva.");
        return variante;
    }

    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularVentaRequest request)
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

        return await _calculoService.CalcularVentaAsync(entradas, request.ClienteId, _currentUser.RolId, request.CodigoPromocional, request.CostoEnvioId, request.EnvioExonerado, request.MotivoExoneracionEnvio);
    }

    private async Task CalcularTotalesAsync(Venta venta, string? codigoPromocional, int? costoEnvioId, bool envioExonerado, string? motivoExoneracionEnvio)
    {
        var entradas = venta.Detalles.Select(d => new DetalleCalculoInput
        {
            ProductoId = d.ProductoId,
            CategoriaId = null,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario
        }).ToList();

        foreach (var entrada in entradas)
        {
            var producto = await _productoRepository.GetByIdAsync(entrada.ProductoId);
            entrada.CategoriaId = producto?.CategoriaId;
        }

        var resultado = await _calculoService.CalcularVentaAsync(
            entradas, venta.ClienteId, _currentUser.RolId, codigoPromocional, costoEnvioId, envioExonerado, motivoExoneracionEnvio);

        venta.ImporteBruto = resultado.ImporteBruto;
        venta.ImporteProductos = resultado.ImporteProductos;
        venta.Subtotal = resultado.Subtotal;
        venta.Descuento = resultado.TotalDescuento;
        venta.Impuesto = resultado.TotalImpuesto;
        venta.CostoEnvioId = resultado.CostoEnvioId;
        venta.CostoEnvioNombreSnapshot = resultado.CostoEnvioNombre;
        venta.CostoEnvioMontoSnapshot = resultado.CostoEnvio;
        venta.CostoEnvio = resultado.CostoEnvio;
        venta.EnvioExonerado = resultado.EnvioExonerado;
        venta.MotivoExoneracionEnvio = resultado.MotivoExoneracionEnvio;
        venta.Total = resultado.Total;
        venta.CostoTotal = venta.Detalles.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad);
        venta.UtilidadBruta = venta.Detalles.Sum(d => d.UtilidadBruta) - venta.Descuento;

        venta.DescuentosAplicados = resultado.DescuentosAplicados.Select(d => new VentaDescuento
        {
            DescuentoId = d.DescuentoId,
            DescuentoNombreSnapshot = d.Nombre,
            DescuentoCodigoSnapshot = d.Codigo ?? string.Empty,
            TipoSnapshot = Enum.Parse<TipoDescuento>(d.Tipo),
            ValorSnapshot = d.Valor,
            MontoAplicado = d.Monto
        }).ToList();

        venta.ImpuestosAplicados = resultado.ImpuestosAplicados.Select(i => new VentaImpuesto
        {
            ImpuestoId = i.ImpuestoId,
            ImpuestoNombreSnapshot = i.Nombre,
            ImpuestoCodigoSnapshot = i.Codigo,
            TasaSnapshot = i.Tasa,
            BaseImponible = i.BaseImponible,
            MontoAplicado = i.Monto
        }).ToList();

        if (venta.Total < 0)
            throw new BusinessRuleException("El total de la venta no puede ser negativo.");
    }

    private static string CrearNumeroTemporal(string prefijo)
    {
        var token = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return $"{prefijo}-TMP-{token}";
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum valorPorDefecto) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var resultado) ? resultado : valorPorDefecto;

    private static VentaDto ToDto(Venta v) => new()
    {
        Id = v.Id,
        NumeroVenta = v.NumeroVenta,
        Fecha = v.Fecha,
        ClienteId = v.ClienteId,
        ClienteNombre = v.ClienteNombre,
        ClienteTelefono = v.ClienteTelefono,
        ClienteIdentidadORTN = v.ClienteIdentidadORTN,
        ClienteCorreo = v.ClienteCorreo,
        ClienteDireccion = v.ClienteDireccion,
        Estado = v.Estado.ToString(),
        EstadoPago = v.EstadoPago.ToString(),
        MetodoPago = v.MetodoPago.ToString(),
        ImporteBruto = v.ImporteBruto,
        ImporteProductos = v.ImporteProductos,
        Subtotal = v.Subtotal,
        Descuento = v.Descuento,
        Impuesto = v.Impuesto,
        CostoEnvio = v.CostoEnvio,
        CostoEnvioId = v.CostoEnvioId,
        CostoEnvioNombre = v.CostoEnvioNombreSnapshot,
        EnvioExonerado = v.EnvioExonerado,
        MotivoExoneracionEnvio = v.MotivoExoneracionEnvio,
        Total = v.Total,
        CostoTotal = v.CostoTotal,
        UtilidadBruta = v.UtilidadBruta,
        Notas = v.Notas,
        Detalles = v.Detalles.Select(d => new VentaDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            ProductoNombre = d.ProductoNombreSnapshot,
            ProductoMarca = d.ProductoMarcaSnapshot,
            ProductoModelo = d.ProductoModeloSnapshot,
            ProductoColor = d.ProductoColorSnapshot,
            ProductoSku = d.ProductoSkuSnapshot,
            ProductoImagenPrincipalUrl = d.Producto?.ImagenPrincipal?.Url,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Subtotal,
            UtilidadBruta = d.UtilidadBruta
        }).ToList(),
        DescuentosAplicados = v.DescuentosAplicados.Select(d => new DescuentoAplicadoDto
        {
            DescuentoId = d.DescuentoId,
            Nombre = d.DescuentoNombreSnapshot,
            Codigo = d.DescuentoCodigoSnapshot,
            Tipo = d.TipoSnapshot.ToString(),
            Valor = d.ValorSnapshot,
            Monto = d.MontoAplicado
        }).ToList(),
        ImpuestosAplicados = v.ImpuestosAplicados.Select(i => new ImpuestoAplicadoDto
        {
            ImpuestoId = i.ImpuestoId,
            Nombre = i.ImpuestoNombreSnapshot,
            Codigo = i.ImpuestoCodigoSnapshot,
            Tasa = i.TasaSnapshot,
            BaseImponible = i.BaseImponible,
            Monto = i.MontoAplicado
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
