using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class AjusteInventarioService : IAjusteInventarioService
{
    private readonly IAjusteInventarioRepository _repository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IInventarioConcurrencyService _inventarioConcurrency;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IExistenciaVarianteConcurrencyService? _existenciaVarianteConcurrency;

    public AjusteInventarioService(
        IAjusteInventarioRepository repository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IInventarioConcurrencyService inventarioConcurrency,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IExistenciaVarianteConcurrencyService? existenciaVarianteConcurrency = null)
    {
        _repository = repository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _inventarioConcurrency = inventarioConcurrency;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _existenciaVarianteConcurrency = existenciaVarianteConcurrency;
    }

    public async Task<List<AjusteInventarioDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<AjusteInventarioDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var ajuste = await _repository.GetByIdAsync(id);
        return ajuste is null ? null : ToDto(ajuste);
    }

    public async Task<AjusteInventarioDto> CreateAsync(CreateAjusteInventarioDto dto)
    {
        ValidarCabecera(dto.Motivo, dto.Observaciones, dto.Detalles);
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        AjusteInventario? creado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            creado = await CrearBorradorInternoAsync(dto, usuarioId, nombreUsuario);
        });

        creado ??= await _repository.GetByIdAsync(creado?.Id ?? 0)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste recién creado.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Crear,
            $"Ajuste de inventario creado como borrador: {creado.NumeroAjuste}",
            creado.Id,
            entidad: nameof(AjusteInventario));

        return ToDto(creado);
    }

    public async Task<AjusteInventarioDto?> UpdateAsync(int id, UpdateAjusteInventarioDto dto)
    {
        ValidarCabecera(dto.Motivo, dto.Observaciones, dto.Detalles);
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ajuste = await _repository.GetByIdForUpdateAsync(id);
            if (ajuste is null) return;
            encontrado = true;

            if (ajuste.Estado != EstadoAjusteInventario.Borrador)
                throw new BusinessRuleException("Solo los ajustes en estado Borrador pueden editarse.");

            ajuste.FechaAjuste = dto.FechaAjuste ?? ajuste.FechaAjuste;
            ajuste.Motivo = dto.Motivo.Trim();
            ajuste.Observaciones = NormalizarOpcional(dto.Observaciones);
            ajuste.ActualizadoPorUsuarioId = usuarioId;
            ajuste.ActualizadoPorNombreUsuario = nombreUsuario;
            ajuste.FechaActualizacion = DateTime.UtcNow;

            ajuste.Detalles.Clear();
            await ReemplazarDetallesAsync(ajuste, dto.Detalles);
            _repository.Update(ajuste);
            await _repository.SaveChangesAsync();
        });

        if (!encontrado) return null;

        var actualizado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste actualizado.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Editar,
            $"Ajuste de inventario actualizado: {actualizado.NumeroAjuste}",
            actualizado.Id,
            entidad: nameof(AjusteInventario));

        return ToDto(actualizado);
    }

    public async Task<AjusteInventarioDto?> ConfirmarAsync(int id)
    {
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ajuste = await _repository.GetByIdForUpdateAsync(id);
            if (ajuste is null) return;
            encontrado = true;
            await ConfirmarInternoAsync(ajuste, usuarioId, nombreUsuario, null);
        });

        if (!encontrado) return null;

        var confirmado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste confirmado.");

        return ToDto(confirmado);
    }

    public async Task<AjusteStockResultadoDto> AjustarStockCompatibilidadAsync(
        int productoId,
        int? varianteId,
        AjusteStockRequest request)
    {
        ValidarSolicitudCompatibilidad(productoId, varianteId, request);
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var motivo = request.Motivo.Trim();
        AjusteInventario? confirmado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            confirmado = await CrearBorradorInternoAsync(
                new CreateAjusteInventarioDto
                {
                    Motivo = motivo,
                    Observaciones = "Creado y confirmado atómicamente por adaptador de compatibilidad legacy.",
                    Detalles =
                    {
                        new AjusteInventarioDetalleInputDto
                        {
                            ProductoId = productoId,
                            ProductoVarianteId = varianteId,
                            CantidadObjetivo = request.CantidadNueva
                        }
                    }
                },
                usuarioId,
                nombreUsuario);

            var cantidadesEsperadas = new Dictionary<(int ProductoId, int? ProductoVarianteId), int>
            {
                [(productoId, varianteId)] = request.CantidadActualEsperada
            };

            await ConfirmarInternoAsync(
                confirmado,
                usuarioId,
                nombreUsuario,
                cantidadesEsperadas);
        });

        var ajuste = confirmado
            ?? throw new InvalidOperationException("No se pudo materializar el ajuste formal de compatibilidad.");
        var detalle = ajuste.Detalles.Single(d =>
            d.ProductoId == productoId && d.ProductoVarianteId == varianteId);

        var cantidadAnterior = detalle.CantidadAnteriorSnapshot
            ?? throw new InvalidOperationException("El ajuste confirmado no materializó el stock anterior.");
        var cantidadNueva = detalle.CantidadNuevaSnapshot
            ?? throw new InvalidOperationException("El ajuste confirmado no materializó el stock nuevo.");

        return new AjusteStockResultadoDto
        {
            ProductoId = productoId,
            ProductoVarianteId = varianteId,
            CantidadAnterior = cantidadAnterior,
            CantidadNueva = cantidadNueva,
            Diferencia = detalle.DiferenciaSnapshot ?? cantidadNueva - cantidadAnterior,
            Motivo = motivo
        };
    }

    public async Task<AjusteInventarioDto?> AnularAsync(int id, string motivoAnulacion)
    {
        if (string.IsNullOrWhiteSpace(motivoAnulacion) || motivoAnulacion.Trim().Length > 500)
            throw new BusinessRuleException("El motivo de anulación es obligatorio y no puede exceder 500 caracteres.");

        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var motivo = motivoAnulacion.Trim();
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ajuste = await _repository.GetByIdForUpdateAsync(id);
            if (ajuste is null) return;
            encontrado = true;

            if (ajuste.Estado != EstadoAjusteInventario.Confirmado)
                throw new BusinessRuleException("Solo los ajustes confirmados pueden anularse.");
            if (ajuste.Detalles.Any(d => !d.TieneSnapshotConfirmacion))
                throw new BusinessRuleException("El ajuste confirmado no posee snapshots íntegros y no puede anularse de forma segura.");

            var lockRequest = ajuste.Detalles
                .OrderBy(d => d.ProductoId)
                .ThenBy(d => d.ProductoVarianteId)
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, 1))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearInventarioParaReversionAsync(lockRequest);

            foreach (var detalle in ajuste.Detalles.OrderBy(d => d.ProductoId).ThenBy(d => d.ProductoVarianteId))
            {
                if (!inventario.Productos.TryGetValue(detalle.ProductoId, out var producto))
                    throw new BusinessRuleException($"El producto ID '{detalle.ProductoId}' ya no existe físicamente.");

                var diferenciaOriginal = detalle.DiferenciaSnapshot
                    ?? throw new BusinessRuleException("El ajuste no contiene una diferencia histórica válida para revertir.");
                var costoUnitario = detalle.CostoUnitarioSnapshot
                    ?? throw new BusinessRuleException("El ajuste no contiene costo histórico válido para revertir.");

                int stockAnteriorReversion;
                int stockNuevoReversion;
                ProductoVariante? variante = null;

                if (detalle.ProductoVarianteId.HasValue)
                {
                    if (!inventario.Variantes.TryGetValue(detalle.ProductoVarianteId.Value, out variante))
                        throw new BusinessRuleException($"La variante ID '{detalle.ProductoVarianteId.Value}' ya no existe físicamente.");

                    stockAnteriorReversion = variante.Cantidad;
                    stockNuevoReversion = stockAnteriorReversion - diferenciaOriginal;
                    if (stockNuevoReversion < 0)
                    {
                        throw new BusinessRuleException(
                            $"No es posible anular {ajuste.NumeroAjuste}: la reversión dejaría stock negativo en la variante '{detalle.SkuSnapshot ?? variante.Sku}'.");
                    }

                    variante.Cantidad = stockNuevoReversion;
                    _productoVarianteRepository.Update(variante);
                    producto.Cantidad -= diferenciaOriginal;
                }
                else
                {
                    stockAnteriorReversion = producto.Cantidad;
                    stockNuevoReversion = stockAnteriorReversion - diferenciaOriginal;
                    if (stockNuevoReversion < 0)
                    {
                        throw new BusinessRuleException(
                            $"No es posible anular {ajuste.NumeroAjuste}: la reversión dejaría stock negativo en '{detalle.NombreSnapshot ?? producto.Nombre}'.");
                    }

                    producto.Cantidad = stockNuevoReversion;
                }

                if (producto.Cantidad < 0)
                    throw new BusinessRuleException($"No es posible anular {ajuste.NumeroAjuste}: el stock consolidado quedaría negativo.");

                _productoRepository.Update(producto);

                await _movimientoInventarioRepository.AddConOrigenTipadoAsync(
                    new MovimientoInventario
                    {
                        ProductoId = detalle.ProductoId,
                        ProductoVarianteId = detalle.ProductoVarianteId,
                        ProductoColorSnapshot = detalle.ColorSnapshot,
                        ProductoSkuSnapshot = detalle.SkuSnapshot,
                        Tipo = TipoMovimientoInventario.Reversion,
                        Causa = CausaMovimientoInventario.AjusteManual,
                        Cantidad = Math.Abs(diferenciaOriginal),
                        StockAnterior = stockAnteriorReversion,
                        StockNuevo = stockNuevoReversion,
                        CostoUnitario = costoUnitario,
                        Descripcion = $"Reversión del ajuste {ajuste.NumeroAjuste}. Motivo: {motivo}",
                        CreadoPorUsuarioId = usuarioId,
                        CreadoPorNombreUsuario = nombreUsuario,
                        Fecha = DateTime.UtcNow
                    },
                    OrigenMovimientoInventario.DesdeAjusteInventario(ajuste.Id));
            }

            var ahora = DateTime.UtcNow;
            ajuste.Anular(usuarioId, nombreUsuario, motivo, ahora);
            ajuste.ActualizadoPorUsuarioId = usuarioId;
            ajuste.ActualizadoPorNombreUsuario = nombreUsuario;
            ajuste.FechaActualizacion = ahora;
            _repository.Update(ajuste);
            await _repository.SaveChangesAsync();

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Inventario,
                AccionPermiso.Anular,
                $"Ajuste de inventario anulado: {ajuste.NumeroAjuste}",
                ajuste.Id,
                entidad: nameof(AjusteInventario),
                motivo: motivo);
        });

        if (!encontrado) return null;

        var anulado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste anulado.");

        return ToDto(anulado);
    }

    private async Task<AjusteInventario> CrearBorradorInternoAsync(
        CreateAjusteInventarioDto dto,
        int usuarioId,
        string nombreUsuario)
    {
        ValidarCabecera(dto.Motivo, dto.Observaciones, dto.Detalles);
        var ahora = DateTime.UtcNow;
        var ajuste = new AjusteInventario
        {
            NumeroAjuste = $"TMP-{Guid.NewGuid():N}"[..20],
            FechaAjuste = dto.FechaAjuste ?? ahora,
            Motivo = dto.Motivo.Trim(),
            Observaciones = NormalizarOpcional(dto.Observaciones),
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = nombreUsuario,
            ActualizadoPorUsuarioId = usuarioId,
            ActualizadoPorNombreUsuario = nombreUsuario,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };

        await ReemplazarDetallesAsync(ajuste, dto.Detalles);
        await _repository.AddAsync(ajuste);
        await _repository.SaveChangesAsync();

        ajuste.NumeroAjuste = $"AI-{ajuste.Id:D6}";
        _repository.Update(ajuste);
        await _repository.SaveChangesAsync();
        return ajuste;
    }

    private async Task ConfirmarInternoAsync(
        AjusteInventario ajuste,
        int usuarioId,
        string nombreUsuario,
        IReadOnlyDictionary<(int ProductoId, int? ProductoVarianteId), int>? cantidadesEsperadas)
    {
        if (ajuste.Estado != EstadoAjusteInventario.Borrador)
            throw new BusinessRuleException("Solo los ajustes en estado Borrador pueden confirmarse.");
        if (ajuste.Detalles.Count == 0)
            throw new BusinessRuleException("El ajuste debe contener al menos un detalle para confirmarse.");

        var lockRequest = ajuste.Detalles
            .OrderBy(d => d.ProductoId)
            .ThenBy(d => d.ProductoVarianteId)
            .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, 1))
            .ToList();
        var inventario = await _inventarioConcurrency.BloquearInventarioParaReversionAsync(lockRequest);

        var productosCompletos = new Dictionary<int, Producto>();
        foreach (var productoId in ajuste.Detalles.Select(d => d.ProductoId).Distinct().OrderBy(x => x))
        {
            productosCompletos[productoId] = await _productoRepository.GetByIdAsync(productoId)
                ?? throw new BusinessRuleException($"El producto ID '{productoId}' ya no está disponible para confirmar el ajuste.");
        }

        foreach (var detalle in ajuste.Detalles.OrderBy(d => d.ProductoId).ThenBy(d => d.ProductoVarianteId))
        {
            if (!inventario.Productos.TryGetValue(detalle.ProductoId, out var producto))
                throw new BusinessRuleException($"El producto ID '{detalle.ProductoId}' ya no existe físicamente.");
            if (producto.Eliminado)
                throw new BusinessRuleException($"El producto '{producto.Nombre}' fue eliminado y no puede ajustarse.");

            var productoCompleto = productosCompletos[detalle.ProductoId];
            ProductoVariante? variante = null;
            int cantidadAnterior;
            decimal costoUnitario;

            if (detalle.ProductoVarianteId.HasValue)
            {
                if (!inventario.Variantes.TryGetValue(detalle.ProductoVarianteId.Value, out variante))
                    throw new BusinessRuleException($"La variante ID '{detalle.ProductoVarianteId.Value}' ya no existe físicamente.");
                if (variante.ProductoId != detalle.ProductoId)
                    throw new BusinessRuleException("La variante indicada ya no pertenece al producto del ajuste.");
                if (variante.Eliminado)
                    throw new BusinessRuleException($"La variante '{variante.Sku}' fue eliminada y no puede ajustarse.");

                cantidadAnterior = variante.Cantidad;
                costoUnitario = variante.Costo ?? producto.Costo;
            }
            else
            {
                var variantesOperativas = productoCompleto.Variantes.Where(v => !v.Eliminado).ToList();
                if (variantesOperativas.Count > 0)
                {
                    throw new BusinessRuleException(
                        $"El producto '{producto.Nombre}' posee variantes. El ajuste debe identificar una variante concreta.");
                }

                cantidadAnterior = producto.Cantidad;
                costoUnitario = producto.Costo;
            }

            var key = (detalle.ProductoId, detalle.ProductoVarianteId);
            if (cantidadesEsperadas is not null &&
                cantidadesEsperadas.TryGetValue(key, out var cantidadEsperada) &&
                cantidadEsperada != cantidadAnterior)
            {
                throw new BusinessRuleException(
                    $"El stock actual cambió desde la lectura del cliente. Esperado: {cantidadEsperada}; actual: {cantidadAnterior}. Actualiza la información y vuelve a intentar.");
            }

            if (detalle.CantidadObjetivo == cantidadAnterior)
            {
                throw new BusinessRuleException(
                    $"El detalle del producto '{producto.Nombre}' no produce una diferencia real de inventario.");
            }

            detalle.MaterializarConfirmacion(cantidadAnterior, costoUnitario);
            AplicarSnapshotsIdentidad(detalle, productoCompleto, variante);

            var diferencia = detalle.CantidadObjetivo - cantidadAnterior;
            if (variante is not null)
            {
                variante.Cantidad = detalle.CantidadObjetivo;
                _productoVarianteRepository.Update(variante);
                producto.Cantidad += diferencia;
            }
            else
            {
                producto.Cantidad = detalle.CantidadObjetivo;
            }
            _productoRepository.Update(producto);

            await _movimientoInventarioRepository.AddConOrigenTipadoAsync(
                new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId,
                    ProductoVarianteId = detalle.ProductoVarianteId,
                    ProductoColorSnapshot = detalle.ColorSnapshot,
                    ProductoSkuSnapshot = detalle.SkuSnapshot,
                    Tipo = TipoMovimientoInventario.Ajuste,
                    Causa = CausaMovimientoInventario.AjusteManual,
                    Cantidad = Math.Abs(diferencia),
                    StockAnterior = cantidadAnterior,
                    StockNuevo = detalle.CantidadObjetivo,
                    CostoUnitario = costoUnitario,
                    Descripcion = $"Ajuste formal de inventario {ajuste.NumeroAjuste}. Motivo: {ajuste.Motivo}",
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = nombreUsuario,
                    Fecha = DateTime.UtcNow
                },
                OrigenMovimientoInventario.DesdeAjusteInventario(ajuste.Id));
        }

        var ahora = DateTime.UtcNow;
        ajuste.Confirmar(usuarioId, nombreUsuario, ahora);
        ajuste.ActualizadoPorUsuarioId = usuarioId;
        ajuste.ActualizadoPorNombreUsuario = nombreUsuario;
        ajuste.FechaActualizacion = ahora;
        _repository.Update(ajuste);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Confirmar,
            $"Ajuste de inventario confirmado: {ajuste.NumeroAjuste}",
            ajuste.Id,
            entidad: nameof(AjusteInventario));
    }

    private async Task ReemplazarDetallesAsync(
        AjusteInventario ajuste,
        IReadOnlyCollection<AjusteInventarioDetalleInputDto> detalles)
    {
        var duplicado = detalles
            .GroupBy(d => (d.ProductoId, d.ProductoVarianteId, d.AlmacenId, d.UbicacionAlmacenId))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicado is not null)
            throw new BusinessRuleException("Cada producto/variante/almacén/ubicación puede aparecer una sola vez en el ajuste.");

        foreach (var entrada in detalles
            .OrderBy(d => d.ProductoId)
            .ThenBy(d => d.ProductoVarianteId)
            .ThenBy(d => d.AlmacenId)
            .ThenBy(d => d.UbicacionAlmacenId))
        {
            var producto = await _productoRepository.GetByIdAsync(entrada.ProductoId)
                ?? throw new BusinessRuleException($"El producto ID '{entrada.ProductoId}' no existe.");

            var variantes = (producto.Variantes ?? Array.Empty<ProductoVariante>())
                .Where(v => !v.Eliminado)
                .ToList();

            if (entrada.ProductoVarianteId.HasValue)
            {
                var variante = variantes.FirstOrDefault(v => v.Id == entrada.ProductoVarianteId.Value)
                    ?? throw new BusinessRuleException("La variante indicada no pertenece al producto seleccionado.");
            }
            else if (variantes.Count > 0)
            {
                throw new BusinessRuleException(
                    $"El producto '{producto.Nombre}' posee variantes. Selecciona la variante concreta que deseas ajustar.");
            }

            ajuste.Detalles.Add(new AjusteInventarioDetalle
            {
                ProductoId = entrada.ProductoId,
                ProductoVarianteId = entrada.ProductoVarianteId,
                AlmacenId = entrada.AlmacenId > 0 ? entrada.AlmacenId : null,
                UbicacionAlmacenId = entrada.UbicacionAlmacenId,
                CantidadObjetivo = entrada.CantidadObjetivo,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            });
        }
    }

    private static void ValidarCabecera(
        string motivo,
        string? observaciones,
        IReadOnlyCollection<AjusteInventarioDetalleInputDto> detalles)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500)
            throw new BusinessRuleException("El motivo es obligatorio y no puede exceder 500 caracteres.");
        if (observaciones?.Trim().Length > 1000)
            throw new BusinessRuleException("Las observaciones no pueden exceder 1000 caracteres.");
        if (detalles is null || detalles.Count == 0)
            throw new BusinessRuleException("El ajuste debe contener al menos un detalle.");
        if (detalles.Count > 200)
            throw new BusinessRuleException("El ajuste no puede contener más de 200 líneas.");
        if (detalles.Any(d => d.ProductoId <= 0 || d.ProductoVarianteId <= 0 || d.CantidadObjetivo < 0))
            throw new BusinessRuleException("Cada línea debe indicar producto/variante válidos y una cantidad objetivo no negativa.");
    }

    private static void ValidarSolicitudCompatibilidad(
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
    }

    private (int UsuarioId, string NombreUsuario) ObtenerUsuarioActual()
    {
        var usuarioId = _currentUser.UsuarioId;
        var nombreUsuario = _currentUser.NombreUsuario?.Trim();
        if (!usuarioId.HasValue || usuarioId.Value <= 0 || string.IsNullOrWhiteSpace(nombreUsuario))
            throw new BusinessRuleException("No fue posible identificar al usuario autenticado para registrar el ajuste.");
        return (usuarioId.Value, nombreUsuario);
    }

    private static void AplicarSnapshotsIdentidad(
        AjusteInventarioDetalle detalle,
        Producto producto,
        ProductoVariante? variante)
    {
        var varianteCompleta = detalle.ProductoVarianteId.HasValue
            ? producto.Variantes.FirstOrDefault(v => v.Id == detalle.ProductoVarianteId.Value)
            : null;

        detalle.NombreSnapshot = producto.Nombre;
        detalle.SkuSnapshot = varianteCompleta?.Sku ?? variante?.Sku;
        detalle.MarcaSnapshot = varianteCompleta?.Marca?.Nombre ?? producto.Marca;
        detalle.ModeloSnapshot = varianteCompleta?.Modelo?.Nombre ?? producto.Modelo;
        detalle.ColorSnapshot = varianteCompleta?.Color?.Nombre ?? producto.Color?.Nombre;
        detalle.TallaSnapshot = varianteCompleta?.Talla?.Nombre ?? producto.Talla?.Nombre;
        detalle.FechaActualizacion = DateTime.UtcNow;
    }

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AjusteInventarioDto ToDto(AjusteInventario ajuste) => new()
    {
        Id = ajuste.Id,
        NumeroAjuste = ajuste.NumeroAjuste,
        FechaAjuste = ajuste.FechaAjuste,
        Estado = ajuste.Estado.ToString(),
        Motivo = ajuste.Motivo,
        Observaciones = ajuste.Observaciones,
        FechaConfirmacion = ajuste.FechaConfirmacion,
        ConfirmadoPorNombreUsuario = ajuste.ConfirmadoPorNombreUsuario,
        FechaAnulacion = ajuste.FechaAnulacion,
        AnuladoPorNombreUsuario = ajuste.AnuladoPorNombreUsuario,
        MotivoAnulacion = ajuste.MotivoAnulacion,
        ImpactoCostoTotalSnapshot = ajuste.Detalles
            .Where(d => d.ImpactoCostoSnapshot.HasValue)
            .Sum(d => d.ImpactoCostoSnapshot ?? 0m),
        Detalles = ajuste.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new AjusteInventarioDetalleDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                AlmacenId = d.AlmacenId ?? 0,
                UbicacionAlmacenId = d.UbicacionAlmacenId,
                CantidadObjetivo = d.CantidadObjetivo,
                CantidadAnteriorSnapshot = d.CantidadAnteriorSnapshot,
                CantidadNuevaSnapshot = d.CantidadNuevaSnapshot,
                DiferenciaSnapshot = d.DiferenciaSnapshot,
                CostoUnitarioSnapshot = d.CostoUnitarioSnapshot,
                ImpactoCostoSnapshot = d.ImpactoCostoSnapshot,
                NombreSnapshot = d.NombreSnapshot,
                SkuSnapshot = d.SkuSnapshot,
                MarcaSnapshot = d.MarcaSnapshot,
                ModeloSnapshot = d.ModeloSnapshot,
                ColorSnapshot = d.ColorSnapshot,
                TallaSnapshot = d.TallaSnapshot
            })
            .ToList()
    };
}
