import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, HostListener, Inject, OnDestroy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
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
      <a class="skip-link" href="#main-content">Saltar al contenido principal</a>
      <div class="layout">
        @if (sidebarAbierto) {
          <button class="overlay" type="button" (click)="cerrarSidebar()" aria-label="Cerrar menú lateral"></button>
        }
        <aside id="main-sidebar" class="sidebar" [class.abierto]="sidebarAbierto" aria-label="Menú principal">
          <div class="brand">
            <img class="brand-logo" [src]="identidad.logoUrl()" [alt]="identidad.nombreSistema()">
            <span>{{ identidad.nombreSistema() }}</span>
            <button mat-icon-button class="cerrar-sidebar" (click)="cerrarSidebar()" aria-label="Cerrar menú">
              <mat-icon>close</mat-icon>
            </button>
          </div>
          <nav aria-label="Navegación principal" (click)="cerrarSidebarEnMovil()">
            @if (permisosRuntime.puede('Dashboard', 'Ver')) {
              <a routerLink="/dashboard" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>dashboard</mat-icon> Dashboard</a>
            }
            @if (permisosRuntime.puede('Productos', 'Ver')) {
              <a routerLink="/productos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>widgets</mat-icon> Productos</a>
            }
            @if (permisosRuntime.puede('Categorias', 'Ver')) {
              <a routerLink="/categorias" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>category</mat-icon> Categorías</a>
            }
            @if (permisosRuntime.puede('Compras', 'Ver')) {
              <a routerLink="/compras" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>shopping_cart</mat-icon> Compras</a>
            }
            @if (permisosRuntime.puede('Proveedores', 'Ver')) {
              <a routerLink="/proveedores" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>local_shipping</mat-icon> Proveedores</a>
            }
            @if (permisosRuntime.puede('Ventas', 'Ver')) {
              <a routerLink="/ventas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>point_of_sale</mat-icon> Ventas</a>
            }
            @if (permisosRuntime.puede('Clientes', 'Ver')) {
              <a routerLink="/clientes" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>groups</mat-icon> Clientes</a>
            }
            @if (permisosRuntime.puede('Finanzas', 'Ver')) {
              <a routerLink="/finanzas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>account_balance_wallet</mat-icon> Finanzas</a>
            }
            @if (permisosRuntime.puede('MovimientosInventario', 'Ver')) {
              <a routerLink="/inventario/movimientos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>sync_alt</mat-icon> Movimientos</a>
            }
            @if (permisosRuntime.puede('Usuarios', 'Ver')) {
              <a routerLink="/usuarios" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>manage_accounts</mat-icon> Usuarios</a>
            }
            @if (permisosRuntime.puede('Roles', 'Ver')) {
              <a routerLink="/roles" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>admin_panel_settings</mat-icon> Roles</a>
            }
            @if (permisosRuntime.puede('Descuentos', 'Ver')) {
              <a routerLink="/descuentos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>sell</mat-icon> Descuentos</a>
            }
            @if (permisosRuntime.puede('Impuestos', 'Ver')) {
              <a routerLink="/impuestos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>request_quote</mat-icon> Impuestos</a>
            }
            @if (permisosRuntime.puede('Permisos', 'Administrar')) {
              <a routerLink="/permisos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>lock_outline</mat-icon> Permisos</a>
            }
            @if (permisosRuntime.esAdministrador() && permisosRuntime.puede('Auditoria', 'Ver')) {
              <a routerLink="/auditoria" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>manage_search</mat-icon> Auditoría</a>
            }
            @if (permisosRuntime.puede('Configuracion', 'Ver')) {
              <a routerLink="/configuracion" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>settings</mat-icon> Configuración</a>
            }
          </nav>
        </aside>
        <div class="main">
          <header class="topbar">
            <button
              mat-icon-button
              class="menu-toggle"
              (click)="toggleSidebar()"
              aria-controls="main-sidebar"
              [attr.aria-expanded]="sidebarAbierto"
              aria-label="Abrir menú principal">
              <mat-icon>menu</mat-icon>
            </button>
            <span class="header-text">
              @if (identidad.config().encabezadoActivo) {
                {{ identidad.descripcionSistema() }}
              }
            </span>
            <div class="user">
              <div class="user-copy">
                <span class="user-name">{{ auth.nombreCompleto() }}</span>
                <span class="user-role">{{ auth.rol() }}</span>
              </div>
              <button mat-icon-button class="profile-button" routerLink="/perfil" aria-label="Abrir mi perfil" title="Mi perfil">
                @if (auth.fotoPerfilUrl(); as foto) {
                  <img class="user-avatar" [src]="foto" [alt]="'Perfil de ' + (auth.nombreCompleto() || auth.nombreUsuario() || 'usuario')">
                } @else {
                  <span class="user-initials" aria-hidden="true">{{ inicialesUsuario() }}</span>
                }
              </button>
              <button mat-icon-button (click)="logout()" aria-label="Cerrar sesión" title="Cerrar sesión">
                <mat-icon>logout</mat-icon>
              </button>
            </div>
          </header>
          <main id="main-content" class="content" tabindex="-1">
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
export class AppComponent implements OnDestroy {
  sidebarAbierto = false;

  constructor(
    public auth: AuthService,
    public permisosRuntime: PermisosRuntimeService,
    public identidad: EmpresaIdentidadService,
    private sessionActivity: SessionActivityService,
    private router: Router,
    private themeApplier: ThemeApplierService,
    @Inject(DOCUMENT) private document: Document
  ) {
    this.themeApplier.aplicarTemaGuardado();
    this.identidad.cargar().subscribe();
    if (this.auth.isAuthenticated()) {
      this.permisosRuntime.cargar().subscribe();
      this.sessionActivity.iniciar();
    }
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) this.cerrarSidebar();
    });
  }

  ngOnDestroy(): void {
    this.document.body.style.removeProperty('overflow');
  }

  toggleSidebar(): void {
    this.sidebarAbierto = !this.sidebarAbierto;
    this.sincronizarScrollMovil();
  }

  cerrarSidebar(): void {
    this.sidebarAbierto = false;
    this.sincronizarScrollMovil();
  }

  cerrarSidebarEnMovil(): void {
    if (window.innerWidth <= 900) this.cerrarSidebar();
  }

  inicialesUsuario(): string {
    const nombre = this.auth.nombreCompleto()?.trim() || this.auth.nombreUsuario()?.trim() || 'Usuario';
    return nombre.split(/\s+/).slice(0, 2).map(parte => parte.charAt(0).toUpperCase()).join('');
  }

  @HostListener('window:keydown.escape')
  onEscape(): void {
    this.cerrarSidebar();
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth > 900 && this.sidebarAbierto) this.cerrarSidebar();
  }

  logout(): void {
    this.cerrarSidebar();
    this.sessionActivity.cerrarManual();
  }

  private sincronizarScrollMovil(): void {
    if (this.sidebarAbierto && window.innerWidth <= 900) {
      this.document.body.style.overflow = 'hidden';
    } else {
      this.document.body.style.removeProperty('overflow');
    }
  }
}