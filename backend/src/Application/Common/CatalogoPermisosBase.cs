using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Common;

/// Única fuente de verdad de las combinaciones Módulo/Acción válidas.
public static class CatalogoPermisosBase
{
    private static readonly AccionPermiso[] AccionesMantenimiento =
    {
        AccionPermiso.Ver,
        AccionPermiso.Crear,
        AccionPermiso.Editar,
        AccionPermiso.Activar,
        AccionPermiso.Desactivar,
        AccionPermiso.EliminarLogico
    };

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

        (ModuloSistema.Colores, AccionesMantenimiento),
        (ModuloSistema.Tallas, AccionesMantenimiento),
        (ModuloSistema.Marcas, AccionesMantenimiento),
        (ModuloSistema.Modelos, AccionesMantenimiento),

        (ModuloSistema.Clientes, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.TiposClientes, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.ConsultarHistorial
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
            AccionPermiso.Exportar, AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial,
            AccionPermiso.ExonerarEnvio
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

        (ModuloSistema.InsumosAdministrativos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.AjustarStock, AccionPermiso.RegistrarConsumo,
            AccionPermiso.ConsultarHistorial, AccionPermiso.Exportar
        }),

        (ModuloSistema.CargasMasivas, new[]
        {
            AccionPermiso.Ver,
            AccionPermiso.Crear,
            AccionPermiso.Confirmar,
            AccionPermiso.Exportar,
            AccionPermiso.ConsultarHistorial
        }),

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
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar,
            AccionPermiso.Administrar, AccionPermiso.ConsultarHistorial
        }),

        (ModuloSistema.Auditoria, new[] { AccionPermiso.Ver, AccionPermiso.Exportar }),
        (ModuloSistema.ReportesAdministrativos, new[] { AccionPermiso.Ver, AccionPermiso.Exportar }),
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

    /// Permisos incorporados de forma acumulativa a roles administrativos existentes.
    /// Si una fila ya existe, su valor Permitido se conserva y nunca se sobrescribe.
    public static readonly (ModuloSistema Modulo, AccionPermiso Accion)[] NuevosPermisosAdministrador =
    {
        (ModuloSistema.TiposClientes, AccionPermiso.Ver),
        (ModuloSistema.TiposClientes, AccionPermiso.Crear),
        (ModuloSistema.TiposClientes, AccionPermiso.Editar),
        (ModuloSistema.TiposClientes, AccionPermiso.Activar),
        (ModuloSistema.TiposClientes, AccionPermiso.Desactivar),
        (ModuloSistema.TiposClientes, AccionPermiso.EliminarLogico),
        (ModuloSistema.TiposClientes, AccionPermiso.ConsultarHistorial),

        (ModuloSistema.InsumosAdministrativos, AccionPermiso.Ver),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.Crear),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.Editar),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.Activar),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.Desactivar),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.EliminarLogico),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.AjustarStock),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.RegistrarConsumo),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.ConsultarHistorial),
        (ModuloSistema.InsumosAdministrativos, AccionPermiso.Exportar),

        (ModuloSistema.Ventas, AccionPermiso.ExonerarEnvio)
    };

    /// Los roles no administrativos dependen exclusivamente de su matriz persistida.
    public static readonly (ModuloSistema Modulo, AccionPermiso Accion)[] DefaultVendedor = { };
}
