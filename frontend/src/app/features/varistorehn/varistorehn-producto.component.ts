import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import {
  CategoriaTienda,
  ModeloTienda,
  ProductoCatalogoPublico,
  ProductoTienda,
  crearCatalogoEjemplo,
  mapearProducto,
  precioVenta,
  telefonoWhatsapp
} from './varistorehn.catalog';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

type EstadoProductoPublico = 'loading' | 'error' | 'not-found' | 'success';
interface CaracteristicaPublica { etiqueta: string; valor: string; }

@Component({
  selector: 'app-varistorehn-producto',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn-producto.component.html',
  styleUrl: './varistorehn-producto.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnProductoComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);

  readonly identidad = inject(EmpresaIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carritoStore = inject(VaristorehnCarritoService);

  @ViewChild('lightbox') private lightbox?: ElementRef<HTMLDialogElement>;
  @ViewChild('botonImagenPrincipal') private botonImagenPrincipal?: ElementRef<HTMLButtonElement>;

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly busqueda = signal('');
  readonly slugSolicitado = signal('');
  readonly producto = signal<ProductoTienda | null>(null);
  readonly estado = signal<EstadoProductoPublico>('loading');
  readonly error = signal('');
  readonly aviso = signal('');
  readonly vistaWhatsapp = signal('');
  readonly categorias = signal<CategoriaTienda[]>([]);
  readonly catalogoContexto = signal<ProductoTienda[]>([]);
  readonly contextoCarritoCargado = this.carritoStore.listo;
  readonly carrito = this.carritoStore.items;
  readonly modeloClave = signal('');
  readonly cantidad = signal(0);
  readonly imagenActiva = signal(0);
  readonly imagenesFallidas = signal<Set<string>>(new Set());
  readonly lightboxAbierto = signal(false);

  readonly telefono = computed(() => telefonoWhatsapp(this.identidad.config().whatsApp));
  readonly permiteWhatsapp = computed(() => this.config.modoCarrito !== 'tarjeta' && Boolean(this.telefono()));
  readonly categoriasNavegacion = computed(() => this.categorias().map(categoria => categoria.nombre));
  readonly categoriaProducto = computed(() => {
    const producto = this.producto();
    if (!producto) return null;
    return this.categorias().find(categoria =>
      (producto.categoriaId !== null && categoria.id === producto.categoriaId) || categoria.nombre === producto.categoria
    ) || null;
  });

  readonly modeloSeleccionado = computed<ModeloTienda | null>(() => {
    const producto = this.producto();
    if (!producto?.modelos.length) return null;
    return producto.modelos.find(modelo => modelo.clave === this.modeloClave())
      || producto.modelos.find(modelo => modelo.disponible)
      || producto.modelos[0];
  });
  readonly imagenes = computed(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    if (!producto) return [];
    return [...new Set([...(modelo?.imagenes || []), ...producto.imagenes].map(url => url.trim()).filter(Boolean))];
  });
  readonly imagenActual = computed(() => this.imagenes()[this.imagenActiva()] || '');
  readonly skuVisible = computed(() => this.modeloSeleccionado()?.sku || this.producto()?.sku || '');
  readonly precioNormal = computed(() => this.modeloSeleccionado()?.precio ?? this.producto()?.precio ?? 0);
  readonly precioActual = computed(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    return producto && modelo ? precioVenta(producto, modelo) : 0;
  });
  readonly tienePromocion = computed(() => this.precioActual() < this.precioNormal());
  readonly stockSeleccionado = computed(() => this.modeloSeleccionado()?.stock ?? 0);
  readonly puedeSeleccionarCantidad = computed(() => Boolean(this.modeloSeleccionado()?.disponible && this.stockRestante() > 0));
  readonly unidadesEnCarrito = computed(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    return producto && modelo ? this.carritoStore.unidadesDe(producto.id, modelo.clave) : 0;
  });
  readonly stockRestante = computed(() => Math.max(0, this.stockSeleccionado() - this.unidadesEnCarrito()));
  readonly puedeAgregar = computed(() => Boolean(
    this.estado() === 'success'
    && this.contextoCarritoCargado()
    && this.producto()?.activo
    && this.modeloSeleccionado()?.disponible
    && this.cantidad() > 0
    && this.cantidad() <= this.stockRestante()
  ));
  readonly totalUnidades = computed<number | null>(() => this.carritoStore.listo() ? this.carritoStore.totalUnidades() : null);
  readonly subtotal = computed<number | null>(() => this.carritoStore.listo() ? this.carritoStore.subtotal() : null);

  readonly caracteristicas = computed<CaracteristicaPublica[]>(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    if (!producto || !modelo) return [];
    return [
      { etiqueta: 'Marca', valor: modelo.marca || producto.marca },
      { etiqueta: 'Modelo', valor: modelo.nombre !== 'Modelo general' ? modelo.nombre : '' },
      { etiqueta: 'SKU', valor: modelo.sku || producto.sku },
      { etiqueta: 'Categoría', valor: producto.categoria }
    ].filter(item => item.valor.trim());
  });
  readonly relacionados = computed(() => {
    const actual = this.producto();
    if (!actual) return [];
    return this.catalogoContexto()
      .filter(producto => producto.activo && producto.id !== actual.id
        && (actual.categoriaId !== null ? producto.categoriaId === actual.categoriaId : producto.categoria === actual.categoria))
      .slice(0, 4);
  });

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: VARISTOREHN_PATHS.productos,
    categorias: VARISTOREHN_PATHS.categorias,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  private cargaProducto?: Subscription;
  private cargaCatalogo?: Subscription;
  private cargaCategorias?: Subscription;
  private identidadLista = false;
  private inicioSwipe: { x: number; y: number } | null = null;
  private suprimirClickImagen = false;
  private restaurarFocoLightbox = true;

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.slugSolicitado.set((params.get('slug') || '').trim().slice(0, 180));
      if (this.identidadLista) this.cargarProducto();
    });
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.identidadLista = true;
      this.cargarProducto();
      this.cargarContextoCatalogo();
      this.cargarCategorias();
    });
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.carritoStore.reiniciarContexto();
    this.cantidad.set(0); this.imagenActiva.set(0); this.vistaWhatsapp.set('');
    this.cargarProducto(); this.cargarContextoCatalogo(); this.cargarCategorias();
  }
  recargar(): void { this.cargarProducto(); this.cargarContextoCatalogo(); this.cargarCategorias(); }
  actualizarBusqueda(texto: string): void { this.busqueda.set(texto.slice(0, 180)); }
  buscar(): void {
    const q = this.busqueda().trim();
    void this.router.navigate(['/varistorehn/productos'], { queryParams: q ? { q } : {} });
  }
  seleccionarCategoria(nombre: string): void {
    if (!nombre) { void this.router.navigate(['/varistorehn/productos']); return; }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    void this.router.navigate(['/varistorehn/productos'], { queryParams: categoria ? { categoria: categoria.slug } : {} });
  }
  abrirCarrito(): void { void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito); }

  seleccionarModelo(clave: string): void {
    const producto = this.producto();
    if (!producto?.modelos.some(modelo => modelo.clave === clave)) return;
    this.modeloClave.set(clave); this.imagenActiva.set(0); this.cerrarLightbox(false); this.reiniciarCantidad();
  }
  cambiarCantidad(cambio: number): void {
    if (!Number.isInteger(cambio) || !this.puedeSeleccionarCantidad()) return;
    this.cantidad.set(Math.max(1, Math.min(this.stockRestante(), this.cantidad() + cambio)));
  }
  establecerCantidad(valor: string): void {
    if (!this.puedeSeleccionarCantidad()) { this.cantidad.set(0); return; }
    const numero = Number(valor);
    const cantidad = Number.isFinite(numero) ? Math.floor(numero) : 1;
    this.cantidad.set(Math.max(1, Math.min(this.stockRestante(), cantidad)));
  }
  agregarAlCarrito(): void {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    if (!producto || !modelo || !this.puedeAgregar()) return;
    const agregadas = this.carritoStore.agregar(producto, modelo, this.cantidad());
    if (!agregadas) return;
    this.reiniciarCantidad();
    this.aviso.set(`${agregadas} ${agregadas === 1 ? 'unidad agregada' : 'unidades agregadas'} de ${producto.nombre}.`);
  }
  comprarWhatsapp(): void {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    const telefono = this.telefono();
    if (!producto || !modelo || !telefono || !this.permiteWhatsapp() || !modelo.disponible || modelo.stock <= 0) return;
    const unidades = Math.max(1, Math.min(this.cantidad() || 1, modelo.stock));
    const subtotal = this.precioActual() * unidades;
    const sku = this.skuVisible() ? `\nSKU: ${this.skuVisible()}` : '';
    const mensaje = `Hola ${this.identidad.config().nombreComercial}, deseo consultar este producto:\n\n${producto.nombre}\nModelo: ${modelo.nombre}${sku}\nCantidad: ${unidades}\nPrecio unitario: ${this.moneda(this.precioActual())}\nSubtotal: ${this.moneda(subtotal)}\n\nPor favor confirmar disponibilidad y total final.`;
    if (!this.utilizarDatosBaseDatos()) { this.vistaWhatsapp.set(`VISTA PREVIA — NO ENVIADO\n\n${mensaje}`); return; }
    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;
    if (url.length > 7500) { this.aviso.set('El mensaje es demasiado largo para abrirse de forma segura en WhatsApp.'); return; }
    this.document.defaultView?.open(url, '_blank', 'noopener,noreferrer');
  }

  seleccionarImagen(indice: number): void {
    if (Number.isInteger(indice) && indice >= 0 && indice < this.imagenes().length) this.imagenActiva.set(indice);
  }
  moverImagen(cambio: number): void {
    const total = this.imagenes().length;
    if (total <= 1 || !Number.isInteger(cambio)) return;
    this.imagenActiva.set((this.imagenActiva() + cambio + total) % total);
  }
  iniciarSwipe(evento: PointerEvent): void {
    if (evento.pointerType !== 'mouse') this.inicioSwipe = { x: evento.clientX, y: evento.clientY };
  }
  finalizarSwipe(evento: PointerEvent): void {
    if (!this.inicioSwipe || evento.pointerType === 'mouse') return;
    const deltaX = evento.clientX - this.inicioSwipe.x;
    const deltaY = evento.clientY - this.inicioSwipe.y;
    this.inicioSwipe = null;
    if (Math.abs(deltaX) < 48 || Math.abs(deltaX) <= Math.abs(deltaY) * 1.2) return;
    this.moverImagen(deltaX < 0 ? 1 : -1);
    this.suprimirClickImagen = true;
    this.document.defaultView?.setTimeout(() => { this.suprimirClickImagen = false; }, 0);
  }
  cancelarSwipe(): void { this.inicioSwipe = null; }
  abrirLightbox(): void {
    if (this.suprimirClickImagen || !this.imagenValida(this.imagenActual())) return;
    const dialogo = this.lightbox?.nativeElement;
    if (!dialogo || dialogo.open) return;
    this.restaurarFocoLightbox = true; dialogo.showModal(); this.lightboxAbierto.set(true);
  }
  cerrarLightbox(devolverFoco = true): void {
    const dialogo = this.lightbox?.nativeElement;
    this.restaurarFocoLightbox = devolverFoco;
    if (dialogo?.open) dialogo.close();
    else {
      this.lightboxAbierto.set(false);
      if (devolverFoco) queueMicrotask(() => this.botonImagenPrincipal?.nativeElement.focus());
      this.restaurarFocoLightbox = true;
    }
  }
  cerrarLightboxDesdeFondo(evento: MouseEvent): void {
    if (evento.target === this.lightbox?.nativeElement) this.cerrarLightbox(true);
  }
  alCerrarLightbox(): void {
    const devolverFoco = this.restaurarFocoLightbox;
    this.restaurarFocoLightbox = true; this.lightboxAbierto.set(false);
    if (devolverFoco) queueMicrotask(() => this.botonImagenPrincipal?.nativeElement.focus());
  }
  imagenValida(url?: string): boolean { return Boolean(url && !this.imagenesFallidas().has(url)); }
  errorImagen(url: string): void {
    this.imagenesFallidas.update(actual => new Set([...actual, url]));
    if (url === this.imagenActual() && this.lightboxAbierto()) this.cerrarLightbox(false);
  }
  stockBajo(): boolean { const stock = this.stockSeleccionado(); return stock > 0 && stock <= 3; }
  textoDisponibilidad(): string {
    const modelo = this.modeloSeleccionado();
    if (!modelo?.disponible) return 'Agotado';
    if (this.stockBajo()) return `Últimas ${modelo.stock} unidades`;
    return `${modelo.stock} ${modelo.stock === 1 ? 'unidad disponible' : 'unidades disponibles'}`;
  }
  rutaProducto(producto: ProductoTienda): string { return producto.slug ? VARISTOREHN_PATHS.producto(producto.slug) : VARISTOREHN_PATHS.productos; }
  rutaCategoria(): string {
    const categoria = this.categoriaProducto();
    return categoria ? VARISTOREHN_PATHS.categoria(categoria.slug) : VARISTOREHN_PATHS.categorias;
  }
  imagenRelacionado(producto: ProductoTienda): string {
    const modelo = producto.modelos.find(item => item.disponible) || producto.modelos[0];
    return modelo?.imagenes[0] || producto.imagenes[0] || '';
  }
  precioRelacionado(producto: ProductoTienda): number {
    const modelo = producto.modelos.find(item => item.disponible) || producto.modelos[0];
    return modelo ? precioVenta(producto, modelo) : producto.precio;
  }
  moneda(valor: number): string {
    try { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: this.identidad.config().moneda || 'HNL' }).format(valor); }
    catch { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor); }
  }

  private cargarProducto(): void {
    this.cargaProducto?.unsubscribe();
    this.producto.set(null); this.error.set(''); this.vistaWhatsapp.set(''); this.estado.set('loading');
    this.modeloClave.set(''); this.cantidad.set(0); this.imagenActiva.set(0); this.cerrarLightbox(false);
    const slug = this.slugSolicitado();
    if (!slug) { this.estado.set('not-found'); return; }
    if (!this.utilizarDatosBaseDatos()) {
      const producto = crearCatalogoEjemplo().find(item => item.slug === slug && item.activo) || null;
      if (!producto) { this.estado.set('not-found'); return; }
      this.establecerProducto(producto, slug); return;
    }
    this.cargaProducto = this.servicio.obtenerProductoPorSlug(slug).pipe(map(mapearProducto), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: producto => this.establecerProducto(producto, slug),
      error: error => {
        if (this.esNoEncontrado(error)) { this.estado.set('not-found'); return; }
        this.error.set('No pudimos cargar este producto. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.estado.set('error');
      }
    });
  }
  private establecerProducto(producto: ProductoTienda, slugSolicitado: string): void {
    if (!producto.activo) { this.estado.set('not-found'); return; }
    this.producto.set(producto);
    const modelo = producto.modelos.find(item => item.disponible) || producto.modelos[0];
    this.modeloClave.set(modelo?.clave || ''); this.estado.set('success'); this.reiniciarCantidad();
    if (producto.slug && producto.slug !== slugSolicitado) void this.router.navigateByUrl(VARISTOREHN_PATHS.producto(producto.slug), { replaceUrl: true });
  }
  private cargarContextoCatalogo(): void {
    this.cargaCatalogo?.unsubscribe();
    this.catalogoContexto.set([]);
    this.carritoStore.reiniciarContexto();
    const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCatalogo() : of(null);
    this.cargaCatalogo = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: datos => {
        const productos = datos === null ? crearCatalogoEjemplo() : datos.map(mapearProducto);
        this.catalogoContexto.set(productos);
        const resultado = this.carritoStore.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carritoStore.aviso());
        this.reiniciarCantidad();
      },
      error: () => this.aviso.set('El producto puede consultarse, pero no pudimos actualizar relacionados ni validar el carrito. Tu selección guardada no fue reemplazada.')
    });
  }
  private cargarCategorias(): void {
    this.cargaCategorias?.unsubscribe(); this.categorias.set([]);
    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());
    this.cargaCategorias = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => this.categorias.set(categorias),
      error: () => this.aviso.set('No pudimos actualizar la navegación de categorías en esta página.')
    });
  }
  private reiniciarCantidad(): void {
    this.cantidad.set(this.stockRestante() > 0 && this.modeloSeleccionado()?.disponible ? 1 : 0);
  }
  private esNoEncontrado(error: unknown): boolean {
    if (error instanceof HttpErrorResponse) return error.status === 404;
    return error instanceof Error && ['Producto no encontrado.', 'Slug de producto no válido.'].includes(error.message);
  }
}
