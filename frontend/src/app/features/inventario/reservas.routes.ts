import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const RESERVAS_INVENTARIO_ROUTES: Routes = [
  {
    path: 'inventario/reservas',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./reservas-inventario-list.component').then(m => m.ReservasInventarioListComponent)
  }
];
