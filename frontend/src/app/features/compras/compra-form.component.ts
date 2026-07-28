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
import { Producto, ProductoVariante } from '../../core/models/producto.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { ResultadoCalculo } from '../../core/models/compra.model';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

@Component({
  selector: 'app-compra-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, ProductoImagenComponent
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

  readonly buscadorProveedor = new FormControl('');
  readonly opcionesProveedor = signal<Proveedor[]>([]);
  readonly buscandoProveedor = signal(false);
  readonly proveedorSeleccionado = signal<Proveedor | null>(null);
  readonly errorBusquedaProveedor = signal<string | null>(null);
  private proveedorId: number | null = null;

  form = this.fb.group({
    proveedorNombre: ['', Validators.required], proveedorTelefono: [''], proveedorDocumento: [''],
    documentoReferencia: [''], metodoPago: ['Efectivo', Validators.required],
    estadoPago: ['Pendiente', Validators.required], notas: [''], detalles: this.fb.array([])
  });

  constructor(
    private compraService: CompraService,
    private productoService: ProductoService,
    private proveedorService: ProveedorService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  get detalles(): FormArray { return this.form.get('detalles') as FormArray; }

  ngOnInit(): void {
    this.productoService.getPaged({ page: 1, pageSize: 200, sortBy: 'Nombre' }).subscribe((res) =>
      this.productos.set(res.data.items.filter((producto) => producto.activo))
    );

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) { this.isEdit.set(true); this.compraId = Number(idParam); this.cargarCompra(this.compraId); }
    else this.agregarDetalle();

    this.buscadorProveedor.valueChanges.pipe(
      debounceTime(350), distinctUntilChanged(),
      switchMap((termino) => {
        if (this.proveedorSeleccionado() && termino !== this.proveedorSeleccionado()!.nombre) {
          this.proveedorSeleccionado.set(null); this.proveedorId = null;
        }
        if (!termino || termino.trim().length < 2) { this.opcionesProveedor.set([]); return of(null); }
        this.buscandoProveedor.set(true); this.errorBusquedaProveedor.set(null);
        return this.proveedorService.buscar(termino).pipe(catchError(() => {
          this.errorBusquedaProveedor.set('No se pudo buscar proveedores. Intenta de nuevo.'); return of(null);
        }));
      })
    ).subscribe((res) => { this.buscandoProveedor.set(false); if (res) this.opcionesProveedor.set(res.data); });

    this.form.valueChanges.pipe(
      debounceTime(500), distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    ).subscribe(() => this.recalcular());
  }

  onProveedorSeleccionado(event: MatAutocompleteSelectedEvent): void {
    const proveedor: Proveedor = event.option.value;
    this.proveedorSeleccionado.set(proveedor); this.proveedorId = proveedor.id;
    this.buscadorProveedor.setValue(proveedor.nombre, { emitEvent: false });
    this.form.patchValue({ proveedorNombre: proveedor.nombre, proveedorTelefono: proveedor.telefono, proveedorDocumento: proveedor.documento });
  }

  limpiarProveedorSeleccionado(): void {
    this.proveedorSeleccionado.set(null); this.proveedorId = null; this.buscadorProveedor.setValue('');
    this.form.patchValue({ proveedorNombre: '', proveedorTelefono: '', proveedorDocumento: '' });
  }

  displayProveedor(proveedor: Proveedor): string { return proveedor?.nombre ?? ''; }

  private cargarCompra(id: number): void {
    this.loading.set(true);
    this.compraService.getById(id).subscribe({
      next: (res) => {
        const c = res.data;
        this.buscadorProveedor.setValue(c.proveedorNombre, { emitEvent: false });
        this.form.patchValue({
          proveedorNombre: c.proveedorNombre, proveedorTelefono: c.proveedorTelefono,
          proveedorDocumento: c.proveedorDocumento, documentoReferencia: c.documentoReferencia,
          metodoPago: c.metodoPago, estadoPago: c.estadoPago, notas: c.notas
        });
        c.detalles.forEach((d) => this.agregarDetalle(d.productoId, d.productoVarianteId ?? null, d.cantidad, d.costoUnitario));

        const importeBruto = c.detalles.reduce((total, detalle) => total + detalle.subtotal, 0);
        const totalDespuesDescuento = Math.max(0, importeBruto - c.descuento);
        const asumirIncluidos = Math.abs(c.total - totalDespuesDescuento) <= 0.01;
        const impuestos = c.impuestosAplicados.map((impuesto) => ({ ...impuesto, incluidoEnPrecio: impuesto.incluidoEnPrecio || asumirIncluidos }));
        const impuestoIncluido = impuestos.filter((i) => i.incluidoEnPrecio).reduce((total, i) => total + i.monto, 0);
        const impuestoAdicional = impuestos.filter((i) => !i.incluidoEnPrecio).reduce((total, i) => total + i.monto, 0);
        this.resultado.set({
          importeBruto, subtotal: c.subtotal, subtotalNeto: c.subtotal, descuentosAplicados: [],
          totalDescuento: c.descuento, impuestosAplicados: impuestos, totalImpuesto: c.impuesto,
          impuestoIncluido, impuestoAdicional, total: c.total
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  agregarDetalle(productoId: number | null = null, productoVarianteId: number | null = null, cantidad = 1, costoUnitario = 0): void {
    this.detalles.push(this.fb.group({
      productoId: [productoId, Validators.required],
      productoVarianteId: [productoVarianteId],
      cantidad: [cantidad, [Validators.required, Validators.min(1)]],
      costoUnitario: [costoUnitario, [Validators.required, Validators.min(0.01)]]
    }));
  }

  onProductoSeleccionado(index: number, productoId: number): void {
    const producto = this.productos().find((p) => p.id === productoId);
    if (!producto) return;
    const activas = (producto.variantes ?? []).filter((v) => v.activo);
    if (activas.length === 1) {
      this.detalles.at(index).patchValue({ productoVarianteId: activas[0].id, costoUnitario: activas[0].costo });
      this.errorMessage.set(null);
    } else {
      this.detalles.at(index).patchValue({ productoVarianteId: null, costoUnitario: producto.costo });
      if (producto.usaVariantes && activas.length === 0) {
        this.errorMessage.set(`El producto '${producto.nombre}' no tiene variantes activas disponibles.`);
      }
    }
  }

  onVarianteSeleccionada(index: number, varianteId: number): void {
    const grupo = this.detalles.at(index);
    const variante = this.variantesDisponibles(grupo).find((v) => v.id === varianteId);
    if (variante) {
      grupo.patchValue({ costoUnitario: variante.costo });
      this.errorMessage.set(null);
    }
  }

  requiereVariante(group: AbstractControl): boolean {
    return (this.productoSeleccionado(group)?.variantes ?? []).length > 0;
  }

  variantesDisponibles(group: AbstractControl): ProductoVariante[] {
    return (this.productoSeleccionado(group)?.variantes ?? []).filter((v) => v.activo);
  }

  productoSeleccionado(group: AbstractControl): Producto | undefined {
    return this.productos().find((producto) => producto.id === Number(group.value.productoId));
  }

  quitarDetalle(index: number): void { this.detalles.removeAt(index); this.recalcular(); }
  subtotalDetalle(group: AbstractControl): number { return (group.value.cantidad || 0) * (group.value.costoUnitario || 0); }

  recalcular(): void {
    const detallesValidos = this.detalles.controls.map((g) => g.value)
      .filter((d) => d.productoId && d.cantidad > 0 && d.costoUnitario >= 0)
      .map((d) => ({
        productoId: d.productoId,
        productoVarianteId: d.productoVarianteId,
        cantidad: d.cantidad,
        precioUnitario: d.costoUnitario
      }));

    if (detallesValidos.length === 0 || this.detalles.controls.some((g) => this.requiereVariante(g) && !g.value.productoVarianteId)) {
      this.resultado.set(null); return;
    }

    this.calculando.set(true);
    this.compraService.calcular(this.proveedorId, detallesValidos).subscribe({
      next: (res) => { this.resultado.set(res.data); this.calculando.set(false); this.errorMessage.set(null); },
      error: (err) => { this.calculando.set(false); this.resultado.set(null); this.errorMessage.set(err.error?.message ?? 'No se pudo calcular la compra.'); }
    });
  }

  submit(): void {
    if (this.form.invalid || this.detalles.length === 0 || !this.resultado() || this.saving()) return;
    this.saving.set(true); this.errorMessage.set(null);
    const value = { ...this.form.getRawValue(), descuento: 0, impuesto: 0 } as any;
    const request$ = this.isEdit() ? this.compraService.update(this.compraId!, value) : this.compraService.create(value);
    request$.subscribe({
      next: (res) => { this.saving.set(false); this.router.navigate(['/compras', res.data.id]); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la compra.'); }
    });
  }
}
