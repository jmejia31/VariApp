import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardResumen } from '../../core/models/dashboard.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly resumen = signal<DashboardResumen | null>(null);
  readonly loading = signal(true);
  readonly esAdministrador = this.permisosRuntime.esAdministrador;
  readonly puedeVerVentas = signal(false);
  readonly puedeVerProductos = signal(false);

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.puedeVerVentas.set(this.permisosRuntime.puede('Ventas', 'Ver'));
    this.puedeVerProductos.set(this.permisosRuntime.puede('Productos', 'Ver'));

    this.dashboardService.getResumen().subscribe({
      next: (res) => {
        this.resumen.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  maximoActividad(r: DashboardResumen): number {
    const valores = [
      ...r.ultimasVentas.map((v) => v.total),
      ...(this.esAdministrador() ? r.ultimasCompras.map((c) => c.total) : [])
    ];
    return Math.max(1, ...valores);
  }

  barraPorcentaje(valor: number, r: DashboardResumen): number {
    return Math.max(8, Math.round((valor / this.maximoActividad(r)) * 100));
  }

  ventasRecientes(r: DashboardResumen) {
    return r.ultimasVentas.slice(0, 7);
  }

  comprasRecientes(r: DashboardResumen) {
    return this.esAdministrador() ? r.ultimasCompras.slice(0, 7) : [];
  }

  totalOperativo(r: DashboardResumen): number {
    const compras = this.esAdministrador() ? r.comprasDelMes : 0;
    return Math.max(1, r.ventasDelMes + compras + r.productosStockBajo.length);
  }

  porcentaje(valor: number, total: number): number {
    return Math.round((valor / Math.max(1, total)) * 100);
  }

  donutBackground(r: DashboardResumen): string {
    const total = this.totalOperativo(r);
    const ventas = this.porcentaje(r.ventasDelMes, total);
    const compras = this.esAdministrador() ? this.porcentaje(r.comprasDelMes, total) : 0;
    return `conic-gradient(var(--color-primary) 0 ${ventas}%, var(--color-success) ${ventas}% ${ventas + compras}%, var(--color-danger) ${ventas + compras}% 100%)`;
  }
}
