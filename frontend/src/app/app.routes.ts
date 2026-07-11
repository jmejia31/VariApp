import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'productos',
    canActivate: [authGuard],
    loadComponent: () => import('./features/productos/productos-list.component').then(m => m.ProductosListComponent)
  },
  {
    path: 'productos/nuevo',
    canActivate: [authGuard],
    loadComponent: () => import('./features/productos/producto-form.component').then(m => m.ProductoFormComponent)
  },
  {
    path: 'productos/:id/editar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/productos/producto-form.component').then(m => m.ProductoFormComponent)
  },
  {
    path: 'categorias',
    canActivate: [authGuard],
    loadComponent: () => import('./features/categorias/categorias-list.component').then(m => m.CategoriasListComponent)
  },
  {
    path: 'categorias/nueva',
    canActivate: [authGuard],
    loadComponent: () => import('./features/categorias/categoria-form.component').then(m => m.CategoriaFormComponent)
  },
  {
    path: 'categorias/:id/editar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/categorias/categoria-form.component').then(m => m.CategoriaFormComponent)
  },
  {
    path: 'proveedores',
    canActivate: [authGuard],
    loadComponent: () => import('./features/proveedores/proveedores-list.component').then(m => m.ProveedoresListComponent)
  },
  {
    path: 'proveedores/nuevo',
    canActivate: [authGuard],
    loadComponent: () => import('./features/proveedores/proveedor-form.component').then(m => m.ProveedorFormComponent)
  },
  {
    path: 'proveedores/:id/editar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/proveedores/proveedor-form.component').then(m => m.ProveedorFormComponent)
  },
  {
    path: 'clientes',
    canActivate: [authGuard],
    loadComponent: () => import('./features/clientes/clientes-list.component').then(m => m.ClientesListComponent)
  },
  {
    path: 'clientes/nuevo',
    canActivate: [authGuard],
    loadComponent: () => import('./features/clientes/cliente-form.component').then(m => m.ClienteFormComponent)
  },
  {
    path: 'clientes/:id/editar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/clientes/cliente-form.component').then(m => m.ClienteFormComponent)
  },
  {
    path: 'usuarios',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./features/usuarios/usuarios.component').then(m => m.UsuariosComponent)
  },
  {
    path: 'compras',
    canActivate: [authGuard],
    loadComponent: () => import('./features/compras/compras-list.component').then(m => m.ComprasListComponent)
  },
  {
    path: 'compras/nueva',
    canActivate: [authGuard],
    loadComponent: () => import('./features/compras/compra-form.component').then(m => m.CompraFormComponent)
  },
  {
    path: 'compras/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/compras/compra-detail.component').then(m => m.CompraDetailComponent)
  },
  {
    path: 'compras/:id/editar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/compras/compra-form.component').then(m => m.CompraFormComponent)
  },
  {
    path: 'ventas',
    canActivate: [authGuard],
    loadComponent: () => import('./features/ventas/ventas-list.component').then(m => m.VentasListComponent)
  },
  {
    path: 'ventas/nueva',
    canActivate: [authGuard],
    loadComponent: () => import('./features/ventas/venta-form.component').then(m => m.VentaFormComponent)
  },
  {
    path: 'ventas/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/ventas/venta-detail.component').then(m => m.VentaDetailComponent)
  },
  {
    path: 'ventas/:id/editar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/ventas/venta-form.component').then(m => m.VentaFormComponent)
  },
  {
    path: 'facturas/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/facturas/factura-view.component').then(m => m.FacturaViewComponent)
  },
  {
    path: 'finanzas',
    canActivate: [authGuard],
    loadComponent: () => import('./features/finanzas/finanzas.component').then(m => m.FinanzasComponent)
  },
  {
    path: 'inventario/movimientos',
    canActivate: [authGuard],
    loadComponent: () => import('./features/inventario/movimientos-list.component').then(m => m.MovimientosListComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
