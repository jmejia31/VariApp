namespace InventoryApp.Application.Interfaces;

/// <summary>
/// Contexto de seguridad legacy del usuario autenticado. Se conserva únicamente
/// para consumidores todavía no migrados a un contexto tenant-aware.
/// </summary>
public sealed record UsuarioScopeActual(
    int UsuarioId,
    int RolId,
    string RolNombre,
    bool EsAdministrador);

/// <summary>
/// Contexto tenant verificado server-side. EmpresaId y RolId proceden de una
/// membresía UsuarioEmpresa activa; nunca de un claim o rol global del usuario.
/// </summary>
public sealed record UsuarioTenantScopeActual(
    int UsuarioId,
    int EmpresaId,
    int RolId,
    string RolNombre,
    bool EsAdministrador);

public interface IUsuarioScopeService
{
    /// <summary>
    /// Compatibilidad legacy. No concede autoridad sobre una empresa concreta.
    /// Los flujos tenant-owned deben usar la sobrecarga con EmpresaId.
    /// </summary>
    Task<UsuarioScopeActual?> ObtenerActualAsync();

    /// <summary>
    /// Resuelve el contexto efectivo para la empresa solicitada a partir de
    /// UsuarioEmpresa y del estado vigente de Usuario, Empresa y Rol. Cualquier
    /// ausencia, inactividad o mismatch falla cerrado devolviendo null.
    /// </summary>
    Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
        int empresaId,
        CancellationToken cancellationToken = default);
}
