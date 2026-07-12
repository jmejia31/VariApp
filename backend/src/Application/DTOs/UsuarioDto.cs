namespace InventoryApp.Application.DTOs;

public class UsuarioDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateUsuarioDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "Vendedor"; // "Administrador" | "Vendedor" (legado, fallback)

    /// Id del rol dinámico (Domain.Entities.Rol). Si se informa, tiene prioridad
    /// sobre `Rol` y es la vía recomendada de asignación (sección 4/10: AsignarRol).
    public int? RolId { get; set; }
}

public class UpdateUsuarioEstadoDto
{
    public bool Activo { get; set; }
}

public class UpdateUsuarioDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = "Vendedor";
    public int? RolId { get; set; }

    /// Si se informa, se resetea la contraseña del usuario. Si es null/vacío, se conserva la actual.
    public string? NuevaPassword { get; set; }
}
