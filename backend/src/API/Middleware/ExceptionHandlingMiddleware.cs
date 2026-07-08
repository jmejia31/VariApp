using System.Net;
using System.Text.Json;
using FluentValidation;
using InventoryApp.Application.Common;

namespace InventoryApp.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Error de validación");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var errores = ex.Errors.Select(e => e.ErrorMessage).ToList();
            var response = ApiResponse<object>.Fail("Error de validación.", errores);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<object>.Fail("Ocurrió un error interno. Intenta nuevamente más tarde.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
