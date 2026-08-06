from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, content: str) -> None:
    (ROOT / path).write_text(content, encoding="utf-8")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: se esperaba 1 coincidencia y se encontraron {count}")
    return text.replace(old, new, 1)


def replace_between(text: str, start: str, end: str, replacement: str, label: str) -> str:
    i = text.find(start)
    if i < 0:
        raise RuntimeError(f"{label}: inicio no encontrado")
    j = text.find(end, i)
    if j < 0:
        raise RuntimeError(f"{label}: final no encontrado")
    return text[:i] + replacement + text[j:]


dto_path = "backend/src/Application/DTOs/CargaMasivaDto.cs"
dto = read(dto_path)
dto = replace_once(
    dto,
    '''    public Dictionary<string, string?> Datos { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Mensajes { get; set; } = new();''',
    '''    public Dictionary<string, string?> Datos { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Mensajes { get; set; } = new();

    // Snapshot interno utilizado para impedir que una confirmación sobrescriba
    // cambios de inventario realizados después de validar el archivo.
    public int? ProductoIdSnapshot { get; set; }
    public int? ProductoVarianteIdSnapshot { get; set; }
    public int? CantidadActualSnapshot { get; set; }
    public DateTime? FechaValidacionSnapshot { get; set; }''',
    "propiedades snapshot")
write(dto_path, dto)

service_path = "backend/src/Infrastructure/Services/CargaMasivaService.cs"
service = read(service_path)

service = replace_once(
    service,
    '''        var variantesConMovimientos = await _db.MovimientosInventario.AsNoTracking()
            .Where(x => x.ProductoVarianteId.HasValue)
            .Select(x => x.ProductoVarianteId!.Value)
            .Distinct()
            .ToListAsync(ct);
''',
    "",
    "eliminar restricción por historial")

service = replace_once(
    service,
    '''            var existente = existentePorSku ?? existentePorColor;
            if (existente is not null && variantesConMovimientos.Contains(existente.Id) && Entero(fila, "Cantidad") != existente.Cantidad)
                AgregarError(errores, fila.NumeroFila, "Cantidad", "STOCK_CON_HISTORIAL", "No se puede reemplazar por carga masiva el stock de una variante que ya tiene movimientos. Usa un movimiento de inventario.", V(fila, "Cantidad"));

            var clave = $"{producto.Id}|{color.Id}|{NormalizarClave(sku)}";''',
    '''            var existente = existentePorSku ?? existentePorColor;
            fila.ProductoIdSnapshot = producto.Id;
            fila.ProductoVarianteIdSnapshot = existente?.Id;
            fila.CantidadActualSnapshot = existente?.Cantidad;
            fila.FechaValidacionSnapshot = DateTime.UtcNow;

            var clave = $"{producto.Id}|{color.Id}|{NormalizarClave(sku)}";''',
    "capturar snapshot variante")

confirmar_start = '''    public async Task<CargaMasivaDetalleDto> ConfirmarAsync(int id, CancellationToken cancellationToken = default)
    {'''
confirmar_end = '''    public async Task<ArchivoDescargableDto> DescargarErroresAsync'''
confirmar_new = '''    public async Task<CargaMasivaDetalleDto> ConfirmarAsync(int id, CancellationToken cancellationToken = default)
    {
        CargaMasiva? carga = null;
        List<CargaMasivaFilaDto> filas = new();
        var creados = 0;
        var actualizados = 0;
        var confirmadaAhora = false;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!_currentUser.EsAdministrador && !_currentUser.UsuarioId.HasValue)
                throw new ForbiddenAccessException("No tienes acceso a esta carga masiva.");

            if (_currentUser.EsAdministrador)
            {
                carga = await _db.CargasMasivas
                    .FromSqlInterpolated($"SELECT c.* FROM CargasMasivas c WHERE c.Id = {id} FOR UPDATE")
                    .AsTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else
            {
                var usuarioId = _currentUser.UsuarioId!.Value;
                carga = await _db.CargasMasivas
                    .FromSqlInterpolated($"SELECT c.* FROM CargasMasivas c WHERE c.Id = {id} AND c.CreadoPorUsuarioId = {usuarioId} FOR UPDATE")
                    .AsTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }

            if (carga is null)
            {
                var existe = await _db.CargasMasivas
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == id, cancellationToken);
                if (existe)
                    throw new ForbiddenAccessException("No tienes acceso a esta carga masiva.");
                throw new BusinessRuleException("La carga masiva no existe.");
            }

            await _db.Entry(carga)
                .Collection(x => x.Errores)
                .LoadAsync(cancellationToken);

            filas = DeserializarFilas(carga.DatosNormalizadosJson);

            if (carga.Estado == EstadoCargaMasiva.Confirmada)
            {
                await transaction.CommitAsync(cancellationToken);
                return MapDetalle(
                    carga,
                    filas,
                    carga.Errores.Select(MapError).ToList());
            }

            if (carga.Estado != EstadoCargaMasiva.Validada || carga.FilasConError > 0)
                throw new BusinessRuleException("La carga contiene errores o no ha sido validada correctamente.");
            if (filas.Count == 0 || filas.Any(x => !x.EsValida))
                throw new BusinessRuleException("La vista previa validada no contiene filas confirmables.");

            (creados, actualizados) = carga.Tipo switch
            {
                TipoCargaMasiva.Clientes => await AplicarClientesAsync(filas, cancellationToken),
                TipoCargaMasiva.Proveedores => await AplicarProveedoresAsync(filas, cancellationToken),
                TipoCargaMasiva.Colores => await AplicarColoresAsync(filas, cancellationToken),
                TipoCargaMasiva.Productos => await AplicarProductosAsync(filas, cancellationToken),
                TipoCargaMasiva.VariantesInventario => await AplicarVariantesAsync(carga.Id, filas, cancellationToken),
                _ => throw new BusinessRuleException("El tipo de carga no es válido.")
            };

            carga.Estado = EstadoCargaMasiva.Confirmada;
            carga.FilasProcesadas = filas.Count;
            carga.RegistrosCreados = creados;
            carga.RegistrosActualizados = actualizados;
            carga.FechaConfirmacion = DateTime.UtcNow;
            carga.ConfirmadoPorUsuarioId = _currentUser.UsuarioId;
            carga.ConfirmadoPorNombreUsuario = _currentUser.NombreUsuario;
            carga.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            carga.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            carga.FechaActualizacion = DateTime.UtcNow;
            carga.ErrorGeneral = null;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            confirmadaAhora = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();

            if (carga is not null && carga.Estado != EstadoCargaMasiva.Confirmada)
            {
                var cargaFallida = await _db.CargasMasivas
                    .FirstAsync(x => x.Id == id, cancellationToken);
                cargaFallida.Estado = EstadoCargaMasiva.Fallida;
                cargaFallida.ErrorGeneral =
                    "La confirmación fue revertida completamente. Revalida el archivo antes de intentar nuevamente.";
                cargaFallida.FechaActualizacion = DateTime.UtcNow;
                cargaFallida.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                cargaFallida.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                await _db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogError(ex, "Falló la confirmación transaccional de la carga masiva {CargaId}", id);

            if (carga is not null)
            {
                await _auditoria.RegistrarAsync(
                    ModuloSistema.CargasMasivas,
                    AccionPermiso.Confirmar,
                    $"Falló la confirmación de la carga masiva #{id}; la transacción fue revertida.",
                    id,
                    entidad: "CargaMasiva",
                    resultado: "Error",
                    error: "Confirmación transaccional revertida");
            }

            if (ex is BusinessRuleException or ForbiddenAccessException)
                throw;

            throw new BusinessRuleException(
                "La importación no pudo confirmarse y ningún cambio fue aplicado. Revalida el archivo.");
        }

        if (confirmadaAhora)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.CargasMasivas,
                AccionPermiso.Confirmar,
                $"Confirmó la carga masiva #{id}: {creados} registros creados y {actualizados} actualizados.",
                id,
                entidad: "CargaMasiva",
                valoresNuevos: new
                {
                    Tipo = carga!.Tipo,
                    FilasProcesadas = filas.Count,
                    RegistrosCreados = creados,
                    RegistrosActualizados = actualizados
                });
        }

        return await GetByIdAsync(id)
            ?? throw new BusinessRuleException("No se pudo recuperar la carga confirmada.");
    }

'''
service = replace_between(service, confirmar_start, confirmar_end, confirmar_new, "confirmación bloqueada")

aplicar_start = '''    private async Task<(int Creados, int Actualizados)> AplicarVariantesAsync'''
aplicar_end = '''    private static CargaMasivaDto MapResumenExpression'''
aplicar_new = '''    private async Task<(int Creados, int Actualizados)> AplicarVariantesAsync(
        int cargaId,
        List<CargaMasivaFilaDto> filas,
        CancellationToken ct)
    {
        if (_db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La confirmación de variantes requiere una transacción activa.");

        if (filas.Any(x => !x.ProductoIdSnapshot.HasValue))
            throw new BusinessRuleException(
                "La carga no contiene snapshots completos. Valida el archivo nuevamente.");

        var productoIds = filas
            .Select(x => x.ProductoIdSnapshot!.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var varianteIds = filas
            .Where(x => x.ProductoVarianteIdSnapshot.HasValue)
            .Select(x => x.ProductoVarianteIdSnapshot!.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var productos = new Dictionary<int, Producto>();
        foreach (var productoId in productoIds)
        {
            var producto = await _db.Productos
                .FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {productoId} AND p.Eliminado = 0 FOR UPDATE")
                .AsTracking()
                .SingleOrDefaultAsync(ct)
                ?? throw new BusinessRuleException(
                    $"El producto ID '{productoId}' ya no existe. Revalida el archivo.");
            productos.Add(producto.Id, producto);
        }

        var variantes = new Dictionary<int, ProductoVariante>();
        foreach (var varianteId in varianteIds)
        {
            var variante = await _db.ProductoVariantes
                .FromSqlInterpolated($"SELECT v.* FROM ProductoVariantes v WHERE v.Id = {varianteId} AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking()
                .SingleOrDefaultAsync(ct)
                ?? throw new BusinessRuleException(
                    $"La variante ID '{varianteId}' ya no existe. Revalida el archivo.");
            variantes.Add(variante.Id, variante);
        }

        var colores = await _db.CatalogosProducto
            .Where(x => x.Tipo == TipoCatalogoProducto.Color && x.Activo && !x.Eliminado)
            .ToListAsync(ct);

        var movimientos = new List<(Producto Producto, ProductoVariante Variante, CargaMasivaFilaDto Fila, int Anterior, int Nueva)>();
        var productosAfectados = new HashSet<int>();
        var creados = 0;
        var actualizados = 0;

        foreach (var fila in filas.OrderBy(x => x.ProductoIdSnapshot).ThenBy(x => x.ProductoVarianteIdSnapshot))
        {
            var producto = productos[fila.ProductoIdSnapshot!.Value];
            var color = colores.FirstOrDefault(
                x => NormalizarClave(x.Nombre) == NormalizarClave(V(fila, "Color")))
                ?? throw new BusinessRuleException(
                    "Uno de los colores dejó de estar disponible. Revalida el archivo.");
            var sku = V(fila, "SKU")!;
            var codigoBarras = NuloSiVacio(V(fila, "CodigoBarras"));
            ProductoVariante variante;
            int cantidadAnterior;

            if (fila.ProductoVarianteIdSnapshot.HasValue)
            {
                variante = variantes[fila.ProductoVarianteIdSnapshot.Value];
                if (variante.ProductoId != producto.Id)
                    throw new BusinessRuleException("La variante cambió de producto. Revalida el archivo.");
                if (!fila.CantidadActualSnapshot.HasValue ||
                    variante.Cantidad != fila.CantidadActualSnapshot.Value)
                {
                    throw new BusinessRuleException(
                        "El inventario cambió después de validar el archivo. Revalida la carga antes de confirmarla.");
                }

                cantidadAnterior = variante.Cantidad;
                actualizados++;
            }
            else
            {
                var conflicto = await _db.ProductoVariantes
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(x => !x.Eliminado &&
                        (x.Sku == sku ||
                         (x.ProductoId == producto.Id && x.ColorId == color.Id) ||
                         (codigoBarras != null && x.CodigoBarras == codigoBarras)), ct);
                if (conflicto)
                {
                    throw new BusinessRuleException(
                        "Una variante fue creada o modificada después de validar el archivo. Revalida la carga.");
                }

                variante = new ProductoVariante
                {
                    ProductoId = producto.Id,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                _db.ProductoVariantes.Add(variante);
                cantidadAnterior = 0;
                creados++;
            }

            var cantidadNueva = Entero(fila, "Cantidad");
            variante.ColorId = color.Id;
            variante.Sku = sku;
            variante.CodigoBarras = codigoBarras;
            variante.Cantidad = cantidadNueva;
            variante.UmbralStockBajo = Entero(fila, "UmbralStockBajo");
            variante.Costo = Decimal(fila, "Costo");
            variante.Precio = Decimal(fila, "Precio");
            variante.Activo = Booleano(fila, "Activo");
            variante.Eliminado = false;
            variante.FechaEliminacion = null;
            variante.EliminadoPorUsuarioId = null;
            MarcarActualizacion(variante);

            movimientos.Add((producto, variante, fila, cantidadAnterior, cantidadNueva));
            productosAfectados.Add(producto.Id);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var item in movimientos.Where(x => x.Anterior != x.Nueva))
        {
            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = item.Producto.Id,
                ProductoVarianteId = item.Variante.Id,
                ProductoColorSnapshot = V(item.Fila, "Color"),
                ProductoSkuSnapshot = item.Variante.Sku,
                Tipo = TipoMovimientoInventario.Ajuste,
                Cantidad = Math.Abs(item.Nueva - item.Anterior),
                StockAnterior = item.Anterior,
                StockNuevo = item.Nueva,
                CostoUnitario = item.Variante.Costo,
                PrecioUnitario = item.Variante.Precio,
                ReferenciaTipo = "CargaMasiva",
                ReferenciaId = cargaId,
                Descripcion = $"Ajuste por carga masiva #{cargaId}",
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });
        }

        foreach (var producto in productos.Values.Where(x => productosAfectados.Contains(x.Id)))
        {
            var lista = await _db.ProductoVariantes
                .Where(x => x.ProductoId == producto.Id && !x.Eliminado)
                .ToListAsync(ct);
            var total = lista.Sum(x => x.Cantidad);
            producto.Cantidad = total;
            if (lista.Count > 0)
            {
                producto.Costo = total > 0
                    ? Math.Round(
                        lista.Sum(x => (x.Costo ?? 0m) * x.Cantidad) / total,
                        2,
                        MidpointRounding.AwayFromZero)
                    : lista.Average(x => x.Costo ?? producto.Costo);
                var activas = lista.Where(x => x.Activo).ToList();
                producto.Precio = (activas.Count > 0 ? activas : lista)
                    .Min(x => x.Precio ?? producto.Precio);
                producto.ColorId = lista.Count == 1 ? lista[0].ColorId : null;
            }
            MarcarActualizacion(producto);
        }

        return (creados, actualizados);
    }

'''
service = replace_between(service, aplicar_start, aplicar_end, aplicar_new, "aplicar variantes con snapshot")
write(service_path, service)

print("Carga masiva protegida con snapshots y locks.")
