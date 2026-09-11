using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Raiz de dominio canónica para una empresa/tenant.
///
/// N6.1 introduce solamente la identidad empresarial y sus invariantes mínimas.
/// El aislamiento tenant-aware, la relación física con sucursales, membresías,
/// configuración independiente y storage aislado pertenecen a parents posteriores.
/// </summary>
public sealed class Empresa : AuditableEntity
{
    private Empresa()
    {
    }

    public Empresa(string nombre)
    {
        CambiarNombre(nombre);
    }

    /// <summary>
    /// Nombre identificable de la empresa. No sustituye EmpresaConfiguracion,
    /// que conserva su responsabilidad de configuración global hasta N6.7.
    /// </summary>
    public string Nombre { get; private set; } = string.Empty;

    public bool Activa { get; private set; } = true;

    /// <summary>
    /// La Empresa usa ciclo de vida activa/inactiva y no soft-delete persistido.
    /// Esta proyección explícita mantiene compatibles los guards de aplicación
    /// sin introducir una columna o migración fuera de N6.4.D.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool Eliminado => false;

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la empresa es obligatorio.", nameof(nombre));
        }

        Nombre = nombre.Trim();
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;
}
