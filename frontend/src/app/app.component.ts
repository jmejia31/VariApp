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
      <div class="sr-only" role="status" aria-live="polite" aria-atomic="true">{{ routeAnnouncement }}</div>
      <div class="layout">
        @if (sidebarAbierto) {
          <button class="overlay" type="button" (click)="cerrarSidebar(true)" aria-label="Cerrar menú lateral"></button>
        }
        <aside id="main-sidebar" class="sidebar" [class.abierto]="sidebarAbierto" aria-label="Menú principal">
          <div class="brand">
            <img class="brand-logo" [src]="identidad.logoUrl()" [alt]="identidad.nombreSistema()">
            <span>{{ identidad.nombreSistema() }}</span>
            <button mat-icon-button class="cerrar-sidebar" (click)="cerrarSidebar(true)" aria-label="Cerrar menú">
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
            @if (permisosRuntime.puede('Sucursales', 'Ver')) {
              <a routerLink="/sucursales" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>storefront</mat-icon> Sucursales</a>
            }
            @if (permisosRuntime.puede('Almacenes', 'Ver')) {
              <a routerLink="/almacenes" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>warehouse</mat-icon> Almacenes</a>
            }
            @if (permisosRuntime.puede('UbicacionesAlmacen', 'Ver')) {
              <a routerLink="/ubicaciones-almacen" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>account_tree</mat-icon> Ubicaciones</a>
            }
            @if (permisosRuntime.puede('Colores', 'Ver')) {
              <a routerLink="/colores" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>palette</mat-icon> Colores</a>
            }
            @if (permisosRuntime.puede('Tallas', 'Ver')) {
              <a routerLink="/tallas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>straighten</mat-icon> Tallas</a>
            }
            @if (permisosRuntime.puede('Marcas', 'Ver')) {
              <a routerLink="/marcas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>branding_watermark</mat-icon> Marcas</a>
            }
            @if (permisosRuntime.puede('Modelos', 'Ver')) {
              <a routerLink="/modelos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>devices</mat-icon> Modelos</a>
            }
            @if (permisosRuntime.puede('MetodosPago', 'Ver')) {
              <a routerLink="/metodos-pago" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>payments</mat-icon> Métodos de pago</a>
            }
            @if (permisosRuntime.puede('Compras', 'Ver')) {
              <a routerLink="/solicitudes-compra" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>request_quote</mat-icon> Solicitudes de compra</a>
              <a routerLink="/ordenes-compra" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>receipt_long</mat-icon> Órdenes de compra</a>
              <a routerLink="/recepciones-compra" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>inventory_2</mat-icon> Recepciones de compra</a>
              <a routerLink="/devoluciones-proveedor" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>assignment_return</mat-icon> Devoluciones a proveedor</a>
              <a routerLink="/compras" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>shopping_cart</mat-icon> Compras</a>
            }
            @if (permisosRuntime.puede('Proveedores', 'Ver')) {
              <a routerLink="/proveedores" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>local_shipping</mat-icon> Proveedores</a>
            }
            @if (permisosRuntime.puede('Ventas', 'Ver')) {
              <a routerLink="/ventas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>point_of_sale</mat-icon> Ventas</a>
              <a routerLink="/pedidos-venta" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>shopping_bag</mat-icon> Pedidos</a>
            }
            @if (permisosRuntime.puede('Clientes', 'Ver')) {
              <a routerLink="/clientes" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>groups</mat-icon> Clientes</a>
            }
            @if (permisosRuntime.puede('Finanzas', 'Ver')) {
              <a routerLink="/finanzas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>account_balance_wallet</mat-icon> Finanzas</a>
              <a routerLink="/plan-cuentas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>account_tree</mat-icon> Plan de cuentas</a>
            }
            @if (permisosRuntime.puede('MovimientosInventario', 'Ver')) {
              <a routerLink="/inventario/movimientos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>sync_alt</mat-icon> Movimientos</a>
              <a routerLink="/inventario/transferencias" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>swap_horiz</mat-icon> Transferencias</a>
              <a routerLink="/inventario/conteos" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>fact_check</mat-icon> Conteos físicos</a>
              <a routerLink="/inventario/reservas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>event_available</mat-icon> Reservas</a>
              <a routerLink="/inventario/costeo" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>calculate</mat-icon> Costeo</a>
            }
            @if (permisosRuntime.puede('Inventario', 'Ver')) {
              <a routerLink="/inventario/ajustes" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>tune</mat-icon> Ajustes de inventario</a>
            }
            @if (permisosRuntime.puede('CargasMasivas', 'Ver')) {
              <a routerLink="/cargas-masivas" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>upload_file</mat-icon> Cargas masivas</a>
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
              <a routerLink="/periodos-contables" routerLinkActive="active" ariaCurrentWhenActive="page"><mat-icon>calendar_month</mat-icon> Periodos contables</a>
            }
          </nav>
        </aside>
        <div class="main">
          <header class="topbar">
            <button
              id="menu-toggle"
              mat-icon-button
              class="menu-toggle"
              (click)="toggleSidebar()"
              aria-controls="main-sidebar"
              [attr.aria-expanded]="sidebarAbierto"
              [attr.aria-label]="sidebarAbierto ? 'Cerrar menú principal' : 'Abrir menú principal'">
              <mat-icon>{{ sidebarAbierto ? 'close' : 'menu' }}</mat-icon>
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
              <button mat-icon-button class="topbar-icon-button" (click)="logout()" aria-label="Cerrar sesión" title="Cerrar sesión">
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
  routeAnnouncement = '';

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
      if (event instanceof NavigationEnd) {
        this.cerrarSidebar();
        this.gestionarFocoTrasNavegacion();
      }
    });
  }

  ngOnDestroy(): void {
    this.sessionActivity.detener();
    this.document.body.style.removeProperty('overflow');
  }

  toggleSidebar(): void {
    if (this.sidebarAbierto) {
      this.cerrarSidebar(true);
      return;
    }

    this.sidebarAbierto = true;
    this.sincronizarScrollMovil();
    if (window.innerWidth <= 900) {
      window.setTimeout(() => {
        this.document.querySelector<HTMLElement>('#main-sidebar .cerrar-sidebar')?.focus();
      });
    }
  }

  cerrarSidebar(devolverFoco = false): void {
    const estabaAbierto = this.sidebarAbierto;
    this.sidebarAbierto = false;
    this.sincronizarScrollMovil();
    if (devolverFoco && estabaAbierto && window.innerWidth <= 900) {
      window.setTimeout(() => this.document.getElementById('menu-toggle')?.focus());
    }
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
    if (this.sidebarAbierto) this.cerrarSidebar(true);
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth > 900 && this.sidebarAbierto) this.cerrarSidebar();
  }

  logout(): void {
    this.cerrarSidebar();
    this.sessionActivity.cerrarManual();
  }

  private gestionarFocoTrasNavegacion(): void {
    if (!this.auth.isAuthenticated()) return;

    window.setTimeout(() => {
      const main = this.document.getElementById('main-content');
      if (!main) return;

      const titulo = main.querySelector('h1')?.textContent?.trim();
      this.routeAnnouncement = titulo ? `Página cargada: ${titulo}` : 'Página cargada';
      main.focus({ preventScroll: true });
      main.scrollIntoView({ block: 'start', behavior: 'auto' });
    });
  }

  private sincronizarScrollMovil(): void {
    if (this.sidebarAbierto && window.innerWidth <= 900) {
      this.document.body.style.overflow = 'hidden';
    } else {
      this.document.body.style.removeProperty('overflow');
    }
  }
}
