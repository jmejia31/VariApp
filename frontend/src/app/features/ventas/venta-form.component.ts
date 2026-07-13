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
import { VentaService } from '../../services/venta.service';
import { ProductoService } from '../../services/producto.service';
import { ClienteService } from '../../services/cliente.service';
import { Producto } from '../../core/models/producto.model';
import { Cliente } from '../../core/models/cliente.model';
import { ResultadoCalculo } from '../../core/models/venta.model';

@Component({
  selector: 'app-venta-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './venta-form.component.html',
  styleUrl: './venta-form.component.scss'
})
export class VentaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly calculando = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly resultado = signal<ResultadoCalculo | null>(null);
  private ventaId: number | null = null;

  // --- Autocompletado remoto de clientes (sección 16) ---
  readonly buscadorCliente = new FormControl('');
  readonly opcionesCliente = signal<Cliente[]>([]);
  readonly buscandoCliente = signal(false);
  readonly clienteSeleccionado = signal<Cliente | null>(null);
  readonly errorBusquedaCliente = signal<string | null>(null);
  private clienteId: number | null = null;

  form = this.fb.group({
    clienteNombre: ['Cliente final', Validators.required],
    clienteTelefono: [''],
    clienteIdentidadORTN: [''],
    clienteCorreo: [''],
    clienteDireccion: [''],
    metodoPago: ['Efectivo', Validators.required],
    estadoPago: ['Pendiente', Validators.required],
    codigoPromocional: [''],
    notas: [''],
    detalles: this.fb.array([])
  });

  constructor(
    private ventaService: VentaService,
    private productoService: ProductoService,
    private clienteService: ClienteService,
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
      this.ventaId = Number(idParam);
      this.cargarVenta(this.ventaId);
    } else {
      this.agregarDetalle();
    }

    // Búsqueda remota de clientes: debounce + cancelación de la solicitud
    // anterior (switchMap) cada vez que el usuario escribe. No se carga
    // nunca la lista completa de clientes en memoria.
    this.buscadorCliente.valueChanges.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      switchMap((termino) => {
        // Si el usuario edita el texto después de haber seleccionado un
        // cliente, se vuelve a modo "cliente nuevo" (limpieza de selección).
        if (this.clienteSeleccionado() && termino !== this.clienteSeleccionado()!.nombre) {
          this.clienteSeleccionado.set(null);
          this.clienteId = null;
        }
        if (!termino || termino.trim().length < 2) {
          this.opcionesCliente.set([]);
          return of(null);
        }
        this.buscandoCliente.set(true);
        this.errorBusquedaCliente.set(null);
        return this.clienteService.buscar(termino).pipe(
          catchError(() => {
            this.errorBusquedaCliente.set('No se pudo buscar clientes. Intenta de nuevo.');
            return of(null);
          })
        );
      })
    ).subscribe((res) => {
      this.buscandoCliente.set(false);
      if (res) this.opcionesCliente.set(res.data);
    });

    // Recalcula el desglose real (backend) cada vez que cambian los productos,
    // cantidades, precios o el código promocional — con debounce para no
    // saturar la API mientras el usuario escribe.
    this.form.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    ).subscribe(() => this.recalcular());
  }

  onClienteSeleccionado(event: MatAutocompleteSelectedEvent): void {
    const cliente: Cliente = event.option.value;
    this.clienteSeleccionado.set(cliente);
    this.clienteId = cliente.id;
    this.buscadorCliente.setValue(cliente.nombre, { emitEvent: false });
    this.form.patchValue({
      clienteNombre: cliente.nombre,
      clienteTelefono: cliente.telefono,
      clienteIdentidadORTN: cliente.identidadORTN,
      clienteCorreo: cliente.correo,
      clienteDireccion: cliente.direccion
    });
  }

  limpiarClienteSeleccionado(): void {
    this.clienteSeleccionado.set(null);
    this.clienteId = null;
    this.buscadorCliente.setValue('');
    this.form.patchValue({
      clienteNombre: 'Cliente final', clienteTelefono: '', clienteIdentidadORTN: '',
      clienteCorreo: '', clienteDireccion: ''
    });
  }

  displayCliente(cliente: Cliente): string {
    return cliente?.nombre ?? '';
  }

  private cargarVenta(id: number): void {
    this.loading.set(true);
    this.ventaService.getById(id).subscribe({
      next: (res) => {
        const v = res.data;
        this.form.patchValue({
          clienteNombre: v.clienteNombre,
          clienteTelefono: v.clienteTelefono,
          clienteIdentidadORTN: v.clienteIdentidadORTN,
          clienteCorreo: v.clienteCorreo,
          clienteDireccion: v.clienteDireccion,
          metodoPago: v.metodoPago,
          estadoPago: v.estadoPago,
          notas: v.notas
        });
        this.buscadorCliente.setValue(v.clienteNombre, { emitEvent: false });
        v.detalles.forEach((d) => this.agregarDetalle(d.productoId, d.cantidad, d.precioUnitario));
        this.resultado.set({
          subtotal: v.subtotal,
          descuentosAplicados: v.descuentosAplicados,
          totalDescuento: v.descuento,
          impuestosAplicados: v.impuestosAplicados,
          totalImpuesto: v.impuesto,
          total: v.total
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  agregarDetalle(productoId: number | null = null, cantidad = 1, precioUnitario = 0): void {
    this.detalles.push(this.fb.group({
      productoId: [productoId, Validators.required],
      cantidad: [cantidad, [Validators.required, Validators.min(1)]],
      precioUnitario: [precioUnitario, [Validators.required, Validators.min(0.01)]]
    }));
  }

  onProductoSeleccionado(index: number, productoId: number): void {
    // Autocompletar precio sugerido con el precio de venta del producto
    const producto = this.productos().find((p) => p.id === productoId);
    if (producto) {
      this.detalles.at(index).patchValue({ precioUnitario: producto.precio });
    }
  }

  quitarDetalle(index: number): void {
    this.detalles.removeAt(index);
    this.recalcular();
  }

  subtotalDetalle(group: AbstractControl): number {
    const cantidad = group.value.cantidad || 0;
    const precio = group.value.precioUnitario || 0;
    return cantidad * precio;
  }

  /** Llama al backend para obtener el desglose REAL (descuentos/impuestos
   * desde el catálogo). Nunca se calcula en el cliente. */
  recalcular(): void {
    const detallesValidos = this.detalles.controls
      .map((g) => g.value)
      .filter((d) => d.productoId && d.cantidad > 0 && d.precioUnitario >= 0);

    if (detallesValidos.length === 0) {
      this.resultado.set(null);
      return;
    }

    this.calculando.set(true);
    const codigo = this.form.value.codigoPromocional || null;
    this.ventaService.calcular(this.clienteId, codigo, detallesValidos).subscribe({
      next: (res) => { this.resultado.set(res.data); this.calculando.set(false); },
      error: () => { this.calculando.set(false); }
    });
  }

  submit(): void {
    if (this.form.invalid || this.detalles.length === 0) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    const value = {
      ...raw,
      // Descuento/Impuesto ya no se editan manualmente: el backend siempre
      // recalcula desde el catálogo real (sección 13). Se envían en 0 solo
      // por compatibilidad del contrato del DTO.
      descuento: 0,
      impuesto: 0
    } as any;

    const request$ = this.isEdit()
      ? this.ventaService.update(this.ventaId!, value)
      : this.ventaService.create(value);

    request$.subscribe({
      next: (res) => {
        this.saving.set(false);
        this.router.navigate(['/ventas', res.data.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la venta.');
      }
    });
  }
}
