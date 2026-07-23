using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Mappings;

public static class ProductoMapper
{
    public static ProductoDto ToDto(Producto p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Marca = p.Marca,
        Modelo = p.Modelo,
        Descripcion = p.Descripcion,
        Cantidad = p.Cantidad,
        Costo = p.Costo,
        Precio = p.Precio,
        UmbralStockBajo = p.UmbralStockBajo,
        TieneStockBajo = p.TieneStockBajo,
        Activo = p.Activo,
        CategoriaId = p.CategoriaId,
        CategoriaNombre = p.Categoria?.Nombre,
        ImagenPrincipalUrl = p.ImagenPrincipal?.Url,
        TotalImagenes = p.Imagenes.Count,
        Imagenes = p.Imagenes
            .OrderBy(i => i.Orden)
            .Select(i => new ProductoImagenDto
            {
                Id = i.Id,
                Url = i.Url,
                Orden = i.Orden,
                EsPrincipal = i.EsPrincipal
            })
            .ToList(),
        CreadoPorUsuarioId = p.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = p.CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId = p.ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario = p.ActualizadoPorNombreUsuario,
        FechaCreacion = p.FechaCreacion,
        FechaActualizacion = p.FechaActualizacion
    };
}
