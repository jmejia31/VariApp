using InventoryApp.Domain.Security;

namespace InventoryApp.Application.Interfaces;

/// <summary>
/// Contexto de almacenamiento tenant-aware construido exclusivamente desde un
/// <see cref="ContextoTenantActual"/> ya verificado por el servidor.
///
/// Este contrato separa identidad/autorización del actor de la propiedad tenant
/// del recurso y evita que rutas, carpetas o EmpresaId enviados por el cliente se
/// utilicen como autoridad de almacenamiento.
/// </summary>
public sealed record StorageTenantContext
{
    private StorageTenantContext(int empresaId, int usuarioId)
    {
        EmpresaId = empresaId;
        UsuarioId = usuarioId;
    }

    public int EmpresaId { get; }

    /// <summary>
    /// Actor autenticado que originó la operación. Es metadato de autorización/
    /// auditoría y nunca sustituye <see cref="EmpresaId"/> como scope del recurso.
    /// </summary>
    public int UsuarioId { get; }

    /// <summary>
    /// Prefijo lógico canónico para persistencia tenant-owned. La capa de
    /// infraestructura puede anteponer además su prefijo de ambiente.
    /// </summary>
    public string TenantPrefix => $"empresas/{EmpresaId}";

    /// <summary>
    /// Construye el scope únicamente desde un contexto tenant previamente
    /// verificado. La ausencia de tenant falla cerrado.
    /// </summary>
    public static StorageTenantContext Desde(ContextoTenantActual? contexto)
    {
        if (contexto is null)
        {
            throw new InvalidOperationException(
                "Se requiere un contexto tenant verificado para operar almacenamiento tenant-owned.");
        }

        if (contexto.EmpresaId <= 0 || contexto.UsuarioId <= 0)
        {
            throw new InvalidOperationException(
                "El contexto tenant verificado contiene identificadores inválidos.");
        }

        return new StorageTenantContext(contexto.EmpresaId, contexto.UsuarioId);
    }

    /// <summary>
    /// Construye el scope de almacenamiento desde el resultado tenant-aware de
    /// <see cref="IUsuarioScopeService"/>. Ese resultado ya fue resuelto
    /// server-side contra una membresía UsuarioEmpresa activa; nunca se aceptan
    /// EmpresaId/UsuarioId suministrados directamente por el cliente.
    /// </summary>
    public static StorageTenantContext Desde(UsuarioTenantScopeActual? contexto)
    {
        if (contexto is null)
        {
            throw new InvalidOperationException(
                "Se requiere un contexto tenant verificado para operar almacenamiento tenant-owned.");
        }

        if (contexto.EmpresaId <= 0 || contexto.UsuarioId <= 0)
        {
            throw new InvalidOperationException(
                "El contexto tenant verificado contiene identificadores inválidos.");
        }

        return new StorageTenantContext(contexto.EmpresaId, contexto.UsuarioId);
    }

    /// <summary>
    /// Impide reutilizar este scope para un recurso de otra Empresa.
    /// </summary>
    public void ExigirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(empresaId),
                "El identificador de Empresa debe ser positivo.");
        }

        if (EmpresaId != empresaId)
        {
            throw new InvalidOperationException(
                "El recurso de almacenamiento no pertenece al contexto tenant verificado.");
        }
    }

    /// <summary>
    /// Devuelve un prefijo relativo seguro bajo empresas/{EmpresaId}. No acepta
    /// segmentos vacíos ni con traversal.
    /// </summary>
    public string ConstruirPrefijo(string segmento)
    {
        if (string.IsNullOrWhiteSpace(segmento))
        {
            throw new ArgumentException("El segmento de almacenamiento es obligatorio.", nameof(segmento));
        }

        var normalizado = segmento.Trim().Trim('/');
        if (normalizado.Length == 0 ||
            normalizado.Contains("..", StringComparison.Ordinal) ||
            normalizado.Contains('\\'))
        {
            throw new ArgumentException(
                "El segmento de almacenamiento no puede escapar del scope tenant.",
                nameof(segmento));
        }

        return $"{TenantPrefix}/{normalizado}";
    }
}
