import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';
import { TRANSFERENCIAS_INVENTARIO_ROUTES } from './transferencias.routes';

export const AJUSTES_INVENTARIO_ROUTES: Routes = [
  {
    path: 'inventario/existencias',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./existencias-variante-list.component').then(m => m.ExistenciasVarianteListComponent)
  },
  {
    path: 'inventario/existencias/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Crear' },
    loadComponent: () => import('./existencia-variante-form.component').then(m => m.ExistenciaVarianteFormComponent)
  },
  {
    path: 'inventario/existencias/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Editar' },
    loadComponent: () => import('./existencia-variante-form.component').then(m => m.ExistenciaVarianteFormComponent)
  },
  {
    path: 'inventario/ajustes',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./ajustes-list.component').then(m => m.AjustesListComponent)
  },
  {
    path: 'inventario/ajustes/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Crear' },
    loadComponent: () => import('./ajuste-form.component').then(m => m.AjusteFormComponent)
  },
  {
    path: 'inventario/ajustes/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Editar' },
    loadComponent: () => import('./ajuste-form.component').then(m => m.AjusteFormComponent)
  },
  {
    path: 'inventario/ajustes/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./ajuste-detail.component').then(m => m.AjusteDetailComponent)
  },
  ...TRANSFERENCIAS_INVENTARIO_ROUTES
];
