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


service_path = "backend/src/Infrastructure/Services/CargaMasivaService.cs"
service = read(service_path)
service = replace_once(
    service,
    '''    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;''',
    '''    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;''',
    "campo unit of work")
service = replace_once(
    service,
    '''    public CargaMasivaService(
        AppDbContext db,
        ICurrentUserService currentUser,''',
    '''    public CargaMasivaService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,''',
    "parámetro unit of work")
service = replace_once(
    service,
    '''    {
        _db = db;
        _currentUser = currentUser;''',
    '''    {
        _db = db;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;''',
    "asignación unit of work")

start = '''    public async Task<CargaMasivaDetalleDto> ConfirmarAsync(int id, CancellationToken cancellationToken = default)
    {'''
end = '''    public async Task<ArchivoDescargableDto> DescargarErroresAsync'''
replacement = r'''    public async Task<CargaMasivaDetalleDto> ConfirmarAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CargaMasiva? carga = null;
        List<CargaMasivaFilaDto> filas = new();
        var creados = 0;
        var actualizados = 0;
        var confirmadaAhora = false;
        var yaEstabaConfirmada = false;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Todos los valores y entidades se recargan en cada intento. Esto
                // es obligatorio porque UnitOfWork limpia el ChangeTracker después
                // de un 1205/1213 antes de repetir la transacción completa.
                carga = null;
                filas = new List<CargaMasivaFilaDto>();
                creados = 0;
                actualizados = 0;
                confirmadaAhora = false;
                yaEstabaConfirmada = false;

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
                    yaEstabaConfirmada = true;
                    return;
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
                confirmadaAhora = true;
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();

            // Esta actualización se ejecuta únicamente después de que UnitOfWork
            // agotó los reintentos transitorios y revirtió la transacción.
            try
            {
                var cargaFallida = await _db.CargasMasivas
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (cargaFallida is not null &&
                    cargaFallida.Estado != EstadoCargaMasiva.Confirmada)
                {
                    cargaFallida.Estado = EstadoCargaMasiva.Fallida;
                    cargaFallida.ErrorGeneral =
                        "La confirmación fue revertida completamente. Revalida el archivo antes de intentar nuevamente.";
                    cargaFallida.FechaActualizacion = DateTime.UtcNow;
                    cargaFallida.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                    cargaFallida.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception persistenciaEx)
            {
                _logger.LogError(
                    persistenciaEx,
                    "No se pudo registrar el estado fallido de la carga masiva {CargaId}",
                    id);
            }

            _logger.LogError(
                ex,
                "Falló la confirmación reintentable de la carga masiva {CargaId}",
                id);

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

        if (yaEstabaConfirmada)
        {
            return await GetByIdAsync(id)
                ?? throw new BusinessRuleException("No se pudo recuperar la carga confirmada.");
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
service = replace_between(service, start, end, replacement, "confirmación reintentable")
write(service_path, service)

# La prueba de integración construye el servicio manualmente.
test_path = "backend/tests/InventoryApp.Tests/CargaMasivaConcurrencyTests.cs"
test = read(test_path)
test = replace_once(
    test,
    '''        return new CargaMasivaService(
            context,
            CrearUsuarioActual().Object,''',
    '''        return new CargaMasivaService(
            context,
            new UnitOfWork(context),
            CrearUsuarioActual().Object,''',
    "constructor prueba carga")
write(test_path, test)

# Un ajuste sin diferencia no debe crear ruido contable ni de auditoría.
adjust_path = "backend/src/Application/Services/InventarioAjusteService.cs"
adjust = read(adjust_path)
adjust = replace_once(
    adjust,
    '''        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new BusinessRuleException("El motivo del ajuste de inventario es obligatorio.");

        var motivo = request.Motivo.Trim();''',
    '''        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new BusinessRuleException("El motivo del ajuste de inventario es obligatorio.");
        if (request.CantidadActualEsperada == request.CantidadNueva)
            throw new BusinessRuleException("La nueva cantidad debe ser diferente del stock actual.");

        var motivo = request.Motivo.Trim();''',
    "rechazar ajuste sin diferencia")
write(adjust_path, adjust)

adjust_test_path = "backend/tests/InventoryApp.Tests/InventarioAjusteServiceTests.cs"
adjust_test = read(adjust_test_path)
insert_marker = '''    [Theory]
    [InlineData(-1, 0, "motivo")]'''
new_test = '''    [Fact]
    public async Task AjustarProductoAsync_SinDiferencia_NoCreaMovimiento()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AjustarProductoAsync(10, new AjusteStockRequest
            {
                CantidadActualEsperada = 5,
                CantidadNueva = 5,
                Motivo = "Conteo físico"
            }));

        _concurrency.Verify(
            x => x.AjustarStockPesimistaAsync(
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
        _movimientos.Verify(
            x => x.AddAsync(It.IsAny<MovimientoInventario>()),
            Times.Never);
    }

    [Theory]
    [InlineData(-1, 0, "motivo")]'''
adjust_test = replace_once(
    adjust_test,
    insert_marker,
    new_test,
    "prueba ajuste sin diferencia")
write(adjust_test_path, adjust_test)

print("Carga masiva integrada con UnitOfWork y ajustes nulos bloqueados.")
