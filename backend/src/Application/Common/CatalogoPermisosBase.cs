using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Common;

/// Única fuente de verdad de qué combinaciones Módulo/Acción son válidas en el
/// sistema (sección 7 del prompt). Se usa para: sembrar el catálogo de Permiso,
/// precargar la matriz por defecto de un rol nuevo, y validar la UI de matriz.
/// No todos los módulos tienen todas las acciones — cada módulo declara
/// solamente las que tienen sentido para él.
public static class CatalogoPermisosBase
{
    public static readonly (ModuloSistema Modulo, AccionPermiso[] Acciones)[] Definicion =
    {
        (ModuloSistema.Dashboard, new[] { AccionPermiso.Ver }),

        (ModuloSistema.Productos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Actualizar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Exportar, AccionPermiso.Duplicar
        }),

        (ModuloSistema.Categorias, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente
        }),

        (ModuloSistema.Clientes, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Proveedores, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Compras, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.Exportar,
            AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Ventas, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.Exportar,
            AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Facturacion, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Exportar, AccionPermiso.Imprimir, AccionPermiso.Compartir
        }),

        (ModuloSistema.Finanzas, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Anular, AccionPermiso.Exportar, AccionPermiso.Administrar
        }),

        (ModuloSistema.Inventario, new[] { AccionPermiso.Ver, AccionPermiso.Exportar }),

        (ModuloSistema.MovimientosInventario, new[] { AccionPermiso.Ver, AccionPermiso.Exportar }),

        (ModuloSistema.Usuarios, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.AsignarRol,
            AccionPermiso.RestablecerContrasena, AccionPermiso.CambiarEstado,
            AccionPermiso.EliminarLogico
        }),

        (ModuloSistema.Roles, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Permisos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Administrar, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Auditoria, new[] { AccionPermiso.Ver, AccionPermiso.Exportar }),

        (ModuloSistema.Configuracion, new[] { AccionPermiso.Ver, AccionPermiso.Editar, AccionPermiso.Administrar }),

        (ModuloSistema.Descuentos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar, AccionPermiso.Aplicar,
            AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Impuestos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar, AccionPermiso.Aplicar,
            AccionPermiso.ConsultarHistorial
        }),
    };

    /// Los roles no administrativos dependen exclusivamente de su matriz persistida.
    public static readonly (ModuloSistema Modulo, AccionPermiso Accion)[] DefaultVendedor = { };
}
