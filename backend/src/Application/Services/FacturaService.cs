using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class FacturaService : IFacturaService
{
    private readonly IFacturaRepository _repository;
    private readonly IEmpresaConfiguracionService _empresaConfiguracionService;

    public FacturaService(IFacturaRepository repository, IEmpresaConfiguracionService empresaConfiguracionService)
    {
        _repository = repository;
        _empresaConfiguracionService = empresaConfiguracionService;
    }

    public async Task<FacturaDto?> GetByIdAsync(int id)
    {
        var factura = await _repository.GetByIdAsync(id);
        return factura is null ? null : await ToDtoAsync(factura);
    }

    public async Task<FacturaDto?> GetByVentaIdAsync(int ventaId)
    {
        var factura = await _repository.GetByVentaIdAsync(ventaId);
        return factura is null ? null : await ToDtoAsync(factura);
    }

    public async Task<List<FacturaDto>> GetAllAsync()
    {
        var facturas = await _repository.GetAllAsync();
        var resultado = new List<FacturaDto>();
        foreach (var f in facturas)
            resultado.Add(await ToDtoAsync(f));
        return resultado;
    }

    private async Task<FacturaDto> ToDtoAsync(Factura f)
    {
        var empresa = await _empresaConfiguracionService.GetActivaAsync();
        var dto = ToDto(f);
        dto.EmpresaLogoUrl = empresa?.LogoUrl;
        return dto;
    }

    private static FacturaDto ToDto(Factura f) => new()
    {
        Id = f.Id,
        VentaId = f.VentaId,
        NumeroVentaOrigen = f.Venta?.NumeroVenta ?? string.Empty,
        NumeroFactura = f.NumeroFactura,
        FechaEmision = f.FechaEmision,
        Estado = f.Estado.ToString(),
        EmpresaNombre = f.EmpresaNombre,
        EmpresaRTN = f.EmpresaRTN,
        EmpresaTelefono = f.EmpresaTelefono,
        EmpresaCorreo = f.EmpresaCorreo,
        EmpresaDireccion = f.EmpresaDireccion,
        ClienteNombre = f.ClienteNombre,
        ClienteTelefono = f.ClienteTelefono,
        ClienteIdentidadORTN = f.ClienteIdentidadORTN,
        ClienteCorreo = f.ClienteCorreo,
        ClienteDireccion = f.ClienteDireccion,
        VendedorNombreUsuario = f.VendedorNombreUsuario,
        GeneradaPorNombreUsuario = f.GeneradaPorNombreUsuario,
        Subtotal = f.Subtotal,
        Descuento = f.Descuento,
        Impuesto = f.Impuesto,
        Total = f.Total,
        MetodoPago = f.Venta?.MetodoPago.ToString() ?? string.Empty,
        EstadoPago = f.Venta?.EstadoPago.ToString() ?? string.Empty,
        Observaciones = f.Observaciones,
        Detalles = f.Detalles.Select(d => new FacturaDetalleDto
        {
            ProductoNombre = d.ProductoNombre,
            ProductoMarca = d.ProductoMarca,
            ProductoModelo = d.ProductoModelo,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Descuento = d.Descuento,
            Subtotal = d.Subtotal
        }).ToList(),
        FechaAnulacion = f.FechaAnulacion,
        AnuladaPorNombreUsuario = f.AnuladaPorNombreUsuario,
        MotivoAnulacion = f.MotivoAnulacion
    };
}
