using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

/// Crea configuraciones fiscales iniciales solo cuando no existen. Nunca
/// reactiva, modifica tasas ni revierte decisiones realizadas desde la interfaz.
public class SeedFiscalService
{
    private readonly AppDbContext _context;

    public SeedFiscalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultsAsync()
    {
        await CrearImpuestoSiNoExisteAsync(
            codigo: "ISV15",
            nombre: "ISV 15%",
            descripcion: "Impuesto sobre ventas general, incluido en el precio de venta cuando corresponda.",
            tasa: 15m,
            prioridad: 10,
            incluidoEnPrecio: true,
            activo: true,
            operaciones: new[] { OperacionImpuesto.Venta });

        await CrearImpuestoSiNoExisteAsync(
            codigo: "ISC5",
            nombre: "ISC 5%",
            descripcion: "Impuesto selectivo al consumo disponible para compras cuando corresponda.",
            tasa: 5m,
            prioridad: 20,
            incluidoEnPrecio: true,
            activo: false,
            operaciones: new[] { OperacionImpuesto.Compra });

        await CrearDescuentoSiNoExisteAsync(
            codigo: "VARISTORE10",
            nombre: "Promoción VariStorehn 10%",
            descripcion: "Descuento promocional global para ventas usando el código VARISTORE10.",
            valor: 10m);

        await _context.SaveChangesAsync();
    }

    private async Task CrearImpuestoSiNoExisteAsync(
        string codigo,
        string nombre,
        string descripcion,
        decimal tasa,
        int prioridad,
        bool incluidoEnPrecio,
        bool activo,
        OperacionImpuesto[] operaciones)
    {
        var existe = await _context.Impuestos.AnyAsync(i => i.Codigo == codigo);
        if (existe) return;

        var impuesto = new Impuesto
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = descripcion,
            Tipo = TipoImpuesto.Porcentaje,
            Tasa = tasa,
            MontoFijo = null,
            IncluidoEnPrecio = incluidoEnPrecio,
            SeCalculaAntesDescuento = false,
            Acumulativo = true,
            Prioridad = prioridad,
            RequiereRetencion = false,
            Activo = activo,
            Eliminado = false,
            FechaCreacion = DateTime.UtcNow,
            Operaciones = operaciones
                .Distinct()
                .Select(o => new ImpuestoOperacion { Operacion = o })
                .ToList()
        };

        _context.Impuestos.Add(impuesto);
    }

    private async Task CrearDescuentoSiNoExisteAsync(
        string codigo,
        string nombre,
        string descripcion,
        decimal valor)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        var existe = await _context.Descuentos
            .AnyAsync(d => d.CodigoPromocionalNormalizado == normalizado);
        if (existe) return;

        _context.Descuentos.Add(new Descuento
        {
            CodigoPromocional = codigo,
            CodigoPromocionalNormalizado = normalizado,
            Nombre = nombre,
            Descripcion = descripcion,
            Tipo = TipoDescuento.Porcentaje,
            Valor = valor,
            RequiereAprobacion = false,
            Acumulable = true,
            Prioridad = 10,
            Activo = true,
            Eliminado = false,
            FechaCreacion = DateTime.UtcNow
        });
    }
}
