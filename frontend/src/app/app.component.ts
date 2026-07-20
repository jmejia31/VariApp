import { Component, HostListener } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from './core/auth/auth.service';
import { PermisosRuntimeService } from './core/auth/permisos-runtime.service';
import { ThemeApplierService } from './services/theme-applier.service';
import { EmpresaIdentidadService } from './services/empresa-identidad.service';
import { SessionActivityService } from './core/auth/session-activity.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatButtonModule],
  template: `
    @if (auth.isAuthenticated()) {
      <div class="layout">
        @if (sidebarAbierto) {
          <div class="overlay" (click)="cerrarSidebar()"></div>
        }
        <aside class="sidebar" [class.abierto]="sidebarAbierto">
          <div class="brand">
            <img class="brand-logo" [src]="identidad.logoUrl()" [alt]="identidad.nombreSistema()">
            <span>{{ identidad.nombreSistema() }}</span>
            <button mat-icon-button class="cerrar-sidebar" (click)="cerrarSidebar()" title="Cerrar menú">
              <mat-icon>close</mat-icon>
            </button>
          </div>
          <nav (click)="cerrarSidebarEnMovil()">
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
            @if (permisosRuntime.puede('Descuentos', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/descuentos" routerLinkActive="active"><mat-icon>sell</mat-icon> Descuentos</a>
            }
            @if (permisosRuntime.puede('Impuestos', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/impuestos" routerLinkActive="active"><mat-icon>request_quote</mat-icon> Impuestos</a>
            }
            @if (permisosRuntime.puede('Permisos', 'Administrar') || auth.esAdministrador()) {
              <a routerLink="/permisos" routerLinkActive="active"><mat-icon>lock_outline</mat-icon> Permisos</a>
            }
            @if (permisosRuntime.puede('Auditoria', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/auditoria" routerLinkActive="active"><mat-icon>manage_search</mat-icon> Auditoría</a>
            }
            @if (permisosRuntime.puede('Configuracion', 'Ver') || auth.esAdministrador()) {
              <a routerLink="/configuracion" routerLinkActive="active"><mat-icon>settings</mat-icon> Configuración</a>
            }
          </nav>
        </aside>
        <div class="main">
          <header class="topbar">
            <button mat-icon-button class="menu-toggle" (click)="toggleSidebar()" title="Abrir menú">
              <mat-icon>menu</mat-icon>
            </button>
            <span class="header-text">
              @if (identidad.config().encabezadoActivo) {
                {{ identidad.config().encabezadoTexto || identidad.config().descripcionSistema }}
              }
            </span>
            <div class="user">
              <span class="user-name">{{ auth.nombreCompleto() }}</span>
              <span class="user-role">{{ auth.rol() }}</span>
              <button mat-icon-button routerLink="/perfil" title="Mi perfil">
                <mat-icon>account_circle</mat-icon>
              </button>
              <button mat-icon-button (click)="logout()" title="Cerrar sesión">
                <mat-icon>logout</mat-icon>
              </button>
            </div>
          </header>
          <main class="content">
            <router-outlet></router-outlet>
          </main>
          @if (identidad.config().piePaginaActivo || identidad.mostrarCopyright()) {
            <footer class="app-footer">
              @if (identidad.config().piePaginaActivo && identidad.config().piePaginaTexto) {
                <span>{{ identidad.config().piePaginaTexto }}</span>
              }
              @if (identidad.mostrarCopyright()) {
                <span>{{ identidad.copyright() }}</span>
              }
            </footer>
          }
        </div>
      </div>
    } @else {
      <router-outlet></router-outlet>
    }
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  sidebarAbierto = false;

  constructor(
    public auth: AuthService,
    public permisosRuntime: PermisosRuntimeService,
    public identidad: EmpresaIdentidadService,
    private sessionActivity: SessionActivityService,
    private router: Router,
    private themeApplier: ThemeApplierService
  ) {
    this.themeApplier.aplicarTemaGuardado();
    this.identidad.cargar().subscribe();
    if (this.auth.isAuthenticated()) {
      this.permisosRuntime.cargar().subscribe();
      this.sessionActivity.iniciar();
    }
    // Cierra el menú móvil automáticamente al cambiar de ruta (evita que
    // quede abierto tapando la pantalla después de navegar).
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) this.sidebarAbierto = false;
    });
  }

  toggleSidebar(): void {
    this.sidebarAbierto = !this.sidebarAbierto;
  }

  cerrarSidebar(): void {
    this.sidebarAbierto = false;
  }

  cerrarSidebarEnMovil(): void {
    if (window.innerWidth <= 900) this.sidebarAbierto = false;
  }

  @HostListener('window:keydown.escape')
  onEscape(): void {
    this.sidebarAbierto = false;
  }

  logout(): void {
    this.sessionActivity.cerrarManual();
  }
}
