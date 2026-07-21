import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { ProveedorService } from '../../services/proveedor.service';
import { Proveedor } from '../../core/models/proveedor.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-proveedores-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './proveedores-list.component.html',
  styleUrl: './proveedores-list.component.scss'
})
export class ProveedoresListComponent implements OnInit {
  readonly proveedores = signal<Proveedor[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private proveedorService: ProveedorService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Proveedores', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Proveedores', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Proveedores', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.proveedorService.getAll().subscribe({
      next: (res) => { this.proveedores.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleActivo(p: Proveedor): void {
    this.proveedorService.update(p.id, {
      nombre: p.nombre, telefono: p.telefono, documento: p.documento,
      correo: p.correo, direccion: p.direccion, activo: !p.activo
    }).subscribe(() => this.cargar());
  }

  eliminar(p: Proveedor): void {
    if (!confirm(`Eliminar el proveedor "${p.nombre}"? Se ocultara sin borrar sus compras historicas.`)) return;

    this.proveedorService.delete(p.id).subscribe({
      next: () => {
        this.snackBar.open('Proveedor eliminado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el proveedor.', 'Cerrar', { duration: 5000 })
    });
  }
}
