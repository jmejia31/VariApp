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
import { CompraService } from '../../services/compra.service';
import { ProductoService } from '../../services/producto.service';
import { Producto } from '../../core/models/producto.model';

@Component({
  selector: 'app-compra-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './compra-form.component.html',
  styleUrl: './compra-form.component.scss'
})
export class CompraFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly productos = signal<Producto[]>([]);
  private compraId: number | null = null;

  form = this.fb.group({
    proveedorNombre: ['', Validators.required],
    proveedorTelefono: [''],
    proveedorDocumento: [''],
    documentoReferencia: [''],
    metodoPago: ['Efectivo', Validators.required],
    estadoPago: ['Pendiente', Validators.required],
    descuento: [0, [Validators.min(0)]],
    impuesto: [0, [Validators.min(0)]],
    notas: [''],
    detalles: this.fb.array([])
  });

  constructor(
    private compraService: CompraService,
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
      this.compraId = Number(idParam);
      this.cargarCompra(this.compraId);
    } else {
      this.agregarDetalle();
    }
  }

  private cargarCompra(id: number): void {
    this.loading.set(true);
    this.compraService.getById(id).subscribe({
      next: (res) => {
        const c = res.data;
        this.form.patchValue({
          proveedorNombre: c.proveedorNombre,
          proveedorTelefono: c.proveedorTelefono,
          proveedorDocumento: c.proveedorDocumento,
          documentoReferencia: c.documentoReferencia,
          metodoPago: c.metodoPago,
          estadoPago: c.estadoPago,
          descuento: c.descuento,
          impuesto: c.impuesto,
          notas: c.notas
        });
        c.detalles.forEach((d) => this.agregarDetalle(d.productoId, d.cantidad, d.costoUnitario));
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
  }

  subtotalDetalle(group: AbstractControl): number {
    const cantidad = group.value.cantidad || 0;
    const costo = group.value.costoUnitario || 0;
    return cantidad * costo;
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
