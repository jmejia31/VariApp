using InventoryApp.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.API.Filters;

internal static class TenantPermissionContext
{
    internal const string EmpresaHeader = "X-Empresa-Id";

    internal static int RequireEmpresaId(HttpContext httpContext)
    {
        var raw = httpContext.Request.Headers[EmpresaHeader].FirstOrDefault();
        if (!int.TryParse(raw, out var empresaId) || empresaId <= 0)
        {
            throw new ForbiddenAccessException(
                "Se requiere un contexto tenant válido para autorizar esta operación.");
        }

        return empresaId;
    }
}
