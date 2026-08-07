import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, AbstractControl, FormBuilder, FormArray, FormControl, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, finalize } from 'rxjs/operators';
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
import { Producto, ProductoEscaneadoCompra, ProductoVariante } from '../../core/models/producto.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { ResultadoCalculo } from '../../core/models/compra.model';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';
import { CodigoScannerInputComponent } from '../../shared/codigo-scanner-input/codigo-scanner-input.component';

@Component({
  selector: 'app-compra-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, ProductoImagenComponent, CodigoScannerInputComponent
  ],
  templateUrl: './compra-form.component.html',
  styleUrl: './compra-form.component.scss'
})
export class CompraFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  @ViewChild(CodigoScannerInputComponent) private scannerInput?: CodigoScannerInputComponent;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly calculando = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly resultado = signal<ResultadoCalculo | null>(null);
  readonly procesandoEscaneo = signal(false);
  readonly mensajeEscaneo = signal<string | null>(null);
  readonly errorEscaneo = signal(false);
  private compraId: number | null = null;

  readonly buscadorProveedor = new FormControl('');
  readonly opcionesProveedor = signal<Proveedor[]>([]);
  readonly buscandoProveedor = signal(false);
  readonly proveedorSeleccionado = signal<Proveedor | null>(null);
  readonly errorBusquedaProveedor = signal<string | null>(null);
  private proveedorId: number | null = null;

  readonly buscadorProducto = new FormControl<string | ProductoEscaneadoCompra>('');
  readonly opcionesProducto = signal<ProductoEscaneadoCompra[]>([]);
  readonly buscandoProducto = signal(false);
  readonly errorBusquedaProducto = signal<string | null>(null);
  readonly mensajeBusquedaProducto = signal<string | null>(null);

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

    this.buscadorProducto.valueChanges.pipe(
      debounceTime(300),
      switchMap((valor) => {
        const termino = typeof valor === 'string' ? valor.trim() : '';
        this.mensajeBusquedaProducto.set(null);
        this.errorBusquedaProducto.set(null);
        if (termino.length < 2) {
          this.buscandoProducto.set(false);
          this.opcionesProducto.set([]);
          return of(null);
        }

        this.buscandoProducto.set(true);
        return this.compraService.buscarProductos(termino, 30).pipe(
          catchError((err) => {
            this.errorBusquedaProducto.set(err.error?.message ?? 'No se pudieron buscar productos. Intenta de nuevo.');
            return of(null);
          }),
          finalize(() => this.buscandoProducto.set(false))
        );
      })
    ).subscribe((res) => this.opcionesProducto.set(res?.data ?? []));

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

  displayProductoOperacion(item: ProductoEscaneadoCompra | string | null): string {
    if (!item || typeof item === 'string') return typeof item === 'string' ? item : '';
    const variante = item.colorNombre || (item.esVarianteTecnica ? 'Predeterminada' : item.sku);
    return `${item.productoNombre}${variante ? ` · ${variante}` : ''}`;
  }

  onProductoBuscadoSeleccionado(event: MatAutocompleteSelectedEvent): void {
    const item = event.option.value as ProductoEscaneadoCompra;
    const mensaje = this.aplicarProductoOperacion(item);
    this.errorBusquedaProducto.set(null);
    this.mensajeBusquedaProducto.set(mensaje);
    this.buscadorProducto.setValue('', { emitEvent: false });
    this.opcionesProducto.set([]);
  }

  procesarCodigoEscaner(codigo: string): void {
    if (this.procesandoEscaneo()) return;
    this.procesandoEscaneo.set(true);
    this.mensajeEscaneo.set(null);
    this.errorEscaneo.set(false);

    this.compraService.buscarProductoPorCodigo(codigo).pipe(
      finalize(() => {
        this.procesandoEscaneo.set(false);
        this.scannerInput?.reenfocar();
      })
    ).subscribe({
      next: (res) => this.mensajeEscaneo.set(this.aplicarProductoOperacion(res.data)),
      error: (err) => {
        this.errorEscaneo.set(true);
        this.mensajeEscaneo.set(err.error?.message ?? 'No se pudo resolver el SKU o código de barras.');
      }
    });
  }

  private aplicarProductoOperacion(item: ProductoEscaneadoCompra): string {
    const coincidencias = this.detalles.controls
      .map((grupo, index) => ({ grupo, index }))
      .filter(({ grupo }) =>
        Number(grupo.value.productoId) === item.productoId
        && Number(grupo.value.productoVarianteId) === item.productoVarianteId
      );

    if (coincidencias.length > 0) {
      const cantidadActual = coincidencias.reduce(
        (total, { grupo }) => total + Number(grupo.value.cantidad || 0),
        0
      );
      const nuevaCantidad = cantidadActual + 1;
      coincidencias[0].grupo.patchValue({
        cantidad: nuevaCantidad,
        costoUnitario: item.costo
      });
      coincidencias
        .slice(1)
        .map(({ index }) => index)
        .sort((a, b) => b - a)
        .forEach((index) => this.detalles.removeAt(index));

      return `${item.productoNombre}: cantidad consolidada en ${nuevaCantidad}.`;
    }

    this.incorporarProductoOperacion(item);
    const filaVacia = this.detalles.controls.find((grupo) => !grupo.value.productoId);
    const valores = {
      productoId: item.productoId,
      productoVarianteId: item.productoVarianteId,
      cantidad: 1,
      costoUnitario: item.costo
    };
    if (filaVacia) filaVacia.patchValue(valores);
    else this.agregarDetalle(item.productoId, item.productoVarianteId, 1, item.costo);

    this.errorMessage.set(null);
    return `${item.productoNombre}${item.colorNombre ? ` · ${item.colorNombre}` : ''} agregado a la compra.`;
  }

  private incorporarProductoOperacion(item: ProductoEscaneadoCompra): void {
    const variante: ProductoVariante = {
      id: item.productoVarianteId,
      productoId: item.productoId,
      productoNombre: item.productoNombre,
      colorId: item.colorId ?? 0,
      colorNombre: item.colorNombre ?? (item.esVarianteTecnica ? 'Predeterminada' : 'Sin color'),
      sku: item.sku,
      codigoBarras: item.codigoBarras ?? undefined,
      cantidad: item.cantidadDisponible,
      umbralStockBajo: 0,
      costo: item.costo,
      precio: item.precio,
      activo: true,
      tieneStockBajo: false,
      estaAgotada: item.cantidadDisponible <= 0,
      estadoInventario: item.cantidadDisponible > 0 ? 'Disponible' : 'Agotado',
      fechaCreacion: '',
      fechaActualizacion: ''
    };

    const actuales = [...this.productos()];
    const indice = actuales.findIndex((producto) => producto.id === item.productoId);
    if (indice >= 0) {
      const producto = actuales[indice];
      const variantes = [...(producto.variantes ?? [])];
      const indiceVariante = variantes.findIndex((actual) => actual.id === item.productoVarianteId);
      if (indiceVariante >= 0) variantes[indiceVariante] = { ...variantes[indiceVariante], ...variante };
      else variantes.push(variante);
      actuales[indice] = {
        ...producto,
        cantidad: variantes.reduce((total, actual) => total + actual.cantidad, 0),
        costo: item.costo,
        precio: item.precio,
        imagenPrincipalUrl: item.imagenMiniaturaUrl ?? producto.imagenPrincipalUrl,
        variantes,
        totalVariantes: variantes.length
      };
    } else {
      actuales.push({
        id: item.productoId,
        nombre: item.productoNombre,
        marca: item.marca,
        modelo: item.modelo,
        cantidad: item.cantidadDisponible,
        costo: item.costo,
        precio: item.precio,
        precioMinimo: item.precio,
        precioMaximo: item.precio,
        umbralStockBajo: 0,
        tieneStockBajo: false,
        estaAgotado: item.cantidadDisponible <= 0,
        estadoInventario: item.cantidadDisponible > 0 ? 'Disponible' : 'Agotado',
        activo: true,
        imagenPrincipalUrl: item.imagenMiniaturaUrl ?? undefined,
        imagenes: [],
        totalImagenes: 0,
        variantes: [variante],
        totalVariantes: 1,
        usaVariantes: true,
        fechaCreacion: '',
        fechaActualizacion: ''
      });
    }
    this.productos.set(actuales.sort((a, b) => a.nombre.localeCompare(b.nombre)));
  }

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
        this.hidratarProductosReferenciados(c.detalles.map((detalle) => detalle.productoId));

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

  private hidratarProductosReferenciados(productoIds: number[]): void {
    [...new Set(productoIds)].forEach((productoId) => {
      this.productoService.getById(productoId).subscribe({
        next: (res) => {
          const actuales = this.productos().filter((producto) => producto.id !== productoId);
          this.productos.set([...actuales, res.data].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        },
        error: () => this.errorMessage.set('No se pudo cargar la información actual de uno de los productos del borrador.')
      });
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
