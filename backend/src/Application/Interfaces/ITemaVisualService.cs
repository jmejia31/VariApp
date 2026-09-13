using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface ITemaVisualService
{
    Task<TemaVisualDto> GetAsync();
    Task<TemaVisualDto> UpdateAsync(ActualizarTemaVisualDto dto);
    Task<TemaVisualDto> RestaurarPredeterminadoAsync();
}
