import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, AbstractControl, FormBuilder, FormArray, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { VentaService } from '../../services/venta.service';
import { ProductoService } from '../../services/producto.service';
import { Producto } from '../../core/models/producto.model';

@Component({
  selector: 'app-venta-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './venta-form.component.html',
  styleUrl: './venta-form.component.scss'
})
export class VentaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly productos = signal<Producto[]>([]);
  private ventaId: number | null = null;

  form = this.fb.group({
    clienteNombre: ['Cliente final', Validators.required],
    clienteTelefono: [''],
    clienteIdentidadORTN: [''],
    clienteCorreo: [''],
    clienteDireccion: [''],
    metodoPago: ['Efectivo', Validators.required],
    estadoPago: ['Pendiente', Validators.required],
    descuento: [0, [Validators.min(0)]],
    impuesto: [0, [Validators.min(0)]],
    notas: [''],
    detalles: this.fb.array([])
  });

  constructor(
    private ventaService: VentaService,
    private productoService: ProductoService,
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
          descuento: v.descuento,
          impuesto: v.impuesto,
          notas: v.notas
        });
        v.detalles.forEach((d) => this.agregarDetalle(d.productoId, d.cantidad, d.precioUnitario));
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
  }

  subtotalDetalle(group: AbstractControl): number {
    const cantidad = group.value.cantidad || 0;
    const precio = group.value.precioUnitario || 0;
    return cantidad * precio;
  }

  get subtotalGeneral(): number {
    return this.detalles.controls.reduce((acc, g) => acc + this.subtotalDetalle(g), 0);
  }

  get totalGeneral(): number {
    const descuento = this.form.value.descuento || 0;
    const impuesto = this.form.value.impuesto || 0;
    return this.subtotalGeneral - descuento + impuesto;
  }

  submit(): void {
    if (this.form.invalid || this.detalles.length === 0) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue() as any;

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
