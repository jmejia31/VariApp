using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
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
    public async Task<IActionResult> Get()
    {
        var config = await _service.GetActivaAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(config));
    }

    [HttpPut]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update([FromBody] UpdateEmpresaConfiguracionDto dto)
    {
        var actualizada = await _service.UpdateAsync(dto);
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Configuración de empresa actualizada."));
    }
}
