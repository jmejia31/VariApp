using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Mappings;

public static class ProductoMapper
{
    public static ProductoDto ToDto(Producto p)
    {
        var variantes = p.Variantes
            .Where(v => !v.Eliminado)
            .OrderBy(v => v.Marca?.Nombre)
            .ThenBy(v => v.Modelo?.Nombre)
            .ThenBy(v => v.Color?.Nombre)
            .ThenBy(v => v.Talla?.Nombre)
            .ThenBy(v => v.Sku)
            .ToList();
        var activas = variantes.Where(v => v.Activo).ToList();
        var operativas = activas.Count > 0 ? activas : variantes;
        var stockTotal = variantes.Sum(v => v.Cantidad);
        var costo = CalcularCosto(varianteFuente: variantes, fallback: p.Costo);
        var precios = operativas.Where(v => v.Precio.HasValue).Select(v => v.Precio!.Value).ToList();
        var precio = precios.Count > 0 ? precios.Min() : p.Precio;
        var precioMaximo = precios.Count > 0 ? precios.Max() : p.Precio;
        var umbral = variantes.Count > 0 ? variantes.Sum(v => v.UmbralStockBajo) : p.UmbralStockBajo;
        var agotado = p.Activo && !p.Eliminado && (activas.Count == 0 || activas.All(v => v.Cantidad <= 0));
        var stockBajo = p.Activo && !p.Eliminado && !agotado && activas.Any(v => v.TieneStockBajo);

        var marcaId = variantes.Count > 0
            ? ValorComun(varianteFuente: variantes, selector: v => v.MarcaId)
            : p.MarcaId;
        var modeloId = variantes.Count > 0
            ? ValorComun(varianteFuente: variantes, selector: v => v.ModeloId)
            : p.ModeloId;
        var colorId = variantes.Count > 0
            ? ValorComun(varianteFuente: variantes, selector: v => v.ColorId)
            : p.ColorId;
        var tallaId = variantes.Count > 0
            ? ValorComun(varianteFuente: variantes, selector: v => v.TallaId)
            : p.TallaId;

        var marcaNombres = variantes.Select(v => v.Marca?.Nombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var modeloNombres = variantes.Select(v => v.Modelo?.Nombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var marca = variantes.Count > 0 ? string.Join(" / ", marcaNombres!) : (p.MarcaCatalogo?.Nombre ?? p.Marca);
        var modelo = variantes.Count > 0 ? string.Join(" / ", modeloNombres!) : (p.ModeloCatalogo?.Nombre ?? p.Modelo);

        var color = variantes.Count > 0 && colorId.HasValue
            ? variantes.FirstOrDefault(v => v.ColorId == colorId)?.Color
            : p.ColorCatalogo;
        var talla = variantes.Count > 0 && tallaId.HasValue
            ? variantes.FirstOrDefault(v => v.TallaId == tallaId)?.Talla
            : p.TallaCatalogo;
        var marcaEntidad = variantes.Count > 0 && marcaId.HasValue
            ? variantes.FirstOrDefault(v => v.MarcaId == marcaId)?.Marca
            : p.MarcaCatalogo;
        var modeloEntidad = variantes.Count > 0 && modeloId.HasValue
            ? variantes.FirstOrDefault(v => v.ModeloId == modeloId)?.Modelo
            : p.ModeloCatalogo;

        var imagenesGenerales = p.Imagenes
            .Where(i => i.ProductoVarianteId == null)
            .OrderBy(i => i.Orden)
            .ToList();

        return new ProductoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Marca = marca,
            Modelo = modelo,
            Descripcion = p.Descripcion,
            TipoInventario = p.TipoInventario,
            Cantidad = variantes.Count > 0 ? stockTotal : p.Cantidad,
            Costo = costo,
            Precio = precio,
            PrecioMinimo = precio,
            PrecioMaximo = precioMaximo,
            UmbralStockBajo = umbral,
            TieneStockBajo = variantes.Count > 0 ? stockBajo : p.TieneStockBajo,
            EstaAgotado = variantes.Count > 0 ? agotado : p.EstaAgotado,
            EstadoInventario = (variantes.Count > 0 ? agotado : p.EstaAgotado)
                ? "Agotado"
                : (variantes.Count > 0 ? stockBajo : p.TieneStockBajo) ? "Stock bajo" : "Disponible",
            Activo = p.Activo,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre,
            ColorId = colorId,
            ColorNombre = color?.Nombre,
            ColorCodigoVisual = color?.CodigoVisual,
            TallaId = tallaId,
            TallaNombre = talla?.Nombre,
            MarcaId = marcaId,
            MarcaNombre = marcaEntidad?.Nombre,
            ModeloId = modeloId,
            ModeloNombre = modeloEntidad?.Nombre,
            ImagenPrincipalUrl = p.ImagenPrincipal?.Url,
            TotalImagenes = imagenesGenerales.Count,
            Imagenes = imagenesGenerales.Select(i => new ProductoImagenDto
            {
                Id = i.Id,
                Url = i.Url,
                Orden = i.Orden,
                EsPrincipal = i.EsPrincipal,
                ProductoVarianteId = null
            }).ToList(),
            Variantes = variantes.Select(v => new ProductoVarianteDto
            {
                Id = v.Id,
                ProductoId = v.ProductoId,
                ProductoNombre = p.Nombre,
                MarcaId = v.MarcaId,
                MarcaNombre = v.Marca?.Nombre,
                ModeloId = v.ModeloId,
                ModeloNombre = v.Modelo?.Nombre,
                ColorId = v.ColorId,
                ColorNombre = v.Color?.Nombre,
                ColorCodigoVisual = v.Color?.CodigoVisual,
                TallaId = v.TallaId,
                TallaNombre = v.Talla?.Nombre,
                Etiqueta = ConstruirEtiqueta(v),
                Sku = v.Sku ?? string.Empty,
                CodigoBarras = v.CodigoBarras,
                Cantidad = v.Cantidad,
                UmbralStockBajo = v.UmbralStockBajo,
                Costo = v.Costo ?? 0m,
                Precio = v.Precio ?? 0m,
                Activo = v.Activo,
                EsTecnica = v.EsTecnica,
                TieneStockBajo = v.TieneStockBajo,
                EstaAgotada = v.EstaAgotada,
                EstadoInventario = v.EstaAgotada ? "Agotada" : v.TieneStockBajo ? "Stock bajo" : "Disponible",
                FechaCreacion = v.FechaCreacion,
                FechaActualizacion = v.FechaActualizacion
            }).ToList(),
            TotalVariantes = variantes.Count,
            CreadoPorUsuarioId = p.CreadoPorUsuarioId,
            CreadoPorNombreUsuario = p.CreadoPorNombreUsuario,
            ActualizadoPorUsuarioId = p.ActualizadoPorUsuarioId,
            ActualizadoPorNombreUsuario = p.ActualizadoPorNombreUsuario,
            FechaCreacion = p.FechaCreacion,
            FechaActualizacion = p.FechaActualizacion
        };
    }

    private static decimal CalcularCosto(IReadOnlyCollection<ProductoVariante> varianteFuente, decimal fallback)
    {
        if (varianteFuente.Count == 0) return fallback;
        var total = varianteFuente.Sum(v => v.Cantidad);
        if (total > 0)
            return Math.Round(varianteFuente.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / total, 2, MidpointRounding.AwayFromZero);

        var costos = varianteFuente.Where(v => v.Costo.HasValue).Select(v => v.Costo!.Value).ToList();
        return costos.Count > 0 ? Math.Round(costos.Average(), 2, MidpointRounding.AwayFromZero) : 0m;
    }

    private static int? ValorComun(
        IReadOnlyCollection<ProductoVariante> varianteFuente,
        Func<ProductoVariante, int?> selector)
    {
        if (varianteFuente.Count == 0) return null;
        var valores = varianteFuente.Select(selector).Distinct().Take(2).ToList();
        return valores.Count == 1 ? valores[0] : null;
    }

    private static bool NoVacio(string? valor) => !string.IsNullOrWhiteSpace(valor);

    private static string ConstruirEtiqueta(ProductoVariante variante)
    {
        var partes = new[] { variante.Marca?.Nombre, variante.Modelo?.Nombre, variante.Color?.Nombre, variante.Talla?.Nombre, variante.Sku };
        return string.Join(" · ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
    }
}
