import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductoService } from '../../services/producto.service';
import { CategoriaService } from '../../services/categoria.service';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { Categoria } from '../../core/models/categoria.model';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { ProductoImagen } from '../../core/models/producto.model';

interface ImagenPreview {
  id?: number;
  url: string;
  esPrincipal: boolean;
  archivo?: File;
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
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly categorias = signal<Categoria[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly tallas = signal<CatalogoProducto[]>([]);
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly modelos = signal<CatalogoProducto[]>([]);
  readonly cargandoModelos = signal(false);
  readonly imagenes = signal<ImagenPreview[]>([]);
  readonly maxImagenes = MAX_IMAGENES;
  readonly auditoria = signal<{ creadoPor?: string; actualizadoPor?: string } | null>(null);

  private productoId: number | null = null;
  private imagenesAEliminarIds: number[] = [];

  form = this.fb.group({
    nombre: ['', Validators.required],
    marcaId: [null as number | null, Validators.required],
    modeloId: [null as number | null, Validators.required],
    colorId: [null as number | null],
    tallaId: [null as number | null],
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
    private catalogoService: CatalogoProductoService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      categorias: this.categoriaService.getActivas(),
      colores: this.catalogoService.getActivos('Color'),
      tallas: this.catalogoService.getActivos('Talla'),
      marcas: this.catalogoService.getActivos('Marca')
    }).subscribe({
      next: (res) => {
        this.categorias.set(res.categorias.data);
        this.colores.set(res.colores.data);
        this.tallas.set(res.tallas.data);
        this.marcas.set(res.marcas.data);

        const idParam = this.route.snapshot.paramMap.get('id');
        if (idParam) {
          this.isEdit.set(true);
          this.productoId = Number(idParam);
          this.cargarProducto(this.productoId);
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar los catálogos necesarios para el producto.');
      }
    });

    this.form.controls.marcaId.valueChanges.subscribe((marcaId) => {
      this.form.controls.modeloId.setValue(null, { emitEvent: false });
      this.cargarModelos(marcaId);
    });
  }

  private cargarProducto(id: number): void {
    this.productoService.getById(id).subscribe({
      next: (res) => {
        const p = res.data;
        const marcaId = p.marcaId ?? this.marcas().find(m => m.nombre.toLowerCase() === p.marca.toLowerCase())?.id ?? null;

        this.form.patchValue({
          nombre: p.nombre,
          marcaId,
          colorId: p.colorId ?? null,
          tallaId: p.tallaId ?? null,
          descripcion: p.descripcion,
          cantidad: p.cantidad,
          costo: p.costo,
          precio: p.precio,
          umbralStockBajo: p.umbralStockBajo,
          categoriaId: p.categoriaId ?? null
        }, { emitEvent: false });

        this.cargarModelos(marcaId, () => {
          const modeloId = p.modeloId ?? this.modelos().find(m => m.nombre.toLowerCase() === p.modelo.toLowerCase())?.id ?? null;
          this.form.controls.modeloId.setValue(modeloId, { emitEvent: false });
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
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar el producto.');
      }
    });
  }

  private cargarModelos(marcaId: number | null, alFinalizar?: () => void): void {
    if (!marcaId) {
      this.modelos.set([]);
      alFinalizar?.();
      return;
    }

    this.cargandoModelos.set(true);
    this.catalogoService.getActivos('Modelo', marcaId).subscribe({
      next: (res) => {
        this.modelos.set(res.data);
        this.cargandoModelos.set(false);
        alFinalizar?.();
      },
      error: () => {
        this.modelos.set([]);
        this.cargandoModelos.set(false);
        this.errorMessage.set('No se pudieron cargar los modelos de la marca seleccionada.');
        alFinalizar?.();
      }
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
    if (quitada.archivo) URL.revokeObjectURL(quitada.url);
    if (quitada.esPrincipal && actuales.length > 0) actuales[0].esPrincipal = true;
    this.imagenes.set(actuales);
  }

  marcarComoPrincipal(index: number): void {
    const actuales = this.imagenes().map((img, i) => ({ ...img, esPrincipal: i === index }));
    this.imagenes.set(actuales);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    const imagenPrincipal = this.imagenes().find((img) => img.esPrincipal);
    const marca = this.marcas().find(item => item.id === this.form.value.marcaId);
    const modelo = this.modelos().find(item => item.id === this.form.value.modeloId);

    if (!marca || !modelo) {
      this.saving.set(false);
      this.errorMessage.set('Selecciona una marca y un modelo válidos.');
      return;
    }

    const value = {
      nombre: this.form.value.nombre!,
      marca: marca.nombre,
      modelo: modelo.nombre,
      marcaId: marca.id,
      modeloId: modelo.id,
      colorId: this.form.value.colorId,
      tallaId: this.form.value.tallaId,
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
