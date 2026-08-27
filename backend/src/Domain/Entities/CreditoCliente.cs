using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// N3.10.B — autoridad mínima de dominio para la política de crédito comercial de un cliente.
/// La persistencia/cardinalidad física se materializa en N3.10.C; API/RBAC/UI pertenecen a N3.10.D+.
/// </summary>
public class CreditoCliente : AuditableEntity
{
    private CreditoCliente()
    {
    }

    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public string Moneda { get; private set; } = "HNL";
    public decimal LimiteCredito { get; private set; }
    public int DiasCredito { get; private set; }
    public decimal PorcentajeAlerta { get; private set; }
    public bool BloqueadoAutomaticamente { get; private set; }
    public string? MotivoBloqueo { get; private set; }
    public decimal? MontoExcepcion { get; private set; }
    public DateTime? ExcepcionVigenteHastaUtc { get; private set; }
    public string? ExcepcionAutorizadaPor { get; private set; }
    public DateTime? ExcepcionAutorizadaUtc { get; private set; }

    public static CreditoCliente Crear(
        Cliente cliente,
        string moneda,
        decimal limiteCredito,
        int diasCredito,
        decimal porcentajeAlerta)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        if (cliente.Id <= 0)
            throw new InvalidOperationException("El cliente debe estar persistido para configurar crédito.");

        var credito = new CreditoCliente
        {
            ClienteId = cliente.Id,
            Cliente = cliente
        };

        credito.ActualizarPolitica(moneda, limiteCredito, diasCredito, porcentajeAlerta);
        return credito;
    }

    public void ActualizarPolitica(
        string moneda,
        decimal limiteCredito,
        int diasCredito,
        decimal porcentajeAlerta)
    {
        Moneda = NormalizarMoneda(moneda);
        if (limiteCredito < 0m)
            throw new ArgumentOutOfRangeException(nameof(limiteCredito), "El límite de crédito no puede ser negativo.");
        if (diasCredito < 0)
            throw new ArgumentOutOfRangeException(nameof(diasCredito), "Los días de crédito no pueden ser negativos.");
        if (porcentajeAlerta <= 0m || porcentajeAlerta > 100m)
            throw new ArgumentOutOfRangeException(nameof(porcentajeAlerta), "El porcentaje de alerta debe estar entre 0 exclusivo y 100 inclusive.");

        LimiteCredito = decimal.Round(limiteCredito, 4, MidpointRounding.AwayFromZero);
        DiasCredito = diasCredito;
        PorcentajeAlerta = decimal.Round(porcentajeAlerta, 4, MidpointRounding.AwayFromZero);
    }

    public void EvaluarBloqueoAutomatico(decimal saldoComprometido, DateTime ahoraUtc)
    {
        ValidarSaldo(saldoComprometido);
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));

        var debeBloquear = saldoComprometido > LimiteCredito;
        BloqueadoAutomaticamente = debeBloquear;
        MotivoBloqueo = debeBloquear ? "LIMITE_CREDITO_EXCEDIDO" : null;

        if (!EsExcepcionVigente(ahoraUtc))
            LimpiarExcepcion();
    }

    public decimal ObtenerCreditoDisponible(decimal saldoComprometido, string moneda, DateTime ahoraUtc)
    {
        ValidarSaldo(saldoComprometido);
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));
        if (!string.Equals(Moneda, NormalizarMoneda(moneda), StringComparison.Ordinal))
            throw new InvalidOperationException("La moneda del consumo debe coincidir con la política de crédito.");

        var disponibleBase = Math.Max(0m, LimiteCredito - saldoComprometido);
        var excepcion = EsExcepcionVigente(ahoraUtc) ? MontoExcepcion.GetValueOrDefault() : 0m;
        return decimal.Round(disponibleBase + excepcion, 4, MidpointRounding.AwayFromZero);
    }

    public bool PuedeConsumir(decimal saldoComprometido, decimal montoSolicitado, string moneda, DateTime ahoraUtc)
    {
        if (montoSolicitado <= 0m)
            throw new ArgumentOutOfRangeException(nameof(montoSolicitado), "El monto solicitado debe ser mayor que cero.");

        return montoSolicitado <= ObtenerCreditoDisponible(saldoComprometido, moneda, ahoraUtc);
    }

    public bool DebeAlertar(decimal saldoComprometido)
    {
        ValidarSaldo(saldoComprometido);
        if (LimiteCredito == 0m)
            return saldoComprometido > 0m;

        var utilizacion = saldoComprometido / LimiteCredito * 100m;
        return utilizacion >= PorcentajeAlerta;
    }

    public void AutorizarExcepcion(
        decimal monto,
        DateTime vigenteHastaUtc,
        string autorizadoPor,
        DateTime ahoraUtc)
    {
        if (monto <= 0m)
            throw new ArgumentOutOfRangeException(nameof(monto), "La excepción debe autorizar un monto mayor que cero.");
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));
        ValidarUtc(vigenteHastaUtc, nameof(vigenteHastaUtc));
        if (vigenteHastaUtc <= ahoraUtc)
            throw new ArgumentOutOfRangeException(nameof(vigenteHastaUtc), "La vigencia de la excepción debe ser futura.");
        if (string.IsNullOrWhiteSpace(autorizadoPor))
            throw new ArgumentException("La autorización excepcional debe identificar al actor responsable.", nameof(autorizadoPor));

        MontoExcepcion = decimal.Round(monto, 4, MidpointRounding.AwayFromZero);
        ExcepcionVigenteHastaUtc = vigenteHastaUtc;
        ExcepcionAutorizadaPor = autorizadoPor.Trim();
        ExcepcionAutorizadaUtc = ahoraUtc;
    }

    public void RevocarExcepcion() => LimpiarExcepcion();

    private bool EsExcepcionVigente(DateTime ahoraUtc) =>
        MontoExcepcion is > 0m && ExcepcionVigenteHastaUtc is not null && ExcepcionVigenteHastaUtc > ahoraUtc;

    private void LimpiarExcepcion()
    {
        MontoExcepcion = null;
        ExcepcionVigenteHastaUtc = null;
        ExcepcionAutorizadaPor = null;
        ExcepcionAutorizadaUtc = null;
    }

    private static void ValidarSaldo(decimal saldoComprometido)
    {
        if (saldoComprometido < 0m)
            throw new ArgumentOutOfRangeException(nameof(saldoComprometido), "El saldo comprometido no puede ser negativo.");
    }

    private static void ValidarUtc(DateTime valor, string parametro)
    {
        if (valor.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);
    }

    private static string NormalizarMoneda(string moneda)
    {
        if (string.IsNullOrWhiteSpace(moneda) || moneda.Trim().Length != 3)
            throw new ArgumentException("La moneda debe usar un código de tres caracteres.", nameof(moneda));
        return moneda.Trim().ToUpperInvariant();
    }
}
