namespace InventoryApp.Application.DTOs;

public class EmpresaConfiguracionDto
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Eslogan { get; set; } = string.Empty;
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsApp { get; set; }
    public string? LogoUrl { get; set; }
    public string NombreVisibleSistema { get; set; } = string.Empty;
    public string DescripcionSistema { get; set; } = string.Empty;
    public string MensajeLogin { get; set; } = string.Empty;
    public string Copyright { get; set; } = string.Empty;
    public bool MostrarCopyright { get; set; }
    public bool UsarAnioAutomaticoCopyright { get; set; }
    public bool EncabezadoActivo { get; set; }
    public string? EncabezadoTexto { get; set; }
    public bool PiePaginaActivo { get; set; }
    public string? PiePaginaTexto { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string FormatoFecha { get; set; } = "dd/MM/yyyy";
    public string? InformacionFiscal { get; set; }
    public string? TextoLegal { get; set; }
    public string? TextoFactura { get; set; }
    public string? TextoReportes { get; set; }
}

public class UpdateEmpresaConfiguracionDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Eslogan { get; set; } = string.Empty;
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsApp { get; set; }
    public string NombreVisibleSistema { get; set; } = string.Empty;
    public string DescripcionSistema { get; set; } = string.Empty;
    public string MensajeLogin { get; set; } = string.Empty;
    public string Copyright { get; set; } = string.Empty;
    public bool MostrarCopyright { get; set; }
    public bool UsarAnioAutomaticoCopyright { get; set; }
    public bool EncabezadoActivo { get; set; }
    public string? EncabezadoTexto { get; set; }
    public bool PiePaginaActivo { get; set; }
    public string? PiePaginaTexto { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string FormatoFecha { get; set; } = "dd/MM/yyyy";
    public string? InformacionFiscal { get; set; }
    public string? TextoLegal { get; set; }
    public string? TextoFactura { get; set; }
    public string? TextoReportes { get; set; }
}
