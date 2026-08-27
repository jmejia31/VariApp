using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface ICreditoClienteService
{
    Task<CreditoClienteDto> GetByIdAsync(int id);
    Task<IReadOnlyList<CreditoClienteDto>> GetByClienteIdAsync(int clienteId);
    Task<CreditoClienteDto> CrearAsync(CreateCreditoClienteDto dto);
    Task<CreditoClienteDto> ActualizarPoliticaAsync(int id, UpdateCreditoClienteDto dto);
}
