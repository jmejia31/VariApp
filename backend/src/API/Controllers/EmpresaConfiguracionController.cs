using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("empresa-configuracion")]
public class EmpresaConfiguracionController : ControllerBase
{
    private readonly IEmpresaConfiguracionService _service;

    public EmpresaConfiguracionController(IEmpresaConfiguracionService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Get()
    {
        var config = await _service.GetActivaAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(config));
    }

    [HttpPut]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> Update([FromBody] UpdateEmpresaConfiguracionDto dto)
    {
        var actualizada = await _service.UpdateAsync(dto);
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Configuración de empresa actualizada."));
    }
}
