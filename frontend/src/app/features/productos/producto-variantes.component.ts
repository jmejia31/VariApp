import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
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
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly modelos = signal<CatalogoProducto[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly tallas = signal<CatalogoProducto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly ajustandoId = signal<number | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly editandoId = signal<number | null>(null);
  readonly displayedColumns = ['variante', 'sku', 'codigoBarras', 'stock', 'costo', 'precio', 'estado', 'acciones'];
  productoId = 0;

  readonly form = this.fb.group({
    marcaId: [null as number | null],
    modeloId: [null as number | null],
    colorId: [null as number | null],
    tallaId: [null as number | null],
    sku: ['', Validators.maxLength(80)],
    codigoBarras: ['', Validators.maxLength(120)],
    cantidad: [0, [Validators.required, Validators.min(0)]],
    umbralStockBajo: [5, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0)]],
    precio: [0, [Validators.required, Validators.min(0.01)]]
  });

  ngOnInit(): void {
    this.productoId = Number(this.route.snapshot.paramMap.get('id'));
    forkJoin({
      marcas: this.catalogoService.getActivos('Marca'),
      modelos: this.catalogoService.getAll('Modelo'),
      colores: this.catalogoService.getActivos('Color'),
      tallas: this.catalogoService.getActivos('Talla')
    }).subscribe({
      next: (res) => {
        this.marcas.set(res.marcas.data);
        this.modelos.set(res.modelos.data.filter(modelo => modelo.activo));
        this.colores.set(res.colores.data);
        this.tallas.set(res.tallas.data);
      },
      error: () => this.errorMessage.set('No se pudieron cargar los catálogos de variantes.')
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

  modelosDeMarca(marcaId: number | null | undefined): CatalogoProducto[] {
    if (!marcaId) return [];
    return this.modelos().filter(modelo => modelo.catalogoPadreId === Number(marcaId));
  }

  onMarcaChange(): void {
    const marcaId = this.form.controls.marcaId.value;
    const modeloId = this.form.controls.modeloId.value;
    if (modeloId && !this.modelosDeMarca(marcaId).some(modelo => modelo.id === modeloId)) {
      this.form.controls.modeloId.setValue(null);
    }
  }

  editar(variante: ProductoVariante): void {
    if (variante.esTecnica) {
      this.errorMessage.set('La variante técnica representa un producto simple. Convierte el producto a variantes desde Editar producto.');
      return;
    }
    this.editandoId.set(variante.id);
    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.setValue({
      marcaId: variante.marcaId ?? null,
      modeloId: variante.modeloId ?? null,
      colorId: variante.colorId ?? null,
      tallaId: variante.tallaId ?? null,
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
    this.form.reset({
      marcaId: null,
      modeloId: null,
      colorId: null,
      tallaId: null,
      sku: '',
      codigoBarras: '',
      cantidad: 0,
      umbralStockBajo: 5,
      costo: 0,
      precio: 0
    });
  }

  ajustarStock(variante: ProductoVariante): void {
    if (this.ajustandoId() !== null) return;

    const cantidadTexto = window.prompt(
      `Stock actual de ${variante.etiqueta || variante.sku}: ${variante.cantidad}. Ingresa la nueva cantidad:`,
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
        this.snackBar.open('Inventario de la variante exacta ajustado correctamente.', 'Cerrar', { duration: 3500 });
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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const value: ProductoVarianteFormValue = {
      marcaId: raw.marcaId || null,
      modeloId: raw.modeloId || null,
      colorId: raw.colorId || null,
      tallaId: raw.tallaId || null,
      sku: raw.sku?.trim()?.toUpperCase() || undefined,
      codigoBarras: raw.codigoBarras?.trim() || undefined,
      cantidad: Number(raw.cantidad),
      umbralStockBajo: Number(raw.umbralStockBajo),
      costo: Number(raw.costo),
      precio: Number(raw.precio)
    };

    if (!value.marcaId && !value.modeloId && !value.colorId && !value.tallaId) {
      this.errorMessage.set('Define al menos una dimensión: Marca, Modelo, Color o Talla.');
      return;
    }
    if (value.modeloId && !value.marcaId) {
      this.errorMessage.set('Para utilizar Modelo debes seleccionar su Marca.');
      return;
    }

    const duplicada = this.variantes().some(v =>
      !v.esTecnica &&
      v.id !== this.editandoId() &&
      (v.marcaId ?? null) === value.marcaId &&
      (v.modeloId ?? null) === value.modeloId &&
      (v.colorId ?? null) === value.colorId &&
      (v.tallaId ?? null) === value.tallaId);
    if (duplicada) {
      this.errorMessage.set('Ya existe una variante con la misma combinación de Marca, Modelo, Color y Talla.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    const request$ = this.editandoId()
      ? this.productoService.actualizarVariante(this.productoId, this.editandoId()!, value)
      : this.productoService.crearVariante(this.productoId, value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelar();
        this.snackBar.open('Variante multidimensional guardada correctamente.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la variante.');
      }
    });
  }

  cambiarEstado(variante: ProductoVariante): void {
    if (variante.esTecnica) return;
    this.productoService.cambiarEstadoVariante(this.productoId, variante.id, !variante.activo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo cambiar el estado.')
    });
  }

  eliminar(variante: ProductoVariante): void {
    if (variante.esTecnica) return;
    if (!window.confirm(`¿Eliminar lógicamente la variante ${variante.etiqueta || variante.sku}? Solo es posible con stock cero.`)) return;
    this.productoService.eliminarVariante(this.productoId, variante.id).subscribe({
      next: () => {
        this.snackBar.open('Variante eliminada lógicamente.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo eliminar la variante.')
    });
  }
}
