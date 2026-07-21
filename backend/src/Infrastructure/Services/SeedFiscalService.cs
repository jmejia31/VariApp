using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public class SeedFiscalService
{
    private readonly AppDbContext _context;

    public SeedFiscalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultsAsync()
    {
        await EnsureImpuestoAsync(
            codigo: "ISV15",
            nombre: "ISV 15%",
            descripcion: "Impuesto sobre ventas general de Honduras.",
            tasa: 15m,
            prioridad: 10,
            operaciones: new[] { OperacionImpuesto.Venta, OperacionImpuesto.Compra });

        await EnsureImpuestoAsync(
            codigo: "ISC5",
            nombre: "ISC 5%",
            descripcion: "Impuesto selectivo al consumo para compras cuando aplique.",
            tasa: 5m,
            prioridad: 20,
            operaciones: new[] { OperacionImpuesto.Compra });

        await EnsureDescuentoAsync(
            codigo: "VARISTORE10",
            nombre: "Promocion VariStorehn 10%",
            descripcion: "Descuento promocional global para ventas usando el codigo VARISTORE10.",
            valor: 10m);

        await _context.SaveChangesAsync();
    }

    private async Task EnsureImpuestoAsync(
        string codigo,
        string nombre,
        string descripcion,
        decimal tasa,
        int prioridad,
        OperacionImpuesto[] operaciones)
    {
        var impuesto = await _context.Impuestos
            .Include(i => i.Operaciones)
            .FirstOrDefaultAsync(i => i.Codigo == codigo);

        if (impuesto is null)
        {
            impuesto = new Impuesto
            {
                Codigo = codigo,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Impuestos.Add(impuesto);
        }

        impuesto.Nombre = nombre;
        impuesto.Descripcion = descripcion;
        impuesto.Tipo = TipoImpuesto.Porcentaje;
        impuesto.Tasa = tasa;
        impuesto.MontoFijo = null;
        impuesto.IncluidoEnPrecio = false;
        impuesto.SeCalculaAntesDescuento = false;
        impuesto.Acumulativo = true;
        impuesto.Prioridad = prioridad;
        impuesto.RequiereRetencion = false;
        impuesto.Activo = true;
        impuesto.Eliminado = false;
        impuesto.FechaEliminacion = null;
        impuesto.FechaActualizacion = DateTime.UtcNow;

        foreach (var operacion in operaciones)
        {
            if (!impuesto.Operaciones.Any(o => o.Operacion == operacion))
            {
                impuesto.Operaciones.Add(new ImpuestoOperacion { Operacion = operacion });
            }
        }
    }

    private async Task EnsureDescuentoAsync(string codigo, string nombre, string descripcion, decimal valor)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        var descuento = await _context.Descuentos
            .FirstOrDefaultAsync(d => d.CodigoPromocionalNormalizado == normalizado);

        if (descuento is null)
        {
            descuento = new Descuento
            {
                CodigoPromocional = codigo,
                CodigoPromocionalNormalizado = normalizado,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Descuentos.Add(descuento);
        }

        descuento.Nombre = nombre;
        descuento.Descripcion = descripcion;
        descuento.Tipo = TipoDescuento.Porcentaje;
        descuento.Valor = valor;
        descuento.MontoMinimo = null;
        descuento.MontoMaximoDescuento = null;
        descuento.CantidadMinima = null;
        descuento.RequiereAprobacion = false;
        descuento.Acumulable = true;
        descuento.Prioridad = 10;
        descuento.LimiteTotalUsos = null;
        descuento.LimiteUsosPorCliente = null;
        descuento.Activo = true;
        descuento.Eliminado = false;
        descuento.FechaEliminacion = null;
        descuento.FechaActualizacion = DateTime.UtcNow;
    }
}
