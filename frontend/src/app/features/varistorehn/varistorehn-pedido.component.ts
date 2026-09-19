import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { EstadoRecursoPublico, ReciboPedidoPublico } from './varistorehn.models';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnPedidoService } from './varistorehn-pedido.service';
import { IconoTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn-pedido',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent],
  templateUrl: './varistorehn-pedido.component.html',
  styleUrl: './varistorehn-pedido.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnPedidoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly pedidos = inject(VaristorehnPedidoService);
  private readonly destroyRef = inject(DestroyRef);

  readonly identidad = inject(EmpresaIdentidadService);
  readonly carrito = inject(VaristorehnCarritoService);
  readonly recibo = signal<ReciboPedidoPublico | null>(null);
  readonly estado = signal<EstadoRecursoPublico>('loading');
  readonly error = signal('');
  readonly cargando = computed(() => this.estado() === 'loading');
  readonly totalUnidades = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotal = computed<number | null>(() => this.carrito.listo() ? this.carrito.subtotal() : null);

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: VARISTOREHN_PATHS.productos,
    carrito: VARISTOREHN_PATHS.carrito
  } as const;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const referencia = this.route.snapshot.paramMap.get('id') || '';
        const recibo = this.pedidos.obtener(referencia);
        this.recibo.set(recibo);
        this.estado.set(recibo ? 'success' : 'not-found');
      },
      error: () => {
        this.error.set('No pudimos cargar la confirmación en este momento. Intenta nuevamente.');
        this.estado.set('error');
      }
    });
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito);
  }

  titulo(recibo: ReciboPedidoPublico): string {
    if (recibo.estado === 'whatsapp-preparado') return 'Tu solicitud quedó preparada para WhatsApp';
    if (recibo.estado === 'tarjeta-redirigida') return 'Tu pago seguro fue iniciado';
    return 'Vista previa completada';
  }

  descripcion(recibo: ReciboPedidoPublico): string {
    if (recibo.estado === 'whatsapp-preparado') {
      return 'La referencia resume lo que validamos antes de abrir WhatsApp. La compra queda sujeta a confirmación del comercio; esta pantalla no representa una factura ni una reserva de inventario.';
    }
    if (recibo.estado === 'tarjeta-redirigida') {
      return 'La referencia corresponde al inicio del flujo con el proveedor seguro. La confirmación definitiva del pago debe provenir del proveedor o del comercio.';
    }
    return 'Este recorrido fue ejecutado con datos de demostración. No se generó un cobro, reserva ni pedido real.';
  }

  moneda(valor: number, moneda: string): string {
    try {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: moneda || 'HNL' }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }
}
