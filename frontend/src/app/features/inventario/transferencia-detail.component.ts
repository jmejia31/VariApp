import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TransferenciaInventario } from '../../core/models/transferencia-inventario.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TransferenciaInventarioService } from '../../services/transferencia-inventario.service';

@Component({
  selector: 'app-transferencia-detail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="page" aria-labelledby="transferencia-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="transferencia-title">{{ item?.numero || 'Transferencia' }}</h1>
          <p *ngIf="item">{{ item.almacenOrigenNombre || ('Almacén #' + item.almacenOrigenId) }} → {{ item.almacenDestinoNombre || ('Almacén #' + item.almacenDestinoId) }}</p>
        </div>
        <div class="header-actions">
          <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Volver</button>
          <button *ngIf="item?.estado === 'Borrador' && puedeEditar" mat-stroked-button type="button" (click)="editar()"><mat-icon>edit</mat-icon>Editar</button>
        </div>
      </header>

      <div class="state" *ngIf="loading"><mat-spinner diameter="36"></mat-spinner><span>Cargando transferencia…</span></div>
      <div class="state error" *ngIf="!loading && error">{{ error }}</div>

      <ng-container *ngIf="!loading && item as transferencia">
        <section class="summary">
          <div><span>Estado</span><strong>{{ transferencia.estado }}</strong></div>
          <div><span>Origen</span><strong>{{ transferencia.almacenOrigenNombre || ('#' + transferencia.almacenOrigenId) }}</strong></div>
          <div><span>Destino</span><strong>{{ transferencia.almacenDestinoNombre || ('#' + transferencia.almacenDestinoId) }}</strong></div>
          <div><span>Líneas</span><strong>{{ transferencia.detalles.length }}</strong></div>
        </section>

        <section class="timeline">
          <h2>Trazabilidad</h2>
          <div class="dates">
            <span *ngIf="transferencia.fechaSolicitud">Solicitada: {{ transferencia.fechaSolicitud | date:'short' }}</span>
            <span *ngIf="transferencia.fechaAprobacion">Aprobada: {{ transferencia.fechaAprobacion | date:'short' }}</span>
            <span *ngIf="transferencia.fechaDespacho">Despachada: {{ transferencia.fechaDespacho | date:'short' }}</span>
            <span *ngIf="transferencia.fechaRecepcion">Recibida: {{ transferencia.fechaRecepcion | date:'short' }}</span>
            <span *ngIf="transferencia.fechaCancelacion">Cancelada: {{ transferencia.fechaCancelacion | date:'short' }}</span>
          </div>
          <p *ngIf="transferencia.observaciones"><strong>Observaciones:</strong> {{ transferencia.observaciones }}</p>
          <p *ngIf="transferencia.motivoCancelacion" class="error"><strong>Motivo de cancelación:</strong> {{ transferencia.motivoCancelacion }}</p>
        </section>

        <section class="details">
          <h2>Detalle físico</h2>
          <div class="table-wrap">
            <table>
              <thead><tr><th>Variante</th><th>SKU</th><th>Solicitada</th><th>Aprobada</th><th>Despachada</th><th>Recibida</th><th>Faltante</th><th>Dañada</th><th>Sobrante</th></tr></thead>
              <tbody>
                <tr *ngFor="let detalle of transferencia.detalles">
                  <td>#{{ detalle.productoVarianteId }}</td>
                  <td>{{ detalle.productoSkuSnapshot || '—' }}</td>
                  <td>{{ detalle.cantidadSolicitada }}</td>
                  <td>{{ detalle.cantidadAprobada }}</td>
                  <td>{{ detalle.cantidadDespachada }}</td>
                  <td>{{ detalle.cantidadRecibida }}</td>
                  <td>{{ detalle.cantidadFaltante }}</td>
                  <td>{{ detalle.cantidadDanada }}</td>
                  <td>{{ detalle.cantidadSobrante }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="lifecycle">
          <h2>Acciones de lifecycle</h2>
          <div class="actions">
            <button *ngIf="transferencia.estado === 'Borrador' && puedeCambiarEstado" mat-flat-button color="primary" type="button" (click)="solicitar()" [disabled]="busy">Solicitar</button>
            <button *ngIf="transferencia.estado === 'Solicitada' && puedeAprobar" mat-flat-button color="primary" type="button" (click)="aprobar()" [disabled]="busy">Aprobar</button>
            <button *ngIf="transferencia.estado === 'Aprobada' && puedeConfirmar" mat-flat-button color="primary" type="button" (click)="despachar()" [disabled]="busy">Despachar</button>
            <button *ngIf="transferencia.estado === 'EnTransito' && puedeConfirmar" mat-flat-button color="primary" type="button" (click)="recibir()" [disabled]="busy">Recibir</button>
            <button *ngIf="transferencia.estado !== 'Recibida' && transferencia.estado !== 'Cancelada' && puedeAnular" mat-stroked-button color="warn" type="button" (click)="cancelar()" [disabled]="busy">Cancelar</button>
            <mat-spinner *ngIf="busy" diameter="24"></mat-spinner>
          </div>
          <p class="error" *ngIf="actionError">{{ actionError }}</p>
        </section>
      </ng-container>
    </section>
  `,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px}.header-actions,.actions{display:flex;gap:10px;flex-wrap:wrap;align-items:center}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:12px;font-weight:700;margin:0}.header h1{margin:4px 0}.header p{margin:0;color:var(--text-secondary,#667085)}.summary{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.summary div,.timeline,.details,.lifecycle{padding:18px;border:1px solid rgba(0,0,0,.12);border-radius:12px}.summary span{display:block;color:var(--text-secondary,#667085);font-size:12px}.summary strong{display:block;margin-top:6px}.dates{display:flex;gap:14px;flex-wrap:wrap}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:12px;text-align:left;border-bottom:1px solid rgba(0,0,0,.08);white-space:nowrap}.state{min-height:160px;display:flex;justify-content:center;align-items:center;gap:12px}.error{color:#b42318}@media(max-width:800px){.summary{grid-template-columns:1fr 1fr}.header{flex-direction:column}}@media(max-width:520px){.page{padding:16px}.summary{grid-template-columns:1fr}}
  `]
})
export class TransferenciaDetailComponent implements OnInit {
  readonly id: number;
  item: TransferenciaInventario | null = null;
  loading = false;
  busy = false;
  error = '';
  actionError = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly service: TransferenciaInventarioService,
    private readonly permisos: PermisosRuntimeService
  ) {
    this.id = Number(this.route.snapshot.paramMap.get('id')) || 0;
  }

  ngOnInit(): void { this.cargar(); }

  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }
  get puedeCambiarEstado(): boolean { return this.permisos.puede('MovimientosInventario', 'CambiarEstado'); }
  get puedeAprobar(): boolean { return this.permisos.puede('MovimientosInventario', 'Aprobar'); }
  get puedeConfirmar(): boolean { return this.permisos.puede('MovimientosInventario', 'Confirmar'); }
  get puedeAnular(): boolean { return this.permisos.puede('MovimientosInventario', 'Anular'); }

  cargar(): void {
    this.loading = true;
    this.error = '';
    this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo cargar la transferencia.'; return; }
        this.item = response.data;
      },
      error: () => this.error = 'No se pudo cargar la transferencia.'
    });
  }

  solicitar(): void {
    if (!window.confirm('¿Solicitar esta transferencia? Después de solicitarla ya no podrá editarse como borrador.')) return;
    this.runAction(() => this.service.solicitar(this.id));
  }

  aprobar(): void {
    if (!this.item || !window.confirm('¿Aprobar las cantidades solicitadas de esta transferencia?')) return;
    this.runAction(() => this.service.aprobar(this.id, {
      detalles: this.item.detalles.map(d => ({ detalleId: d.id, cantidadAprobada: d.cantidadSolicitada }))
    }));
  }

  despachar(): void {
    if (!this.item || !window.confirm('¿Despachar las cantidades aprobadas? Esta acción afecta stock físico.')) return;
    this.runAction(() => this.service.despachar(this.id, {
      detalles: this.item.detalles.map(d => ({ detalleId: d.id, cantidadDespachada: d.cantidadAprobada }))
    }));
  }

  recibir(): void {
    if (!this.item || !window.confirm('¿Registrar recepción completa de lo despachado, sin discrepancias?')) return;
    this.runAction(() => this.service.recibir(this.id, {
      detalles: this.item.detalles.map(d => ({
        detalleId: d.id,
        cantidadRecibida: d.cantidadDespachada,
        cantidadFaltante: 0,
        cantidadDanada: 0,
        cantidadSobrante: 0
      }))
    }));
  }

  cancelar(): void {
    const motivo = window.prompt('Motivo obligatorio de cancelación:')?.trim();
    if (!motivo) return;
    this.runAction(() => this.service.cancelar(this.id, { motivo }));
  }

  editar(): void { void this.router.navigate(['/inventario/transferencias', this.id, 'editar']); }
  volver(): void { void this.router.navigate(['/inventario/transferencias']); }

  private runAction(operationFactory: () => ReturnType<TransferenciaInventarioService['solicitar']>): void {
    this.busy = true;
    this.actionError = '';
    operationFactory().pipe(finalize(() => this.busy = false)).subscribe({
      next: response => {
        if (!response.success) { this.actionError = response.message || 'La operación no pudo completarse.'; return; }
        this.item = response.data;
      },
      error: error => this.actionError = error?.error?.message || 'La operación no pudo completarse.'
    });
  }
}
