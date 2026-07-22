using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Common;

/// Única fuente de verdad de las combinaciones Módulo/Acción válidas.
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
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.EliminarLogico,
            AccionPermiso.Exportar, AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Ventas, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.EliminarLogico,
            AccionPermiso.Exportar, AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial
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
