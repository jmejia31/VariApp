using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryApp.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiereAlgunoPermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly ModuloSistema _modulo;
    private readonly AccionPermiso[] _acciones;

    public RequiereAlgunoPermisoAttribute(
        ModuloSistema modulo,
        params AccionPermiso[] acciones)
    {
        if (acciones is null || acciones.Length == 0)
            throw new ArgumentException("Debe indicarse al menos una acción permitida.", nameof(acciones));

        _modulo = modulo;
        _acciones = acciones.Distinct().ToArray();
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var permisoService = context.HttpContext.RequestServices.GetRequiredService<IPermisoService>();
        foreach (var accion in _acciones)
        {
            if (await permisoService.TienePermisoAsync(_modulo, accion))
            {
                await next();
                return;
            }
        }

        throw new ForbiddenAccessException(
            $"No tienes permisos para ejecutar esta operación en el módulo {_modulo}.");
    }
}
