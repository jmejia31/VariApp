import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { debounceTime, Subject } from 'rxjs';
import { MovimientoInventarioService } from '../../services/movimiento-inventario.service';
import { MovimientoInventario } from '../../core/models/movimiento-inventario.model';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

type OrdenMovimiento = 'fecha' | 'producto' | 'tipo';

@Component({
  selector: 'app-movimientos-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, MatIconModule, MatButtonModule, MatPaginatorModule, ProductoImagenComponent
  ],
  templateUrl: './movimientos-list.component.html',
  styleUrl: './movimientos-list.component.scss'
})
export class MovimientosListComponent implements OnInit {
  readonly movimientos = signal<MovimientoInventario[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);

  filtroTipo = '';
  search = '';
  sortBy: OrdenMovimiento = 'fecha';
  sortDirection: 'asc' | 'desc' = 'desc';
  page = 1;
  pageSize = 10;

  private movimientosOrigen: MovimientoInventario[] = [];
  private readonly navigationDefaults = { filtroTipo: '', search: '', sortBy: 'fecha', sortDirection: 'desc', page: 1, pageSize: 10 };
  private readonly searchSubject = new Subject<string>();

  constructor(
    private movimientoService: MovimientoInventarioService,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubject.pipe(debounceTime(250)).subscribe(() => { this.page = 1; this.aplicarVista(); });
  }

  ngOnInit(): void {
    const state = this.navigationState.restore('inventario-movimientos', this.route, this.navigationDefaults);
    this.filtroTipo = ['', 'Entrada', 'Salida', 'Reversion', 'Ajuste'].includes(state.filtroTipo) ? state.filtroTipo : '';
    this.search = state.search;
    this.sortBy = ['fecha', 'producto', 'tipo'].includes(state.sortBy) ? state.sortBy as OrdenMovimiento : 'fecha';
    this.sortDirection = state.sortDirection === 'asc' ? 'asc' : 'desc';
    this.page = Math.max(1, Math.trunc(state.page));
    this.pageSize = [10, 25, 50].includes(state.pageSize) ? state.pageSize : 10;
    this.persistirEstado();
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.movimientoService.getFiltered(undefined, this.filtroTipo || undefined).subscribe({
      next: (res) => {
        this.movimientosOrigen = res.data;
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
    this.cargar();
  }

  limpiarFiltros(): void {
    this.navigationState.clear('inventario-movimientos');
    this.filtroTipo = '';
    this.search = '';
    this.sortBy = 'fecha';
    this.sortDirection = 'desc';
    this.page = 1;
    this.pageSize = 10;
    this.cargar();
  }

  ordenarPor(campo: OrdenMovimiento): void {
    if (this.sortBy === campo) this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = campo; this.sortDirection = campo === 'fecha' ? 'desc' : 'asc'; }
    this.page = 1;
    this.aplicarVista();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.aplicarVista();
  }

  private aplicarVista(): void {
    const term = this.search.trim().toLowerCase();
    let data = this.movimientosOrigen.filter((m) => {
      if (!term) return true;
      return [m.productoNombre, m.productoColor, m.productoSku, m.referenciaTipo, m.creadoPorNombreUsuario]
        .some((value) => value?.toLowerCase().includes(term))
        || String(m.referenciaId).includes(term);
    });

    const direction = this.sortDirection === 'asc' ? 1 : -1;
    data = data.sort((a, b) => {
      if (this.sortBy === 'producto') return a.productoNombre.localeCompare(b.productoNombre, 'es', { sensitivity: 'base' }) * direction;
      if (this.sortBy === 'tipo') return a.tipo.localeCompare(b.tipo, 'es', { sensitivity: 'base' }) * direction;
      return (Date.parse(a.fecha) - Date.parse(b.fecha)) * direction;
    });

    this.totalCount.set(data.length);
    const maxPage = Math.max(1, Math.ceil(data.length / this.pageSize));
    this.page = Math.min(Math.max(1, this.page), maxPage);
    const start = (this.page - 1) * this.pageSize;
    this.movimientos.set(data.slice(start, start + this.pageSize));
    this.persistirEstado();
  }

  private persistirEstado(): void {
    this.navigationState.persist('inventario-movimientos', this.route, {
      filtroTipo: this.filtroTipo,
      search: this.search,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      page: this.page,
      pageSize: this.pageSize
    }, this.navigationDefaults);
  }
}
