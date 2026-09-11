import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import {
  CategoriaTienda,
  EstadoConsultaPublica,
  ItemCarrito,
  ModeloTienda,
  OrdenCatalogo,
  ProductoCatalogoPublico,
  ProductoTienda,
  agregarItem,
  crearCatalogoEjemplo,
  filtrarProductos,
  mapearProducto,
  referenciasCarrito,
  restaurarCarrito,
  totalCarrito
} from './varistorehn.catalog';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn-productos',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn-productos.component.html',
  styleUrl: './varistorehn-productos.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnProductosComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly identidad = inject(EmpresaIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly permiteWhatsapp = this.config.modoCarrito !== 'tarjeta';

  readonly productos = signal<ProductoTienda[]>([]);
  readonly cargando = signal(true);
  readonly errorCatalogo = signal('');
  readonly estadoCatalogo = computed<EstadoConsultaPublica>(() => {
    if (this.cargando()) return 'loading';
    if (this.errorCatalogo()) return 'error';
    return this.productos().length ? 'success' : 'empty';
  });

  readonly categorias = signal<CategoriaTienda[]>([]);
  readonly cargandoCategorias = signal(true);
  readonly errorCategorias = signal('');
  readonly categoriasNavegacion = computed(() => this.categorias().map(categoria => categoria.nombre));

  readonly busqueda = signal('');
  readonly categoriaSlug = signal('');
  readonly categoriaActiva = signal('');
  readonly soloDisponibles = signal(false);
  readonly precioMaximo = signal<number | null>(null);
  readonly orden = signal<OrdenCatalogo>('destacados');
  readonly pagina = signal(1);
  readonly tamanoPagina = 12;
  readonly filtrosAbiertos = signal(false);
  readonly modelosActivos = signal<Record<number, string>>({});
  readonly imagenesFallidas = signal<Set<string>>(new Set());

  readonly carrito = signal<ItemCarrito[]>([]);
  readonly totalUnidades = computed(() => this.carrito().reduce((total, item) => total + item.unidades, 0));
  readonly subtotal = computed(() => totalCarrito(this.carrito()));
  readonly aviso = signal('');

  readonly resultados = computed(() => filtrarProductos(this.productos(), {
    busqueda: this.busqueda(),
    categoria: this.categoriaActiva(),
    soloDisponibles: this.soloDisponibles(),
    precioMaximo: this.precioMaximo(),
    orden: this.orden()
  }));
  readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.resultados().length / this.tamanoPagina)));
  readonly productosVisibles = computed(() => {
    const inicio = (this.pagina() - 1) * this.tamanoPagina;
    return this.resultados().slice(inicio, inicio + this.tamanoPagina);
  });
  readonly hayFiltros = computed(() => Boolean(
    this.busqueda().trim()
    || this.categoriaSlug()
    || this.soloDisponibles()
    || this.precioMaximo() !== null
  ));
  readonly categoriaFiltroInvalida = computed(() => Boolean(
    !this.cargandoCategorias()
    && !this.errorCategorias()
    && this.categoriaSlug()
    && !this.categoriaActiva()
  ));

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: VARISTOREHN_PATHS.productos,
    categorias: VARISTOREHN_PATHS.categorias,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  private cargaCatalogo?: Subscription;
  private cargaCategorias?: Subscription;

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.busqueda.set((params.get('q') || '').trim().slice(0, 180));
      this.categoriaSlug.set((params.get('categoria') || '').trim().slice(0, 180));
      this.pagina.set(1);
      this.aplicarCategoriaDesdeSlug();
    });

    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.recargar());
  }

  recargar(): void {
    this.cargarCatalogo();
    this.cargarCategoriasPublicas();
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.modelosActivos.set({});
    this.recargar();
  }

  actualizarBusqueda(texto: string): void {
    this.busqueda.set(texto.slice(0, 180));
    this.pagina.set(1);
  }

  buscar(): void {
    this.pagina.set(1);
    this.sincronizarUrl();
    this.irInicioCatalogo();
  }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) {
      this.categoriaActiva.set('');
      this.categoriaSlug.set('');
      this.pagina.set(1);
      this.sincronizarUrl();
      return;
    }

    const categoria = this.categorias().find(item => item.nombre === nombre);
    if (!categoria) {
      this.aviso.set('La categoría seleccionada ya no está disponible.');
      return;
    }
    this.seleccionarCategoriaPorSlug(categoria.slug);
  }

  seleccionarCategoriaPorSlug(slug: string): void {
    const categoria = this.categorias().find(item => item.slug === slug);
    if (!categoria) return;
    this.categoriaSlug.set(categoria.slug);
    this.categoriaActiva.set(categoria.nombre);
    this.pagina.set(1);
    this.sincronizarUrl();
  }

  cambiarDisponibilidad(valor: boolean): void {
    this.soloDisponibles.set(valor);
    this.pagina.set(1);
  }

  cambiarPrecio(valor: string): void {
    const numero = Number(valor);
    this.precioMaximo.set(valor.trim() && Number.isFinite(numero) && numero >= 0 ? numero : null);
    this.pagina.set(1);
  }

  cambiarOrden(valor: string): void {
    if (['destacados', 'precio-asc', 'precio-desc', 'nombre'].includes(valor)) {
      this.orden.set(valor as OrdenCatalogo);
    }
    this.pagina.set(1);
  }

  limpiarFiltros(): void {
    this.busqueda.set('');
    this.categoriaSlug.set('');
    this.categoriaActiva.set('');
    this.soloDisponibles.set(false);
    this.precioMaximo.set(null);
    this.orden.set('destacados');
    this.pagina.set(1);
    this.sincronizarUrl();
  }

  cambiarPagina(cambio: number): void {
    this.pagina.set(Math.max(1, Math.min(this.totalPaginas(), this.pagina() + cambio)));
    this.irInicioCatalogo();
  }

  abrirCarrito(): void {
    void this.router.navigate(['/varistorehn'], { queryParams: { carrito: '1' } });
  }

  modeloSeleccionado(producto: ProductoTienda): ModeloTienda {
    return producto.modelos.find(modelo => modelo.clave === this.modelosActivos()[producto.id])
      || producto.modelos.find(modelo => modelo.disponible)
      || producto.modelos[0];
  }

  seleccionarModelo(producto: ProductoTienda, clave: string): void {
    if (!producto.modelos.some(modelo => modelo.clave === clave)) return;
    this.modelosActivos.update(actual => ({ ...actual, [producto.id]: clave }));
  }

  disponibleParaAgregar(producto: ProductoTienda): boolean {
    const modelo = this.modeloSeleccionado(producto);
    const actual = this.carrito().find(item => item.productoId === producto.id && item.modeloClave === modelo.clave);
    return modelo.disponible && (actual?.unidades || 0) < modelo.stock;
  }

  agregar(producto: ProductoTienda): void {
    if (!this.disponibleParaAgregar(producto)) return;
    const items = agregarItem(this.carrito(), producto, this.modeloSeleccionado(producto));
    this.guardarCarrito(items);
    this.aviso.set(`${producto.nombre} se agregó al carrito.`);
  }

  imagenValida(url?: string): boolean {
    return Boolean(url && !this.imagenesFallidas().has(url));
  }

  errorImagen(url: string): void {
    this.imagenesFallidas.update(actual => new Set([...actual, url]));
  }

  stockBajo(modelo: ModeloTienda): boolean {
    return modelo.disponible && modelo.stock > 0 && modelo.stock <= 3;
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', {
        style: 'currency',
        currency: this.identidad.config().moneda || 'HNL'
      }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }

  cargarCatalogo(): void {
    this.cargaCatalogo?.unsubscribe();
    this.cargando.set(true);
    this.errorCatalogo.set('');
    this.productos.set([]);
    this.carrito.set([]);

    const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCatalogo()
      : of(null);

    this.cargaCatalogo = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: datos => {
        const productos = datos === null ? crearCatalogoEjemplo() : datos.map(mapearProducto);
        this.productos.set(productos);
        const guardado = this.leerReferenciasCarrito();
        const restaurado = restaurarCarrito(guardado, productos);
        this.guardarCarrito(restaurado, false);
        if (Array.isArray(guardado) && JSON.stringify(guardado) !== JSON.stringify(referenciasCarrito(restaurado))) {
          this.aviso.set('Actualizamos tu carrito con los modelos y existencias disponibles.');
        }
        this.cargando.set(false);
      },
      error: () => {
        this.errorCatalogo.set('No pudimos cargar el catálogo. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.cargando.set(false);
      }
    });
  }

  private cargarCategoriasPublicas(): void {
    this.cargaCategorias?.unsubscribe();
    this.cargandoCategorias.set(true);
    this.errorCategorias.set('');
    this.categorias.set([]);
    this.categoriaActiva.set('');

    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());

    this.cargaCategorias = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => {
        this.categorias.set(categorias);
        this.cargandoCategorias.set(false);
        this.aplicarCategoriaDesdeSlug();
      },
      error: () => {
        this.errorCategorias.set('No pudimos cargar las categorías públicas. Los demás filtros siguen disponibles.');
        this.cargandoCategorias.set(false);
      }
    });
  }

  private aplicarCategoriaDesdeSlug(): void {
    const slug = this.categoriaSlug();
    if (!slug) {
      this.categoriaActiva.set('');
      return;
    }
    const categoria = this.categorias().find(item => item.slug === slug);
    this.categoriaActiva.set(categoria?.nombre || '');
  }

  private sincronizarUrl(): void {
    const q = this.busqueda().trim();
    const categoria = this.categoriaSlug();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { q: q || null, categoria: categoria || null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private irInicioCatalogo(): void {
    queueMicrotask(() => this.document.getElementById('catalogo-productos')?.scrollIntoView({ block: 'start' }));
  }

  private claveCarrito(): string {
    return `varistorehn:carrito:v2:${this.identidad.config().id}:${this.utilizarDatosBaseDatos() ? 'bd' : 'demo'}`;
  }

  private leerReferenciasCarrito(): unknown {
    try {
      return JSON.parse(this.document.defaultView?.localStorage.getItem(this.claveCarrito()) || '[]');
    } catch {
      return [];
    }
  }

  private guardarCarrito(items: ItemCarrito[], anunciarError = true): void {
    this.carrito.set(items);
    try {
      this.document.defaultView?.localStorage.setItem(this.claveCarrito(), JSON.stringify(referenciasCarrito(items)));
    } catch {
      if (anunciarError) this.aviso.set('No pudimos guardar el carrito en este navegador.');
    }
  }
}
