import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type EstadoCaja = 1 | 2;
export type EstadoCajaSesion = 1 | 2 | 3 | 4;
export type TipoMovimientoCaja = 1 | 2 | 3 | 4 | 5;

export interface CajaVista {
  id: number;
  nombre: string;
  estado: EstadoCaja;
  sesionActivaId: number | null;
}

export interface CajaMovimientoVista {
  id: number;
  cajaSesionId: number;
  usuarioId: number;
  tipo: TipoMovimientoCaja;
  monto: number;
  referencia: string;
  fechaOperacion: string | Date;
  impactoSaldo: number;
}

export interface CajaSesionVista {
  id: number;
  cajaId: number;
  usuarioId: number;
  fechaApertura: string | Date;
  fechaCierre: string | Date | null;
  estado: EstadoCajaSesion;
  fondoInicial: number;
  totalIngresos: number;
  totalRetiros: number;
  totalDepositos: number;
  saldoEsperado: number | null;
  saldoContado: number | null;
  diferencia: number | null;
  observacionesArqueo: string | null;
  movimientos: ReadonlyArray<CajaMovimientoVista>;
}

export type CajaAccionUi = 'ABRIR' | 'INICIAR_OPERACIONES' | 'REGISTRAR_MOVIMIENTO' | 'INICIAR_ARQUEO' | 'CERRAR';

@Component({
  selector: 'app-caja-flujo-shell',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="caja-shell" aria-labelledby="caja-title">
      <header class="hero">
        <div>
          <p class="eyebrow">Caja</p>
          <h1 id="caja-title">{{ caja?.nombre || 'Operación de caja' }}</h1>
          <p class="subtitle">Apertura → Operaciones → Arqueo → Cierre</p>
        </div>
        <span class="status" [class.active]="caja?.estado === 2">
          {{ caja?.estado === 2 ? 'Activa' : 'Inactiva' }}
        </span>
      </header>

      <div *ngIf="loading" class="state-panel" role="status" aria-live="polite">
        <mat-spinner diameter="32"></mat-spinner>
        <span>Cargando información de caja…</span>
      </div>

      <div *ngIf="!loading && error" class="state-panel error" role="alert">
        <mat-icon aria-hidden="true">error_outline</mat-icon>
        <span>{{ error }}</span>
      </div>

      <ng-container *ngIf="!loading && !error">
        <nav class="steps" aria-label="Ciclo de vida de la sesión">
          <div *ngFor="let step of pasos" class="step" [class.current]="sesion?.estado === step.id" [class.done]="sesion && sesion.estado > step.id">
            <span>{{ step.id }}</span><strong>{{ step.label }}</strong>
          </div>
        </nav>

        <div *ngIf="!sesion" class="state-panel empty">
          <mat-icon aria-hidden="true">point_of_sale</mat-icon>
          <div>
            <strong>No hay una sesión activa</strong>
            <p>La caja está lista para iniciar una nueva apertura cuando el flujo autorizado lo permita.</p>
          </div>
          <button mat-flat-button type="button" [disabled]="!puedeOperar || caja?.estado !== 2" (click)="accion.emit('ABRIR')">
            Abrir sesión
          </button>
        </div>

        <ng-container *ngIf="sesion as s">
          <section class="metrics" aria-label="Resumen de sesión">
            <article><span>Fondo inicial</span><strong>{{ s.fondoInicial | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article><span>Ingresos</span><strong>{{ s.totalIngresos | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article><span>Retiros</span><strong>{{ s.totalRetiros | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article><span>Depósitos</span><strong>{{ s.totalDepositos | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article *ngIf="s.saldoEsperado !== null"><span>Saldo esperado</span><strong>{{ s.saldoEsperado | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article *ngIf="s.diferencia !== null"><span>Diferencia</span><strong>{{ s.diferencia | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
          </section>

          <div class="actions" aria-label="Acciones disponibles">
            <button mat-stroked-button type="button" *ngIf="s.estado === 1" [disabled]="!puedeOperar" (click)="accion.emit('INICIAR_OPERACIONES')">Iniciar operaciones</button>
            <button mat-flat-button type="button" *ngIf="s.estado === 2" [disabled]="!puedeOperar" (click)="accion.emit('REGISTRAR_MOVIMIENTO')">Registrar movimiento</button>
            <button mat-stroked-button type="button" *ngIf="s.estado === 2" [disabled]="!puedeOperar" (click)="accion.emit('INICIAR_ARQUEO')">Iniciar arqueo</button>
            <button mat-flat-button type="button" *ngIf="s.estado === 3" [disabled]="!puedeOperar" (click)="accion.emit('CERRAR')">Cerrar sesión</button>
          </div>

          <section class="movements" aria-labelledby="movements-title">
            <div class="section-heading">
              <div><p class="eyebrow">Trazabilidad</p><h2 id="movements-title">Movimientos</h2></div>
              <span>{{ s.movimientos.length }} registro{{ s.movimientos.length === 1 ? '' : 's' }}</span>
            </div>

            <div *ngIf="s.movimientos.length === 0" class="state-panel empty compact">
              <mat-icon aria-hidden="true">receipt_long</mat-icon>
              <span>Aún no hay movimientos registrados en esta sesión.</span>
            </div>

            <div class="table-wrap" *ngIf="s.movimientos.length > 0">
              <table>
                <thead><tr><th>Fecha</th><th>Tipo</th><th>Referencia</th><th class="amount">Monto</th><th class="amount">Impacto</th></tr></thead>
                <tbody>
                  <tr *ngFor="let movimiento of s.movimientos; trackBy: trackMovimiento">
                    <td>{{ movimiento.fechaOperacion | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td><span class="movement-type">{{ tipoMovimiento(movimiento.tipo) }}</span></td>
                    <td>{{ movimiento.referencia || '—' }}</td>
                    <td class="amount">{{ movimiento.monto | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                    <td class="amount" [class.negative]="movimiento.impactoSaldo < 0">{{ movimiento.impactoSaldo | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </ng-container>
      </ng-container>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .caja-shell { display: grid; gap: 1rem; padding: 1rem; color: var(--mat-app-text-color, #1f2937); }
    .hero, .section-heading, .actions { display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
    .hero h1, .section-heading h2 { margin: 0; }
    .eyebrow { margin: 0 0 .25rem; font-size: .75rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; opacity: .65; }
    .subtitle { margin: .25rem 0 0; opacity: .72; }
    .status, .movement-type { border-radius: 999px; padding: .35rem .65rem; background: rgba(107,114,128,.12); font-weight: 600; }
    .status.active { background: rgba(22,163,74,.12); }
    .steps { display: grid; grid-template-columns: repeat(4, minmax(0,1fr)); gap: .5rem; }
    .step { display: flex; align-items: center; gap: .5rem; padding: .75rem; border: 1px solid rgba(107,114,128,.22); border-radius: .75rem; opacity: .62; }
    .step > span { width: 1.75rem; height: 1.75rem; display: grid; place-items: center; border-radius: 50%; background: rgba(107,114,128,.12); }
    .step.current, .step.done { opacity: 1; border-color: currentColor; }
    .metrics { display: grid; grid-template-columns: repeat(3, minmax(0,1fr)); gap: .75rem; }
    .metrics article { display: grid; gap: .35rem; padding: 1rem; border: 1px solid rgba(107,114,128,.18); border-radius: .85rem; }
    .metrics span { font-size: .8rem; opacity: .68; } .metrics strong { font-size: 1.2rem; }
    .state-panel { min-height: 7rem; display: flex; align-items: center; justify-content: center; gap: .75rem; padding: 1rem; border: 1px dashed rgba(107,114,128,.28); border-radius: .85rem; text-align: left; }
    .state-panel p { margin: .25rem 0 0; opacity: .7; } .state-panel.error { color: #b91c1c; } .state-panel.compact { min-height: 4rem; }
    .movements { display: grid; gap: .75rem; } .section-heading > span { opacity: .65; font-size: .85rem; }
    .table-wrap { overflow-x: auto; border: 1px solid rgba(107,114,128,.18); border-radius: .85rem; }
    table { width: 100%; border-collapse: collapse; min-width: 680px; } th, td { padding: .8rem; text-align: left; border-bottom: 1px solid rgba(107,114,128,.14); } th { font-size: .75rem; text-transform: uppercase; letter-spacing: .04em; opacity: .65; }
    tbody tr:last-child td { border-bottom: 0; } .amount { text-align: right; font-variant-numeric: tabular-nums; } .negative { color: #b91c1c; }
    @media (max-width: 840px) { .steps { grid-template-columns: repeat(2,minmax(0,1fr)); } .metrics { grid-template-columns: repeat(2,minmax(0,1fr)); } }
    @media (max-width: 520px) { .caja-shell { padding: .5rem; } .steps, .metrics { grid-template-columns: 1fr; } .actions > button { width: 100%; } }
  `]
})
export class CajaFlujoShellComponent {
  @Input() caja: CajaVista | null = null;
  @Input() sesion: CajaSesionVista | null = null;
  @Input() loading = false;
  @Input() error = '';
  @Input() puedeOperar = false;
  @Output() readonly accion = new EventEmitter<CajaAccionUi>();

  readonly pasos: ReadonlyArray<{ id: EstadoCajaSesion; label: string }> = [
    { id: 1, label: 'Apertura' },
    { id: 2, label: 'Operaciones' },
    { id: 3, label: 'Arqueo' },
    { id: 4, label: 'Cierre' }
  ];

  trackMovimiento(_: number, movimiento: CajaMovimientoVista): number { return movimiento.id; }

  tipoMovimiento(tipo: TipoMovimientoCaja): string {
    switch (tipo) {
      case 1: return 'Ingreso';
      case 2: return 'Retiro';
      case 3: return 'Depósito bancario';
      case 4: return 'Diferencia sobrante';
      case 5: return 'Diferencia faltante';
    }
  }
}
