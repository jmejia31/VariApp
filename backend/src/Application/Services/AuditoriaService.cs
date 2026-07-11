using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(IAuditoriaRepository repository, ICurrentUserService currentUser, ILogger<AuditoriaService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task RegistrarAsync(ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null)
    {
        try
        {
            await _repository.AddAsync(new RegistroAuditoria
            {
                UsuarioId = _currentUser.UsuarioId,
                NombreUsuario = _currentUser.NombreUsuario ?? "Sistema",
                Modulo = modulo,
                Accion = accion,
                ReferenciaId = referenciaId,
                Descripcion = descripcion
            });
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Una falla al escribir la bitácora NUNCA debe interrumpir la operación de negocio.
            _logger.LogError(ex, "No se pudo registrar entrada de auditoría: {Descripcion}", descripcion);
        }
    }

    public async Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro)
    {
        var (items, total) = await _repository.GetFilteredAsync(
            filtro.UsuarioId, filtro.Modulo, filtro.Accion, filtro.Desde, filtro.Hasta, filtro.Page, filtro.PageSize);

        return new PagedResult<RegistroAuditoriaDto>
        {
            Items = items.Select(r => new RegistroAuditoriaDto
            {
                Id = r.Id,
                Fecha = r.Fecha,
                NombreUsuario = r.NombreUsuario,
                Modulo = r.Modulo.ToString(),
                Accion = r.Accion.ToString(),
                ReferenciaId = r.ReferenciaId,
                Descripcion = r.Descripcion
            }).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }
}
