using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class DevolucionCliente : AuditableEntity
{
    public string NumeroDevolucion { get; set; } = string.Empty;
    public int VentaId { get; set; }
    public int? FacturaId { get; set; }
    public int? ClienteId { get; set; }
    public string ClienteNombreSnapshot { get; set; } = "Cliente final";
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public EstadoDevolucionCliente Estado { get; private set; } = EstadoDevolucionCliente.Borrador;
    public DateTime? FechaConfirmacionUtc { get; private set; }
    public int? ConfirmadaPorUsuarioId { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public int? AnuladaPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public ICollection<DevolucionClienteDetalle> Detalles { get; set; } = new List<DevolucionClienteDetalle>();

    public bool EsEditable => Estado == EstadoDevolucionCliente.Borrador;
    public decimal TotalReferencial => decimal.Round(Detalles.Sum(x => x.Subtotal), 4, MidpointRounding.AwayFromZero);

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una devolución de cliente en borrador puede modificarse.");
    }

    public void Confirmar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoDevolucionCliente.Borrador)
            throw new InvalidOperationException("Solo una devolución de cliente en borrador puede confirmarse.");
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
        if (fechaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de confirmación debe expresarse en UTC.", nameof(fechaUtc));

        ValidarDocumento();

        Estado = EstadoDevolucionCliente.Confirmada;
        FechaConfirmacionUtc = fechaUtc;
        ConfirmadaPorUsuarioId = usuarioId;
    }

    public void Anular(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoDevolucionCliente.Confirmada)
            throw new InvalidOperationException("Solo una devolución de cliente confirmada puede anularse.");
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));
        if (fechaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de anulación debe expresarse en UTC.", nameof(fechaUtc));

        Estado = EstadoDevolucionCliente.Anulada;
        FechaAnulacionUtc = fechaUtc;
        AnuladaPorUsuarioId = usuarioId;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroDevolucion))
            throw new InvalidOperationException("El número de devolución es obligatorio.");
        if (VentaId <= 0)
            throw new InvalidOperationException("La venta origen es obligatoria.");
        if (FacturaId is <= 0)
            throw new InvalidOperationException("La factura, cuando se informa, debe ser válida.");
        if (ClienteId is <= 0)
            throw new InvalidOperationException("El cliente, cuando se informa, debe ser válido.");
        if (string.IsNullOrWhiteSpace(ClienteNombreSnapshot))
            throw new InvalidOperationException("El snapshot del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException("El motivo de devolución es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La devolución debe contener al menos un detalle.");

        foreach (var detalle in Detalles)
            detalle.Validar();

        if (Detalles.GroupBy(x => x.VentaDetalleId).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Un detalle de venta no puede repetirse dentro de la misma devolución.");
    }
}
