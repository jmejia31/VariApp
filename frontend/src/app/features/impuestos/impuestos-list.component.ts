import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImpuestoService } from '../../services/impuesto.service';
import { Impuesto } from '../../core/models/impuesto.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-impuestos-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './impuestos-list.component.html',
  styleUrl: './impuestos-list.component.scss'
})
export class ImpuestosListComponent implements OnInit {
  readonly impuestos = signal<Impuesto[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeDuplicar = signal(false);

  constructor(private service: ImpuestoService, private permisosRuntime: PermisosRuntimeService, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Impuestos', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Impuestos', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Impuestos', 'EliminarLogico'));
    this.puedeDuplicar.set(this.permisosRuntime.puede('Impuestos', 'Duplicar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (res) => { this.impuestos.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleActivo(i: Impuesto): void {
    const accion$ = i.activo ? this.service.desactivar(i.id) : this.service.activar(i.id);
    accion$.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 })
    });
  }

  eliminar(i: Impuesto): void {
    if (!confirm(`¿Eliminar el impuesto "${i.nombre}"?`)) return;
    this.service.eliminarLogico(i.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar.', 'Cerrar', { duration: 5000 })
    });
  }

  duplicar(i: Impuesto): void {
    const nuevoNombre = prompt(`Nombre para la copia de "${i.nombre}":`, `${i.nombre} (copia)`);
    if (!nuevoNombre) return;
    const nuevoCodigo = prompt(`Código para la copia (debe ser único):`, `${i.codigo}-COPIA`);
    if (!nuevoCodigo) return;
    this.service.duplicar(i.id, nuevoNombre, nuevoCodigo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar.', 'Cerrar', { duration: 5000 })
    });
  }
}
