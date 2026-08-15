from pathlib import Path
import re

service_path = Path('backend/src/Application/Services/AjusteInventarioService.cs')
dto_path = Path('backend/src/Application/DTOs/AjusteStockDto.cs')
service = service_path.read_text(encoding='utf-8-sig')
dto = dto_path.read_text(encoding='utf-8-sig')


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise SystemExit(f'{label}: se esperaba 1 coincidencia y se encontraron {count}.')
    return text.replace(old, new, 1)

# El adaptador legacy debe recibir contexto físico explícito; jamás inferir almacén.
dto = replace_once(
    dto,
    'public sealed class AjusteStockRequest\n{\n    public int CantidadActualEsperada { get; set; }',
    'public sealed class AjusteStockRequest\n{\n    public int AlmacenId { get; set; }\n    public int? UbicacionAlmacenId { get; set; }\n    public int CantidadActualEsperada { get; set; }',
    'DTO contexto físico')

service = replace_once(
    service,
    '                            ProductoId = productoId,\n                            ProductoVarianteId = varianteId,\n                            CantidadObjetivo = request.CantidadNueva',
    '                            ProductoId = productoId,\n                            ProductoVarianteId = varianteId,\n                            AlmacenId = request.AlmacenId,\n                            UbicacionAlmacenId = request.UbicacionAlmacenId,\n                            CantidadObjetivo = request.CantidadNueva',
    'compatibilidad -> detalle físico')

anular_pattern = re.compile(
    r'    public async Task<AjusteInventarioDto\?> AnularAsync\(int id, string motivoAnulacion\)\n    \{.*?\n    \}\n\n    private async Task<AjusteInventario> CrearBorradorInternoAsync',
    re.S)
anular_match = anular_pattern.search(service)
if not anular_match:
    raise SystemExit('No se encontró el bloque AnularAsync.')

anular_new = '''    public async Task<AjusteInventarioDto?> AnularAsync(int id, string motivoAnulacion)
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

            var cutover = CrearCutoverExistencias();
            var existencias = await cutover.BloquearParaReversionAsync(ajuste.Detalles);

            // Bridge de compatibilidad: estas filas se bloquean únicamente para mantener
            // Producto.Cantidad/ProductoVariante.Cantidad como proyección agregada.
            // Nunca se leen para decidir stock, snapshots ni viabilidad de la reversión.
            var lockRequest = ajuste.Detalles
                .OrderBy(d => d.ProductoId)
                .ThenBy(d => d.ProductoVarianteId)
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, 1))
                .ToList();
            var inventarioLegacy = await _inventarioConcurrency.BloquearInventarioParaReversionAsync(lockRequest);

            foreach (var detalle in ajuste.Detalles
                         .OrderBy(d => d.ProductoVarianteId)
                         .ThenBy(d => d.AlmacenId)
                         .ThenBy(d => d.UbicacionAlmacenId))
            {
                if (!inventarioLegacy.Productos.TryGetValue(detalle.ProductoId, out var producto))
                    throw new BusinessRuleException($"El producto ID '{detalle.ProductoId}' ya no existe físicamente.");
                if (!detalle.ProductoVarianteId.HasValue ||
                    !inventarioLegacy.Variantes.TryGetValue(detalle.ProductoVarianteId.Value, out var variante))
                {
                    throw new BusinessRuleException(
                        "N1.4 requiere una variante concreta para revertir existencias de forma segura.");
                }
                if (variante.ProductoId != detalle.ProductoId)
                    throw new BusinessRuleException("La variante histórica ya no pertenece al producto del ajuste.");

                var costoUnitario = detalle.CostoUnitarioSnapshot
                    ?? throw new BusinessRuleException("El ajuste no contiene costo histórico válido para revertir.");
                var transicion = await cutover.AplicarReversionConSnapshotAsync(existencias, detalle);

                SincronizarProyeccionLegacy(producto, variante, transicion.Diferencia, ajuste.NumeroAjuste);

                await _movimientoInventarioRepository.AddConOrigenTipadoAsync(
                    new MovimientoInventario
                    {
                        ProductoId = detalle.ProductoId,
                        ProductoVarianteId = detalle.ProductoVarianteId,
                        AlmacenId = detalle.AlmacenId,
                        UbicacionAlmacenId = detalle.UbicacionAlmacenId,
                        ProductoColorSnapshot = detalle.ColorSnapshot,
                        ProductoSkuSnapshot = detalle.SkuSnapshot,
                        Tipo = TipoMovimientoInventario.Reversion,
                        Causa = CausaMovimientoInventario.AjusteManual,
                        Cantidad = Math.Abs(transicion.Diferencia),
                        StockAnterior = transicion.StockAnterior,
                        StockNuevo = transicion.StockNuevo,
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

    private async Task<AjusteInventario> CrearBorradorInternoAsync'''
service = service[:anular_match.start()] + anular_new + service[anular_match.end():]

confirm_pattern = re.compile(
    r'    private async Task ConfirmarInternoAsync\(\n        AjusteInventario ajuste,\n        int usuarioId,\n        string nombreUsuario,\n        IReadOnlyDictionary<\(int ProductoId, int\? ProductoVarianteId\), int>\? cantidadesEsperadas\)\n    \{.*?\n    \}\n\n    private async Task ReemplazarDetallesAsync',
    re.S)
confirm_match = confirm_pattern.search(service)
if not confirm_match:
    raise SystemExit('No se encontró el bloque ConfirmarInternoAsync.')

confirm_new = '''    private async Task ConfirmarInternoAsync(
        AjusteInventario ajuste,
        int usuarioId,
        string nombreUsuario,
        IReadOnlyDictionary<(int ProductoId, int? ProductoVarianteId), int>? cantidadesEsperadas)
    {
        if (ajuste.Estado != EstadoAjusteInventario.Borrador)
            throw new BusinessRuleException("Solo los ajustes en estado Borrador pueden confirmarse.");
        if (ajuste.Detalles.Count == 0)
            throw new BusinessRuleException("El ajuste debe contener al menos un detalle para confirmarse.");

        var cutover = CrearCutoverExistencias();
        var existencias = await cutover.BloquearParaConfirmacionAsync(ajuste.Detalles);

        // Bridge legacy bloqueado después de la autoridad física. Sus cantidades sólo
        // reciben el delta autoritativo y no participan en ninguna decisión de stock.
        var lockRequest = ajuste.Detalles
            .OrderBy(d => d.ProductoId)
            .ThenBy(d => d.ProductoVarianteId)
            .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, 1))
            .ToList();
        var inventarioLegacy = await _inventarioConcurrency.BloquearInventarioParaReversionAsync(lockRequest);

        var productosCompletos = new Dictionary<int, Producto>();
        foreach (var productoId in ajuste.Detalles.Select(d => d.ProductoId).Distinct().OrderBy(x => x))
        {
            productosCompletos[productoId] = await _productoRepository.GetByIdAsync(productoId)
                ?? throw new BusinessRuleException($"El producto ID '{productoId}' ya no está disponible para confirmar el ajuste.");
        }

        foreach (var detalle in ajuste.Detalles
                     .OrderBy(d => d.ProductoVarianteId)
                     .ThenBy(d => d.AlmacenId)
                     .ThenBy(d => d.UbicacionAlmacenId))
        {
            if (!inventarioLegacy.Productos.TryGetValue(detalle.ProductoId, out var producto))
                throw new BusinessRuleException($"El producto ID '{detalle.ProductoId}' ya no existe físicamente.");
            if (producto.Eliminado)
                throw new BusinessRuleException($"El producto '{producto.Nombre}' fue eliminado y no puede ajustarse.");
            if (!detalle.ProductoVarianteId.HasValue ||
                !inventarioLegacy.Variantes.TryGetValue(detalle.ProductoVarianteId.Value, out var variante))
            {
                throw new BusinessRuleException(
                    "N1.4 requiere una variante concreta para ajustar existencias de forma segura.");
            }
            if (variante.ProductoId != detalle.ProductoId)
                throw new BusinessRuleException("La variante indicada ya no pertenece al producto del ajuste.");
            if (variante.Eliminado)
                throw new BusinessRuleException($"La variante '{variante.Sku}' fue eliminada y no puede ajustarse.");

            var productoCompleto = productosCompletos[detalle.ProductoId];
            var existencia = AjusteInventarioExistenciaStock.ObtenerExistencia(existencias, detalle);
            var cantidadAnterior = existencia.StockFisico;
            var costoUnitario = variante.Costo ?? producto.Costo;

            var key = (detalle.ProductoId, detalle.ProductoVarianteId);
            if (cantidadesEsperadas is not null &&
                cantidadesEsperadas.TryGetValue(key, out var cantidadEsperada) &&
                cantidadEsperada != cantidadAnterior)
            {
                throw new BusinessRuleException(
                    $"El stock físico autoritativo cambió desde la lectura del cliente. Esperado: {cantidadEsperada}; actual: {cantidadAnterior}. Actualiza la información y vuelve a intentar.");
            }

            detalle.MaterializarConfirmacion(cantidadAnterior, costoUnitario);
            AplicarSnapshotsIdentidad(detalle, productoCompleto, variante);

            var transicion = await cutover.AplicarConfirmacionConSnapshotAsync(existencias, detalle);
            SincronizarProyeccionLegacy(producto, variante, transicion.Diferencia, ajuste.NumeroAjuste);

            await _movimientoInventarioRepository.AddConOrigenTipadoAsync(
                new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId,
                    ProductoVarianteId = detalle.ProductoVarianteId,
                    AlmacenId = detalle.AlmacenId,
                    UbicacionAlmacenId = detalle.UbicacionAlmacenId,
                    ProductoColorSnapshot = detalle.ColorSnapshot,
                    ProductoSkuSnapshot = detalle.SkuSnapshot,
                    Tipo = TipoMovimientoInventario.Ajuste,
                    Causa = CausaMovimientoInventario.AjusteManual,
                    Cantidad = Math.Abs(transicion.Diferencia),
                    StockAnterior = transicion.StockAnterior,
                    StockNuevo = transicion.StockNuevo,
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

    private async Task ReemplazarDetallesAsync'''
service = service[:confirm_match.start()] + confirm_new + service[confirm_match.end():]

service = replace_once(
    service,
    '        if (detalles.Any(d => d.ProductoId <= 0 || d.ProductoVarianteId <= 0 || d.CantidadObjetivo < 0))\n            throw new BusinessRuleException("Cada línea debe indicar producto/variante válidos y una cantidad objetivo no negativa.");',
    '        if (detalles.Any(d =>\n                d.ProductoId <= 0 ||\n                !d.ProductoVarianteId.HasValue || d.ProductoVarianteId.Value <= 0 ||\n                d.AlmacenId <= 0 ||\n                (d.UbicacionAlmacenId.HasValue && d.UbicacionAlmacenId.Value <= 0) ||\n                d.CantidadObjetivo < 0))\n        {\n            throw new BusinessRuleException(\n                "Cada línea debe indicar producto, variante y almacén válidos, ubicación positiva cuando aplique y una cantidad objetivo no negativa.");\n        }',
    'ValidarCabecera contexto físico')

service = replace_once(
    service,
    '        if (productoId <= 0 || varianteId <= 0)\n            throw new BusinessRuleException("El producto o la variante indicada no es válida.");',
    '        if (productoId <= 0 || !varianteId.HasValue || varianteId.Value <= 0)\n            throw new BusinessRuleException("N1.4 requiere producto y variante concretos para ajustar stock.");\n        if (request.AlmacenId <= 0 ||\n            (request.UbicacionAlmacenId.HasValue && request.UbicacionAlmacenId.Value <= 0))\n        {\n            throw new BusinessRuleException(\n                "El ajuste debe indicar un almacén válido y una ubicación positiva cuando aplique; no se infiere contexto físico.");\n        }',
    'ValidarSolicitudCompatibilidad contexto físico')

insert_anchor = '    private (int UsuarioId, string NombreUsuario) ObtenerUsuarioActual()\n'
if insert_anchor not in service:
    raise SystemExit('No se encontró anchor para helpers N1.4.')
helper = '''    private AjusteInventarioExistenciaCutoverService CrearCutoverExistencias() =>
        new(_existenciaVarianteConcurrency
            ?? throw new InvalidOperationException(
                "N1.4.D requiere IExistenciaVarianteConcurrencyService para operar stock autoritativo."));

    private void SincronizarProyeccionLegacy(
        Producto producto,
        ProductoVariante variante,
        int diferenciaAutoritativa,
        string referencia)
    {
        int nuevaVariante;
        int nuevoProducto;
        try
        {
            nuevaVariante = checked(variante.Cantidad + diferenciaAutoritativa);
            nuevoProducto = checked(producto.Cantidad + diferenciaAutoritativa);
        }
        catch (OverflowException ex)
        {
            throw new BusinessRuleException(
                $"La proyección legacy de {referencia} excede el rango soportado: {ex.Message}");
        }

        if (nuevaVariante < 0 || nuevoProducto < 0)
        {
            throw new BusinessRuleException(
                $"La proyección legacy de {referencia} está inconsistente con ExistenciaVariante y no puede sincronizarse de forma segura.");
        }

        variante.Cantidad = nuevaVariante;
        producto.Cantidad = nuevoProducto;
        _productoVarianteRepository.Update(variante);
        _productoRepository.Update(producto);
    }

'''
service = service.replace(insert_anchor, helper + insert_anchor, 1)

# Garantía: ningún snapshot/movimiento autoritativo debe usar Cantidad legacy como origen.
for forbidden in (
    'cantidadAnterior = variante.Cantidad;',
    'cantidadAnterior = producto.Cantidad;',
    'stockAnteriorReversion = variante.Cantidad;',
    'stockAnteriorReversion = producto.Cantidad;'):
    if forbidden in service:
        raise SystemExit(f'Quedó una lectura decisoria legacy prohibida: {forbidden}')

service_path.write_text(service, encoding='utf-8')
dto_path.write_text(dto, encoding='utf-8')
print('N1.4.D AjusteInventario writer transformado correctamente.')
