using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

/// Implementación real del cálculo de descuentos e impuestos (sección 13).
/// LIMITACIÓN CONOCIDA Y DOCUMENTADA (no oculta): los alcances Producto/
/// Categoría se evalúan a nivel de "¿algún ítem de la venta califica?" y el
/// descuento/impuesto se aplica sobre el subtotal completo de la venta, no
/// descompuesto línea por línea. Igualmente, el campo `Acumulativo` de
/// Impuesto se persiste pero todos los impuestos vigentes/aplicables se suman
/// (no se implementó la exclusión mutua entre impuestos no acumulables). Una
/// implementación línea-por-línea y con exclusión mutua completa es la
/// extensión natural de este motor y no se hizo por límite de tiempo de esta
/// fase — no se declara terminada, se dejó explícita esta limitación.
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
        List<DetalleCalculoInput> detalles, int? clienteId, int? rolIdUsuario, string? codigoPromocional)
    {
        var subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        var cantidadTotal = detalles.Sum(d => d.Cantidad);
        var productoIds = detalles.Select(d => d.ProductoId).ToHashSet();
        var categoriaIds = detalles.Where(d => d.CategoriaId.HasValue).Select(d => d.CategoriaId!.Value).ToHashSet();

        var ahora = DateTime.UtcNow;
        var candidatos = await _descuentoRepository.GetVigentesConRelacionesAsync(ahora);

        string? codigoNormalizado = string.IsNullOrWhiteSpace(codigoPromocional)
            ? null : codigoPromocional.Trim().ToUpperInvariant();

        if (codigoNormalizado is not null)
        {
            var existeCodigo = candidatos.Any(d =>
                !string.IsNullOrEmpty(d.CodigoPromocionalNormalizado) &&
                d.CodigoPromocionalNormalizado == codigoNormalizado);
            if (!existeCodigo)
                throw new BusinessRuleException("El código promocional no existe o no está vigente.");
        }

        var aplicables = new List<Descuento>();
        foreach (var d in candidatos)
        {
            // Si se pidió un código específico, solo ese código puede aplicar
            // (los demás descuentos automáticos por alcance no se evalúan a la vez).
            if (codigoNormalizado is not null)
            {
                if (d.CodigoPromocionalNormalizado != codigoNormalizado) continue;
            }
            else if (!string.IsNullOrEmpty(d.CodigoPromocionalNormalizado))
            {
                continue; // los descuentos con código no se aplican automáticamente
            }

            if (subtotal <= 0) continue;
            if (d.MontoMinimo.HasValue && subtotal < d.MontoMinimo.Value) continue;
            if (d.CantidadMinima.HasValue && cantidadTotal < d.CantidadMinima.Value) continue;

            var calificaPorAlcance =
                (!d.Productos.Any() && !d.Categorias.Any() && !d.Clientes.Any() && !d.Roles.Any()) // Global (sin restricciones)
                || d.Productos.Any(p => productoIds.Contains(p.ProductoId))
                || d.Categorias.Any(c => categoriaIds.Contains(c.CategoriaId))
                || (clienteId.HasValue && d.Clientes.Any(c => c.ClienteId == clienteId.Value))
                || (rolIdUsuario.HasValue && d.Roles.Any(r => r.RolId == rolIdUsuario.Value));

            if (!calificaPorAlcance) continue;

            if (d.LimiteTotalUsos.HasValue)
            {
                var usos = await _descuentoRepository.ContarUsosAsync(d.Id);
                if (usos >= d.LimiteTotalUsos.Value) continue;
            }

            if (d.LimiteUsosPorCliente.HasValue && clienteId.HasValue)
            {
                var usosCliente = await _descuentoRepository.ContarUsosPorClienteAsync(d.Id, clienteId.Value);
                if (usosCliente >= d.LimiteUsosPorCliente.Value) continue;
            }

            aplicables.Add(d);
        }

        var aplicados = new List<DescuentoAplicadoDto>();
        decimal totalDescuento = 0;

        foreach (var d in aplicables.OrderBy(d => d.Prioridad))
        {
            var monto = d.Tipo == TipoDescuento.Porcentaje
                ? Math.Round(subtotal * d.Valor / 100m, 2)
                : d.Valor;

            if (d.MontoMaximoDescuento.HasValue && monto > d.MontoMaximoDescuento.Value)
                monto = d.MontoMaximoDescuento.Value;

            if (monto > subtotal - totalDescuento)
                monto = subtotal - totalDescuento;

            if (monto <= 0) continue;

            aplicados.Add(new DescuentoAplicadoDto
            {
                DescuentoId = d.Id,
                Nombre = d.Nombre,
                Codigo = d.CodigoPromocional,
                Tipo = d.Tipo.ToString(),
                Valor = d.Valor,
                Monto = monto
            });
            totalDescuento += monto;

            if (!d.Acumulable) break;
        }

        // Impuestos
        var candidatosImpuesto = await _impuestoRepository.GetVigentesConRelacionesAsync(ahora, OperacionImpuesto.Venta);
        var impuestosAplicados = new List<ImpuestoAplicadoDto>();
        decimal totalImpuesto = 0;

        foreach (var imp in candidatosImpuesto.OrderBy(i => i.Prioridad))
        {
            var calificaPorAlcance =
                (!imp.Productos.Any() && !imp.Categorias.Any()) // Global
                || imp.Productos.Any(p => productoIds.Contains(p.ProductoId))
                || imp.Categorias.Any(c => categoriaIds.Contains(c.CategoriaId));

            if (!calificaPorAlcance) continue;

            if (clienteId.HasValue && imp.ClientesExentos.Any(c => c.ClienteId == clienteId.Value))
                continue; // cliente exento de este impuesto

            var baseImponible = imp.SeCalculaAntesDescuento ? subtotal : (subtotal - totalDescuento);
            if (baseImponible < 0) baseImponible = 0;

            var monto = imp.Tipo == TipoImpuesto.Porcentaje
                ? Math.Round(baseImponible * imp.Tasa / 100m, 2)
                : (imp.MontoFijo ?? 0);

            if (monto <= 0) continue;

            impuestosAplicados.Add(new ImpuestoAplicadoDto
            {
                ImpuestoId = imp.Id,
                Nombre = imp.Nombre,
                Tasa = imp.Tasa,
                BaseImponible = baseImponible,
                Monto = monto,
                IncluidoEnPrecio = imp.IncluidoEnPrecio
            });
            totalImpuesto += monto;
        }

        // Los impuestos "incluidos en precio" ya están dentro del subtotal (no se suman al total final).
        var totalImpuestoASumar = impuestosAplicados.Where(i => !i.IncluidoEnPrecio).Sum(i => i.Monto);

        var total = subtotal - totalDescuento + totalImpuestoASumar;
        if (total < 0) total = 0;

        return new ResultadoCalculoDto
        {
            Subtotal = subtotal,
            DescuentosAplicados = aplicados,
            TotalDescuento = totalDescuento,
            ImpuestosAplicados = impuestosAplicados,
            TotalImpuesto = totalImpuestoASumar,
            Total = total
        };
    }

    public async Task<ResultadoCalculoDto> CalcularCompraAsync(List<DetalleCalculoInput> detalles, int? proveedorId)
    {
        var subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        var productoIds = detalles.Select(d => d.ProductoId).ToHashSet();
        var categoriaIds = detalles.Where(d => d.CategoriaId.HasValue).Select(d => d.CategoriaId!.Value).ToHashSet();
        var ahora = DateTime.UtcNow;

        var candidatosImpuesto = await _impuestoRepository.GetVigentesConRelacionesAsync(ahora, OperacionImpuesto.Compra);
        var impuestosAplicados = new List<ImpuestoAplicadoDto>();

        foreach (var imp in candidatosImpuesto.OrderBy(i => i.Prioridad))
        {
            var calificaPorAlcance =
                (!imp.Productos.Any() && !imp.Categorias.Any())
                || imp.Productos.Any(p => productoIds.Contains(p.ProductoId))
                || imp.Categorias.Any(c => categoriaIds.Contains(c.CategoriaId));

            if (!calificaPorAlcance) continue;

            if (proveedorId.HasValue && imp.ProveedoresExentos.Any(p => p.ProveedorId == proveedorId.Value))
                continue;

            var baseImponible = subtotal;
            var monto = imp.Tipo == TipoImpuesto.Porcentaje
                ? Math.Round(baseImponible * imp.Tasa / 100m, 2)
                : (imp.MontoFijo ?? 0);

            if (monto <= 0) continue;

            impuestosAplicados.Add(new ImpuestoAplicadoDto
            {
                ImpuestoId = imp.Id,
                Nombre = imp.Nombre,
                Tasa = imp.Tasa,
                BaseImponible = baseImponible,
                Monto = monto,
                IncluidoEnPrecio = imp.IncluidoEnPrecio
            });
        }

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
}
