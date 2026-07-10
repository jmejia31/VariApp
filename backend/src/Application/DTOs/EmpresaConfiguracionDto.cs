namespace InventoryApp.Application.DTOs;

public class EmpresaConfiguracionDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string Eslogan { get; set; } = string.Empty;
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateEmpresaConfiguracionDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string Eslogan { get; set; } = string.Empty;
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
}
