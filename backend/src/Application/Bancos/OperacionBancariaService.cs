using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Domain.Enums;
using InventoryApp.Domain.Enums.Bancos;

namespace InventoryApp.Application.Bancos;

public class OperacionBancariaService : IOperacionBancariaService
{
    private readonly ICuentaBancariaRepository _cuentaRepo;
    private readonly IMovimientoFinancieroRepository _movimientoRepo;
    private readonly IUnitOfWork _unitOfWork;

    private sealed record ExpectedMovimiento(
        TipoMovimientoFinanciero Tipo,
        CategoriaMovimientoFinanciero Categoria,
        int CuentaId,
        decimal Monto,
        string Concepto,
        string DescripcionSuffix = "");

    public OperacionBancariaService(
        ICuentaBancariaRepository cuentaRepo,
        IMovimientoFinancieroRepository movimientoRepo,
        IUnitOfWork unitOfWork)
    {
        _cuentaRepo = cuentaRepo;
        _movimientoRepo = movimientoRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task RegistrarDepositoAsync(DepositoBancarioDto dto, int usuarioId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var key = BancosIdempotencyKey.Create(dto.IdempotencyKey);
            if (await IsReplayOrThrowConflictAsync(key.Value, usuarioId,
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Ingreso,
                    CategoriaMovimientoFinanciero.Otro,
                    dto.CuentaId,
                    dto.Monto,
                    $"Depósito Bancario - {dto.Referencia}")))
                return;

            var origen = await _cuentaRepo.GetByIdAsync(dto.CuentaId);
            if (origen is null) throw new ArgumentException("Cuenta no encontrada.", nameof(dto.CuentaId));

            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.Deposito, dto.Monto);

            var mov = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Otro,
                Concepto = $"Depósito Bancario - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = origen.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}"
            };

            await _movimientoRepo.AddAsync(mov);
            await _movimientoRepo.SaveChangesAsync();
        });
    }

    public async Task RegistrarRetiroAsync(RetiroBancarioDto dto, int usuarioId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var key = BancosIdempotencyKey.Create(dto.IdempotencyKey);
            if (await IsReplayOrThrowConflictAsync(key.Value, usuarioId,
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Egreso,
                    CategoriaMovimientoFinanciero.Otro,
                    dto.CuentaId,
                    dto.Monto,
                    $"Retiro Bancario - {dto.Referencia}")))
                return;

            var origen = await _cuentaRepo.GetByIdAsync(dto.CuentaId);
            if (origen is null) throw new ArgumentException("Cuenta no encontrada.", nameof(dto.CuentaId));

            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.Retiro, dto.Monto);

            var mov = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.Otro,
                Concepto = $"Retiro Bancario - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = origen.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}"
            };

            await _movimientoRepo.AddAsync(mov);
            await _movimientoRepo.SaveChangesAsync();
        });
    }

    public async Task RegistrarTransferenciaAsync(TransferenciaBancariaDto dto, int usuarioId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var key = BancosIdempotencyKey.Create(dto.IdempotencyKey);
            var origen = await _cuentaRepo.GetByIdAsync(dto.CuentaId);
            if (origen is null) throw new ArgumentException("Cuenta origen no encontrada.", nameof(dto.CuentaId));

            var destino = await _cuentaRepo.GetByIdAsync(dto.CuentaDestinoId);
            if (destino is null) throw new ArgumentException("Cuenta destino no encontrada.", nameof(dto.CuentaDestinoId));

            if (await IsReplayOrThrowConflictAsync(key.Value, usuarioId,
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Egreso,
                    CategoriaMovimientoFinanciero.Otro,
                    dto.CuentaId,
                    dto.Monto,
                    $"Transferencia a cuenta {destino.NumeroCuenta} - {dto.Referencia}",
                    "-Egreso"),
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Ingreso,
                    CategoriaMovimientoFinanciero.Otro,
                    dto.CuentaDestinoId,
                    dto.Monto,
                    $"Transferencia de cuenta {origen.NumeroCuenta} - {dto.Referencia}",
                    "-Ingreso")))
                return;

            BancosOperationPolicy.ValidarOperacionBancaria(origen, destino, TipoOperacionBancaria.Transferencia, dto.Monto);

            var movEgreso = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.Otro,
                Concepto = $"Transferencia a cuenta {destino.NumeroCuenta} - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = origen.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}-Egreso"
            };

            var movIngreso = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Otro,
                Concepto = $"Transferencia de cuenta {origen.NumeroCuenta} - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = destino.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}-Ingreso"
            };

            await _movimientoRepo.AddAsync(movEgreso);
            await _movimientoRepo.AddAsync(movIngreso);
            await _movimientoRepo.SaveChangesAsync();
        });
    }

    public async Task RegistrarComisionAsync(ComisionBancariaDto dto, int usuarioId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var key = BancosIdempotencyKey.Create(dto.IdempotencyKey);
            if (await IsReplayOrThrowConflictAsync(key.Value, usuarioId,
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Egreso,
                    CategoriaMovimientoFinanciero.GastoOperativo,
                    dto.CuentaId,
                    dto.Monto,
                    $"Comisión Bancaria - {dto.Referencia}")))
                return;

            var origen = await _cuentaRepo.GetByIdAsync(dto.CuentaId);
            if (origen is null) throw new ArgumentException("Cuenta no encontrada.", nameof(dto.CuentaId));

            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.Comision, dto.Monto);

            var mov = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.GastoOperativo,
                Concepto = $"Comisión Bancaria - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = origen.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}"
            };

            await _movimientoRepo.AddAsync(mov);
            await _movimientoRepo.SaveChangesAsync();
        });
    }

    public async Task RegistrarInteresAsync(InteresBancarioDto dto, int usuarioId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var key = BancosIdempotencyKey.Create(dto.IdempotencyKey);
            if (await IsReplayOrThrowConflictAsync(key.Value, usuarioId,
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Ingreso,
                    CategoriaMovimientoFinanciero.Otro,
                    dto.CuentaId,
                    dto.Monto,
                    $"Interés Bancario - {dto.Referencia}")))
                return;

            var origen = await _cuentaRepo.GetByIdAsync(dto.CuentaId);
            if (origen is null) throw new ArgumentException("Cuenta no encontrada.", nameof(dto.CuentaId));

            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.Interes, dto.Monto);

            var mov = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Otro,
                Concepto = $"Interés Bancario - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = origen.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}"
            };

            await _movimientoRepo.AddAsync(mov);
            await _movimientoRepo.SaveChangesAsync();
        });
    }

    public async Task RegistrarConciliacionAsync(ConciliacionBancariaDto dto, int usuarioId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var key = BancosIdempotencyKey.Create(dto.IdempotencyKey);
            if (await IsReplayOrThrowConflictAsync(key.Value, usuarioId,
                new ExpectedMovimiento(
                    TipoMovimientoFinanciero.Ajuste,
                    CategoriaMovimientoFinanciero.Ajuste,
                    dto.CuentaId,
                    dto.Monto,
                    $"Ajuste por Conciliación Bancaria - {dto.Referencia}")))
                return;

            var origen = await _cuentaRepo.GetByIdAsync(dto.CuentaId);
            if (origen is null) throw new ArgumentException("Cuenta no encontrada.", nameof(dto.CuentaId));

            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.ConciliacionAjuste, dto.Monto);

            var mov = new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ajuste,
                Categoria = CategoriaMovimientoFinanciero.Ajuste,
                Concepto = $"Ajuste por Conciliación Bancaria - {dto.Referencia}",
                Monto = dto.Monto,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = false,
                ModuloOrigen = "Bancos",
                ReferenciaId = origen.Id,
                CreadoPorUsuarioId = usuarioId,
                Descripcion = $"IdempotencyKey: {key.Value}"
            };

            await _movimientoRepo.AddAsync(mov);
            await _movimientoRepo.SaveChangesAsync();
        });
    }

    private async Task<bool> IsReplayOrThrowConflictAsync(
        string key,
        int usuarioId,
        params ExpectedMovimiento[] expected)
    {
        var existing = await _movimientoRepo.GetByBancosIdempotencyKeyAsync(key, usuarioId);
        if (existing.Count == 0)
            return false;

        var equivalent = existing.Count == expected.Length && expected.All(e =>
            existing.Any(m =>
                m.Tipo == e.Tipo &&
                m.Categoria == e.Categoria &&
                m.ReferenciaId == e.CuentaId &&
                m.Monto == e.Monto &&
                m.Concepto == e.Concepto &&
                m.Descripcion == $"IdempotencyKey: {key}{e.DescripcionSuffix}"));

        if (equivalent)
            return true;

        throw new ConflictException(
            "La IdempotencyKey ya fue utilizada por una operación bancaria con un payload diferente.");
    }
}
