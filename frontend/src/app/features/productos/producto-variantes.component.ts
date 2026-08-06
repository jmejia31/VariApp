import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { Producto, ProductoVariante, ProductoVarianteFormValue } from '../../core/models/producto.model';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { ProductoService } from '../../services/producto.service';

@Component({
  selector: 'app-producto-variantes',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, MatTableModule
  ],
  templateUrl: './producto-variantes.component.html',
  styleUrl: './producto-variantes.component.scss'
})
export class ProductoVariantesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productoService = inject(ProductoService);
  private readonly catalogoService = inject(CatalogoProductoService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly producto = signal<Producto | null>(null);
  readonly variantes = signal<ProductoVariante[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly ajustandoId = signal<number | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly editandoId = signal<number | null>(null);
  readonly displayedColumns = ['color', 'sku', 'codigoBarras', 'stock', 'costo', 'precio', 'estado', 'acciones'];
  productoId = 0;

  readonly form = this.fb.nonNullable.group({
    colorId: [0, [Validators.required, Validators.min(1)]],
    sku: ['', [Validators.required, Validators.maxLength(80)]],
    codigoBarras: ['', Validators.maxLength(120)],
    cantidad: [0, [Validators.required, Validators.min(0)]],
    umbralStockBajo: [5, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0)]],
    precio: [0, [Validators.required, Validators.min(0.01)]]
  });

  ngOnInit(): void {
    this.productoId = Number(this.route.snapshot.paramMap.get('id'));
    this.catalogoService.getActivos('Color').subscribe({
      next: (res) => this.colores.set(res.data),
      error: () => this.errorMessage.set('No se pudieron cargar los colores activos.')
    });
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.productoService.getById(this.productoId).subscribe({
      next: (res) => {
        this.producto.set(res.data);
        this.variantes.set(res.data.variantes ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el producto.');
        this.loading.set(false);
      }
    });
  }

  editar(variante: ProductoVariante): void {
    this.editandoId.set(variante.id);
    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.setValue({
      colorId: variante.colorId,
      sku: variante.sku,
      codigoBarras: variante.codigoBarras ?? '',
      cantidad: variante.cantidad,
      umbralStockBajo: variante.umbralStockBajo,
      costo: variante.costo,
      precio: variante.precio
    });
    this.form.controls.cantidad.disable({ emitEvent: false });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancelar(): void {
    this.editandoId.set(null);
    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.reset({ colorId: 0, sku: '', codigoBarras: '', cantidad: 0, umbralStockBajo: 5, costo: 0, precio: 0 });
  }

  ajustarStock(variante: ProductoVariante): void {
    if (this.ajustandoId() !== null) return;

    const cantidadTexto = window.prompt(
      `Stock actual de ${variante.sku}: ${variante.cantidad}. Ingresa la nueva cantidad:`,
      String(variante.cantidad)
    );
    if (cantidadTexto === null) return;

    const cantidadNueva = Number(cantidadTexto.trim());
    if (!Number.isInteger(cantidadNueva) || cantidadNueva < 0) {
      this.errorMessage.set('La nueva cantidad debe ser un entero mayor o igual que cero.');
      return;
    }

    const motivo = window.prompt('Motivo obligatorio del ajuste:')?.trim();
    if (!motivo) {
      this.errorMessage.set('El motivo del ajuste es obligatorio.');
      return;
    }

    this.ajustandoId.set(variante.id);
    this.errorMessage.set(null);
    this.productoService.ajustarStockVariante(this.productoId, variante.id, {
      cantidadActualEsperada: variante.cantidad,
      cantidadNueva,
      motivo
    }).subscribe({
      next: () => {
        this.ajustandoId.set(null);
        this.snackBar.open('Inventario de la variante ajustado correctamente.', 'Cerrar', { duration: 3500 });
        this.cancelar();
        this.cargar();
      },
      error: (err) => {
        this.ajustandoId.set(null);
        this.errorMessage.set(err.error?.message ?? 'No se pudo ajustar el inventario de la variante.');
        this.cargar();
      }
    });
  }

  guardar(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    const raw = this.form.getRawValue();
    const value: ProductoVarianteFormValue = {
      ...raw,
      sku: raw.sku.trim().toUpperCase(),
      codigoBarras: raw.codigoBarras.trim() || undefined
    };
    const request$ = this.editandoId()
      ? this.productoService.actualizarVariante(this.productoId, this.editandoId()!, value)
      : this.productoService.crearVariante(this.productoId, value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelar();
        this.snackBar.open('Variante guardada correctamente.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la variante.');
      }
    });
  }

  cambiarEstado(variante: ProductoVariante): void {
    this.productoService.cambiarEstadoVariante(this.productoId, variante.id, !variante.activo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo cambiar el estado.')
    });
  }

  eliminar(variante: ProductoVariante): void {
    if (!window.confirm(`¿Eliminar lógicamente la variante ${variante.sku}? Solo es posible con stock cero.`)) return;
    this.productoService.eliminarVariante(this.productoId, variante.id).subscribe({
      next: () => {
        this.snackBar.open('Variante eliminada lógicamente.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo eliminar la variante.')
    });
  }
}
