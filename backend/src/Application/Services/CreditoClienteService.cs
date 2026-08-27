using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class CreditoClienteService : ICreditoClienteService
{
    private readonly ICreditoClienteRepository _repository;
    private readonly IClienteRepository _clientes;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreditoClienteService(
        ICreditoClienteRepository repository,
        IClienteRepository clientes,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreditoClienteDto> GetByIdAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de crédito debe ser mayor que cero.");
        var credito = await _repository.GetByIdAsync(id) ?? throw new ResourceNotFoundException($"Crédito de cliente con Id {id} no encontrado.");
        return Map(credito);
    }

    public async Task<IReadOnlyList<CreditoClienteDto>> GetByClienteIdAsync(int clienteId)
    {
        if (clienteId <= 0) throw new BusinessRuleException("ClienteId debe ser mayor que cero.");
        var items = await _repository.GetByClienteIdAsync(clienteId);
        return items.Select(Map).ToList();
    }

    public async Task<CreditoClienteDto> CrearAsync(CreateCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuario();
        var id = 0;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var cliente = await _clientes.GetByIdAsync(dto.ClienteId) ?? throw new ResourceNotFoundException($"Cliente con Id {dto.ClienteId} no encontrado.");
            var credito = CreditoCliente.Crear(cliente, dto.Moneda, dto.LimiteCredito, dto.DiasCredito, dto.UmbralAlertaPorcentaje);
            await _repository.AddAsync(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Crear,
                "Configuración de crédito de cliente creada.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.ClienteId, credito.Moneda, credito.LimiteCredito, credito.DiasCredito, credito.UmbralAlertaPorcentaje });
            id = credito.Id;
        });
        return await GetByIdAsync(id);
    }

    public async Task<CreditoClienteDto> ActualizarPoliticaAsync(int id, UpdateCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var credito = await _repository.GetByIdForUpdateAsync(id) ?? throw new ResourceNotFoundException($"Crédito de cliente con Id {id} no encontrado.");
            credito.ActualizarPolitica(dto.Moneda, dto.LimiteCredito, dto.DiasCredito, dto.UmbralAlertaPorcentaje);
            _repository.Update(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Editar,
                "Política de crédito de cliente actualizada.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.Moneda, credito.LimiteCredito, credito.DiasCredito, credito.UmbralAlertaPorcentaje });
        });
        return await GetByIdAsync(id);
    }

    private void RequerirUsuario()
    {
        if (_currentUser.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");
    }

    private static CreditoClienteDto Map(CreditoCliente x) => new()
    {
        Id = x.Id,
        ClienteId = x.ClienteId,
        Moneda = x.Moneda,
        LimiteCredito = x.LimiteCredito,
        DiasCredito = x.DiasCredito,
        UmbralAlertaPorcentaje = x.UmbralAlertaPorcentaje,
        BloqueadoAutomaticamente = x.BloqueadoAutomaticamente,
        MotivoBloqueo = x.MotivoBloqueo,
        BloqueadoUtc = x.BloqueadoUtc,
        MontoExcepcion = x.MontoExcepcion,
        ExcepcionVigenteHastaUtc = x.ExcepcionVigenteHastaUtc,
        ExcepcionAutorizadaPor = x.ExcepcionAutorizadaPor,
        ExcepcionAutorizadaUtc = x.ExcepcionAutorizadaUtc
    };
}
