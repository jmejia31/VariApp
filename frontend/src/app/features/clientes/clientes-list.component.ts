import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { ClienteService } from '../../services/cliente.service';
import { Cliente } from '../../core/models/cliente.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-clientes-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './clientes-list.component.html',
  styleUrl: './clientes-list.component.scss'
})
export class ClientesListComponent implements OnInit {
  readonly clientes = signal<Cliente[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private clienteService: ClienteService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Clientes', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Clientes', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Clientes', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.clienteService.getAll().subscribe({
      next: (res) => { this.clientes.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleActivo(c: Cliente): void {
    this.clienteService.update(c.id, {
      nombre: c.nombre, telefono: c.telefono, identidadORTN: c.identidadORTN,
      correo: c.correo, direccion: c.direccion, activo: !c.activo
    }).subscribe(() => this.cargar());
  }

  eliminar(c: Cliente): void {
    if (!confirm(`Eliminar el cliente "${c.nombre}"? Se ocultara sin borrar sus ventas historicas.`)) return;

    this.clienteService.delete(c.id).subscribe({
      next: () => {
        this.snackBar.open('Cliente eliminado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el cliente.', 'Cerrar', { duration: 5000 })
    });
  }
}
