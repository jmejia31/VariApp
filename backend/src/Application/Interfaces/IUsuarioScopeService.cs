namespace InventoryApp.Application.Interfaces;

/// <summary>
/// Contexto de seguridad vigente del usuario autenticado, resuelto desde la base
/// de datos y no únicamente desde los claims del JWT.
/// </summary>
public sealed record UsuarioScopeActual(
    int UsuarioId,
    int RolId,
    string RolNombre,
    bool EsAdministrador);

public interface IUsuarioScopeService
{
    /// <summary>
    /// Devuelve el usuario activo, no bloqueado y vinculado a un rol dinámico
    /// activo. Si la sesión no puede resolverse de forma segura, devuelve null
    /// para que las consultas fallen cerradas.
    /// </summary>
    Task<UsuarioScopeActual?> ObtenerActualAsync();
}
