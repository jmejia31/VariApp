import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subscription, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { VaristorehnService } from './varistorehn.service';
import { ModoCarrito, VARISTOREHN_CONFIG } from './varistorehn.config';
import {
  ItemCarrito, ModeloTienda, OrdenCatalogo, ProductoCatalogoPublico, ProductoTienda, agregarItem, cambiarCantidad,
  crearCatalogoEjemplo, filtrarProductos, mapearProducto, referenciasCarrito,
  restaurarCarrito, telefonoWhatsapp, totalCarrito, urlCheckoutSegura
} from './varistorehn.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn', standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn.component.html',
  styleUrls: ['./varistorehn.component.scss', './varistorehn.responsive.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  readonly identidad = inject(EmpresaIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
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
  readonly aviso = signal('');
  readonly carrito = signal<ItemCarrito[]>([]);
  readonly totalUnidades = computed(() => this.carrito().reduce((total, item) => total + item.unidades, 0));
  readonly totalCarrito = computed(() => totalCarrito(this.carrito()));
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
  readonly categorias = computed(() => [...new Set(this.productos().map(p => p.categoria))]
    .map(nombre => ({ nombre, cantidad: this.productos().filter(p => p.categoria === nombre).length,
      producto: this.productos().find(p => p.categoria === nombre)! })));
  readonly categoriasNavegacion = computed(() => this.categorias().map(categoria => categoria.nombre));
  readonly destacados = computed(() => this.productos().filter(p => p.disponible).slice(0, 3));
  readonly resultados = computed(() => filtrarProductos(this.productos(), {
    busqueda: this.busqueda(), categoria: this.categoriaActiva(), soloDisponibles: this.soloDisponibles(),
    precioMaximo: this.precioMaximo(), orden: this.orden()
  }));
  readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.resultados().length / this.tamanoPagina)));
  readonly productosVisibles = computed(() => this.resultados().slice((this.pagina() - 1) * this.tamanoPagina, this.pagina() * this.tamanoPagina));
  readonly hayFiltros = computed(() => Boolean(this.busqueda() || this.categoriaActiva() || this.soloDisponibles() || this.precioMaximo() !== null));
  private cargaActual?: Subscription;
  private claveAlmacenamiento = '';
  private idempotencyKey = '';

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.cargarCatalogo());
  }

  cargarCatalogo(): void {
    this.cargaActual?.unsubscribe();
    this.cerrarDetalle();
    this.cerrarCarrito();
    this.cargando.set(true);
    this.errorCatalogo.set('');
    this.errorPago.set('');
    this.aviso.set('');
    this.productos.set([]);
    this.carrito.set([]);
    this.modelosActivos.set({});
    this.limpiarFiltros();
    this.claveAlmacenamiento = `varistorehn:carrito:v2:${this.identidad.config().id}:${this.utilizarDatosBaseDatos() ? 'bd' : 'demo'}`;
    const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos() ? this.servicio.obtenerCatalogo() : of(null);
    this.cargaActual = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: datos => {
        const productos = datos === null ? crearCatalogoEjemplo() : datos.map(mapearProducto);
        this.productos.set(productos);
        const guardado = this.leerCarrito();
        const actualizado = restaurarCarrito(guardado, productos);
        this.guardarCarrito(actualizado);
        if (Array.isArray(guardado) && JSON.stringify(guardado) !== JSON.stringify(referenciasCarrito(actualizado))) {
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

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || this.procesandoPago() || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.cargarCatalogo();
  }
  cambiarModo(modo: string): void {
    if (!this.controlesVistaPrevia || this.procesandoPago() || !['whatsapp', 'tarjeta', 'ambos'].includes(modo)) return;
    this.modoCarrito.set(modo as ModoCarrito);
    this.errorPago.set(''); this.vistaPedido.set(''); this.vistaTarjeta.set(false);
  }
  buscar(texto: string): void { this.busqueda.set(texto); this.pagina.set(1); }
  seleccionarCategoria(nombre: string, desplazar = false): void {
    this.categoriaActiva.set(nombre); this.pagina.set(1);
    if (desplazar) this.irCatalogo();
  }
  cambiarDisponibilidad(valor: boolean): void { this.soloDisponibles.set(valor); this.pagina.set(1); }
  cambiarPrecio(valor: string): void {
    const numero = Number(valor);
    this.precioMaximo.set(valor.trim() && Number.isFinite(numero) && numero >= 0 ? numero : null);
    this.pagina.set(1);
  }
  cambiarOrden(valor: string): void {
    if (['destacados', 'precio-asc', 'precio-desc', 'nombre'].includes(valor)) this.orden.set(valor as OrdenCatalogo);
    this.pagina.set(1);
  }
  limpiarFiltros(): void {
    this.busqueda.set(''); this.categoriaActiva.set(''); this.soloDisponibles.set(false);
    this.precioMaximo.set(null); this.orden.set('destacados'); this.pagina.set(1);
  }
  cambiarPagina(cambio: number): void {
    this.pagina.set(Math.max(1, Math.min(this.totalPaginas(), this.pagina() + cambio))); this.irCatalogo();
  }
  irCatalogo(evento?: Event): void {
    evento?.preventDefault(); this.document.getElementById('catalogo')?.scrollIntoView({ block: 'start' });
  }

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
    const cantidad = this.fotos(producto).length;
    if (cantidad) this.imagenActiva.set((this.imagenActiva() + cambio + cantidad) % cantidad);
  }
  imagenValida(url?: string): boolean { return Boolean(url && !this.imagenesFallidas().has(url)); }
  errorImagen(url: string): void { this.imagenesFallidas.update(actual => new Set([...actual, url])); }
  abrirDetalle(producto: ProductoTienda): void {
    this.productoDetalle.set(producto); this.imagenActiva.set(0); this.detalleDialog?.nativeElement.showModal();
  }
  cerrarDetalle(): void { this.detalleDialog?.nativeElement.close(); this.productoDetalle.set(null); }
  abrirCarrito(): void {
    this.cerrarDetalle(); this.carritoDialog?.nativeElement.showModal();
  }
  cerrarCarrito(): void { this.carritoDialog?.nativeElement.close(); }
  disponibleParaAgregar(producto: ProductoTienda): boolean {
    const modelo = this.modeloSeleccionado(producto);
    const actual = this.carrito().find(i => i.productoId === producto.id && i.modeloClave === modelo.clave);
    return !this.cargando() && !this.procesandoPago() && modelo.disponible && (actual?.unidades || 0) < modelo.stock;
  }
  agregar(producto: ProductoTienda): void {
    if (!this.disponibleParaAgregar(producto)) return;
    this.guardarCarrito(agregarItem(this.carrito(), producto, this.modeloSeleccionado(producto)));
    this.aviso.set(`${producto.nombre} se agregó al carrito.`); this.abrirCarrito();
  }
  cambiarUnidades(clave: string, cambio: number): void {
    if (!this.procesandoPago()) this.guardarCarrito(cambiarCantidad(this.carrito(), clave, cambio));
  }
  quitar(clave: string): void {
    if (!this.procesandoPago()) this.guardarCarrito(this.carrito().filter(i => i.clave !== clave));
  }
  vaciarCarrito(): void { if (!this.procesandoPago()) this.guardarCarrito([]); }

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
    if (!this.tarjetaConfigurada || !this.config.endpointCheckoutTarjeta) {
      this.errorPago.set('El pago con tarjeta todavía no está habilitado por la tienda.'); return;
    }
    const crypto = this.document.defaultView?.crypto;
    if (!crypto?.randomUUID) { this.errorPago.set('Abre la tienda mediante HTTPS para iniciar el pago.'); return; }
    this.idempotencyKey ||= crypto.randomUUID();
    this.procesandoPago.set(true);
    this.servicio.crearCheckoutTarjeta(this.config.endpointCheckoutTarjeta, referenciasCarrito(this.carrito()), this.idempotencyKey)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: destino => {
          const url = urlCheckoutSegura(destino, this.config.origenesCheckoutPermitidos);
          if (!url) { this.errorPago.set('La pasarela devolvió una dirección no autorizada. No se redirigió el navegador.'); this.procesandoPago.set(false); return; }
          // Do not clear the cart or mark a sale paid. Only a verified backend payment can do that.
          this.document.defaultView?.location.assign(url);
        },
        error: () => {
          this.errorPago.set('No se pudo abrir la pasarela. Tu carrito se conserva; puedes volver a intentar.');
          this.procesandoPago.set(false);
        }
      });
  }

  private leerCarrito(): unknown {
    try { return JSON.parse(this.document.defaultView?.localStorage.getItem(this.claveAlmacenamiento) || '[]'); }
    catch { return []; }
  }
  private guardarCarrito(items: ItemCarrito[]): void {
    this.carrito.set(items); this.idempotencyKey = ''; this.errorPago.set(''); this.vistaPedido.set(''); this.vistaTarjeta.set(false);
    try { this.document.defaultView?.localStorage.setItem(this.claveAlmacenamiento, JSON.stringify(referenciasCarrito(items))); }
    catch { this.aviso.set('El navegador no permite guardar el carrito. Puedes seguir comprando en esta sesión.'); }
  }
}