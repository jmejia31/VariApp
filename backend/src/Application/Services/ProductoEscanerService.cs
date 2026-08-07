using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public sealed class ProductoEscanerService : IProductoEscanerService
{
    private const int LongitudMaximaCodigo = 120;
    private readonly IProductoVarianteRepository _varianteRepository;

    public ProductoEscanerService(IProductoVarianteRepository varianteRepository)
    {
        _varianteRepository = varianteRepository;
    }

    public async Task<ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>> ResolverParaVentaAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var resolucion = await ResolverVarianteAsync(codigo, cancellationToken);
        if (resolucion.Estado != EstadoResolucionProductoEscaner.Encontrado)
        {
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Fallo(
                resolucion.Estado,
                resolucion.Mensaje);
        }

        var variante = resolucion.Dato!;
        if (variante.Cantidad <= 0)
        {
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Fallo(
                EstadoResolucionProductoEscaner.NoOperativo,
                "El producto escaneado no tiene existencias disponibles para la venta.");
        }

        return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Encontrado(
            new ProductoEscaneadoVentaDto
            {
                ProductoId = variante.ProductoId,
                ProductoVarianteId = variante.Id,
                ProductoNombre = variante.Producto.Nombre,
                Marca = variante.Producto.Marca,
                Modelo = variante.Producto.Modelo,
                EsVarianteTecnica = variante.EsTecnica,
                ColorId = variante.ColorId,
                ColorNombre = variante.Color?.Nombre,
                Sku = variante.Sku ?? string.Empty,
                CodigoBarras = variante.CodigoBarras,
                CantidadDisponible = variante.Cantidad,
                Precio = variante.Precio ?? variante.Producto.Precio
            });
    }

    public async Task<ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>> ResolverParaCompraAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var resolucion = await ResolverVarianteAsync(codigo, cancellationToken);
        if (resolucion.Estado != EstadoResolucionProductoEscaner.Encontrado)
        {
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>.Fallo(
                resolucion.Estado,
                resolucion.Mensaje);
        }

        var variante = resolucion.Dato!;
        return ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>.Encontrado(
            new ProductoEscaneadoCompraDto
            {
                ProductoId = variante.ProductoId,
                ProductoVarianteId = variante.Id,
                ProductoNombre = variante.Producto.Nombre,
                Marca = variante.Producto.Marca,
                Modelo = variante.Producto.Modelo,
                EsVarianteTecnica = variante.EsTecnica,
                ColorId = variante.ColorId,
                ColorNombre = variante.Color?.Nombre,
                Sku = variante.Sku ?? string.Empty,
                CodigoBarras = variante.CodigoBarras,
                CantidadDisponible = variante.Cantidad,
                Costo = variante.Costo ?? variante.Producto.Costo,
                Precio = variante.Precio ?? variante.Producto.Precio
            });
    }

    private async Task<ResultadoResolucionProductoEscaner<ProductoVariante>> ResolverVarianteAsync(
        string codigo,
        CancellationToken cancellationToken)
    {
        var codigoNormalizado = codigo?.Trim() ?? string.Empty;
        if (codigoNormalizado.Length == 0)
        {
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(
                EstadoResolucionProductoEscaner.EntradaInvalida,
                "Ingresa un SKU o código de barras.");
        }

        if (codigoNormalizado.Length > LongitudMaximaCodigo)
        {
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(
                EstadoResolucionProductoEscaner.EntradaInvalida,
                $"El código no puede superar {LongitudMaximaCodigo} caracteres.");
        }

        var skuNormalizado = codigoNormalizado.ToUpperInvariant();
        var coincidencias = await _varianteRepository.BuscarPorCodigoAsync(
            skuNormalizado,
            codigoNormalizado,
            cancellationToken);

        if (coincidencias.Count == 0)
        {
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(
                EstadoResolucionProductoEscaner.NoEncontrado,
                "No se encontró un producto con el SKU o código de barras indicado.");
        }

        if (coincidencias.Count > 1)
        {
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(
                EstadoResolucionProductoEscaner.Conflicto,
                "El código coincide con más de una variante. Corrige los identificadores antes de continuar.");
        }

        var variante = coincidencias[0];
        if (variante.Eliminado ||
            !variante.Activo ||
            variante.Producto is null ||
            variante.Producto.Eliminado ||
            !variante.Producto.Activo)
        {
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(
                EstadoResolucionProductoEscaner.NoOperativo,
                "El producto o su variante están inactivos y no pueden utilizarse en esta operación.");
        }

        return ResultadoResolucionProductoEscaner<ProductoVariante>.Encontrado(variante);
    }
}
