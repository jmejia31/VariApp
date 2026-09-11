import { CommonModule, DOCUMENT } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { VaristorehnService } from './varistorehn.service';
import { ModoCarrito, VARISTOREHN_CONFIG } from './varistorehn.config';
import {
  CategoriaTienda, EstadoConsultaPublica, ModeloTienda, OrdenCatalogo, ProductoCatalogoPublico, ProductoTienda,
  crearCatalogoEjemplo, filtrarProductos, mapearProducto, referenciasCarrito, telefonoWhatsapp, urlCheckoutSegura
} from './varistorehn.catalog';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn', standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn.component.html',
  styleUrls: ['./varistorehn.component.scss', './varistorehn.responsive.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnComponent implements OnInit, AfterViewInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly route = inject(ActivatedRoute);
  readonly identidad = inject(EmpresaIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carritoStore = inject(VaristorehnCarritoService);
  @ViewChild('carritoDialog') private carritoDialog?: ElementRef<HTMLDialogElement>;
  @ViewChild('detalleDialog') private detalleDialog?: ElementRef<HTMLDialogElement>;

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly modoCarrito = signal<ModoCarrito>(this.config.modoCarrito);
  readonly permiteWhatsapp = computed(() => this.modoCarrito() !== 'tarjeta');
  readonly permiteTarjeta = computed(() => this.modoCarrito() !== 'whatsapp');
  readonly telefono = computed(() => telefonoWhatsapp(this.identidad.config().whatsApp));
  readonly tarjetaConfigurada = Boolean(this.config.endpointCheckoutTarjeta && this.config.origenesCheckoutPermitidos.length);
  readonly productos = signal<ProductoTienda[]>([]);
  readonly cargando = signal(true);
  readonly errorCatalogo = signal('');
  readonly categoriasTienda = signal<CategoriaTienda[]>([]);
  readonly cargandoCategorias = signal(true);
  readonly errorCategorias = signal('');
  readonly estadoCategorias = computed<EstadoConsultaPublica>(() => {
    if (this.cargandoCategorias()) return 'loading';
    if (this.errorCategorias()) return 'error';
    return this.categoriasTienda().length ? 'success' : 'empty';
  });
  readonly aviso = signal('');
  readonly carrito = this.carritoStore.items;
  readonly totalUnidades = computed(() => this.carritoStore.totalUnidades());
  readonly totalCarrito = computed(() => this.carritoStore.subtotal());
  readonly productoDetalle = signal<ProductoTienda | null>(null);
  readonly modelosActivos = signal<Record<number, string>>({});
  readonly imagenActiva = signal(0);
  readonly imagenesFallidas = signal<Set<string>>(new Set());
  readonly busqueda = signal('');
  readonly categoriaActiva = signal('');
  readonly soloDisponibles = signal(false);
  readonly precioMaximo = signal<number | null>(null);
  readonly orden = signal<OrdenCatalogo>('destacados');
  readonly pagina = signal(1);
  readonly tamanoPagina = 12;
  readonly filtrosAbiertos = signal(false);
  readonly procesandoPago = signal(false);
  readonly errorPago = signal('');
  readonly vistaPedido = signal('');
  readonly vistaTarjeta = signal(false);
  readonly categoriasNavegacion = computed(() => this.categoriasTienda().map(categoria => categoria.nombre));
  readonly destacados = computed(() => this.productos().filter(p => p.disponible).slice(0, 3));
  readonly resultados = computed(() => filtrarProductos(this.productos(), {
    busqueda: this.busqueda(), categoria: this.categoriaActiva(), soloDisponibles: this.soloDisponibles(),
    precioMaximo: this.precioMaximo(), orden: this.orden()
  }));
  readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.resultados().length / this.tamanoPagina)));
  readonly productosVisibles = computed(() => this.resultados().slice((this.pagina() - 1) * this.tamanoPagina, this.pagina() * this.tamanoPagina));
  readonly hayFiltros = computed(() => Boolean(this.busqueda() || this.categoriaActiva() || this.soloDisponibles() || this.precioMaximo() !== null));
  readonly enlaceCategorias = VARISTOREHN_PATHS.categorias;
  private cargaActual?: Subscription;
  private cargaCategoriasActual?: Subscription;
  private idempotencyKey = '';
  private vistaInicializada = false;
  private busquedaInicialAplicada = false;
  private categoriaInicialAplicada = false;
  private abrirCarritoPendiente = this.route.snapshot.queryParamMap.get('carrito') === '1';
  private readonly busquedaInicial = (this.route.snapshot.queryParamMap.get('q') || '').trim().slice(0, 180);
  private readonly categoriaSlugInicial = (this.route.snapshot.queryParamMap.get('categoria') || '').trim().slice(0, 180);

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.cargarCatalogo();
      this.cargarCategorias();
    });
  }
  ngAfterViewInit(): void { this.vistaInicializada = true; this.abrirCarritoSiPendiente(); }

  cargarCatalogo(): void {
    this.cargaActual?.unsubscribe();
    this.cerrarDetalle(); this.cerrarCarrito();
    this.cargando.set(true); this.errorCatalogo.set(''); this.errorPago.set(''); this.aviso.set('');
    this.productos.set([]); this.carritoStore.reiniciarContexto(); this.modelosActivos.set({});
    const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos() ? this.servicio.obtenerCatalogo() : of(null);
    this.cargaActual = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: datos => {
        const productos = datos === null ? crearCatalogoEjemplo() : datos.map(mapearProducto);
        this.productos.set(productos);
        const resultado = this.carritoStore.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carritoStore.aviso());
        this.cargando.set(false);
        this.aplicarBusquedaInicial();
        this.abrirCarritoSiPendiente();
      },
      error: () => {
        this.errorCatalogo.set('No pudimos cargar el catálogo. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.cargando.set(false);
      }
    });
  }

  cargarCategorias(): void {
    this.cargaCategoriasActual?.unsubscribe();
    this.cargandoCategorias.set(true); this.errorCategorias.set(''); this.categoriasTienda.set([]);
    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());
    this.cargaCategoriasActual = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => { this.categoriasTienda.set(categorias); this.cargandoCategorias.set(false); this.aplicarCategoriaInicial(categorias); },
      error: () => { this.errorCategorias.set('No pudimos cargar las categorías. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.'); this.cargandoCategorias.set(false); }
    });
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || this.procesandoPago() || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos); this.carritoStore.reiniciarContexto(); this.limpiarFiltros(); this.cargarCatalogo(); this.cargarCategorias();
  }
  cambiarModo(modo: string): void {
    if (!this.controlesVistaPrevia || this.procesandoPago() || !['whatsapp', 'tarjeta', 'ambos'].includes(modo)) return;
    this.modoCarrito.set(modo as ModoCarrito); this.errorPago.set(''); this.vistaPedido.set(''); this.vistaTarjeta.set(false);
  }
  buscar(texto: string): void { this.busqueda.set(texto); this.pagina.set(1); }
  seleccionarCategoria(nombre: string, desplazar = false): void { this.categoriaActiva.set(nombre); this.pagina.set(1); if (desplazar) this.irCatalogo(); }
  cambiarDisponibilidad(valor: boolean): void { this.soloDisponibles.set(valor); this.pagina.set(1); }
  cambiarPrecio(valor: string): void {
    const numero = Number(valor); this.precioMaximo.set(valor.trim() && Number.isFinite(numero) && numero >= 0 ? numero : null); this.pagina.set(1);
  }
  cambiarOrden(valor: string): void {
    if (['destacados', 'precio-asc', 'precio-desc', 'nombre'].includes(valor)) this.orden.set(valor as OrdenCatalogo); this.pagina.set(1);
  }
  limpiarFiltros(): void { this.busqueda.set(''); this.categoriaActiva.set(''); this.soloDisponibles.set(false); this.precioMaximo.set(null); this.orden.set('destacados'); this.pagina.set(1); }
  cambiarPagina(cambio: number): void { this.pagina.set(Math.max(1, Math.min(this.totalPaginas(), this.pagina() + cambio))); this.irCatalogo(); }
  irCatalogo(evento?: Event): void { evento?.preventDefault(); this.document.getElementById('catalogo')?.scrollIntoView({ block: 'start' }); }

  textoCantidadCategoria(categoria: CategoriaTienda): string {
    const cantidad = categoria.cantidadProductos;
    if (cantidad === null) return 'Cantidad no disponible';
    return `${cantidad} ${cantidad === 1 ? 'producto' : 'productos'}`;
  }
  cantidadCategoriaFiltro(categoria: CategoriaTienda): string { return categoria.cantidadProductos === null ? '—' : String(categoria.cantidadProductos); }
  modeloSeleccionado(producto: ProductoTienda): ModeloTienda {
    return producto.modelos.find(m => m.clave === this.modelosActivos()[producto.id])
      || producto.modelos.filter(m => m.disponible).sort((a, b) => a.precio - b.precio)[0] || producto.modelos[0];
  }
  seleccionarModelo(producto: ProductoTienda, clave: string): void {
    if (!producto.modelos.some(m => m.clave === clave)) return;
    this.modelosActivos.update(estado => ({ ...estado, [producto.id]: clave })); this.imagenActiva.set(0);
  }
  fotos(producto: ProductoTienda): string[] { return this.modeloSeleccionado(producto).imagenes; }
  moverImagen(producto: ProductoTienda, cambio: number): void {
    const cantidad = this.fotos(producto).length; if (cantidad) this.imagenActiva.set((this.imagenActiva() + cambio + cantidad) % cantidad);
  }
  imagenValida(url?: string): boolean { return Boolean(url && !this.imagenesFallidas().has(url)); }
  errorImagen(url: string): void { this.imagenesFallidas.update(actual => new Set([...actual, url])); }
  abrirDetalle(producto: ProductoTienda): void {
    if (!producto.slug) { this.aviso.set('Este producto todavía no tiene una URL pública disponible.'); return; }
    this.document.defaultView?.location.assign(VARISTOREHN_PATHS.producto(producto.slug));
  }
  cerrarDetalle(): void { this.detalleDialog?.nativeElement.close(); this.productoDetalle.set(null); }
  abrirCarrito(): void { this.cerrarDetalle(); this.carritoDialog?.nativeElement.showModal(); }
  cerrarCarrito(): void { this.carritoDialog?.nativeElement.close(); }
  disponibleParaAgregar(producto: ProductoTienda): boolean {
    const modelo = this.modeloSeleccionado(producto);
    return !this.cargando() && !this.procesandoPago() && this.carritoStore.disponibleParaAgregar(producto, modelo);
  }
  agregar(producto: ProductoTienda): void {
    const modelo = this.modeloSeleccionado(producto);
    if (!this.disponibleParaAgregar(producto)) return;
    const agregadas = this.carritoStore.agregar(producto, modelo, 1);
    if (!agregadas) return;
    this.idempotencyKey = ''; this.errorPago.set(''); this.vistaPedido.set(''); this.vistaTarjeta.set(false);
    this.aviso.set(`${producto.nombre} se agregó al carrito.`); this.abrirCarrito();
  }
  cambiarUnidades(clave: string, cambio: number): void {
    if (this.procesandoPago()) return;
    if (cambio > 0) this.carritoStore.incrementar(clave);
    else if (cambio < 0) this.carritoStore.disminuir(clave);
    this.idempotencyKey = '';
  }
  quitar(clave: string): void { if (!this.procesandoPago()) { this.carritoStore.quitar(clave); this.idempotencyKey = ''; } }
  vaciarCarrito(): void { if (!this.procesandoPago()) { this.carritoStore.vaciar(); this.idempotencyKey = ''; } }

  moneda(valor: number): string {
    try { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: this.identidad.config().moneda || 'HNL' }).format(valor); }
    catch { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor); }
  }
  enlaceWhatsapp(): string { return this.telefono() ? `https://wa.me/${this.telefono()}` : ''; }
  enlaceExterno(valor?: string): string | null {
    try { const url = new URL(valor || ''); return ['https:', 'http:'].includes(url.protocol) ? url.href : null; }
    catch { return null; }
  }
  realizarPedido(): void {
    if (!this.permiteWhatsapp() || !this.carrito().length || this.cargando() || this.errorCatalogo() || this.procesandoPago()) return;
    const lineas = this.carrito().map(i => `- ${i.nombre} (${i.modelo}) x${i.unidades}: ${this.moneda(i.precio * i.unidades)}`).join('\n');
    const mensaje = `Hola ${this.identidad.config().nombreComercial}, deseo consultar este pedido:\n\n${lineas}\n\nSubtotal: ${this.moneda(this.totalCarrito())}\nPor favor confirmar disponibilidad, impuestos, envío y total final.`;
    this.errorPago.set(''); this.vistaTarjeta.set(false);
    if (!this.utilizarDatosBaseDatos()) { this.vistaPedido.set(`PEDIDO DE EJEMPLO - NO ENVIADO\n\n${mensaje}`); return; }
    if (!this.telefono()) { this.errorPago.set('Falta configurar un número de WhatsApp válido en los datos de la empresa.'); return; }
    const url = `${this.enlaceWhatsapp()}?text=${encodeURIComponent(mensaje)}`;
    if (url.length > 7500) { this.errorPago.set('El pedido es demasiado largo para enviarlo por enlace. Reduce los artículos o contacta a la tienda.'); return; }
    this.document.defaultView?.open(url, '_blank', 'noopener,noreferrer');
  }
  pagarConTarjeta(): void {
    if (!this.permiteTarjeta() || !this.carrito().length || this.procesandoPago() || this.cargando() || this.errorCatalogo()) return;
    this.errorPago.set(''); this.vistaPedido.set('');
    if (!this.utilizarDatosBaseDatos()) { this.vistaTarjeta.set(true); return; }
    if (!this.tarjetaConfigurada || !this.config.endpointCheckoutTarjeta) { this.errorPago.set('El pago con tarjeta todavía no está habilitado por la tienda.'); return; }
    const crypto = this.document.defaultView?.crypto;
    if (!crypto?.randomUUID) { this.errorPago.set('Abre la tienda mediante HTTPS para iniciar el pago.'); return; }
    this.idempotencyKey ||= crypto.randomUUID(); this.procesandoPago.set(true);
    this.servicio.crearCheckoutTarjeta(this.config.endpointCheckoutTarjeta, referenciasCarrito(this.carrito()), this.idempotencyKey)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: destino => {
          const url = urlCheckoutSegura(destino, this.config.origenesCheckoutPermitidos);
          if (!url) { this.errorPago.set('La pasarela devolvió una dirección no autorizada. No se redirigió el navegador.'); this.procesandoPago.set(false); return; }
          this.document.defaultView?.location.assign(url);
        },
        error: () => { this.errorPago.set('No se pudo abrir la pasarela. Tu carrito se conserva; puedes volver a intentar.'); this.procesandoPago.set(false); }
      });
  }

  private aplicarBusquedaInicial(): void {
    if (this.busquedaInicialAplicada) return; this.busquedaInicialAplicada = true; if (this.busquedaInicial) this.buscar(this.busquedaInicial);
  }
  private aplicarCategoriaInicial(categorias: CategoriaTienda[]): void {
    if (this.categoriaInicialAplicada) return; this.categoriaInicialAplicada = true; if (!this.categoriaSlugInicial) return;
    const categoria = categorias.find(item => item.slug === this.categoriaSlugInicial);
    if (categoria) this.seleccionarCategoria(categoria.nombre); else this.aviso.set('La categoría solicitada ya no está disponible.');
  }
  private abrirCarritoSiPendiente(): void {
    if (!this.abrirCarritoPendiente || !this.vistaInicializada || this.cargando() || this.errorCatalogo()) return;
    this.abrirCarritoPendiente = false; this.abrirCarrito();
  }
}
