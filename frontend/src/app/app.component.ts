import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from './core/auth/auth.service';

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
            <a routerLink="/dashboard" routerLinkActive="active"><mat-icon>dashboard</mat-icon> Dashboard</a>
            <a routerLink="/productos" routerLinkActive="active"><mat-icon>widgets</mat-icon> Productos</a>
            <a routerLink="/categorias" routerLinkActive="active"><mat-icon>category</mat-icon> Categorías</a>
            <a routerLink="/compras" routerLinkActive="active"><mat-icon>shopping_cart</mat-icon> Compras</a>
            @if (auth.esAdministrador()) {
              <a routerLink="/usuarios" routerLinkActive="active"><mat-icon>manage_accounts</mat-icon> Usuarios</a>
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
  constructor(public auth: AuthService, private router: Router) {}

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
