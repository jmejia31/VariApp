import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ReservaInventario } from '../../core/models/reserva-inventario.model';
import { ReservaInventarioService } from '../../services/reserva-inventario.service';

@Component({
  selector: 'app-reserva-inventario-detail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="page" aria-labelledby="reserva-title">
      <header class="header">
        <div><button mat-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Reservas</button><p class="eyebrow">Inventario empresarial</p><h1 id="reserva-title">{{ reserva?.numero || 'Reserva' }}</h1></div>
        <div *ngIf="reserva" class="actions">
          <button *ngIf="puedeEditar && reserva.estado === 'Borrador'" mat-stroked-button type="button" (click)="editar()"><mat-icon>edit</mat-icon>Editar</button>
          <button *ngIf="puedeConfirmar && reserva.estado === 'Borrador'" mat-flat-button color="primary" type="button" [disabled]="procesando" (click)="activar()">Activar</button>
          <button *ngIf="puedeConfirmar && reserva.estado === 'Activa'" mat-flat-button color="primary" type="button" [disabled]="procesando" (click)="consumir()">Consumir</button>
          <button *ngIf="puedeAnular && reserva.estado === 'Activa'" mat-stroked-button type="button" [disabled]="procesando" (click)="liberar()">Liberar</button>
          <button *ngIf="puedeCambiarEstado && reserva.estado === 'Activa'" mat-stroked-button type="button" [disabled]="procesando || !reservaPuedeExpirar" [attr.aria-disabled]="procesando || !reservaPuedeExpirar" [title]="reservaPuedeExpirar ? 'Marcar reserva como expirada' : 'Disponible cuando alcance su fecha de expiración'" (click)="expirar()">Expirar</button>
          <button *ngIf="puedeAnular && (reserva.estado === 'Borrador' || reserva.estado === 'Activa')" mat-stroked-button color="warn" type="button" [disabled]="procesando" (click)="cancelar()">Cancelar</button>
        </div>
      </header>

      <div *ngIf="loading" class="state"><mat-spinner diameter="36"></mat-spinner><span>Cargando reserva…</span></div>
      <div *ngIf="!loading && error" class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error }}</span><button mat-button type="button" (click)="cargar()">Reintentar</button></div>

      <ng-container *ngIf="!loading && reserva as item">
        <section class="summary">
          <div><span>Estado</span><strong>{{ item.estado }}</strong></div><div><span>Venta</span><strong>{{ item.ventaId ? ('#' + item.ventaId) : 'Sin venta' }}</strong></div><div><span>Creada</span><strong>{{ item.fechaCreacion | date:'short' }}</strong></div><div><span>Expira</span><strong>{{ item.fechaExpiracion ? (item.fechaExpiracion | date:'short') : 'Sin expiración' }}</strong></div>
        </section>
        <div *ngIf="mensaje" class="message" role="status">{{ mensaje }}</div>
        <div class="table-wrap"><table><thead><tr><th>SKU / variante</th><th>Almacén</th><th>Ubicación</th><th>Reservado</th><th>Consumido</th><th>Pendiente</th></tr></thead><tbody><tr *ngFor="let detalle of item.detalles"><td><strong>{{ detalle.productoSku || ('Variante #' + detalle.productoVarianteId) }}</strong><small>{{ descripcionProducto(detalle) }}</small></td><td>#{{ detalle.almacenId }}</td><td>{{ detalle.ubicacionAlmacenId ? ('#' + detalle.ubicacionAlmacenId) : 'Sin ubicación' }}</td><td>{{ detalle.cantidadReservada }}</td><td>{{ detalle.cantidadConsumida }}</td><td>{{ detalle.cantidadReservada - detalle.cantidadConsumida }}</td></tr></tbody></table></div>
        <section *ngIf="item.motivoLiberacion || item.motivoCancelacion" class="audit"><strong>Trazabilidad</strong><p *ngIf="item.motivoLiberacion">Liberación: {{ item.motivoLiberacion }}</p><p *ngIf="item.motivoCancelacion">Cancelación: {{ item.motivoCancelacion }}</p></section>
      </ng-container>
    </section>
  `,
  styles: [`.page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.header h1{margin:4px 0}.eyebrow{margin:12px 0 0;text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)}.actions{display:flex;flex-wrap:wrap;gap:8px}.actions mat-icon{margin-right:5px}.state{min-height:180px;display:flex;align-items:center;justify-content:center;gap:12px;border:1px dashed #d0d5dd;border-radius:12px}.state.error{color:#b42318}.summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}.summary div,.audit{padding:14px;border:1px solid #e4e7ec;border-radius:12px;background:#fff}.summary span{display:block;color:#667085;font-size:.78rem;margin-bottom:4px}.message{padding:12px;border-radius:10px;background:#ecfdf3;color:#027a48}.table-wrap{overflow:auto;border:1px solid #e4e7ec;border-radius:12px}table{width:100%;border-collapse:collapse;min-width:780px}th,td{padding:14px 16px;text-align:left;border-bottom:1px solid #eaecf0}th{font-size:.78rem;text-transform:uppercase;color:#667085;background:#f9fafb}td small{display:block;color:#667085;margin-top:3px}.audit p{margin:6px 0 0;color:#475467}@media(max-width:800px){.page{padding:16px}.header{flex-direction:column}.summary{grid-template-columns:1fr 1fr}}@media(max-width:520px){.summary{grid-template-columns:1fr}}`]
})
export class ReservaInventarioDetailComponent implements OnInit {
  reserva: ReservaInventario | null = null;
  loading = false;
  procesando = false;
  error = '';
  mensaje = '';
  private id = 0;

  constructor(private readonly route: ActivatedRoute, private readonly router: Router, private readonly service: ReservaInventarioService, private readonly permisos: PermisosRuntimeService) {}
  ngOnInit(): void { this.id = Number(this.route.snapshot.paramMap.get('id')); if (!Number.isInteger(this.id) || this.id <= 0) { this.error = 'Identificador de reserva inválido.'; return; } this.cargar(); }
  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }
  get puedeConfirmar(): boolean { return this.permisos.puede('MovimientosInventario', 'Confirmar'); }
  get puedeAnular(): boolean { return this.permisos.puede('MovimientosInventario', 'Anular'); }
  get puedeCambiarEstado(): boolean { return this.permisos.puede('MovimientosInventario', 'CambiarEstado'); }
  get reservaPuedeExpirar(): boolean {
    if (!this.reserva?.fechaExpiracion) return false;
    const expiracion = Date.parse(this.reserva.fechaExpiracion);
    return !Number.isNaN(expiracion) && expiracion <= Date.now();
  }

  cargar(): void { this.loading = true; this.error = ''; this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({ next: r => { if (!r.success) { this.error = r.message || 'No se pudo cargar la reserva.'; return; } this.reserva = r.data; }, error: () => this.error = 'No se pudo cargar la reserva.' }); }
  activar(): void { if (!confirm('¿Activar esta reserva y bloquear stock disponible?')) return; this.ejecutar(() => this.service.activar(this.id), 'Reserva activada.'); }
  consumir(): void { if (!confirm('¿Consumir definitivamente esta reserva?')) return; this.ejecutar(() => this.service.consumir(this.id), 'Reserva consumida.'); }
  expirar(): void { if (!this.reservaPuedeExpirar) { this.error = 'La reserva todavía no alcanzó su fecha de expiración.'; return; } if (!confirm('¿Marcar esta reserva como expirada y liberar el stock?')) return; this.ejecutar(() => this.service.expirar(this.id), 'Reserva expirada.'); }
  liberar(): void { const motivo = prompt('Motivo de liberación:')?.trim(); if (!motivo) return; this.ejecutar(() => this.service.liberar(this.id, motivo), 'Reserva liberada.'); }
  cancelar(): void { const motivo = prompt('Motivo de cancelación:')?.trim(); if (!motivo) return; this.ejecutar(() => this.service.cancelar(this.id, motivo), 'Reserva cancelada.'); }
  editar(): void { void this.router.navigate(['/inventario/reservas', this.id, 'editar']); }
  volver(): void { void this.router.navigate(['/inventario/reservas']); }
  descripcionProducto(d: ReservaInventario['detalles'][number]): string { return [d.productoMarca, d.productoModelo, d.productoColor, d.productoTalla].filter(Boolean).join(' · '); }
  private ejecutar(factory: () => ReturnType<ReservaInventarioService['activar']>, ok: string): void { this.procesando = true; this.error = ''; this.mensaje = ''; factory().pipe(finalize(() => this.procesando = false)).subscribe({ next: r => { if (!r.success) { this.error = r.message || 'No se pudo completar la operación.'; return; } this.reserva = r.data; this.mensaje = ok; }, error: () => this.error = 'No se pudo completar la operación.' }); }
}
