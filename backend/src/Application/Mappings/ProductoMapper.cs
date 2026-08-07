using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Mappings;

public static class ProductoMapper
{
    public static ProductoDto ToDto(Producto p)
    {
        var variantes = p.Variantes
            .Where(v => !v.Eliminado)
            .OrderBy(v => v.Color?.Nombre)
            .ThenBy(v => v.Sku)
            .ToList();
        var activas = variantes.Where(v => v.Activo).ToList();
        var precios = activas.Select(v => v.Precio ?? p.Precio).ToList();

        return new ProductoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Marca = p.MarcaCatalogo?.Nombre ?? p.Marca,
            Modelo = p.ModeloCatalogo?.Nombre ?? p.Modelo,
            Descripcion = p.Descripcion,
            TipoInventario = p.TipoInventario,
            Cantidad = variantes.Count > 0 ? variantes.Sum(v => v.Cantidad) : p.Cantidad,
            Costo = p.Costo,
            Precio = p.Precio,
            PrecioMinimo = precios.Count > 0 ? precios.Min() : p.Precio,
            PrecioMaximo = precios.Count > 0 ? precios.Max() : p.Precio,
            UmbralStockBajo = p.UmbralStockBajo,
            TieneStockBajo = p.TieneStockBajo,
            EstaAgotado = p.EstaAgotado,
            EstadoInventario = p.EstaAgotado ? "Agotado" : p.TieneStockBajo ? "Stock bajo" : "Disponible",
            Activo = p.Activo,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre,
            ColorId = p.ColorId,
            ColorNombre = p.Color?.Nombre,
            ColorCodigoVisual = p.Color?.CodigoVisual,
            TallaId = p.TallaId,
            TallaNombre = p.Talla?.Nombre,
            MarcaId = p.MarcaId,
            MarcaNombre = p.MarcaCatalogo?.Nombre,
            ModeloId = p.ModeloId,
            ModeloNombre = p.ModeloCatalogo?.Nombre,
            ImagenPrincipalUrl = p.ImagenPrincipal?.Url,
            TotalImagenes = p.Imagenes.Count,
            Imagenes = p.Imagenes.OrderBy(i => i.Orden).Select(i => new ProductoImagenDto
            {
                Id = i.Id,
                Url = i.Url,
                Orden = i.Orden,
                EsPrincipal = i.EsPrincipal
            }).ToList(),
            Variantes = variantes.Select(v => new ProductoVarianteDto
            {
                Id = v.Id,
                ProductoId = v.ProductoId,
                ProductoNombre = p.Nombre,
                ColorId = v.ColorId ?? 0,
                ColorNombre = v.Color?.Nombre ?? "Sin color",
                ColorCodigoVisual = v.Color?.CodigoVisual,
                Sku = v.Sku ?? string.Empty,
                CodigoBarras = v.CodigoBarras,
                Cantidad = v.Cantidad,
                UmbralStockBajo = v.UmbralStockBajo,
                Costo = v.Costo ?? p.Costo,
                Precio = v.Precio ?? p.Precio,
                Activo = v.Activo,
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
}
