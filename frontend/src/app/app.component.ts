import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from './core/auth/auth.service';
import { PermisosRuntimeService } from './core/auth/permisos-runtime.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatButtonModule],
  template: `
    @if (auth.isAuthenticated()) {
      <div class="layout">
        <aside class="sidebar">
          <div class="brand">
            <mat-icon>inventory_2</mat-icon>
            <span>InventoryApp</span>
          </div>
          <nav>
            @if (permisosRuntime.puede('Dashboard', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/dashboard" routerLinkActive="active"><mat-icon>dashboard</mat-icon> Dashboard</a>
            }
            @if (permisosRuntime.puede('Productos', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/productos" routerLinkActive="active"><mat-icon>widgets</mat-icon> Productos</a>
            }
            @if (permisosRuntime.puede('Categorias', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/categorias" routerLinkActive="active"><mat-icon>category</mat-icon> Categorías</a>
            }
            @if (permisosRuntime.puede('Compras', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/compras" routerLinkActive="active"><mat-icon>shopping_cart</mat-icon> Compras</a>
            }
            @if (permisosRuntime.puede('Proveedores', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/proveedores" routerLinkActive="active"><mat-icon>local_shipping</mat-icon> Proveedores</a>
            }
            @if (permisosRuntime.puede('Ventas', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/ventas" routerLinkActive="active"><mat-icon>point_of_sale</mat-icon> Ventas</a>
            }
            @if (permisosRuntime.puede('Clientes', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/clientes" routerLinkActive="active"><mat-icon>groups</mat-icon> Clientes</a>
            }
            @if (permisosRuntime.puede('Finanzas', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/finanzas" routerLinkActive="active"><mat-icon>account_balance_wallet</mat-icon> Finanzas</a>
            }
            @if (permisosRuntime.puede('Inventario', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/inventario/movimientos" routerLinkActive="active"><mat-icon>sync_alt</mat-icon> Movimientos</a>
            }
            @if (permisosRuntime.puede('Usuarios', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/usuarios" routerLinkActive="active"><mat-icon>manage_accounts</mat-icon> Usuarios</a>
            }
            @if (permisosRuntime.puede('Roles', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/roles" routerLinkActive="active"><mat-icon>admin_panel_settings</mat-icon> Roles</a>
            }
            @if (permisosRuntime.puede('Permisos', 'Administrar') || auth.esAdministrador()) {
              <a routerLink="/permisos" routerLinkActive="active"><mat-icon>lock_outline</mat-icon> Permisos</a>
            }
            @if (permisosRuntime.puede('Auditoria', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/auditoria" routerLinkActive="active"><mat-icon>manage_search</mat-icon> Auditoría</a>
            }
          </nav>
        </aside>
        <div class="main">
          <header class="topbar">
            <span></span>
            <div class="user">
              <span class="user-name">{{ auth.nombreCompleto() }}</span>
              <span class="user-role">{{ auth.rol() }}</span>
              <button mat-icon-button (click)="logout()" title="Cerrar sesión">
                <mat-icon>logout</mat-icon>
              </button>
            </div>
          </header>
          <main class="content">
            <router-outlet></router-outlet>
          </main>
        </div>
      </div>
    } @else {
      <router-outlet></router-outlet>
    }
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  constructor(public auth: AuthService, public permisosRuntime: PermisosRuntimeService, private router: Router) {
    if (this.auth.isAuthenticated()) {
      this.permisosRuntime.cargar().subscribe();
    }
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
