import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, map, of, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { crearCatalogoEjemplo, mapearProducto, telefonoWhatsapp } from './varistorehn.catalog';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { mensajeWhatsappCheckout, normalizarDatosComprador, urlCheckoutPermitida } from './varistorehn-checkout.rules';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { CheckoutItemRequest, CheckoutValidado, DatosCompradorCheckout, ReciboPedidoPublico } from './varistorehn.models';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnPedidoService } from './varistorehn-pedido.service';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent } from './varistorehn.visual';

type EstadoCheckout = 'loading' | 'ready' | 'empty' | 'error';

@Component({
  selector: 'app-varistorehn-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, VaristorehnHeaderComponent, IconoTiendaComponent],
  templateUrl: './varistorehn-checkout.component.html',
  styleUrl: './varistorehn-checkout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnCheckoutComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly pedidos = inject(VaristorehnPedidoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);

  readonly identidad = inject(EmpresaIdentidadService);
  readonly carrito = inject(VaristorehnCarritoService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly estado = signal<EstadoCheckout>('loading');
  readonly error = signal('');
  readonly procesando = signal(false);
  readonly validado = signal<CheckoutValidado | null>(null);
  readonly enlaceWhatsapp = signal('');
  readonly aviso = signal('');

  readonly permiteWhatsapp = computed(() => this.config.modoCarrito !== 'tarjeta');
  readonly permiteTarjeta = computed(() => this.config.modoCarrito !== 'whatsapp');
  readonly whatsappDisponible = computed(() => Boolean(telefonoWhatsapp(this.identidad.config().whatsApp)));
  readonly tarjetaConfigurada = computed(() => Boolean(this.config.endpointCheckoutTarjeta?.trim() && this.config.origenesCheckoutPermitidos.length));
  readonly totalUnidades = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotalHeader = computed<number | null>(() => this.validado()?.subtotal ?? (this.carrito.listo() ? this.carrito.subtotal() : null));
  readonly validacionVigente = computed(() => {
    const expira = Date.parse(this.validado()?.expiraUtc || '');
    return Number.isFinite(expira) && expira > Date.now();
  });

  readonly formulario = new FormGroup({
    nombre: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)] }),
    telefono: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^\+?[0-9 ()-]{8,24}$/)] }),
    correo: new FormControl('', { nonNullable: true, validators: [Validators.email, Validators.maxLength(160)] }),
    notas: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(600)] })
  });

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: VARISTOREHN_PATHS.productos,
    carrito: VARISTOREHN_PATHS.carrito
  } as const;

  ngOnInit(): void {
    if (!environment.production && this.config.mostrarControlesVistaPrevia
      && this.route.snapshot.queryParamMap.get('fuente') === 'bd') {
      this.utilizarDatosBaseDatos.set(true);
    }

    this.identidad.cargar().pipe(
      takeUntilDestroyed(this.destroyRef),
      switchMap(() => this.prepararCheckout())
    ).subscribe({
      next: validado => this.aplicarValidacion(validado),
      error: error => this.fallar(error)
    });
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito);
  }

  revalidar(): void {
    this.procesando.set(true);
    this.error.set('');
    this.enlaceWhatsapp.set('');
    this.prepararCheckout().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: validado => {
        this.procesando.set(false);
        this.aplicarValidacion(validado);
        this.aviso.set('Actualizamos precios y existencias antes de continuar.');
      },
      error: error => {
        this.procesando.set(false);
        this.fallar(error);
      }
    });
  }

  prepararWhatsapp(): void {
    if (!this.permiteWhatsapp() || !this.validarFormulario()) return;
    const destino = telefonoWhatsapp(this.identidad.config().whatsApp);
    if (!destino) {
      this.error.set('Este comercio no tiene un número de WhatsApp válido configurado.');
      return;
    }
    const validado = this.validado();
    if (!validado || !this.validacionVigente()) {
      this.error.set('La validación del carrito venció. Actualízala antes de continuar.');
      return;
    }

    const comprador = this.datosComprador();
    const moneda = this.identidad.config().moneda || 'HNL';
    const mensaje = mensajeWhatsappCheckout(
      this.identidad.nombreSistema(), comprador, validado.validacionId, moneda, validado.lineas, validado.total
    );
    this.enlaceWhatsapp.set(`https://wa.me/${destino}?text=${encodeURIComponent(mensaje)}`);
    this.aviso.set('Solicitud preparada. Revisa el resumen y abre WhatsApp para continuar.');
  }

  confirmarSalidaWhatsapp(): void {
    const validado = this.validado();
    if (!validado || !this.enlaceWhatsapp()) return;
    this.guardarRecibo(validado, this.utilizarDatosBaseDatos() ? 'whatsapp-preparado' : 'demo');
    queueMicrotask(() => void this.router.navigateByUrl(VARISTOREHN_PATHS.pedido(validado.validacionId)));
  }

  continuarTarjeta(): void {
    if (!this.permiteTarjeta() || !this.validarFormulario()) return;
    const validado = this.validado();
    if (!validado || !this.validacionVigente()) {
      this.error.set('La validación del carrito venció. Actualízala antes de continuar.');
      return;
    }

    if (!this.utilizarDatosBaseDatos()) {
      this.guardarRecibo(validado, 'demo');
      void this.router.navigateByUrl(VARISTOREHN_PATHS.pedido(validado.validacionId));
      return;
    }

    const endpoint = this.config.endpointCheckoutTarjeta?.trim();
    if (!endpoint || !this.tarjetaConfigurada()) {
      this.error.set('El pago con tarjeta todavía no tiene un proveedor seguro configurado para este comercio.');
      return;
    }

    this.procesando.set(true);
    this.error.set('');
    const idempotencyKey = this.generarReferencia('pay');
    this.servicio.crearCheckoutTarjeta(endpoint, {
      validacionId: validado.validacionId,
      items: this.referenciasCheckout(),
      comprador: this.datosComprador(),
      idempotencyKey
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: respuesta => {
        this.procesando.set(false);
        const segura = urlCheckoutPermitida(respuesta.checkoutUrl, this.config.origenesCheckoutPermitidos);
        if (!segura) {
          this.error.set('El proveedor devolvió un destino de pago que no está autorizado. No se realizó ninguna redirección.');
          return;
        }
        const referencia = respuesta.referencia?.trim() || validado.validacionId;
        this.guardarRecibo({ ...validado, validacionId: referencia }, 'tarjeta-redirigida');
        this.document.defaultView?.location.assign(segura);
      },
      error: error => {
        this.procesando.set(false);
        this.error.set(this.mensajeError(error, 'No pudimos iniciar el pago seguro. Intenta nuevamente.'));
      }
    });
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: this.identidad.config().moneda || 'HNL' }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }

  mostrarError(campo: 'nombre' | 'telefono' | 'correo' | 'notas'): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.dirty || control.touched);
  }

  private prepararCheckout(): Observable<CheckoutValidado | null> {
    this.estado.set('loading');
    this.error.set('');
    this.validado.set(null);
    this.enlaceWhatsapp.set('');
    this.carrito.reiniciarContexto();

    const catalogo$ = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCatalogo().pipe(map(productos => productos.map(mapearProducto)))
      : of(crearCatalogoEjemplo());

    return catalogo$.pipe(switchMap(productos => {
      this.carrito.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
      if (this.carrito.vacio()) return of(null);
      return this.utilizarDatosBaseDatos()
        ? this.servicio.validarCheckout(this.referenciasCheckout())
        : of(this.validacionDemo());
    }));
  }

  private aplicarValidacion(validado: CheckoutValidado | null): void {
    if (!validado) {
      this.estado.set('empty');
      return;
    }
    this.validado.set(validado);
    this.estado.set('ready');
  }

  private fallar(error: unknown): void {
    this.estado.set('error');
    this.error.set(this.mensajeError(error, 'No pudimos validar precios y existencias para continuar. Tu carrito permanece guardado.'));
  }

  private referenciasCheckout(): CheckoutItemRequest[] {
    return this.carrito.items().map(item => ({
      productoId: item.productoId,
      modeloId: item.modeloId,
      unidades: item.unidades
    }));
  }

  private validacionDemo(): CheckoutValidado {
    const ahora = Date.now();
    return {
      validacionId: this.generarReferencia('demo'),
      expiraUtc: new Date(ahora + 10 * 60 * 1000).toISOString(),
      subtotal: this.carrito.subtotal(),
      total: this.carrito.total(),
      lineas: this.carrito.items().map(item => ({
        productoId: item.productoId,
        modeloId: item.modeloId,
        nombre: item.nombre,
        modelo: item.modelo,
        sku: null,
        unidades: item.unidades,
        stockDisponible: item.stock,
        precioUnitario: item.precio,
        total: item.precio * item.unidades
      }))
    };
  }

  private validarFormulario(): boolean {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      this.error.set('Revisa los datos de contacto marcados antes de continuar.');
      return false;
    }
    this.error.set('');
    return true;
  }

  private datosComprador(): DatosCompradorCheckout {
    return normalizarDatosComprador(this.formulario.getRawValue());
  }

  private guardarRecibo(validado: CheckoutValidado, estado: ReciboPedidoPublico['estado']): void {
    this.pedidos.guardar({
      referencia: validado.validacionId,
      estado,
      creadoUtc: new Date().toISOString(),
      expiraUtc: validado.expiraUtc,
      total: validado.total,
      moneda: this.identidad.config().moneda || 'HNL',
      lineas: validado.lineas
    });
  }

  private generarReferencia(prefijo: string): string {
    const crypto = this.document.defaultView?.crypto;
    const valor = crypto?.randomUUID?.().replace(/-/g, '') || `${Date.now()}${Math.random().toString(36).slice(2, 12)}`;
    return `${prefijo}-${valor}`.slice(0, 72);
  }

  private mensajeError(error: unknown, fallback: string): string {
    if (error && typeof error === 'object') {
      const posible = error as { error?: { message?: string }; message?: string };
      return posible.error?.message?.trim() || posible.message?.trim() || fallback;
    }
    return fallback;
  }
}
