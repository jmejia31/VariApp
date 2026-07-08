using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResumenDto> GetResumenAsync();
}
