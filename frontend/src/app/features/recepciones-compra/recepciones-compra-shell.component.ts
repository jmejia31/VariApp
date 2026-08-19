import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import {
  EstadoRecepcionCompra,
  RecepcionCompra,
  RecepcionCompraDetalleInput,
  RecepcionCompraSaldoOrden
} from '../../core/models/recepcion-compra.model';
import { RecepcionCompraService } from '../../services/recepcion-compra.service';

interface LineaEdicion extends RecepcionCompraDetalleInput {
  producto: string;
  cantidadPendiente: number;
}

@Component({
  selector: 'app-recepciones-compra-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="page-shell" aria-labelledby="recepciones-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Compras empresariales</p>
          <h1 id="recepciones-title">Recepción de mercancía</h1>
          <p>Registra recepciones parciales o totales contra una orden aprobada. El stock sólo cambia al confirmar.</p>
        </div>
      </header>

      <section class="card" aria-labelledby="filtros-title">
        <div class="section-heading">
          <div><p class="eyebrow">Consulta</p><h2 id="filtros-title">Recepciones registradas</h2></div>
          <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()"><mat-icon>refresh</mat-icon> Actualizar</button>
        </div>
        <form class="filters" (ngSubmit)="aplicarFiltros()">
          <mat-form-field appearance="outline">
            <mat-label>ID orden</mat-label>
            <input matInput type="number" min="1" [(ngModel)]="filtroOrdenCompraId" name="filtroOrdenCompraId">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Estado</mat-label>
            <mat-select [(ngModel)]="filtroEstado" name="filtroEstado">
              <mat-option value="">Todos</mat-option>
              <mat-option value="Borrador">Borrador</mat-option>
              <mat-option value="Recibida">Recibida</mat-option>
              <mat-option value="Anulada">Anulada</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-flat-button type="submit"><mat-icon>search</mat-icon> Filtrar</button>
        </form>

        @if (loading()) {
          <div class="state" role="status"><mat-spinner diameter="34"></mat-spinner><span>Cargando recepciones…</span></div>
        } @else if (error()) {
          <div class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span></div>
        } @else if (recepciones().length === 0) {
          <div class="state" role="status"><mat-icon>inventory_2</mat-icon><span>No hay recepciones para los filtros seleccionados.</span></div>
        } @else {
          <div class="table-wrap">
            <table>
              <thead><tr><th>Recepción</th><th>Orden</th><th>Estado</th><th class="numeric">Recibida</th><th class="numeric">Aceptada</th><th class="numeric">Dañada</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (item of recepciones(); track item.id) {
                  <tr>
                    <td><strong>{{ item.numeroRecepcion }}</strong></td>
                    <td>{{ item.numeroOrdenCompra || ('#' + item.ordenCompraId) }}</td>
                    <td><span class="status">{{ etiquetaEstado(item.estado) }}</span></td>
                    <td class="numeric">{{ item.cantidadRecibidaTotal }}</td>
                    <td class="numeric">{{ item.cantidadAceptadaTotal }}</td>
                    <td class="numeric">{{ item.cantidadDanadaTotal }}</td>
                    <td class="actions">
                      @if (esBorrador(item) && permisosRuntime.puede('Compras', 'Confirmar')) {
                        <button mat-stroked-button type="button" (click)="confirmar(item)" [disabled]="accionLoading()"><mat-icon>check_circle</mat-icon> Confirmar</button>
                      }
                      @if (!esAnulada(item) && permisosRuntime.puede('Compras', 'Anular')) {
                        <button mat-button type="button" (click)="anular(item)" [disabled]="accionLoading()"><mat-icon>cancel</mat-icon> Anular</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>

      @if (permisosRuntime.puede('Compras', 'Crear')) {
        <section class="card" aria-labelledby="nueva-title">
          <div class="section-heading"><div><p class="eyebrow">Nueva recepción</p><h2 id="nueva-title">Cargar saldo de orden</h2></div></div>
          <div class="load-order">
            <mat-form-field appearance="outline">
              <mat-label>ID de orden de compra</mat-label>
              <input matInput type="number" min="1" [(ngModel)]="ordenCompraId" name="ordenCompraId">
            </mat-form-field>
            <button mat-flat-button type="button" (click)="cargarSaldo()" [disabled]="saldoLoading() || !ordenCompraId"><mat-icon>download</mat-icon> Cargar saldo</button>
          </div>

          @if (saldoLoading()) {
            <div class="state" role="status"><mat-spinner diameter="32"></mat-spinner><span>Consultando saldo de la orden…</span></div>
          } @else if (saldoError()) {
            <div class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ saldoError() }}</span></div>
          } @else if (saldo(); as orden) {
            <div class="order-summary">
              <strong>{{ orden.numeroOrden }}</strong>
              <span>{{ orden.lineas.length }} líneas</span>
              <span [class.complete]="orden.completa">{{ orden.completa ? 'Recepción completa' : 'Con saldo pendiente' }}</span>
            </div>

            @if (!orden.completa) {
              <div class="table-wrap">
                <table>
                  <thead><tr><th>Producto</th><th class="numeric">Pendiente</th><th>Almacén</th><th>Recibida</th><th>Dañada</th><th>Faltante</th><th>Sobrante</th></tr></thead>
                  <tbody>
                    @for (linea of lineasEdicion(); track linea.ordenCompraDetalleId) {
                      <tr>
                        <td>{{ linea.producto }}</td>
                        <td class="numeric">{{ linea.cantidadPendiente }}</td>
                        <td><input class="cell-input" type="number" min="1" [(ngModel)]="linea.almacenId" [name]="'almacen-' + linea.ordenCompraDetalleId" aria-label="Almacén"></td>
                        <td><input class="cell-input" type="number" min="0" step="0.01" [(ngModel)]="linea.cantidadRecibida" [name]="'recibida-' + linea.ordenCompraDetalleId" aria-label="Cantidad recibida"></td>
                        <td><input class="cell-input" type="number" min="0" step="0.01" [(ngModel)]="linea.cantidadDanada" [name]="'danada-' + linea.ordenCompraDetalleId" aria-label="Cantidad dañada"></td>
                        <td><input class="cell-input" type="number" min="0" step="0.01" [(ngModel)]="linea.cantidadFaltante" [name]="'faltante-' + linea.ordenCompraDetalleId" aria-label="Cantidad faltante"></td>
                        <td><input class="cell-input" type="number" min="0" step="0.01" [(ngModel)]="linea.cantidadSobrante" [name]="'sobrante-' + linea.ordenCompraDetalleId" aria-label="Cantidad sobrante"></td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              <mat-form-field appearance="outline" class="observaciones">
                <mat-label>Observaciones</mat-label>
                <textarea matInput rows="3" maxlength="1000" [(ngModel)]="observaciones" name="observaciones"></textarea>
              </mat-form-field>
              @if (guardarError()) { <div class="state error" role="alert">{{ guardarError() }}</div> }
              @if (guardarMensaje()) { <div class="state success" role="status">{{ guardarMensaje() }}</div> }
              <button mat-flat-button type="button" (click)="crearRecepcion()" [disabled]="guardarLoading() || !puedeGuardar()">
                <mat-icon>save</mat-icon> Guardar borrador
              </button>
            }
          }
        </section>
      }
    </section>
  `,
  styles: [`
    .page-shell{display:grid;gap:1.25rem;max-width:1500px;margin:0 auto}.page-header,.section-heading,.load-order,.order-summary,.actions{display:flex;align-items:center;gap:1rem}.page-header,.section-heading{justify-content:space-between}.eyebrow{margin:0 0 .25rem;text-transform:uppercase;letter-spacing:.08em;font-size:.75rem;font-weight:700;opacity:.7}h1,h2{margin:.1rem 0}.card{background:var(--mat-sys-surface,#fff);border:1px solid rgba(127,127,127,.2);border-radius:16px;padding:1rem}.filters{display:grid;grid-template-columns:repeat(3,minmax(160px,1fr));gap:.75rem;align-items:start}.table-wrap{overflow:auto;border:1px solid rgba(127,127,127,.18);border-radius:12px}table{width:100%;border-collapse:collapse;min-width:850px}th,td{padding:.7rem .75rem;text-align:left;border-bottom:1px solid rgba(127,127,127,.15)}th{font-size:.8rem;text-transform:uppercase;letter-spacing:.04em}.numeric{text-align:right}.status{font-weight:700}.state{min-height:90px;display:flex;align-items:center;justify-content:center;gap:.75rem}.error{color:var(--mat-sys-error,#b3261e)}.success{color:#146c2e}.load-order{flex-wrap:wrap}.load-order mat-form-field{min-width:280px}.order-summary{padding:.75rem 0;flex-wrap:wrap}.complete{font-weight:700}.cell-input{width:90px;box-sizing:border-box;padding:.45rem;border:1px solid rgba(127,127,127,.35);border-radius:8px;background:transparent;color:inherit}.observaciones{width:100%;margin-top:1rem}@media(max-width:900px){.filters{grid-template-columns:1fr}.page-header,.section-heading{align-items:flex-start;flex-direction:column}}
  `]
})
export class RecepcionesCompraShellComponent implements OnInit {
  readonly recepciones = signal<RecepcionCompra[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly saldo = signal<RecepcionCompraSaldoOrden | null>(null);
  readonly saldoLoading = signal(false);
  readonly saldoError = signal('');
  readonly lineasEdicion = signal<LineaEdicion[]>([]);
  readonly guardarLoading = signal(false);
  readonly guardarError = signal('');
  readonly guardarMensaje = signal('');
  readonly accionLoading = signal(false);

  filtroOrdenCompraId: number | null = null;
  filtroEstado = '';
  ordenCompraId: number | null = null;
  observaciones = '';

  constructor(
    private readonly service: RecepcionCompraService,
    public readonly permisosRuntime: PermisosRuntimeService
  ) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading.set(true); this.error.set('');
    this.service.getPaged({ page: 1, pageSize: 50, ordenCompraId: this.filtroOrdenCompraId, estado: this.filtroEstado as 'Borrador' | 'Recibida' | 'Anulada' || null })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({ next: r => this.recepciones.set(r.data?.items ?? []), error: e => this.error.set(this.extraerError(e, 'No fue posible cargar las recepciones.')) });
  }

  aplicarFiltros(): void { this.cargar(); }

  cargarSaldo(): void {
    if (!this.ordenCompraId || this.ordenCompraId < 1) return;
    this.saldoLoading.set(true); this.saldoError.set(''); this.saldo.set(null); this.lineasEdicion.set([]); this.guardarMensaje.set('');
    this.service.getSaldoOrden(this.ordenCompraId)
      .pipe(finalize(() => this.saldoLoading.set(false)))
      .subscribe({
        next: r => {
          const saldo = r.data; this.saldo.set(saldo);
          this.lineasEdicion.set((saldo?.lineas ?? []).filter(x => x.cantidadPendiente > 0).map(x => ({
            ordenCompraDetalleId: x.ordenCompraDetalleId,
            almacenId: 0,
            ubicacionAlmacenId: null,
            cantidadRecibida: x.cantidadPendiente,
            cantidadDanada: 0,
            cantidadFaltante: 0,
            cantidadSobrante: 0,
            producto: x.productoNombreSnapshot || x.productoSkuSnapshot || `Producto #${x.productoId}`,
            cantidadPendiente: x.cantidadPendiente
          })));
        },
        error: e => this.saldoError.set(this.extraerError(e, 'No fue posible consultar el saldo de la orden.'))
      });
  }

  puedeGuardar(): boolean {
    return !!this.ordenCompraId && this.lineasEdicion().some(x => x.almacenId > 0 && (x.cantidadRecibida > 0 || x.cantidadFaltante > 0) && x.cantidadDanada + x.cantidadSobrante <= x.cantidadRecibida);
  }

  crearRecepcion(): void {
    if (!this.ordenCompraId) return;
    const detalles = this.lineasEdicion().filter(x => x.almacenId > 0 && (x.cantidadRecibida > 0 || x.cantidadFaltante > 0)).map(({ producto, cantidadPendiente, ...input }) => input);
    if (!detalles.length) { this.guardarError.set('Registra al menos una línea válida con almacén y cantidad.'); return; }
    if (detalles.some(x => x.cantidadDanada + x.cantidadSobrante > x.cantidadRecibida)) { this.guardarError.set('Dañada + sobrante no puede superar la cantidad recibida.'); return; }
    this.guardarLoading.set(true); this.guardarError.set(''); this.guardarMensaje.set('');
    this.service.create({ ordenCompraId: this.ordenCompraId, observaciones: this.observaciones.trim() || null, detalles })
      .pipe(finalize(() => this.guardarLoading.set(false)))
      .subscribe({ next: r => { this.guardarMensaje.set(`Borrador ${r.data.numeroRecepcion} creado correctamente.`); this.cargar(); this.cargarSaldo(); }, error: e => this.guardarError.set(this.extraerError(e, 'No fue posible guardar la recepción.')) });
  }

  confirmar(item: RecepcionCompra): void {
    if (!globalThis.confirm(`¿Confirmar ${item.numeroRecepcion}? Esta acción materializa el stock físico.`)) return;
    this.accionLoading.set(true);
    this.service.confirmar(item.id).pipe(finalize(() => this.accionLoading.set(false))).subscribe({ next: () => { this.cargar(); if (this.ordenCompraId === item.ordenCompraId) this.cargarSaldo(); }, error: e => this.error.set(this.extraerError(e, 'No fue posible confirmar la recepción.')) });
  }

  anular(item: RecepcionCompra): void {
    const motivo = globalThis.prompt(`Motivo de anulación de ${item.numeroRecepcion}:`)?.trim();
    if (!motivo) return;
    this.accionLoading.set(true);
    this.service.anular(item.id, motivo).pipe(finalize(() => this.accionLoading.set(false))).subscribe({ next: () => { this.cargar(); if (this.ordenCompraId === item.ordenCompraId) this.cargarSaldo(); }, error: e => this.error.set(this.extraerError(e, 'No fue posible anular la recepción.')) });
  }

  etiquetaEstado(estado: EstadoRecepcionCompra): string {
    const map: Record<string, string> = { '1': 'Borrador', '2': 'Recibida', '3': 'Anulada', Borrador: 'Borrador', Recibida: 'Recibida', Anulada: 'Anulada' };
    return map[String(estado)] ?? String(estado);
  }

  esBorrador(item: RecepcionCompra): boolean { return item.estado === 1 || item.estado === 'Borrador'; }
  esAnulada(item: RecepcionCompra): boolean { return item.estado === 3 || item.estado === 'Anulada'; }

  private extraerError(error: any, fallback: string): string {
    return error?.error?.detail || error?.error?.message || error?.message || fallback;
  }
}
