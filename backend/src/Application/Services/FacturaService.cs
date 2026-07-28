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
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            ProductoNombre = d.ProductoNombre,
            ProductoMarca = d.ProductoMarca,
            ProductoModelo = d.ProductoModelo,
            VarianteColor = d.VarianteColor,
            VarianteSku = d.VarianteSku,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Descuento = d.Descuento,
            Impuesto = d.Impuesto,
            Subtotal = d.Subtotal,
            TotalLinea = d.TotalLinea == 0 ? d.Subtotal : d.TotalLinea,
            Observaciones = d.Observaciones
        }).ToList();

        var importeBruto = f.ImporteBruto > 0
            ? f.ImporteBruto
            : detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
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
        var pagos = f.Pagos
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new FacturaPagoDto
            {
                Id = p.Id,
                FechaPago = p.FechaPago,
                Monto = p.Monto,
                MetodoPago = p.MetodoPago.ToString(),
                Referencia = p.Referencia,
                Observaciones = p.Observaciones,
                Anulado = p.Anulado
            }).ToList();
        var totalPagado = f.TotalPagado > 0
            ? f.TotalPagado
            : pagos.Where(p => !p.Anulado).Sum(p => p.Monto);
        var saldoPendiente = f.SaldoPendiente > 0 || f.Total <= totalPagado
            ? f.SaldoPendiente
            : Math.Max(0, f.Total - totalPagado);

        return new FacturaDto
        {
            Id = f.Id,
            VentaId = f.VentaId,
            NumeroVentaOrigen = f.Venta?.NumeroVenta ?? string.Empty,
            NumeroFactura = f.NumeroFactura,
            CodigoInterno = f.CodigoInterno,
            FechaEmision = f.FechaEmision,
            FechaVencimiento = f.FechaVencimiento,
            Estado = f.Estado.ToString(),
            Moneda = f.Moneda,
            CondicionPago = f.CondicionPago,
            Referencia = f.Referencia,
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
            CostoEnvio = f.CostoEnvio,
            CostoEnvioId = f.CostoEnvioId,
            CostoEnvioNombre = f.CostoEnvioNombreSnapshot,
            EnvioExonerado = f.EnvioExonerado,
            MotivoExoneracionEnvio = f.MotivoExoneracionEnvio,
            Total = f.Total,
            TotalPagado = totalPagado,
            SaldoPendiente = saldoPendiente,
            MetodoPago = f.Venta?.MetodoPago.ToString() ?? string.Empty,
            EstadoPago = f.Venta?.EstadoPago.ToString() ?? string.Empty,
            Observaciones = f.Observaciones,
            Detalles = detalles,
            Pagos = pagos,
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
