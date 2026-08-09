import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { FormsModule } from '@angular/forms';
import { debounceTime, Subject } from 'rxjs';
import { ClienteService } from '../../services/cliente.service';
import { Cliente } from '../../core/models/cliente.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

type EstadoClienteFiltro = 'todos' | 'activos' | 'inactivos';
type OrdenCliente = 'nombre' | 'totalVentas' | 'totalVendido';

@Component({
  selector: 'app-clientes-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule,
    MatSlideToggleModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule, FormsModule
  ],
  templateUrl: './clientes-list.component.html',
  styleUrl: './clientes-list.component.scss'
})
export class ClientesListComponent implements OnInit {
  readonly clientes = signal<Cliente[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  search = '';
  estado: EstadoClienteFiltro = 'todos';
  sortBy: OrdenCliente = 'nombre';
  sortDirection: 'asc' | 'desc' = 'asc';
  page = 1;
  pageSize = 10;

  private clientesOrigen: Cliente[] = [];
  private readonly navigationDefaults = { search: '', estado: 'todos', sortBy: 'nombre', sortDirection: 'asc', page: 1, pageSize: 10 };
  private readonly searchSubject = new Subject<string>();

  constructor(
    private clienteService: ClienteService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubject.pipe(debounceTime(250)).subscribe(() => { this.page = 1; this.aplicarVista(); });
  }

  ngOnInit(): void {
    const state = this.navigationState.restore('clientes', this.route, this.navigationDefaults);
    this.search = state.search;
    this.estado = ['todos', 'activos', 'inactivos'].includes(state.estado) ? state.estado as EstadoClienteFiltro : 'todos';
    this.sortBy = ['nombre', 'totalVentas', 'totalVendido'].includes(state.sortBy) ? state.sortBy as OrdenCliente : 'nombre';
    this.sortDirection = state.sortDirection === 'desc' ? 'desc' : 'asc';
    this.page = Math.max(1, Math.trunc(state.page));
    this.pageSize = [10, 25, 50].includes(state.pageSize) ? state.pageSize : 10;

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
      next: (res) => {
        this.clientesOrigen = res.data;
        this.aplicarVista();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.aplicarVista();
  }

  limpiarFiltros(): void {
    this.navigationState.clear('clientes');
    this.search = '';
    this.estado = 'todos';
    this.sortBy = 'nombre';
    this.sortDirection = 'asc';
    this.page = 1;
    this.pageSize = 10;
    this.aplicarVista();
  }

  ordenarPor(campo: OrdenCliente): void {
    if (this.sortBy === campo) this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = campo; this.sortDirection = 'asc'; }
    this.page = 1;
    this.aplicarVista();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.aplicarVista();
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

  private aplicarVista(): void {
    const term = this.search.trim().toLowerCase();
    let data = this.clientesOrigen.filter((cliente) => {
      const coincideEstado = this.estado === 'todos'
        || (this.estado === 'activos' && cliente.activo)
        || (this.estado === 'inactivos' && !cliente.activo);
      if (!coincideEstado) return false;
      if (!term) return true;
      return [cliente.nombre, cliente.telefono, cliente.identidadORTN, cliente.correo, cliente.tipoClienteNombre]
        .some((value) => value?.toLowerCase().includes(term));
    });

    const direction = this.sortDirection === 'asc' ? 1 : -1;
    data = data.sort((a, b) => {
      if (this.sortBy === 'totalVentas') return (a.totalVentas - b.totalVentas) * direction;
      if (this.sortBy === 'totalVendido') return (a.totalVendido - b.totalVendido) * direction;
      return a.nombre.localeCompare(b.nombre, 'es', { sensitivity: 'base' }) * direction;
    });

    this.totalCount.set(data.length);
    const maxPage = Math.max(1, Math.ceil(data.length / this.pageSize));
    this.page = Math.min(Math.max(1, this.page), maxPage);
    const start = (this.page - 1) * this.pageSize;
    this.clientes.set(data.slice(start, start + this.pageSize));
    this.persistirEstado();
  }

  private persistirEstado(): void {
    this.navigationState.persist('clientes', this.route, {
      search: this.search,
      estado: this.estado,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      page: this.page,
      pageSize: this.pageSize
    }, this.navigationDefaults);
  }
}
