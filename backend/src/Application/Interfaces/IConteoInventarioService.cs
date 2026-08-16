using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IConteoInventarioService
{
    Task<PagedResult<ConteoInventarioDto>> GetPagedAsync(ConteoInventarioFiltroDto filtro);
    Task<ConteoInventarioDto?> GetByIdAsync(int id);
    Task<ConteoInventarioDto> CreateAsync(CreateConteoInventarioDto dto);
    Task<ConteoInventarioDto?> UpdateAsync(int id, UpdateConteoInventarioDto dto);
    Task<ConteoInventarioDto?> IniciarAsync(int id);
    Task<ConteoInventarioDto?> CapturarDetalleAsync(int id, int detalleId, CapturarConteoInventarioDetalleDto dto);
    Task<ConteoInventarioDto?> CerrarAsync(int id);
    Task<ConteoInventarioDto?> AprobarAsync(int id);
    Task<ConteoInventarioDto?> CancelarAsync(int id, string motivo);
}
