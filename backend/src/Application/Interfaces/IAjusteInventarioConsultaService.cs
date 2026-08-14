using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IAjusteInventarioConsultaService
{
    Task<PagedResult<AjusteInventarioDto>> GetPagedAsync(AjusteInventarioFiltroDto filtro);
}
