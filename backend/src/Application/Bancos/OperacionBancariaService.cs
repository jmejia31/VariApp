using InventoryApp.Application.DTOs.Bancos;
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
}
