import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { debounceTime, forkJoin, Subject } from 'rxjs';
import { ProductoService } from '../../services/producto.service';
import { CategoriaService } from '../../services/categoria.service';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { Producto } from '../../core/models/producto.model';
import { Categoria } from '../../core/models/categoria.model';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

type EstadoProductoFiltro = 'todos' | 'activos' | 'inactivos' | 'agotados' | 'disponibles';

@Component({
  selector: 'app-productos-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule,
    MatProgressSpinnerModule, MatDialogModule, MatSlideToggleModule
  ],
  templateUrl: './productos-list.component.html',
  styleUrl: './productos-list.component.scss'
})
export class ProductosListComponent implements OnInit {
  readonly productos = signal<Producto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly tallas = signal<CatalogoProducto[]>([]);
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly modelos = signal<CatalogoProducto[]>([]);
  readonly loading = signal(true);
  readonly loadingFilters = signal(true);
  readonly totalCount = signal(0);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  page = 1;
  pageSize = 10;
  search = '';
  sortBy = 'Nombre';
  sortDirection: 'asc' | 'desc' = 'asc';
  categoriaId: number | null = null;
  colorId: number | null = null;
  tallaId: number | null = null;
  marcaId: number | null = null;
  modeloId: number | null = null;
  estado: EstadoProductoFiltro = 'todos';

  private searchSubject = new Subject<string>();

  constructor(
    private productoService: ProductoService,
    private categoriaService: CategoriaService,
    private catalogoService: CatalogoProductoService,
    private dialog: MatDialog,
    private permisosRuntime: PermisosRuntimeService
  ) {
    this.searchSubject.pipe(debounceTime(350)).subscribe(() => {
      this.page = 1;
      this.cargar();
    });
  }

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Productos', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Productos', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Productos', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Productos', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Productos', 'EliminarLogico'));
    this.cargarCatalogosFiltro();
    this.cargar();
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  onMarcaFilterChange(marcaId: number | null): void {
    this.marcaId = marcaId;
    this.modeloId = null;
    this.modelos.set([]);

    if (!marcaId) {
      this.aplicarFiltros();
      return;
    }

    this.catalogoService.getAll('Modelo', '', marcaId).subscribe({
      next: (res) => {
        this.modelos.set(res.data);
        this.aplicarFiltros();
      },
      error: () => this.aplicarFiltros()
    });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.search = '';
    this.categoriaId = null;
    this.colorId = null;
    this.tallaId = null;
    this.marcaId = null;
    this.modeloId = null;
    this.estado = 'todos';
    this.modelos.set([]);
    this.page = 1;
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.productoService.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      search: this.search,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      categoriaId: this.categoriaId ?? undefined,
      colorId: this.colorId ?? undefined,
      tallaId: this.tallaId ?? undefined,
      marcaId: this.marcaId ?? undefined,
      modeloId: this.modeloId ?? undefined,
      activo: this.estado === 'activos' ? true : this.estado === 'inactivos' ? false : undefined,
      agotado: this.estado === 'agotados' ? true : this.estado === 'disponibles' ? false : undefined
    }).subscribe({
      next: (res) => {
        this.productos.set(res.data.items);
        this.totalCount.set(res.data.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  puedeCambiarEstado(producto: Producto): boolean {
    return producto.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  cambiarEstado(producto: Producto): void {
    if (!this.puedeCambiarEstado(producto)) return;

    const operacion = producto.activo
      ? this.productoService.desactivar(producto.id)
      : this.productoService.activar(producto.id);

    operacion.subscribe({
      next: () => this.cargar(),
      error: () => this.cargar()
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  ordenarPor(campo: string): void {
    if (this.sortBy === campo) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = campo;
      this.sortDirection = 'asc';
    }
    this.cargar();
  }

  eliminar(producto: Producto): void {
    if (!this.puedeEliminar()) return;

    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar producto',
        message: `¿Deseas eliminar lógicamente "${producto.nombre}"? Su historial e imágenes se conservarán.`
      }
    });

    ref.afterClosed().subscribe((confirmado) => {
      if (!confirmado) return;
      this.productoService.delete(producto.id).subscribe(() => this.cargar());
    });
  }

  private cargarCatalogosFiltro(): void {
    this.loadingFilters.set(true);
    forkJoin({
      categorias: this.categoriaService.getAll(),
      colores: this.catalogoService.getAll('Color'),
      tallas: this.catalogoService.getAll('Talla'),
      marcas: this.catalogoService.getAll('Marca')
    }).subscribe({
      next: (res) => {
        this.categorias.set(res.categorias.data);
        this.colores.set(res.colores.data);
        this.tallas.set(res.tallas.data);
        this.marcas.set(res.marcas.data);
        this.loadingFilters.set(false);
      },
      error: () => this.loadingFilters.set(false)
    });
  }
}
