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

    public async Task<FacturaDto?> GetByIdParaEnlacePublicoValidadoAsync(int id)
    {
        var factura = await _repository.GetByIdParaEnlacePublicoValidadoAsync(id);
        return factura is null ? null : await ToDtoAsync(factura);
    }

    private async Task<FacturaDto> ToDtoAsync(Factura f)
    {
        var empresa = await _empresaConfiguracionService.GetActivaAsync();
        var dto = ToDto(f);
        dto.EmpresaLogoUrl = empresa?.LogoUrl;
        dto.EmpresaEslogan = empresa?.Eslogan;
        dto.EmpresaTextoFactura = empresa?.TextoFactura;
        dto.EmpresaTextoLegal = empresa?.TextoLegal;
        dto.EmpresaCopyright = empresa?.MostrarCopyright == true ? empresa.Copyright : null;
        return dto;
    }

    private static FacturaDto ToDto(Factura f)
    {
        var detalles = f.Detalles.Select(d => new FacturaDetalleDto
        {
            ProductoNombre = d.ProductoNombre,
            ProductoMarca = d.ProductoMarca,
            ProductoModelo = d.ProductoModelo,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Descuento = d.Descuento,
            Subtotal = d.Subtotal
        }).ToList();

        var importeBruto = detalles.Sum(d => d.Subtotal);
        var impuestos = f.Venta?.ImpuestosAplicados.Select(i => new ImpuestoAplicadoDto
        {
            ImpuestoId = i.ImpuestoId,
            Nombre = i.ImpuestoNombreSnapshot,
            Codigo = i.ImpuestoCodigoSnapshot,
            Tasa = i.TasaSnapshot,
            BaseImponible = i.BaseImponible,
            Monto = i.MontoAplicado,
            IncluidoEnPrecio = i.IncluidoEnPrecioSnapshot
        }).ToList() ?? new List<ImpuestoAplicadoDto>();

        // Compatibilidad con documentos históricos creados antes de almacenar el
        // snapshot: si el total ya coincide con el importe después del descuento,
        // los impuestos existentes se consideran incluidos en el precio.
        var totalDespuesDescuento = Math.Max(0, importeBruto - f.Descuento);
        if (impuestos.Count > 0 &&
            impuestos.All(i => !i.IncluidoEnPrecio) &&
            Math.Abs(f.Total - totalDespuesDescuento) <= 0.01m)
        {
            foreach (var impuesto in impuestos)
                impuesto.IncluidoEnPrecio = true;
        }

        var impuestoIncluido = impuestos.Where(i => i.IncluidoEnPrecio).Sum(i => i.Monto);
        var impuestoAdicional = impuestos.Where(i => !i.IncluidoEnPrecio).Sum(i => i.Monto);

        return new FacturaDto
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
            ImporteBruto = importeBruto,
            Subtotal = f.Subtotal,
            Descuento = f.Descuento,
            Impuesto = f.Impuesto,
            ImpuestoIncluido = impuestoIncluido,
            ImpuestoAdicional = impuestoAdicional,
            Total = f.Total,
            MetodoPago = f.Venta?.MetodoPago.ToString() ?? string.Empty,
            EstadoPago = f.Venta?.EstadoPago.ToString() ?? string.Empty,
            Observaciones = f.Observaciones,
            Detalles = detalles,
            DescuentosAplicados = f.Venta?.DescuentosAplicados.Select(d => new DescuentoAplicadoDto
            {
                DescuentoId = d.DescuentoId,
                Nombre = d.DescuentoNombreSnapshot,
                Codigo = d.DescuentoCodigoSnapshot,
                Tipo = d.TipoSnapshot.ToString(),
                Valor = d.ValorSnapshot,
                Monto = d.MontoAplicado
            }).ToList() ?? new List<DescuentoAplicadoDto>(),
            ImpuestosAplicados = impuestos,
            FechaAnulacion = f.FechaAnulacion,
            AnuladaPorNombreUsuario = f.AnuladaPorNombreUsuario,
            MotivoAnulacion = f.MotivoAnulacion
        };
    }
}
