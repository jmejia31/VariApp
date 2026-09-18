import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { CategoriaTienda, ProductoTienda, crearCatalogoEjemplo, mapearProducto } from './varistorehn.catalog';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

type EstadoCategoriaPublica = 'loading' | 'error' | 'not-found' | 'success';

@Component({
  selector: 'app-varistorehn-categoria',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn-categoria.component.html',
  styleUrl: './varistorehn-categoria.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnCategoriaComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly identidad = inject(EmpresaIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carrito = inject(VaristorehnCarritoService);

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly permiteWhatsapp = this.config.modoCarrito !== 'tarjeta';
  readonly busqueda = signal('');
  readonly categoria = signal<CategoriaTienda | null>(null);
  readonly estado = signal<EstadoCategoriaPublica>('loading');
  readonly error = signal('');
  readonly slugSolicitado = signal('');
  readonly totalUnidadesCarrito = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotalCarrito = computed<number | null>(() => this.carrito.listo() ? this.carrito.subtotal() : null);
  readonly aviso = signal('');

  readonly rutaCatalogoCategoria = computed(() => {
    const slug = this.categoria()?.slug;
    return slug ? `${VARISTOREHN_PATHS.productos}?categoria=${encodeURIComponent(slug)}` : VARISTOREHN_PATHS.productos;
  });

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    categorias: VARISTOREHN_PATHS.categorias,
    productos: VARISTOREHN_PATHS.productos,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  private cargaCategoria?: Subscription;
  private cargaCarrito?: Subscription;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.cargarContextoCarrito());
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.slugSolicitado.set((params.get('slug') || '').trim());
      this.cargarCategoria();
    });
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.carrito.reiniciarContexto();
    this.cargarCategoria();
    this.cargarContextoCarrito();
  }

  recargar(): void {
    this.cargarCategoria();
    this.cargarContextoCarrito();
  }

  actualizarBusqueda(texto: string): void { this.busqueda.set(texto.slice(0, 180)); }

  buscar(): void {
    const q = this.busqueda().trim();
    void this.router.navigate(['/varistorehn/productos'], { queryParams: q ? { q } : {} });
  }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) void this.router.navigate(['/varistorehn/productos']);
  }

  abrirCarrito(): void { void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito); }

  textoCantidad(): string {
    const cantidad = this.categoria()?.cantidadProductos;
    if (cantidad === null || cantidad === undefined) return 'Cantidad no disponible';
    return `${cantidad} ${cantidad === 1 ? 'producto' : 'productos'}`;
  }

  private cargarCategoria(): void {
    this.cargaCategoria?.unsubscribe();
    this.categoria.set(null);
    this.error.set('');
    this.estado.set('loading');
    const slug = this.slugSolicitado();
    if (!slug) { this.estado.set('not-found'); return; }

    if (!this.utilizarDatosBaseDatos()) {
      const categoria = crearCategoriasTiendaEjemplo().find(item => item.slug === slug) || null;
      this.categoria.set(categoria);
      this.estado.set(categoria ? 'success' : 'not-found');
      return;
    }

    this.cargaCategoria = this.servicio.obtenerCategoriaPorSlug(slug).pipe(
      map(mapearCategoriaTienda),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: categoria => {
        this.categoria.set(categoria);
        this.estado.set('success');
        if (categoria.slug !== slug) void this.router.navigateByUrl(VARISTOREHN_PATHS.categoria(categoria.slug), { replaceUrl: true });
      },
      error: error => {
        if (this.esNoEncontrada(error)) { this.estado.set('not-found'); return; }
        this.error.set('No pudimos cargar esta categoría. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.estado.set('error');
      }
    });
  }

  private esNoEncontrada(error: unknown): boolean {
    if (error instanceof HttpErrorResponse) return error.status === 404;
    return error instanceof Error && ['Categoría no encontrada.', 'Slug de categoría no válido.'].includes(error.message);
  }

  private cargarContextoCarrito(): void {
    this.cargaCarrito?.unsubscribe();
    this.carrito.reiniciarContexto();
    const fuente: Observable<ProductoTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCatalogo().pipe(map(productos => productos.map(mapearProducto)))
      : of(crearCatalogoEjemplo());

    this.cargaCarrito = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: productos => {
        const resultado = this.carrito.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carrito.aviso());
      },
      error: () => this.aviso.set('No pudimos actualizar el resumen del carrito en esta página. Tu selección sigue guardada.')
    });
  }
}
