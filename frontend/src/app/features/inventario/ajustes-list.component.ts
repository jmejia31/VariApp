import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import {
  AjusteInventario,
  AjusteInventarioFiltro,
  EstadoAjusteInventario
} from '../../core/models/ajuste-inventario.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AjusteInventarioService } from '../../services/ajuste-inventario.service';

@Component({
  selector: 'app-ajustes-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="ajustes-page" aria-labelledby="ajustes-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="ajustes-title">Ajustes de inventario</h1>
          <p class="subtitle">Consulta y controla el ciclo Borrador → Confirmado → Anulado.</p>
        </div>
        <div class="page-actions">
          <button *ngIf="puedeCrear()" mat-flat-button color="primary" type="button" (click)="nuevo()" [disabled]="loading()">
            <mat-icon>add</mat-icon>
            Nuevo ajuste
          </button>
          <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()">
            <mat-icon>refresh</mat-icon>
            Actualizar
          </button>
        </div>
      </header>

      <form class="filters" (ngSubmit)="aplicarFiltros()">
        <mat-form-field appearance="outline">
          <mat-label>Buscar</mat-label>
          <input matInput name="search" [(ngModel)]="search" placeholder="Número o motivo" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Estado</mat-label>
          <mat-select name="estado" [(ngModel)]="estado">
            <mat-option value="">Todos</mat-option>
            <mat-option value="Borrador">Borrador</mat-option>
            <mat-option value="Confirmado">Confirmado</mat-option>
            <mat-option value="Anulado">Anulado</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Desde</mat-label>
          <input matInput type="date" name="desde" [(ngModel)]="desde" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Hasta</mat-label>
          <input matInput type="date" name="hasta" [(ngModel)]="hasta" />
        </mat-form-field>

        <div class="filter-actions">
          <button mat-flat-button color="primary" type="submit" [disabled]="loading()">Aplicar</button>
          <button mat-button type="button" (click)="limpiarFiltros()" [disabled]="loading()">Limpiar</button>
        </div>
      </form>

      <div class="feedback error" *ngIf="error()" role="alert">
        <mat-icon>error_outline</mat-icon>
        <span>{{ error() }}</span>
        <button mat-button type="button" (click)="cargar()">Reintentar</button>
      </div>

      <div class="loading" *ngIf="loading()" aria-live="polite">
        <mat-spinner diameter="36"></mat-spinner>
        <span>Cargando ajustes…</span>
      </div>

      <ng-container *ngIf="!loading() && !error()">
        <div class="empty" *ngIf="ajustes().length === 0">
          <mat-icon>inventory_2</mat-icon>
          <h2>No hay ajustes para los filtros seleccionados</h2>
          <p>Modifica los filtros o crea un borrador desde el flujo de inventario.</p>
        </div>

        <div class="table-shell" *ngIf="ajustes().length > 0">
          <table>
            <thead>
              <tr>
                <th>Número</th>
                <th>Fecha</th>
                <th>Estado</th>
                <th>Motivo</th>
                <th>Detalles</th>
                <th>Impacto</th>
                <th class="actions-column">Acciones</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let ajuste of ajustes(); trackBy: trackById">
                <td data-label="Número"><strong>{{ ajuste.numeroAjuste }}</strong></td>
                <td data-label="Fecha">{{ ajuste.fechaAjuste | date:'dd/MM/yyyy HH:mm' }}</td>
                <td data-label="Estado">
                  <span class="status" [class]="'status ' + ajuste.estado.toLowerCase()">{{ ajuste.estado }}</span>
                </td>
                <td data-label="Motivo">{{ ajuste.motivo }}</td>
                <td data-label="Detalles">{{ ajuste.detalles.length }}</td>
                <td data-label="Impacto">{{ (ajuste.impactoCostoTotalSnapshot || 0) | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                <td data-label="Acciones" class="row-actions">
                  <button mat-button type="button" [disabled]="processingId() !== null" (click)="ver(ajuste)">
                    <mat-icon>visibility</mat-icon>
                    Ver
                  </button>
                  <button
                    mat-button
                    color="primary"
                    type="button"
                    *ngIf="puedeEditar() && ajuste.estado === 'Borrador'"
                    [disabled]="processingId() !== null"
                    (click)="editar(ajuste)">
                    <mat-icon>edit</mat-icon>
                    Editar
                  </button>
                  <button
                    mat-stroked-button
                    color="primary"
                    type="button"
                    *ngIf="puedeConfirmar() && ajuste.estado === 'Borrador'"
                    [disabled]="processingId() === ajuste.id"
                    (click)="confirmar(ajuste)">
                    <mat-icon>check_circle</mat-icon>
                    Confirmar
                  </button>
                  <button
                    mat-stroked-button
                    color="warn"
                    type="button"
                    *ngIf="puedeAnular() && ajuste.estado === 'Confirmado'"
                    [disabled]="processingId() === ajuste.id"
                    (click)="anular(ajuste)">
                    <mat-icon>undo</mat-icon>
                    Anular
                  </button>
                  <span class="muted" *ngIf="ajuste.estado === 'Anulado'">Solo lectura</span>
                </td>
              </tr>
            </tbody>
          </table>

          <mat-paginator
            [length]="totalCount()"
            [pageIndex]="page - 1"
            [pageSize]="pageSize"
            [pageSizeOptions]="[10, 25, 50]"
            showFirstLastButtons
            (page)="onPageChange($event)">
          </mat-paginator>
        </div>
      </ng-container>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .ajustes-page { padding: 24px; max-width: 1500px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 24px; }
    .page-actions { display: flex; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }
    .eyebrow { margin: 0 0 4px; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .08em; opacity: .65; }
    h1 { margin: 0; font-size: clamp(24px, 3vw, 34px); }
    .subtitle { margin: 6px 0 0; opacity: .72; }
    .filters { display: grid; grid-template-columns: minmax(220px, 2fr) repeat(3, minmax(150px, 1fr)) auto; gap: 12px; align-items: start; margin-bottom: 18px; }
    .filter-actions { display: flex; gap: 8px; min-height: 56px; align-items: center; }
    .feedback, .loading, .empty { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 28px; border-radius: 12px; }
    .feedback.error { justify-content: flex-start; border: 1px solid rgba(244,67,54,.32); background: rgba(244,67,54,.06); }
    .empty { min-height: 220px; flex-direction: column; text-align: center; border: 1px dashed rgba(127,127,127,.35); }
    .empty h2, .empty p { margin: 0; }
    .empty mat-icon { width: 42px; height: 42px; font-size: 42px; opacity: .5; }
    .table-shell { overflow-x: auto; border: 1px solid rgba(127,127,127,.22); border-radius: 12px; }
    table { width: 100%; border-collapse: collapse; min-width: 1120px; }
    th, td { padding: 14px 16px; text-align: left; border-bottom: 1px solid rgba(127,127,127,.16); vertical-align: middle; }
    th { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; opacity: .72; }
    tbody tr:last-child td { border-bottom: 0; }
    .status { display: inline-flex; padding: 4px 9px; border-radius: 999px; font-size: 12px; font-weight: 700; background: rgba(127,127,127,.14); }
    .status.confirmado { background: rgba(46,125,50,.14); }
    .status.anulado { background: rgba(198,40,40,.14); }
    .status.borrador { background: rgba(245,124,0,.14); }
    .actions-column { width: 310px; }
    .row-actions { display: flex; align-items: center; gap: 6px; white-space: nowrap; }
    .muted { opacity: .55; font-size: 13px; }
    @media (max-width: 1050px) { .filters { grid-template-columns: repeat(2, minmax(0, 1fr)); } .filter-actions { grid-column: span 2; } }
    @media (max-width: 640px) { .ajustes-page { padding: 16px; } .page-header { flex-direction: column; } .page-actions { width: 100%; justify-content: flex-start; } .filters { grid-template-columns: 1fr; } .filter-actions { grid-column: auto; } }
  `]
})
export class AjustesListComponent implements OnInit {
  readonly ajustes = signal<AjusteInventario[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly processingId = signal<number | null>(null);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeConfirmar = signal(false);
  readonly puedeAnular = signal(false);

  search = '';
  estado: '' | EstadoAjusteInventario = '';
  desde = '';
  hasta = '';
  page = 1;
  pageSize = 10;

  constructor(
    private readonly ajusteService: AjusteInventarioService,
    private readonly permisosRuntime: PermisosRuntimeService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Inventario', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Inventario', 'Editar'));
    this.puedeConfirmar.set(this.permisosRuntime.puede('Inventario', 'Confirmar'));
    this.puedeAnular.set(this.permisosRuntime.puede('Inventario', 'Anular'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set('');

    const filtro: AjusteInventarioFiltro = {
      page: this.page,
      pageSize: this.pageSize,
      search: this.search.trim() || undefined,
      estado: this.estado || undefined,
      desde: this.desde || undefined,
      hasta: this.hasta || undefined,
      sortBy: 'fechaAjuste',
      sortDirection: 'desc'
    };

    this.ajusteService.getPaged(filtro)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.ajustes.set([]);
            this.totalCount.set(0);
            this.error.set(this.extraerRespuestaFallida(response, 'No fue posible cargar los ajustes.'));
            return;
          }

          this.ajustes.set(response.data.items);
          this.totalCount.set(response.data.totalCount);
          this.page = response.data.page;
          this.pageSize = response.data.pageSize;
        },
        error: (err) => {
          this.ajustes.set([]);
          this.totalCount.set(0);
          this.error.set(this.extraerError(err, 'No fue posible cargar los ajustes.'));
        }
      });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.search = '';
    this.estado = '';
    this.desde = '';
    this.hasta = '';
    this.page = 1;
    this.pageSize = 10;
    this.cargar();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  nuevo(): void {
    if (!this.puedeCrear()) return;
    void this.router.navigate(['/inventario/ajustes/nuevo']);
  }

  ver(ajuste: AjusteInventario): void {
    void this.router.navigate(['/inventario/ajustes', ajuste.id]);
  }

  editar(ajuste: AjusteInventario): void {
    if (!this.puedeEditar() || ajuste.estado !== 'Borrador') return;
    void this.router.navigate(['/inventario/ajustes', ajuste.id, 'editar']);
  }

  confirmar(ajuste: AjusteInventario): void {
    if (!this.puedeConfirmar() || ajuste.estado !== 'Borrador' || this.processingId() !== null) return;
    if (!window.confirm(`¿Confirmar el ajuste ${ajuste.numeroAjuste}? Esta operación aplicará el inventario.`)) return;

    this.processingId.set(ajuste.id);
    this.error.set('');
    this.ajusteService.confirmar(ajuste.id)
      .pipe(finalize(() => this.processingId.set(null)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.error.set(this.extraerRespuestaFallida(response, 'No fue posible confirmar el ajuste.'));
            return;
          }
          this.cargar();
        },
        error: (err) => this.error.set(this.extraerError(err, 'No fue posible confirmar el ajuste.'))
      });
  }

  anular(ajuste: AjusteInventario): void {
    if (!this.puedeAnular() || ajuste.estado !== 'Confirmado' || this.processingId() !== null) return;

    const motivo = window.prompt(`Motivo obligatorio para anular ${ajuste.numeroAjuste}:`, '')?.trim() ?? '';
    if (!motivo) {
      this.error.set('Debes indicar un motivo para anular un ajuste confirmado.');
      return;
    }

    this.processingId.set(ajuste.id);
    this.error.set('');
    this.ajusteService.anular(ajuste.id, motivo)
      .pipe(finalize(() => this.processingId.set(null)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.error.set(this.extraerRespuestaFallida(response, 'No fue posible anular el ajuste.'));
            return;
          }
          this.cargar();
        },
        error: (err) => this.error.set(this.extraerError(err, 'No fue posible anular el ajuste.'))
      });
  }

  trackById(_: number, ajuste: AjusteInventario): number {
    return ajuste.id;
  }

  private extraerRespuestaFallida(response: { message?: string; errors?: string[] }, fallback: string): string {
    if (typeof response.message === 'string' && response.message.trim()) return response.message;
    if (Array.isArray(response.errors) && response.errors.length) return response.errors.join(' ');
    return fallback;
  }

  private extraerError(error: any, fallback: string): string {
    const api = error?.error;
    if (typeof api?.message === 'string' && api.message.trim()) return api.message;
    if (Array.isArray(api?.errors) && api.errors.length) return api.errors.join(' ');
    return fallback;
  }
}
