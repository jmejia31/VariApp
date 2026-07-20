namespace InventoryApp.Domain.Entities;

public class EmpresaConfiguracion
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = "VariStorehn";
    public string? RazonSocial { get; set; }
    public string Eslogan { get; set; } = "Eleva tu mundo digital";
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsApp { get; set; }
    public string? LogoUrl { get; set; }
    public string? LogoPublicId { get; set; }
    public string NombreVisibleSistema { get; set; } = "VariStorehn";
    public string DescripcionSistema { get; set; } = "Gestión de Inventario";
    public string MensajeLogin { get; set; } = "Inicia sesión para administrar VariStorehn";
    public string Copyright { get; set; } = "© 2026 VariStorehn. Todos los derechos reservados.";
    public bool MostrarCopyright { get; set; } = true;
    public bool UsarAnioAutomaticoCopyright { get; set; } = true;
    public bool EncabezadoActivo { get; set; } = true;
    public string? EncabezadoTexto { get; set; }
    public bool PiePaginaActivo { get; set; } = true;
    public string? PiePaginaTexto { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string FormatoFecha { get; set; } = "dd/MM/yyyy";
    public string? InformacionFiscal { get; set; }
    public string? TextoLegal { get; set; }
    public string? TextoFactura { get; set; }
    public string? TextoReportes { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
