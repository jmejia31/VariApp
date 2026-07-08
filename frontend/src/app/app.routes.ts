import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

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
  { path: '**', redirectTo: 'dashboard' }
];
