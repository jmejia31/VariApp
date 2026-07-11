using System.Net;
using System.Text.Json;
using FluentValidation;
using InventoryApp.Application.Common;
using InventoryApp.Application.Exceptions;

namespace InventoryApp.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio violada");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = ApiResponse<object>.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso denegado por permisos");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

            var response = ApiResponse<object>.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<object>.Fail("Ocurrió un error interno. Intenta nuevamente más tarde.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
