import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormArray, FormBuilder, Validators } from '@angular/forms';
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
import { ProductoImagen, ProductoVariante, ProductoVarianteFormValue, TipoInventario } from '../../core/models/producto.model';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

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
    MatInputModule, MatSelectModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, ProductoImagenComponent
  ],
  templateUrl: './producto-form.component.html',
  styleUrls: ['./producto-form.component.scss', './producto-form-variants.component.scss']
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
  readonly TipoInventario = TipoInventario;
  readonly tiposInventario = [
    { value: TipoInventario.MercaderiaVenta, label: 'Mercadería para venta' },
    { value: TipoInventario.InsumoAdministrativo, label: 'Insumo administrativo' }
  ];

  productoId: number | null = null;
  private imagenesAEliminarIds: number[] = [];

  form = this.fb.group({
    nombre: ['', Validators.required],
    tipoInventario: [TipoInventario.MercaderiaVenta, Validators.required],
    marcaId: [null as number | null, Validators.required],
    modeloId: [null as number | null, Validators.required],
    tallaId: [null as number | null],
    descripcion: [''],
    categoriaId: [null as number | null],
    variantes: this.fb.array<any>([])
  });

  constructor(
    private productoService: ProductoService,
    private categoriaService: CategoriaService,
    private catalogoService: CatalogoProductoService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  get variantes(): FormArray { return this.form.controls.variantes; }
  get stockTotal(): number { return this.variantes.getRawValue().reduce((total, variante) => total + Number(variante.cantidad ?? 0), 0); }
  get umbralTotal(): number { return this.variantes.getRawValue().reduce((total, variante) => total + Number(variante.umbralStockBajo ?? 0), 0); }

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
          this.isEdit.set(true); this.productoId = Number(idParam); this.cargarProducto(this.productoId);
        } else { this.agregarVariante(); this.loading.set(false); }
      },
      error: () => { this.loading.set(false); this.errorMessage.set('No se pudieron cargar los catálogos necesarios para el producto.'); }
    });
    this.form.controls.marcaId.valueChanges.subscribe((marcaId) => {
      this.form.controls.modeloId.setValue(null, { emitEvent: false }); this.cargarModelos(marcaId);
    });
  }

  private crearVarianteGroup(variante?: Partial<ProductoVarianteFormValue>) {
    return this.fb.group({
      id: [variante?.id ?? null as number | null], colorId: [variante?.colorId ?? null as number | null, Validators.required],
      sku: [variante?.sku ?? '', Validators.maxLength(80)], codigoBarras: [variante?.codigoBarras ?? '', Validators.maxLength(120)],
      cantidad: [variante?.cantidad ?? 0, [Validators.required, Validators.min(0)]],
      umbralStockBajo: [variante?.umbralStockBajo ?? 5, [Validators.required, Validators.min(0)]],
      costo: [variante?.costo ?? 0, [Validators.required, Validators.min(0.01)]], precio: [variante?.precio ?? 0, [Validators.required, Validators.min(0.01)]],
      activo: [variante?.activo ?? true]
    });
  }

  agregarVariante(): void {
    const primera = this.variantes.length > 0 ? this.variantes.at(0).getRawValue() : null;
    this.variantes.push(this.crearVarianteGroup({ cantidad: 0, umbralStockBajo: primera?.umbralStockBajo ?? 5, costo: primera?.costo ?? 0, precio: primera?.precio ?? 0, activo: true }));
    this.errorMessage.set(null);
  }

  quitarVariante(index: number): void {
    if (this.variantes.length === 1) { this.errorMessage.set('El producto debe conservar al menos un color con su cantidad disponible.'); return; }
    const variante = this.variantes.at(index).getRawValue();
    if (variante.id && Number(variante.cantidad) > 0) { this.errorMessage.set('No puedes quitar un color existente mientras tenga unidades. Déjalo con cantidad 0 y vuelve a guardar antes de retirarlo.'); return; }
    this.variantes.removeAt(index); this.errorMessage.set(null);
  }

  colorYaSeleccionado(colorId: number, indiceActual: number): boolean {
    return this.variantes.controls.some((control, index) => index !== indiceActual && Number(control.get('colorId')?.value) === colorId);
  }

  private cargarProducto(id: number): void {
    this.productoService.getById(id).subscribe({
      next: (res) => {
        const p = res.data;
        const marcaId = p.marcaId ?? this.marcas().find(m => m.nombre.toLowerCase() === p.marca.toLowerCase())?.id ?? null;
        this.form.patchValue({ nombre: p.nombre, tipoInventario: p.tipoInventario ?? TipoInventario.MercaderiaVenta, marcaId, tallaId: p.tallaId ?? null, descripcion: p.descripcion, categoriaId: p.categoriaId ?? null }, { emitEvent: false });
        this.variantes.clear();
        if ((p.variantes ?? []).length > 0) {
          p.variantes.forEach((variante: ProductoVariante) => this.variantes.push(this.crearVarianteGroup({ id: variante.id, colorId: variante.colorId, sku: variante.sku, codigoBarras: variante.codigoBarras, cantidad: variante.cantidad, umbralStockBajo: variante.umbralStockBajo, costo: variante.costo, precio: variante.precio, activo: variante.activo })));
        } else {
          this.variantes.push(this.crearVarianteGroup({ colorId: p.colorId, cantidad: p.cantidad, umbralStockBajo: p.umbralStockBajo, costo: p.costo, precio: p.precio, activo: true }));
        }
        if (this.isEdit()) this.variantes.controls.forEach((control) => control.disable({ emitEvent: false }));
        this.cargarModelos(marcaId, () => {
          const modeloId = p.modeloId ?? this.modelos().find(m => m.nombre.toLowerCase() === p.modelo.toLowerCase())?.id ?? null;
          this.form.controls.modeloId.setValue(modeloId, { emitEvent: false });
        });
        this.imagenes.set((p.imagenes ?? []).map((img: ProductoImagen) => ({ id: img.id, url: img.url, esPrincipal: img.esPrincipal })));
        this.auditoria.set({ creadoPor: p.creadoPorNombreUsuario, actualizadoPor: p.actualizadoPorNombreUsuario }); this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.errorMessage.set('No se pudo cargar el producto.'); }
    });
  }

  private cargarModelos(marcaId: number | null, alFinalizar?: () => void): void {
    if (!marcaId) { this.modelos.set([]); alFinalizar?.(); return; }
    this.cargandoModelos.set(true);
    this.catalogoService.getActivos('Modelo', marcaId).subscribe({
      next: (res) => { this.modelos.set(res.data); this.cargandoModelos.set(false); alFinalizar?.(); },
      error: () => { this.modelos.set([]); this.cargandoModelos.set(false); this.errorMessage.set('No se pudieron cargar los modelos de la marca seleccionada.'); alFinalizar?.(); }
    });
  }

  get espaciosDisponibles(): number { return this.maxImagenes - this.imagenes().length; }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement; const files = Array.from(input.files ?? []); if (files.length === 0) return;
    const disponibles = this.espaciosDisponibles;
    if (files.length > disponibles) this.errorMessage.set(`Solo puedes agregar ${disponibles} foto(s) más (máximo ${this.maxImagenes}).`);
    const nuevas: ImagenPreview[] = files.slice(0, disponibles).map((archivo) => ({ url: URL.createObjectURL(archivo), esPrincipal: this.imagenes().length === 0, archivo }));
    this.imagenes.set([...this.imagenes(), ...nuevas]); input.value = '';
  }

  quitarImagen(index: number): void {
    const actuales = [...this.imagenes()]; const [quitada] = actuales.splice(index, 1);
    if (quitada.id) this.imagenesAEliminarIds.push(quitada.id); if (quitada.archivo) URL.revokeObjectURL(quitada.url);
    if (quitada.esPrincipal && actuales.length > 0) actuales[0].esPrincipal = true; this.imagenes.set(actuales);
  }
  marcarComoPrincipal(index: number): void { this.imagenes.set(this.imagenes().map((img, i) => ({ ...img, esPrincipal: i === index }))); }

  submit(): void {
    if (this.form.invalid || this.variantes.length === 0) { this.form.markAllAsTouched(); this.errorMessage.set('Completa los datos obligatorios y agrega al menos un color con su cantidad.'); return; }
    const variantes = this.variantes.getRawValue().map((variante) => ({ id: variante.id ?? undefined, colorId: Number(variante.colorId), sku: variante.sku?.trim() || undefined, codigoBarras: variante.codigoBarras?.trim() || undefined, cantidad: Number(variante.cantidad), umbralStockBajo: Number(variante.umbralStockBajo), costo: Number(variante.costo), precio: Number(variante.precio), activo: variante.activo !== false }));
    const colores = variantes.map((variante) => variante.colorId);
    if (new Set(colores).size !== colores.length) { this.errorMessage.set('No puedes registrar el mismo color más de una vez para el producto.'); return; }
    const marca = this.marcas().find(item => item.id === this.form.value.marcaId); const modelo = this.modelos().find(item => item.id === this.form.value.modeloId);
    if (!marca || !modelo) { this.errorMessage.set('Selecciona una marca y un modelo válidos.'); return; }
    const total = variantes.reduce((suma, variante) => suma + variante.cantidad, 0);
    const costo = total > 0 ? variantes.reduce((suma, variante) => suma + variante.costo * variante.cantidad, 0) / total : variantes[0].costo;
    const preciosActivos = variantes.filter((variante) => variante.activo).map((variante) => variante.precio);
    const precio = preciosActivos.length > 0 ? Math.min(...preciosActivos) : variantes[0].precio;
    const imagenPrincipal = this.imagenes().find((img) => img.esPrincipal);
    this.saving.set(true); this.errorMessage.set(null);
    const value = {
      nombre: this.form.value.nombre!, tipoInventario: Number(this.form.value.tipoInventario) as TipoInventario,
      marca: marca.nombre, modelo: modelo.nombre, marcaId: marca.id, modeloId: modelo.id,
      colorId: variantes.length === 1 ? variantes[0].colorId : null, tallaId: this.form.value.tallaId,
      descripcion: this.form.value.descripcion || undefined, cantidad: total, costo: Math.round(costo * 100) / 100, precio,
      umbralStockBajo: variantes.reduce((suma, variante) => suma + variante.umbralStockBajo, 0), categoriaId: this.form.value.categoriaId,
      variantes, imagenesNuevas: this.imagenes().filter((img) => img.archivo).map((img) => img.archivo!), imagenesAEliminarIds: this.imagenesAEliminarIds,
      imagenPrincipalId: imagenPrincipal?.id ?? null
    };
    const request$ = this.isEdit() ? this.productoService.update(this.productoId!, value) : this.productoService.create(value);
    request$.subscribe({ next: () => { this.saving.set(false); this.router.navigate(['/productos']); }, error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el producto y sus colores.'); } });
  }
}
