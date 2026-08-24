import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const PEDIDOS_VENTA_ROUTES:Routes=[
 {path:'pedidos-venta',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidosVentaListComponent)},
 {path:'pedidos-venta/nuevo',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Crear'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidoVentaFormComponent)},
 {path:'pedidos-venta/:id/editar',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Editar'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidoVentaFormComponent)},
 {path:'pedidos-venta/:id',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidoVentaDetailComponent)}
];
