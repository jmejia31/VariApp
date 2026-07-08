import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductoService } from '../../services/producto.service';

@Component({
  selector: 'app-producto-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './producto-form.component.html',
  styleUrl: './producto-form.component.scss'
})
export class ProductoFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productoService = inject(ProductoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly previewUrl = signal<string | null>(null);
  readonly isEdit = signal(false);

  private productoId: number | null = null;
  private imagenSeleccionada: File | null = null;
  private eliminarImagen = false;

  form = this.fb.group({
    nombre: ['', Validators.required],
    marca: ['', Validators.required],
    modelo: ['', Validators.required],
    descripcion: [''],
    cantidad: [0, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0.01)]],
    precio: [0, [Validators.required, Validators.min(0.01)]],
    umbralStockBajo: [5, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.productoId = Number(idParam);
      this.cargarProducto(this.productoId);
    }
  }

  private cargarProducto(id: number): void {
    this.loading.set(true);
    this.productoService.getById(id).subscribe({
      next: (res) => {
        const p = res.data;
        this.form.patchValue({
          nombre: p.nombre,
          marca: p.marca,
          modelo: p.modelo,
          descripcion: p.descripcion,
          cantidad: p.cantidad,
          costo: p.costo,
          precio: p.precio,
          umbralStockBajo: p.umbralStockBajo
        });
        if (p.imagenUrl) this.previewUrl.set(p.imagenUrl);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.imagenSeleccionada = file;
    this.eliminarImagen = false;

    const reader = new FileReader();
    reader.onload = () => this.previewUrl.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  quitarImagen(): void {
    this.previewUrl.set(null);
    this.imagenSeleccionada = null;
    this.eliminarImagen = true;
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = {
      ...this.form.getRawValue(),
      nombre: this.form.value.nombre!,
      marca: this.form.value.marca!,
      modelo: this.form.value.modelo!,
      descripcion: this.form.value.descripcion || undefined,
      cantidad: this.form.value.cantidad!,
      costo: this.form.value.costo!,
      precio: this.form.value.precio!,
      umbralStockBajo: this.form.value.umbralStockBajo!,
      imagen: this.imagenSeleccionada,
      eliminarImagen: this.eliminarImagen
    };

    const request$ = this.isEdit()
      ? this.productoService.update(this.productoId!, value)
      : this.productoService.create(value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/productos']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el producto.');
      }
    });
  }
}
