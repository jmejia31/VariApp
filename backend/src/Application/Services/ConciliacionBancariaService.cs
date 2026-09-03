using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Domain.Enums.Bancos;

namespace InventoryApp.Application.Services;

public class ConciliacionBancariaService : IConciliacionBancariaService
{
    private readonly IConciliacionBancariaRepository _conciliacionRepo;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepo;
    private readonly IOperacionBancariaService _operacionBancariaService;
    private readonly IUnitOfWork _unitOfWork;

    public ConciliacionBancariaService(
        IConciliacionBancariaRepository conciliacionRepo,
        IMovimientoFinancieroRepository movimientoFinancieroRepo,
        IOperacionBancariaService operacionBancariaService,
        IUnitOfWork unitOfWork)
    {
        _conciliacionRepo = conciliacionRepo;
        _movimientoFinancieroRepo = movimientoFinancieroRepo;
        _operacionBancariaService = operacionBancariaService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportarEstadoCuentaResponseDto> ImportarEstadoCuentaAsync(ImportarEstadoCuentaRequestDto request, int usuarioId, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        int importados = 0;
        int ignorados = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conciliacion = await _conciliacionRepo.GetActivaByCuentaAsync(request.CuentaBancariaId, cancellationToken);
            if (conciliacion == null)
            {
                errores.Add("No hay una conciliación activa en Borrador o EnProceso para esta cuenta.");
                return;
            }

            if (conciliacion.Estado != EstadoConciliacionBancaria.Borrador && conciliacion.Estado != EstadoConciliacionBancaria.EnProceso)
            {
                errores.Add("La conciliación debe estar en Borrador o EnProceso para importar movimientos.");
                return;
            }

            foreach (var dto in request.Movimientos)
            {
                if (conciliacion.Movimientos.Any(m => m.IdempotencyKey == dto.IdentificadorExternoTransaccion))
                {
                    ignorados++;
                    continue;
                }

                var tipo = dto.Monto >= 0 ? TipoMovimientoEstadoCuenta.Credito : TipoMovimientoEstadoCuenta.Debito;
                var montoAbsoluto = Math.Abs(dto.Monto);

                var movimiento = new MovimientoEstadoCuenta(
                    dto.IdentificadorExternoTransaccion,
                    dto.FechaOperacion,
                    dto.Descripcion,
                    dto.ReferenciaExterna,
                    tipo,
                    montoAbsoluto
                );

                try
                {
                    conciliacion.AgregarMovimiento(movimiento);
                    importados++;
                }
                catch (InvalidOperationException ex)
                {
                    errores.Add($"Error al agregar movimiento {dto.IdentificadorExternoTransaccion}: {ex.Message}");
                }
            }

            _conciliacionRepo.Update(conciliacion);
            await _conciliacionRepo.SaveChangesAsync(cancellationToken);
        });

        return new ImportarEstadoCuentaResponseDto
        {
            CuentaBancariaId = request.CuentaBancariaId,
            MovimientosImportados = importados,
            MovimientosDuplicadosIgnorados = ignorados,
            Errores = errores
        };
    }

    public async Task<ConciliarMovimientosResponseDto> ConciliarMovimientosAsync(ConciliarMovimientosRequestDto request, int usuarioId, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        int exitosos = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conciliacion = await _conciliacionRepo.GetActivaByCuentaAsync(request.CuentaBancariaId, cancellationToken);
            if (conciliacion == null)
            {
                errores.Add("No se encontró una conciliación activa.");
                return;
            }

            if (conciliacion.Estado == EstadoConciliacionBancaria.Borrador)
            {
                conciliacion.MarcarComoEnProceso();
            }

            foreach (var matchDto in request.Matches)
            {
                var movimientoBancario = conciliacion.Movimientos.FirstOrDefault(m => m.IdempotencyKey == matchDto.IdentificadorExternoTransaccion);
                if (movimientoBancario == null)
                {
                    errores.Add($"Movimiento bancario con key {matchDto.IdentificadorExternoTransaccion} no encontrado.");
                    continue;
                }

                var movimientoFinanciero = await _movimientoFinancieroRepo.GetByIdAsync(matchDto.MovimientoInternoId);
                if (movimientoFinanciero == null)
                {
                    errores.Add($"Movimiento financiero con ID {matchDto.MovimientoInternoId} no encontrado.");
                    continue;
                }

                try
                {
                    movimientoBancario.AgregarMatch(movimientoFinanciero.Id, movimientoFinanciero.Monto, TipoMatchConciliacion.Manual);
                    exitosos++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Error en match {matchDto.IdentificadorExternoTransaccion}: {ex.Message}");
                }
            }

            _conciliacionRepo.Update(conciliacion);
            await _conciliacionRepo.SaveChangesAsync(cancellationToken);
        });

        return new ConciliarMovimientosResponseDto
        {
            MatchesExitosos = exitosos,
            Errores = errores
        };
    }

    public async Task<SolicitarAjusteResponseDto> SolicitarAjusteAsync(SolicitarAjusteRequestDto request, int usuarioId, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        int ajustes = 0;

        foreach (var dif in request.Diferencias)
        {
            try
            {
                var operacionDto = new ConciliacionBancariaDto
                {
                    CuentaId = request.CuentaBancariaId,
                    Monto = Math.Abs(dif.DiferenciaMonto),
                    Referencia = dif.Motivo,
                    IdempotencyKey = $"{request.IdempotencyKey}-{dif.IdentificadorExternoTransaccion}"
                };
                await _operacionBancariaService.RegistrarConciliacionAsync(operacionDto, usuarioId);
                ajustes++;
            }
            catch (Exception ex)
            {
                errores.Add($"Error al solicitar ajuste para {dif.IdentificadorExternoTransaccion}: {ex.Message}");
            }
        }

        return new SolicitarAjusteResponseDto
        {
            AjustesSolicitados = ajustes,
            Errores = errores
        };
    }

    public async Task<CerrarPeriodoConciliacionResponseDto> CerrarPeriodoAsync(CerrarPeriodoConciliacionRequestDto request, int usuarioId, CancellationToken cancellationToken = default)
    {
        bool exitoso = false;
        string mensaje = string.Empty;
        var diferencias = new List<string>();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conciliacion = await _conciliacionRepo.GetActivaByCuentaAsync(request.CuentaBancariaId, cancellationToken);
            if (conciliacion == null)
            {
                mensaje = "No se encontró conciliación activa para cerrar.";
                return;
            }

            if (conciliacion.FechaInicio.Month != request.Mes || conciliacion.FechaInicio.Year != request.Anio)
            {
                mensaje = "El periodo de la conciliación activa no coincide con el solicitado.";
                return;
            }

            if (conciliacion.SaldoFinalBanco != request.SaldoFinalEstadoCuenta)
            {
                mensaje = "El saldo final proporcionado no coincide con el de la conciliación.";
                return;
            }

            var movimientosPendientes = conciliacion.Movimientos.Where(m => m.Estado == EstadoMovimientoEstadoCuenta.Pendiente || m.Estado == EstadoMovimientoEstadoCuenta.Parcial).ToList();
            if (movimientosPendientes.Any())
            {
                mensaje = "Existen movimientos pendientes o parciales.";
                diferencias.AddRange(movimientosPendientes.Select(m => m.IdempotencyKey));
                return;
            }

            try
            {
                conciliacion.Completar();
                _conciliacionRepo.Update(conciliacion);
                await _conciliacionRepo.SaveChangesAsync(cancellationToken);
                exitoso = true;
                mensaje = "Conciliación cerrada exitosamente.";
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
            }
        });

        return new CerrarPeriodoConciliacionResponseDto
        {
            Exitoso = exitoso,
            Mensaje = mensaje,
            DiferenciasPendientes = diferencias
        };
    }

    public async Task<ReabrirPeriodoConciliacionResponseDto> ReabrirPeriodoAsync(ReabrirPeriodoConciliacionRequestDto request, int usuarioId, CancellationToken cancellationToken = default)
    {
        bool exitoso = false;
        string mensaje = string.Empty;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conciliacion = await _conciliacionRepo.GetByPeriodoAsync(request.CuentaBancariaId, request.Mes, request.Anio, cancellationToken);
            if (conciliacion == null)
            {
                mensaje = "No se encontró la conciliación para el periodo especificado.";
                return;
            }

            try
            {
                conciliacion.Anular(request.MotivoReapertura);

                var nuevaConciliacion = new ConciliacionBancaria(
                    conciliacion.CuentaBancariaId,
                    conciliacion.FechaInicio,
                    conciliacion.FechaFin,
                    conciliacion.SaldoInicialBanco,
                    conciliacion.SaldoFinalBanco,
                    $"Reapertura de {conciliacion.Id}");

                _conciliacionRepo.Update(conciliacion);
                await _conciliacionRepo.AddAsync(nuevaConciliacion, cancellationToken);
                await _conciliacionRepo.SaveChangesAsync(cancellationToken);
                exitoso = true;
                mensaje = "Conciliación reabierta exitosamente (nueva versión en borrador creada).";
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
            }
        });

        return new ReabrirPeriodoConciliacionResponseDto
        {
            Exitoso = exitoso,
            Mensaje = mensaje
        };
    }

    public async Task<ConciliacionBancariaPageDto> GetConciliacionesAsync(ConciliacionBancariaFilterDto filter, CancellationToken cancellationToken = default)
    {
        var result = await _conciliacionRepo.GetPagedAsync(
            filter.CuentaBancariaId,
            filter.Estado,
            filter.Mes,
            filter.Anio,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken);

        var dtos = result.Items.Select(c => new ConciliacionBancariaResumenDto
        {
            Id = c.Id,
            CuentaBancariaId = c.CuentaBancariaId,
            Estado = c.Estado.ToString(),
            FechaInicio = c.FechaInicio,
            FechaFin = c.FechaFin,
            SaldoInicialBanco = c.SaldoInicialBanco,
            SaldoFinalBanco = c.SaldoFinalBanco,
            SaldoConciliado = c.SaldoConciliado,
            Diferencia = c.Diferencia
        });

        return new ConciliacionBancariaPageDto
        {
            TotalRecords = result.TotalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            Items = dtos
        };
    }
}
