using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IProductoEscanerService
{
    Task<ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>> ResolverParaVentaAsync(
        string codigo,
        CancellationToken cancellationToken = default);

    Task<ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>> ResolverParaCompraAsync(
        string codigo,
        CancellationToken cancellationToken = default);

    Task<List<ProductoEscaneadoVentaDto>> BuscarParaVentaAsync(
        string termino,
        int limite = 30,
        CancellationToken cancellationToken = default);

    Task<List<ProductoEscaneadoCompraDto>> BuscarParaCompraAsync(
        string termino,
        int limite = 30,
        CancellationToken cancellationToken = default);
}
