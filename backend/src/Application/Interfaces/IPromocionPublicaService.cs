using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IPromocionPublicaService
{
    Task<OfertaPublicaDto?> ResolverAsync(
        int productoId,
        int? categoriaId,
        decimal precioNormal,
        DateTime fechaUtc);
}
