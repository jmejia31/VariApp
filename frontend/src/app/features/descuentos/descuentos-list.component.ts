import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DescuentoService } from '../../services/descuento.service';
import { Descuento } from '../../core/models/descuento.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-descuentos-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './descuentos-list.component.html',
  styleUrl: './descuentos-list.component.scss'
})
export class DescuentosListComponent implements OnInit {
  readonly descuentos = signal<Descuento[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeDuplicar = signal(false);

  constructor(private service: DescuentoService, private permisosRuntime: PermisosRuntimeService, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Descuentos', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Descuentos', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Descuentos', 'EliminarLogico'));
    this.puedeDuplicar.set(this.permisosRuntime.puede('Descuentos', 'Duplicar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (res) => { this.descuentos.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleActivo(d: Descuento): void {
    const accion$ = d.activo ? this.service.desactivar(d.id) : this.service.activar(d.id);
    accion$.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 })
    });
  }

  eliminar(d: Descuento): void {
    if (!confirm(`¿Eliminar el descuento "${d.nombre}"?`)) return;
    this.service.eliminarLogico(d.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar.', 'Cerrar', { duration: 5000 })
    });
  }

  duplicar(d: Descuento): void {
    const nuevoNombre = prompt(`Nombre para la copia de "${d.nombre}":`, `${d.nombre} (copia)`);
    if (!nuevoNombre) return;
    this.service.duplicar(d.id, nuevoNombre).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar.', 'Cerrar', { duration: 5000 })
    });
  }
}
