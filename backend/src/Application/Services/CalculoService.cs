using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class CalculoService : ICalculoService
{
    private readonly IDescuentoRepository _descuentoRepository;
    private readonly IImpuestoRepository _impuestoRepository;

    public CalculoService(IDescuentoRepository descuentoRepository, IImpuestoRepository impuestoRepository)
    {
        _descuentoRepository = descuentoRepository;
        _impuestoRepository = impuestoRepository;
    }

    public async Task<ResultadoCalculoDto> CalcularVentaAsync(
        List<DetalleCalculoInput> detalles,
        int? clienteId,
        int? rolIdUsuario,
        string? codigoPromocional)
    {
        var subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        var cantidadTotal = detalles.Sum(d => d.Cantidad);
        var ahora = DateTime.UtcNow;
        var descuentos = await _descuentoRepository.GetVigentesConRelacionesAsync(ahora);
        var codigoNormalizado = string.IsNullOrWhiteSpace(codigoPromocional)
            ? null
            : codigoPromocional.Trim().ToUpperInvariant();

        if (codigoNormalizado is not null && descuentos.All(d => d.CodigoPromocionalNormalizado != codigoNormalizado))
            throw new BusinessRuleException("El codigo promocional no existe o no esta vigente.");

        var descuentosAplicados = new List<DescuentoAplicadoDto>();
        decimal totalDescuento = 0;

        foreach (var descuento in descuentos.OrderBy(d => d.Prioridad))
        {
            if (codigoNormalizado is not null)
            {
                if (descuento.CodigoPromocionalNormalizado != codigoNormalizado) continue;
            }
            else if (!string.IsNullOrWhiteSpace(descuento.CodigoPromocionalNormalizado))
            {
                continue;
            }

            if (subtotal <= 0) continue;
            if (descuento.MontoMinimo.HasValue && subtotal < descuento.MontoMinimo.Value) continue;
            if (descuento.CantidadMinima.HasValue && cantidadTotal < descuento.CantidadMinima.Value) continue;
            if (descuento.Clientes.Any() && (!clienteId.HasValue || descuento.Clientes.All(c => c.ClienteId != clienteId.Value))) continue;
            if (descuento.Roles.Any() && (!rolIdUsuario.HasValue || descuento.Roles.All(r => r.RolId != rolIdUsuario.Value))) continue;

            var baseElegible = CalcularBaseElegible(
                detalles,
                descuento.Productos.Select(p => p.ProductoId),
                descuento.Categorias.Select(c => c.CategoriaId));
            if (baseElegible <= 0) continue;

            if (descuento.LimiteTotalUsos.HasValue &&
                await _descuentoRepository.ContarUsosAsync(descuento.Id) >= descuento.LimiteTotalUsos.Value)
                continue;

            if (descuento.LimiteUsosPorCliente.HasValue && clienteId.HasValue &&
                await _descuentoRepository.ContarUsosPorClienteAsync(descuento.Id, clienteId.Value) >= descuento.LimiteUsosPorCliente.Value)
                continue;

            var monto = descuento.Tipo == TipoDescuento.Porcentaje
                ? Math.Round(baseElegible * descuento.Valor / 100m, 2)
                : descuento.Valor;

            if (descuento.MontoMaximoDescuento.HasValue && monto > descuento.MontoMaximoDescuento.Value)
                monto = descuento.MontoMaximoDescuento.Value;

            var disponible = Math.Max(0, subtotal - totalDescuento);
            if (monto > disponible) monto = disponible;
            if (monto <= 0) continue;

            descuentosAplicados.Add(new DescuentoAplicadoDto
            {
                DescuentoId = descuento.Id,
                Nombre = descuento.Nombre,
                Codigo = descuento.CodigoPromocional,
                Tipo = descuento.Tipo.ToString(),
                Valor = descuento.Valor,
                Monto = monto
            });
            totalDescuento += monto;

            if (!descuento.Acumulable) break;
        }

        var impuestosAplicados = await CalcularImpuestosAsync(
            detalles,
            OperacionImpuesto.Venta,
            subtotal,
            totalDescuento,
            clienteId,
            proveedorId: null);

        var totalImpuesto = impuestosAplicados.Where(i => !i.IncluidoEnPrecio).Sum(i => i.Monto);
        var total = Math.Max(0, subtotal - totalDescuento + totalImpuesto);

        return new ResultadoCalculoDto
        {
            Subtotal = subtotal,
            DescuentosAplicados = descuentosAplicados,
            TotalDescuento = totalDescuento,
            ImpuestosAplicados = impuestosAplicados,
            TotalImpuesto = totalImpuesto,
            Total = total
        };
    }

    public async Task<ResultadoCalculoDto> CalcularCompraAsync(List<DetalleCalculoInput> detalles, int? proveedorId)
    {
        var subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        var impuestosAplicados = await CalcularImpuestosAsync(
            detalles,
            OperacionImpuesto.Compra,
            subtotal,
            totalDescuento: 0,
            clienteId: null,
            proveedorId);

        var totalImpuesto = impuestosAplicados.Where(i => !i.IncluidoEnPrecio).Sum(i => i.Monto);

        return new ResultadoCalculoDto
        {
            Subtotal = subtotal,
            DescuentosAplicados = new List<DescuentoAplicadoDto>(),
            TotalDescuento = 0,
            ImpuestosAplicados = impuestosAplicados,
            TotalImpuesto = totalImpuesto,
            Total = subtotal + totalImpuesto
        };
    }

    public async Task RegistrarUsoVentaAsync(int ventaId, int? clienteId, List<VentaDescuento> descuentos, List<VentaImpuesto> impuestos)
    {
        foreach (var d in descuentos)
        {
            await _descuentoRepository.AddHistorialAsync(new HistorialUsoDescuento
            {
                DescuentoId = d.DescuentoId,
                VentaId = ventaId,
                ClienteId = clienteId,
                MontoAplicado = d.MontoAplicado
            });

            var entidad = await _descuentoRepository.GetByIdAsync(d.DescuentoId);
            if (entidad is not null)
            {
                entidad.UsosRealizados += 1;
                _descuentoRepository.Update(entidad);
            }
        }

        foreach (var i in impuestos)
        {
            await _impuestoRepository.AddHistorialAsync(new HistorialAplicacionImpuesto
            {
                ImpuestoId = i.ImpuestoId,
                DocumentoTipo = "Venta",
                DocumentoId = ventaId,
                BaseImponible = i.BaseImponible,
                TasaAplicada = i.TasaSnapshot,
                MontoAplicado = i.MontoAplicado
            });
        }

        await _descuentoRepository.SaveChangesAsync();
        await _impuestoRepository.SaveChangesAsync();
    }

    public async Task RegistrarUsoCompraAsync(int compraId, List<CompraImpuesto> impuestos)
    {
        foreach (var i in impuestos)
        {
            await _impuestoRepository.AddHistorialAsync(new HistorialAplicacionImpuesto
            {
                ImpuestoId = i.ImpuestoId,
                DocumentoTipo = "Compra",
                DocumentoId = compraId,
                BaseImponible = i.BaseImponible,
                TasaAplicada = i.TasaSnapshot,
                MontoAplicado = i.MontoAplicado
            });
        }

        await _impuestoRepository.SaveChangesAsync();
    }

    private async Task<List<ImpuestoAplicadoDto>> CalcularImpuestosAsync(
        List<DetalleCalculoInput> detalles,
        OperacionImpuesto operacion,
        decimal subtotal,
        decimal totalDescuento,
        int? clienteId,
        int? proveedorId)
    {
        var candidatos = await _impuestoRepository.GetVigentesConRelacionesAsync(DateTime.UtcNow, operacion);
        var impuestosAplicados = new List<ImpuestoAplicadoDto>();

        foreach (var impuesto in candidatos.OrderBy(i => i.Prioridad))
        {
            if (operacion == OperacionImpuesto.Venta &&
                clienteId.HasValue &&
                impuesto.ClientesExentos.Any(c => c.ClienteId == clienteId.Value))
                continue;

            if (operacion == OperacionImpuesto.Compra &&
                proveedorId.HasValue &&
                impuesto.ProveedoresExentos.Any(p => p.ProveedorId == proveedorId.Value))
                continue;

            var baseElegible = CalcularBaseElegible(
                detalles,
                impuesto.Productos.Select(p => p.ProductoId),
                impuesto.Categorias.Select(c => c.CategoriaId));
            if (baseElegible <= 0) continue;

            var descuentoProrrateado = subtotal <= 0 ? 0 : Math.Round(totalDescuento * (baseElegible / subtotal), 2);
            var baseImponible = impuesto.SeCalculaAntesDescuento ? baseElegible : Math.Max(0, baseElegible - descuentoProrrateado);
            var monto = impuesto.Tipo == TipoImpuesto.Porcentaje
                ? Math.Round(baseImponible * impuesto.Tasa / 100m, 2)
                : impuesto.MontoFijo ?? 0;

            if (monto <= 0) continue;

            impuestosAplicados.Add(new ImpuestoAplicadoDto
            {
                ImpuestoId = impuesto.Id,
                Nombre = impuesto.Nombre,
                Codigo = impuesto.Codigo,
                Tasa = impuesto.Tasa,
                BaseImponible = baseImponible,
                Monto = monto,
                IncluidoEnPrecio = impuesto.IncluidoEnPrecio
            });

            if (!impuesto.Acumulativo) break;
        }

        return impuestosAplicados;
    }

    private static decimal CalcularBaseElegible(
        List<DetalleCalculoInput> detalles,
        IEnumerable<int> productoIds,
        IEnumerable<int> categoriaIds)
    {
        var productos = productoIds.ToHashSet();
        var categorias = categoriaIds.ToHashSet();

        if (productos.Count == 0 && categorias.Count == 0)
            return detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

        return detalles
            .Where(d => productos.Contains(d.ProductoId) ||
                (d.CategoriaId.HasValue && categorias.Contains(d.CategoriaId.Value)))
            .Sum(d => d.Cantidad * d.PrecioUnitario);
    }
}
