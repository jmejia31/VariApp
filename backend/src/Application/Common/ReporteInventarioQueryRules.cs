using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Common;

public static class ReporteInventarioQueryRules
{
    public const int MaxHistoricalDays = 366;

    public static string? Validate(ReporteInventarioFiltroBaseDto filtro, params string[] allowedSortFields)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        if (filtro.Desde.HasValue && filtro.Hasta.HasValue)
        {
            if (filtro.Desde.Value > filtro.Hasta.Value)
                return "El rango de fechas es inválido: Desde no puede ser posterior a Hasta.";
            if ((filtro.Hasta.Value - filtro.Desde.Value).TotalDays > MaxHistoricalDays)
                return $"La ventana histórica máxima es de {MaxHistoricalDays} días.";
        }

        var direction = (filtro.SortDirection ?? "desc").Trim().ToLowerInvariant();
        if (direction is not ("asc" or "desc"))
            return "SortDirection sólo admite asc o desc.";

        if (allowedSortFields.Length > 0)
        {
            var sortBy = (filtro.SortBy ?? string.Empty).Trim();
            if (!allowedSortFields.Any(x => string.Equals(x, sortBy, StringComparison.OrdinalIgnoreCase)))
                return $"SortBy no permitido para este reporte: {sortBy}.";
        }

        return null;
    }

    public static string? ValidateStockHealth(ReporteInventarioStockHealthFiltroDto filtro)
    {
        var common = Validate(filtro, "Fecha", "Producto", "Sku", "StockDisponible", "DiasSinMovimiento", "Rotacion");
        if (common is not null) return common;
        if (filtro.Dias is < 1 or > MaxHistoricalDays)
            return $"Dias debe estar entre 1 y {MaxHistoricalDays}.";
        return null;
    }
}
