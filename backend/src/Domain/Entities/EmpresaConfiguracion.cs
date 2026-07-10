namespace InventoryApp.Domain.Entities;

public class EmpresaConfiguracion
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = "VariStorehn";
    public string Eslogan { get; set; } = "Eleva tu mundo digital";
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
    public bool Activa { get; set; } = true;
}
