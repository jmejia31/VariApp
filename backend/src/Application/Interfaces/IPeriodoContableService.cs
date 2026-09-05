using InventoryApp.Application.DTOs.Contabilidad;

namespace InventoryApp.Application.Interfaces;

public interface IPeriodoContableService
{
    Task<List<PeriodoContableDto>> GetAllAsync();
    Task<PeriodoContableDto?> GetByIdAsync(int id);
    Task<PeriodoContableDto> CreateAsync(CrearPeriodoContableDto dto);
    Task CerrarAsync(int id);
    Task ValidarOperacionAsync(DateTime fechaOperacion, bool autorizadoCambioRetroactivo = false);
}
