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
import { AppAlertService } from '../../shared/alerts/app-alert.service';

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
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private clienteService: ClienteService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Clientes', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Clientes', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Clientes', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Clientes', 'Desactivar'));
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

  puedeCambiarEstado(cliente: Cliente): boolean {
    return cliente.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  toggleActivo(c: Cliente): void {
    if (!this.puedeCambiarEstado(c)) return;
    const operacion = c.activo
      ? this.clienteService.desactivar(c.id)
      : this.clienteService.activar(c.id);

    operacion.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado del cliente.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(c: Cliente): Promise<void> {
    const confirmado = await this.alerts.confirmar({ titulo: 'Eliminar cliente', mensaje: `Se ocultará a "${c.nombre}" sin borrar sus ventas históricas.`, tipo: 'peligro', confirmarTexto: 'Eliminar' });
    if (!confirmado) return;

    this.clienteService.delete(c.id).subscribe({
      next: () => {
        this.snackBar.open('Cliente eliminado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el cliente.', 'Cerrar', { duration: 5000 })
    });
  }
}
