using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface INotaCreditoClienteRepository
{
    Task<NotaCreditoCliente?> GetByIdAsync(int id, bool tracking = false);
    Task AddAsync(NotaCreditoCliente notaCredito);
    Task SaveChangesAsync();
}

public interface INotaCreditoClienteService
{
    Task<NotaCreditoClienteDto?> GetByIdAsync(int id);
    Task<NotaCreditoClienteDto> CreateAsync(CreateNotaCreditoClienteDto dto);
}
