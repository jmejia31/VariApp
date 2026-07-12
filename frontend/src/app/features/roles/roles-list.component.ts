import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RolService } from '../../services/rol.service';
import { Rol } from '../../core/models/rol.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './roles-list.component.html',
  styleUrl: './roles-list.component.scss'
})
export class RolesListComponent implements OnInit {
  readonly roles = signal<Rol[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeDuplicar = signal(false);

  constructor(private rolService: RolService, private permisosRuntime: PermisosRuntimeService, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Roles', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Roles', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Roles', 'EliminarLogico'));
    this.puedeDuplicar.set(this.permisosRuntime.puede('Roles', 'Duplicar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.rolService.getAll().subscribe({
      next: (res) => {
        this.roles.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  toggleActivo(rol: Rol): void {
    const accion$ = rol.activo ? this.rolService.desactivar(rol.id) : this.rolService.activar(rol.id);
    accion$.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado del rol.', 'Cerrar', { duration: 5000 })
    });
  }

  eliminar(rol: Rol): void {
    if (!confirm(`¿Eliminar el rol "${rol.nombre}"? Esta acción se puede revertir solo por un administrador desde la base de datos.`)) return;

    this.rolService.eliminarLogico(rol.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el rol.', 'Cerrar', { duration: 5000 })
    });
  }

  duplicar(rol: Rol): void {
    const nuevoNombre = prompt(`Nombre para la copia de "${rol.nombre}":`, `${rol.nombre} (copia)`);
    if (!nuevoNombre) return;

    this.rolService.duplicar(rol.id, nuevoNombre).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar el rol.', 'Cerrar', { duration: 5000 })
    });
  }
}
