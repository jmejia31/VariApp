import { Routes } from '@angular/router';
import { authGuard } from '../../../core/guards/auth.guard';
import { permisoGuard } from '../../../core/guards/permiso.guard';

export const FACTURAS_PROVEEDOR_ROUTES: Routes = [
  {
    path: 'facturas-proveedor/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Crear' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturaProveedorFormComponent)
  },
  {
    path: 'facturas-proveedor/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Editar' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturaProveedorFormComponent)
  },
  {
    path: 'facturas-proveedor/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturaProveedorDetailComponent)
  },
  {
    path: 'facturas-proveedor',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturasProveedorListComponent)
  }
];
