using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IExistenciaVarianteService
{
    Task<PagedResult<ExistenciaVarianteDto>> BuscarAsync(ExistenciaVarianteFiltroDto filtro);
    Task<ExistenciaVarianteDto?> GetByIdAsync(int id);
    Task<ExistenciaVarianteDto> CreateAsync(CreateExistenciaVarianteDto dto);
    Task<ExistenciaVarianteDto?> UpdateConfiguracionAsync(int id, UpdateExistenciaVarianteConfiguracionDto dto);
}
