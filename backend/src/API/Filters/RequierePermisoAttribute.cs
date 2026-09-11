using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryApp.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequierePermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly ModuloSistema _modulo;
    private readonly AccionPermiso _accion;

    public RequierePermisoAttribute(ModuloSistema modulo, AccionPermiso accion)
    {
        _modulo = modulo;
        _accion = accion;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var permisoService = context.HttpContext.RequestServices.GetRequiredService<IPermisoService>();
        await permisoService.VerificarPermisoAsync(_modulo, _accion);
        await next();
    }
}
