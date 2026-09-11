using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Route("empresa-configuracion")]
public class EmpresaConfiguracionController : ControllerBase
{
    private readonly IEmpresaConfiguracionService _service;

    public EmpresaConfiguracionController(IEmpresaConfiguracionService service)
    {
        _service = service;
    }

    [HttpGet("publica")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublica()
    {
        var config = await _service.GetActivaAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(config));
    }

    [HttpGet]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Get()
    {
        var config = await _service.GetActivaAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(config));
    }

    [HttpPut]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> Update([FromBody] UpdateEmpresaConfiguracionDto dto)
    {
        var actualizada = await _service.UpdateAsync(dto);
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Configuracion de empresa actualizada."));
    }

    [HttpPost("logo")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateLogo(IFormFile logo)
    {
        var actualizada = await _service.UpdateLogoAsync(logo);
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Logo actualizado correctamente."));
    }

    [HttpDelete("logo")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> RestaurarLogo()
    {
        var actualizada = await _service.RestaurarLogoAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Logo restaurado correctamente."));
    }
}
