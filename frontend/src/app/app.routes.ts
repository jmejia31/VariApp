import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permisoGuard } from './core/guards/permiso.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Dashboard', accion: 'Ver' },
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'productos',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Productos', accion: 'Ver' },
    loadComponent: () => import('./features/productos/productos-list.component').then(m => m.ProductosListComponent)
  },
  {
    path: 'productos/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Productos', accion: 'Crear' },
    loadComponent: () => import('./features/productos/producto-form.component').then(m => m.ProductoFormComponent)
  },
  {
    path: 'productos/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Productos', accion: 'Editar' },
    loadComponent: () => import('./features/productos/producto-form.component').then(m => m.ProductoFormComponent)
  },
  {
    path: 'categorias',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Categorias', accion: 'Ver' },
    loadComponent: () => import('./features/categorias/categorias-list.component').then(m => m.CategoriasListComponent)
  },
  {
    path: 'categorias/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Categorias', accion: 'Crear' },
    loadComponent: () => import('./features/categorias/categoria-form.component').then(m => m.CategoriaFormComponent)
  },
  {
    path: 'categorias/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Categorias', accion: 'Editar' },
    loadComponent: () => import('./features/categorias/categoria-form.component').then(m => m.CategoriaFormComponent)
  },
  {
    path: 'proveedores',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Proveedores', accion: 'Ver' },
    loadComponent: () => import('./features/proveedores/proveedores-list.component').then(m => m.ProveedoresListComponent)
  },
  {
    path: 'proveedores/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Proveedores', accion: 'Crear' },
    loadComponent: () => import('./features/proveedores/proveedor-form.component').then(m => m.ProveedorFormComponent)
  },
  {
    path: 'proveedores/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Proveedores', accion: 'Editar' },
    loadComponent: () => import('./features/proveedores/proveedor-form.component').then(m => m.ProveedorFormComponent)
  },
  {
    path: 'clientes',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Clientes', accion: 'Ver' },
    loadComponent: () => import('./features/clientes/clientes-list.component').then(m => m.ClientesListComponent)
  },
  {
    path: 'clientes/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Clientes', accion: 'Crear' },
    loadComponent: () => import('./features/clientes/cliente-form.component').then(m => m.ClienteFormComponent)
  },
  {
    path: 'clientes/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Clientes', accion: 'Editar' },
    loadComponent: () => import('./features/clientes/cliente-form.component').then(m => m.ClienteFormComponent)
  },
  {
    path: 'usuarios',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Usuarios', accion: 'Ver' },
    loadComponent: () => import('./features/usuarios/usuarios.component').then(m => m.UsuariosComponent)
  },
  {
    path: 'usuarios/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Usuarios', accion: 'Ver' },
    loadComponent: () => import('./features/usuarios/usuario-detail.component').then(m => m.UsuarioDetailComponent)
  },
  {
    path: 'roles',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Roles', accion: 'Ver' },
    loadComponent: () => import('./features/roles/roles-list.component').then(m => m.RolesListComponent)
  },
  {
    path: 'roles/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Roles', accion: 'Crear' },
    loadComponent: () => import('./features/roles/rol-form.component').then(m => m.RolFormComponent)
  },
  {
    path: 'roles/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Roles', accion: 'Editar' },
    loadComponent: () => import('./features/roles/rol-form.component').then(m => m.RolFormComponent)
  },
  {
    path: 'permisos',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Permisos', accion: 'Administrar' },
    loadComponent: () => import('./features/permisos/permisos-matrix.component').then(m => m.PermisosMatrixComponent)
  },
  {
    path: 'descuentos',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Descuentos', accion: 'Ver' },
    loadComponent: () => import('./features/descuentos/descuentos-list.component').then(m => m.DescuentosListComponent)
  },
  {
    path: 'descuentos/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Descuentos', accion: 'Crear' },
    loadComponent: () => import('./features/descuentos/descuento-form.component').then(m => m.DescuentoFormComponent)
  },
  {
    path: 'descuentos/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Descuentos', accion: 'Editar' },
    loadComponent: () => import('./features/descuentos/descuento-form.component').then(m => m.DescuentoFormComponent)
  },
  {
    path: 'impuestos',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Impuestos', accion: 'Ver' },
    loadComponent: () => import('./features/impuestos/impuestos-list.component').then(m => m.ImpuestosListComponent)
  },
  {
    path: 'impuestos/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Impuestos', accion: 'Crear' },
    loadComponent: () => import('./features/impuestos/impuesto-form.component').then(m => m.ImpuestoFormComponent)
  },
  {
    path: 'impuestos/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Impuestos', accion: 'Editar' },
    loadComponent: () => import('./features/impuestos/impuesto-form.component').then(m => m.ImpuestoFormComponent)
  },
  {
    path: 'auditoria',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Auditoria', accion: 'Ver' },
    loadComponent: () => import('./features/auditoria/auditoria-list.component').then(m => m.AuditoriaListComponent)
  },
  {
    path: 'compras',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('./features/compras/compras-list.component').then(m => m.ComprasListComponent)
  },
  {
    path: 'compras/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Crear' },
    loadComponent: () => import('./features/compras/compra-form.component').then(m => m.CompraFormComponent)
  },
  {
    path: 'compras/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('./features/compras/compra-detail.component').then(m => m.CompraDetailComponent)
  },
  {
    path: 'compras/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Editar' },
    loadComponent: () => import('./features/compras/compra-form.component').then(m => m.CompraFormComponent)
  },
  {
    path: 'ventas',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Ventas', accion: 'Ver' },
    loadComponent: () => import('./features/ventas/ventas-list.component').then(m => m.VentasListComponent)
  },
  {
    path: 'ventas/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Ventas', accion: 'Crear' },
    loadComponent: () => import('./features/ventas/venta-form.component').then(m => m.VentaFormComponent)
  },
  {
    path: 'ventas/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Ventas', accion: 'Ver' },
    loadComponent: () => import('./features/ventas/venta-detail.component').then(m => m.VentaDetailComponent)
  },
  {
    path: 'ventas/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Ventas', accion: 'Editar' },
    loadComponent: () => import('./features/ventas/venta-form.component').then(m => m.VentaFormComponent)
  },
  {
    path: 'facturas/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Facturacion', accion: 'Ver' },
    loadComponent: () => import('./features/facturas/factura-view.component').then(m => m.FacturaViewComponent)
  },
  {
    path: 'finanzas',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Finanzas', accion: 'Ver' },
    loadComponent: () => import('./features/finanzas/finanzas.component').then(m => m.FinanzasComponent)
  },
  {
    path: 'inventario/movimientos',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./features/inventario/movimientos-list.component').then(m => m.MovimientosListComponent)
  },
  {
    path: 'configuracion',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Configuracion', accion: 'Ver' },
    loadComponent: () => import('./features/configuracion/configuracion.component').then(m => m.ConfiguracionComponent)
  },
  {
    // Autogestión: cualquier usuario autenticado administra su propia cuenta,
    // sin importar su rol o permisos de módulo.
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () => import('./features/perfil/perfil.component').then(m => m.PerfilComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
