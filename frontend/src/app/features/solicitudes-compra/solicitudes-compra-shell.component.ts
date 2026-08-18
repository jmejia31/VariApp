import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Subscription } from 'rxjs';
import { EstadoSolicitudCompra, SolicitudCompra, SolicitudCompraDetalle } from '../../core/models/solicitud-compra.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { SolicitudCompraService } from '../../services/solicitud-compra.service';
import { ProveedorService } from '../../services/proveedor.service';

@Component({
  selector: 'app-solicitudes-compra-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './solicitudes-compra-shell.component.html',
  styleUrl: './solicitudes-compra-shell.component.scss'
})
export class SolicitudesCompraShellComponent implements OnInit, OnDestroy {
  readonly solicitudes = signal<SolicitudCompra[]>([]);
  readonly proveedores = signal<Proveedor[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detalleId = signal<number | null>(null);
  readonly detalle = signal<SolicitudCompra | null>(null);
  readonly detalleLoading = signal(false);
  readonly detalleError = signal<string | null>(null);

  readonly estados: EstadoSolicitudCompra[] = ['Borrador', 'Solicitada', 'Aprobada', 'Rechazada'];

  page = 1;
  pageSize = 10;
  numero = '';
  estado: EstadoSolicitudCompra | '' = '';
  proveedorId: number | null = null;
  desde = '';
  hasta = '';

  private listaSubscription?: Subscription;
  private detalleSubscription?: Subscription;
  private routeSubscription?: Subscription;
  private secuenciaLista = 0;
  private secuenciaDetalle = 0;

  constructor(
    private solicitudService: SolicitudCompraService,
    private proveedorService: ProveedorService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.proveedorService.getActivos().subscribe({
      next: res => this.proveedores.set(res.data ?? []),
      error: () => this.proveedores.set([])
    });

    this.routeSubscription = this.route.queryParamMap.subscribe(params => {
      const raw = Number(params.get('detalle'));
      const id = Number.isInteger(raw) && raw > 0 ? raw : null;
      this.detalleId.set(id);
      if (id) this.cargarDetalle(id);
      else {
        this.detalleSubscription?.unsubscribe();
        this.detalle.set(null);
        this.detalleError.set(null);
        this.detalleLoading.set(false);
      }
    });

    this.cargar();
  }

  ngOnDestroy(): void {
    this.listaSubscription?.unsubscribe();
    this.detalleSubscription?.unsubscribe();
    this.routeSubscription?.unsubscribe();
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.page = 1;
    this.pageSize = 10;
    this.numero = '';
    this.estado = '';
    this.proveedorId = null;
    this.desde = '';
    this.hasta = '';
    this.cargar();
  }

  cargar(): void {
    if (this.desde && this.hasta && this.desde > this.hasta) {
      this.error.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }

    const secuencia = ++this.secuenciaLista;
    this.listaSubscription?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);

    this.listaSubscription = this.solicitudService.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      numero: this.numero,
      estado: this.estado || null,
      proveedorId: this.proveedorId,
      desde: this.normalizarFecha(this.desde, false),
      hasta: this.normalizarFecha(this.hasta, true)
    }).subscribe({
      next: res => {
        if (secuencia !== this.secuenciaLista) return;
        this.solicitudes.set(res.data.items ?? []);
        this.totalCount.set(res.data.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => {
        if (secuencia !== this.secuenciaLista) return;
        this.error.set('No fue posible cargar las solicitudes de compra.');
        this.solicitudes.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  recargarDetalle(): void {
    const id = this.detalleId();
    if (id) this.cargarDetalle(id);
  }

  costoEstimadoTotal(solicitud: SolicitudCompra): number {
    return solicitud.detalles.reduce(
      (total, detalle) => total + (detalle.costoEstimadoUnitario ?? 0) * detalle.cantidadSolicitada,
      0
    );
  }

  referenciaProducto(detalle: SolicitudCompraDetalle): string {
    return detalle.productoNombreSnapshot?.trim() || `Producto #${detalle.productoId}`;
  }

  atributosVariante(detalle: SolicitudCompraDetalle): string {
    return [
      detalle.productoMarcaSnapshot,
      detalle.productoModeloSnapshot,
      detalle.productoColorSnapshot,
      detalle.productoTallaSnapshot
    ].filter((valor): valor is string => Boolean(valor?.trim())).join(' · ');
  }

  private cargarDetalle(id: number): void {
    const secuencia = ++this.secuenciaDetalle;
    this.detalleSubscription?.unsubscribe();
    this.detalleLoading.set(true);
    this.detalleError.set(null);
    this.detalle.set(null);

    this.detalleSubscription = this.solicitudService.getById(id).subscribe({
      next: res => {
        if (secuencia !== this.secuenciaDetalle) return;
        this.detalle.set(res.data);
        this.detalleLoading.set(false);
      },
      error: () => {
        if (secuencia !== this.secuenciaDetalle) return;
        this.detalleError.set('No fue posible cargar el detalle de la solicitud.');
        this.detalleLoading.set(false);
      }
    });
  }

  private normalizarFecha(valor: string, finDelDia: boolean): string | null {
    if (!valor) return null;
    return `${valor}T${finDelDia ? '23:59:59.999' : '00:00:00.000'}Z`;
  }
}
