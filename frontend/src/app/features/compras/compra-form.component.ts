import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, AbstractControl, FormBuilder, FormArray, FormControl, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CompraService } from '../../services/compra.service';
import { ProductoService } from '../../services/producto.service';
import { ProveedorService } from '../../services/proveedor.service';
import { Producto } from '../../core/models/producto.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { ResultadoCalculo } from '../../core/models/compra.model';

@Component({
  selector: 'app-compra-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './compra-form.component.html',
  styleUrl: './compra-form.component.scss'
})
export class CompraFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly calculando = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly resultado = signal<ResultadoCalculo | null>(null);
  private compraId: number | null = null;

  // --- Autocompletado remoto de proveedores (sección 17) ---
  readonly buscadorProveedor = new FormControl('');
  readonly opcionesProveedor = signal<Proveedor[]>([]);
  readonly buscandoProveedor = signal(false);
  readonly proveedorSeleccionado = signal<Proveedor | null>(null);
  readonly errorBusquedaProveedor = signal<string | null>(null);
  private proveedorId: number | null = null;

  form = this.fb.group({
    proveedorNombre: ['', Validators.required],
    proveedorTelefono: [''],
    proveedorDocumento: [''],
    documentoReferencia: [''],
    metodoPago: ['Efectivo', Validators.required],
    estadoPago: ['Pendiente', Validators.required],
    notas: [''],
    detalles: this.fb.array([])
  });

  constructor(
    private compraService: CompraService,
    private productoService: ProductoService,
    private proveedorService: ProveedorService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  get detalles(): FormArray {
    return this.form.get('detalles') as FormArray;
  }

  ngOnInit(): void {
    this.productoService.getPaged({ page: 1, pageSize: 200, sortBy: 'Nombre' }).subscribe((res) => this.productos.set(res.data.items));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.compraId = Number(idParam);
      this.cargarCompra(this.compraId);
    } else {
      this.agregarDetalle();
    }

    this.buscadorProveedor.valueChanges.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      switchMap((termino) => {
        if (this.proveedorSeleccionado() && termino !== this.proveedorSeleccionado()!.nombre) {
          this.proveedorSeleccionado.set(null);
          this.proveedorId = null;
        }
        if (!termino || termino.trim().length < 2) {
          this.opcionesProveedor.set([]);
          return of(null);
        }
        this.buscandoProveedor.set(true);
        this.errorBusquedaProveedor.set(null);
        return this.proveedorService.buscar(termino).pipe(
          catchError(() => {
            this.errorBusquedaProveedor.set('No se pudo buscar proveedores. Intenta de nuevo.');
            return of(null);
          })
        );
      })
    ).subscribe((res) => {
      this.buscandoProveedor.set(false);
      if (res) this.opcionesProveedor.set(res.data);
    });

    this.form.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    ).subscribe(() => this.recalcular());
  }

  onProveedorSeleccionado(event: MatAutocompleteSelectedEvent): void {
    const proveedor: Proveedor = event.option.value;
    this.proveedorSeleccionado.set(proveedor);
    this.proveedorId = proveedor.id;
    this.buscadorProveedor.setValue(proveedor.nombre, { emitEvent: false });
    this.form.patchValue({
      proveedorNombre: proveedor.nombre,
      proveedorTelefono: proveedor.telefono,
      proveedorDocumento: proveedor.documento
    });
  }

  limpiarProveedorSeleccionado(): void {
    this.proveedorSeleccionado.set(null);
    this.proveedorId = null;
    this.buscadorProveedor.setValue('');
    this.form.patchValue({ proveedorNombre: '', proveedorTelefono: '', proveedorDocumento: '' });
  }

  displayProveedor(proveedor: Proveedor): string {
    return proveedor?.nombre ?? '';
  }

  private cargarCompra(id: number): void {
    this.loading.set(true);
    this.compraService.getById(id).subscribe({
      next: (res) => {
        const c = res.data;
        this.buscadorProveedor.setValue(c.proveedorNombre, { emitEvent: false });
        this.form.patchValue({
          proveedorNombre: c.proveedorNombre,
          proveedorTelefono: c.proveedorTelefono,
          proveedorDocumento: c.proveedorDocumento,
          documentoReferencia: c.documentoReferencia,
          metodoPago: c.metodoPago,
          estadoPago: c.estadoPago,
          notas: c.notas
        });
        c.detalles.forEach((d) => this.agregarDetalle(d.productoId, d.cantidad, d.costoUnitario));
        this.resultado.set({
          subtotal: c.subtotal,
          descuentosAplicados: [],
          totalDescuento: c.descuento,
          impuestosAplicados: c.impuestosAplicados,
          totalImpuesto: c.impuesto,
          total: c.total
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  agregarDetalle(productoId: number | null = null, cantidad = 1, costoUnitario = 0): void {
    this.detalles.push(this.fb.group({
      productoId: [productoId, Validators.required],
      cantidad: [cantidad, [Validators.required, Validators.min(1)]],
      costoUnitario: [costoUnitario, [Validators.required, Validators.min(0.01)]]
    }));
  }

  quitarDetalle(index: number): void {
    this.detalles.removeAt(index);
    this.recalcular();
  }

  subtotalDetalle(group: AbstractControl): number {
    const cantidad = group.value.cantidad || 0;
    const costo = group.value.costoUnitario || 0;
    return cantidad * costo;
  }

  /** Llama al backend para obtener el desglose REAL de impuestos. */
  recalcular(): void {
    const detallesValidos = this.detalles.controls
      .map((g) => g.value)
      .filter((d) => d.productoId && d.cantidad > 0 && d.costoUnitario >= 0)
      .map((d) => ({ productoId: d.productoId, cantidad: d.cantidad, precioUnitario: d.costoUnitario }));

    if (detallesValidos.length === 0) {
      this.resultado.set(null);
      return;
    }

    this.calculando.set(true);
    this.compraService.calcular(this.proveedorId, detallesValidos).subscribe({
      next: (res) => { this.resultado.set(res.data); this.calculando.set(false); },
      error: () => { this.calculando.set(false); }
    });
  }

  submit(): void {
    if (this.form.invalid || this.detalles.length === 0) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = { ...this.form.getRawValue(), descuento: 0, impuesto: 0 } as any;

    const request$ = this.isEdit()
      ? this.compraService.update(this.compraId!, value)
      : this.compraService.create(value);

    request$.subscribe({
      next: (res) => {
        this.saving.set(false);
        this.router.navigate(['/compras', res.data.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la compra.');
      }
    });
  }
}
