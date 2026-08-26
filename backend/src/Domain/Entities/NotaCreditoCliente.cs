using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// N3.7.B — contrato mínimo de dominio para una nota de crédito de cliente ligada a una factura.
/// No materializa por sí misma devolución física, stock, Kardex, caja, aplicación de saldo ni efectos fiscales externos.
/// Esos efectos permanecen separados y deben resolverse en capas/puntos posteriores con evidencia explícita.
/// </summary>
public class NotaCreditoCliente : AuditableEntity
{
    private NotaCreditoCliente()
    {
    }

    public int FacturaId { get; private set; }
    public Factura Factura { get; private set; } = null!;
    public int VentaId { get; private set; }
    public string Moneda { get; private set; } = "HNL";
    public decimal MontoCredito { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public string? Observaciones { get; private set; }
    public EstadoNotaCreditoCliente Estado { get; private set; } = EstadoNotaCreditoCliente.Borrador;
    public DateTime? FechaEmisionUtc { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public bool EsEditable => Estado == EstadoNotaCreditoCliente.Borrador;

    public static NotaCreditoCliente CrearDesdeFactura(
        Factura factura,
        decimal montoCredito,
        string motivo,
        string? observaciones = null)
    {
        ArgumentNullException.ThrowIfNull(factura);

        if (factura.Id <= 0)
            throw new InvalidOperationException("La factura de origen debe estar persistida.");
        if (factura.VentaId <= 0)
            throw new InvalidOperationException("La factura debe estar asociada a una venta válida.");
        if (factura.Estado is EstadoFactura.Borrador or EstadoFactura.Anulada or EstadoFactura.Cancelada)
            throw new InvalidOperationException("La factura no está en un estado elegible para nota de crédito.");
        if (string.IsNullOrWhiteSpace(factura.Moneda) || factura.Moneda.Trim().Length != 3)
            throw new InvalidOperationException("La moneda de la factura debe usar un código de tres caracteres.");
        if (montoCredito <= 0m)
            throw new ArgumentOutOfRangeException(nameof(montoCredito), "El monto acreditado debe ser mayor que cero.");
        if (factura.Total <= 0m || montoCredito > factura.Total)
            throw new InvalidOperationException("El monto acreditado no puede superar el total de la factura de origen.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la nota de crédito es obligatorio.", nameof(motivo));

        return new NotaCreditoCliente
        {
            FacturaId = factura.Id,
            Factura = factura,
            VentaId = factura.VentaId,
            Moneda = factura.Moneda.Trim().ToUpperInvariant(),
            MontoCredito = decimal.Round(montoCredito, 4, MidpointRounding.AwayFromZero),
            Motivo = motivo.Trim(),
            Observaciones = Normalizar(observaciones)
        };
    }

    public void Actualizar(decimal montoCredito, string motivo, string? observaciones)
    {
        AsegurarEditable();

        if (montoCredito <= 0m || montoCredito > Factura.Total)
            throw new InvalidOperationException("El monto acreditado debe ser mayor que cero y no superar el total de la factura.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la nota de crédito es obligatorio.", nameof(motivo));

        MontoCredito = decimal.Round(montoCredito, 4, MidpointRounding.AwayFromZero);
        Motivo = motivo.Trim();
        Observaciones = Normalizar(observaciones);
    }

    public void Emitir(DateTime fechaUtc)
    {
        if (Estado != EstadoNotaCreditoCliente.Borrador)
            throw new InvalidOperationException("Solo una nota de crédito en borrador puede emitirse.");
        if (fechaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de emisión debe expresarse en UTC.", nameof(fechaUtc));

        ValidarDocumento();
        Estado = EstadoNotaCreditoCliente.Emitida;
        FechaEmisionUtc = fechaUtc;
    }

    public void Anular(string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoNotaCreditoCliente.Emitida)
            throw new InvalidOperationException("Solo una nota de crédito emitida puede anularse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));
        if (fechaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de anulación debe expresarse en UTC.", nameof(fechaUtc));

        Estado = EstadoNotaCreditoCliente.Anulada;
        FechaAnulacionUtc = fechaUtc;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (FacturaId <= 0 || VentaId <= 0)
            throw new InvalidOperationException("La nota de crédito debe conservar una factura y venta de origen válidas.");
        if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Length != 3)
            throw new InvalidOperationException("La moneda debe usar un código de tres caracteres.");
        if (MontoCredito <= 0m || MontoCredito > Factura.Total)
            throw new InvalidOperationException("El monto acreditado no es válido para la factura de origen.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException("El motivo de la nota de crédito es obligatorio.");
    }

    private void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una nota de crédito en borrador puede modificarse.");
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
