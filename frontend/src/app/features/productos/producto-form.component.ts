import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductoService } from '../../services/producto.service';
import { CategoriaService } from '../../services/categoria.service';
import { Categoria } from '../../core/models/categoria.model';
import { ProductoImagen } from '../../core/models/producto.model';

interface ImagenPreview {
  id?: number;       // presente si es una imagen ya existente en el servidor
  url: string;        // URL remota o local (blob) para previsualizar
  esPrincipal: boolean;
  archivo?: File;      // presente si es una imagen nueva aún no subida
}

const MAX_IMAGENES = 5;

@Component({
  selector: 'app-producto-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './producto-form.component.html',
  styleUrl: './producto-form.component.scss'
})
export class ProductoFormComponent implements OnInit {
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly categorias = signal<Categoria[]>([]);
  readonly imagenes = signal<ImagenPreview[]>([]);
  readonly maxImagenes = MAX_IMAGENES;
  readonly auditoria = signal<{ creadoPor?: string; actualizadoPor?: string } | null>(null);

  private readonly fb = inject(FormBuilder);
  private productoId: number | null = null;
  private imagenesAEliminarIds: number[] = [];

  form = this.fb.group({
    nombre: ['', Validators.required],
    marca: ['', Validators.required],
    modelo: ['', Validators.required],
    descripcion: [''],
    cantidad: [0, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0.01)]],
    precio: [0, [Validators.required, Validators.min(0.01)]],
    umbralStockBajo: [5, [Validators.required, Validators.min(0)]],
    categoriaId: [null as number | null]
  });

  constructor(
    private productoService: ProductoService,
    private categoriaService: CategoriaService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.categoriaService.getActivas().subscribe((res) => this.categorias.set(res.data));

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
          umbralStockBajo: p.umbralStockBajo,
          categoriaId: p.categoriaId ?? null
        });
        this.imagenes.set(
          (p.imagenes ?? []).map((img: ProductoImagen) => ({ id: img.id, url: img.url, esPrincipal: img.esPrincipal }))
        );
        this.auditoria.set({
          creadoPor: p.creadoPorNombreUsuario,
          actualizadoPor: p.actualizadoPorNombreUsuario
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  get espaciosDisponibles(): number {
    return this.maxImagenes - this.imagenes().length;
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (files.length === 0) return;

    const disponibles = this.espaciosDisponibles;
    if (files.length > disponibles) {
      this.errorMessage.set(`Solo puedes agregar ${disponibles} foto(s) más (máximo ${this.maxImagenes}).`);
    }

    const aAgregar = files.slice(0, disponibles);
    const nuevas: ImagenPreview[] = aAgregar.map((archivo) => ({
      url: URL.createObjectURL(archivo),
      esPrincipal: this.imagenes().length === 0,
      archivo
    }));

    this.imagenes.set([...this.imagenes(), ...nuevas]);
    input.value = '';
  }

  quitarImagen(index: number): void {
    const actuales = [...this.imagenes()];
    const [quitada] = actuales.splice(index, 1);

    if (quitada.id) this.imagenesAEliminarIds.push(quitada.id);

    // Si se quitó la principal, la primera restante pasa a ser principal
    if (quitada.esPrincipal && actuales.length > 0) actuales[0].esPrincipal = true;

    this.imagenes.set(actuales);
  }

  marcarComoPrincipal(index: number): void {
    const actuales = this.imagenes().map((img, i) => ({ ...img, esPrincipal: i === index }));
    this.imagenes.set(actuales);
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const imagenPrincipal = this.imagenes().find((img) => img.esPrincipal);

    const value = {
      nombre: this.form.value.nombre!,
      marca: this.form.value.marca!,
      modelo: this.form.value.modelo!,
      descripcion: this.form.value.descripcion || undefined,
      cantidad: this.form.value.cantidad!,
      costo: this.form.value.costo!,
      precio: this.form.value.precio!,
      umbralStockBajo: this.form.value.umbralStockBajo!,
      categoriaId: this.form.value.categoriaId,
      imagenesNuevas: this.imagenes().filter((img) => img.archivo).map((img) => img.archivo!),
      imagenesAEliminarIds: this.imagenesAEliminarIds,
      imagenPrincipalId: imagenPrincipal?.id ?? null
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
