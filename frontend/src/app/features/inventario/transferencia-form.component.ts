import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TransferenciaInventarioDetalleInput, TransferenciaInventarioFormValue } from '../../core/models/transferencia-inventario.model';
import { TransferenciaInventarioService } from '../../services/transferencia-inventario.service';

@Component({
  selector: 'app-transferencia-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  template: `
    <section class="page" aria-labelledby="transferencia-form-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="transferencia-form-title">{{ editando ? 'Editar transferencia' : 'Nueva transferencia' }}</h1>
          <p>Define origen, destino y cantidades solicitadas. El lifecycle se ejecuta desde el detalle.</p>
        </div>
        <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Volver</button>
      </header>

      <div class="state" *ngIf="loading"><mat-spinner diameter="36"></mat-spinner><span>Cargando…</span></div>
      <form *ngIf="!loading" class="card" (ngSubmit)="guardar()">
        <div class="grid two">
          <mat-form-field appearance="outline">
            <mat-label>Almacén origen</mat-label>
            <input matInput type="number" min="1" name="almacenOrigenId" [(ngModel)]="model.almacenOrigenId" required />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Almacén destino</mat-label>
            <input matInput type="number" min="1" name="almacenDestinoId" [(ngModel)]="model.almacenDestinoId" required />
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="full">
          <mat-label>Observaciones</mat-label>
          <textarea matInput rows="3" name="observaciones" [(ngModel)]="model.observaciones"></textarea>
        </mat-form-field>

        <div class="details-header">
          <div><h2>Detalle solicitado</h2><p>Una línea por variante y contexto físico.</p></div>
          <button mat-stroked-button type="button" (click)="agregarDetalle()"><mat-icon>add</mat-icon>Agregar línea</button>
        </div>

        <div class="detail" *ngFor="let detalle of model.detalles; let i = index; trackBy: trackByIndex">
          <div class="grid detail-grid">
            <mat-form-field appearance="outline"><mat-label>Variante</mat-label><input matInput type="number" min="1" [name]="'variante-' + i" [(ngModel)]="detalle.productoVarianteId" required /></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Ubicación origen</mat-label><input matInput type="number" min="1" [name]="'ubicacion-origen-' + i" [(ngModel)]="detalle.ubicacionOrigenId" /></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Ubicación destino</mat-label><input matInput type="number" min="1" [name]="'ubicacion-destino-' + i" [(ngModel)]="detalle.ubicacionDestinoId" /></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Cantidad</mat-label><input matInput type="number" min="1" [name]="'cantidad-' + i" [(ngModel)]="detalle.cantidadSolicitada" required /></mat-form-field>
            <button mat-icon-button type="button" aria-label="Eliminar línea" (click)="quitarDetalle(i)" [disabled]="model.detalles.length === 1"><mat-icon>delete</mat-icon></button>
          </div>
        </div>

        <div class="error" *ngIf="error">{{ error }}</div>
        <div class="actions">
          <button mat-stroked-button type="button" (click)="volver()" [disabled]="saving">Cancelar</button>
          <button mat-flat-button color="primary" type="submit" [disabled]="saving">
            <mat-spinner *ngIf="saving" diameter="20"></mat-spinner>
            <span *ngIf="!saving">{{ editando ? 'Guardar cambios' : 'Crear transferencia' }}</span>
          </button>
        </div>
      </form>
    </section>
  `,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:12px;font-weight:700;margin:0}.header h1{margin:4px 0}.header p,.details-header p{margin:0;color:var(--text-secondary,#667085)}.card{display:grid;gap:18px;padding:20px;border:1px solid rgba(0,0,0,.12);border-radius:14px}.grid{display:grid;gap:12px}.two{grid-template-columns:1fr 1fr}.detail-grid{grid-template-columns:1.2fr 1fr 1fr .8fr auto;align-items:start}.full{width:100%}.details-header{display:flex;justify-content:space-between;align-items:center;gap:16px}.details-header h2{margin:0}.detail{padding:14px;border:1px solid rgba(0,0,0,.08);border-radius:10px}.actions{display:flex;justify-content:flex-end;gap:10px}.error{color:#b42318}.state{min-height:160px;display:flex;justify-content:center;align-items:center;gap:12px}@media(max-width:900px){.detail-grid{grid-template-columns:1fr 1fr}.two{grid-template-columns:1fr}}@media(max-width:600px){.page{padding:16px}.header,.details-header{flex-direction:column;align-items:stretch}.detail-grid{grid-template-columns:1fr}}
  `]
})
export class TransferenciaFormComponent implements OnInit {
  readonly id = Number(this.route.snapshot.paramMap.get('id')) || 0;
  readonly editando = this.id > 0;
  loading = false;
  saving = false;
  error = '';
  model: TransferenciaInventarioFormValue = this.nuevoModelo();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly service: TransferenciaInventarioService
  ) {}

  ngOnInit(): void {
    if (!this.editando) return;
    this.loading = true;
    this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo cargar la transferencia.'; return; }
        if (response.data.estado !== 'Borrador') { this.error = 'Sólo las transferencias en borrador pueden editarse.'; return; }
        this.model = {
          almacenOrigenId: response.data.almacenOrigenId,
          almacenDestinoId: response.data.almacenDestinoId,
          observaciones: response.data.observaciones,
          detalles: response.data.detalles.map(item => ({
            productoVarianteId: item.productoVarianteId,
            ubicacionOrigenId: item.ubicacionOrigenId ?? null,
            ubicacionDestinoId: item.ubicacionDestinoId ?? null,
            cantidadSolicitada: item.cantidadSolicitada
          }))
        };
      },
      error: () => this.error = 'No se pudo cargar la transferencia.'
    });
  }

  guardar(): void {
    this.error = '';
    if (!this.esValido()) return;
    this.saving = true;
    const operation = this.editando ? this.service.update(this.id, this.model) : this.service.create(this.model);
    operation.pipe(finalize(() => this.saving = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo guardar la transferencia.'; return; }
        void this.router.navigate(['/inventario/transferencias', response.data.id]);
      },
      error: error => this.error = error?.error?.message || 'No se pudo guardar la transferencia.'
    });
  }

  agregarDetalle(): void { this.model.detalles.push(this.nuevoDetalle()); }
  quitarDetalle(index: number): void { if (this.model.detalles.length > 1) this.model.detalles.splice(index, 1); }
  trackByIndex(index: number): number { return index; }
  volver(): void { void this.router.navigate(['/inventario/transferencias']); }

  private esValido(): boolean {
    if (this.model.almacenOrigenId <= 0 || this.model.almacenDestinoId <= 0) { this.error = 'Debes indicar almacenes válidos.'; return false; }
    if (this.model.almacenOrigenId === this.model.almacenDestinoId) { this.error = 'El almacén de origen y destino deben ser diferentes.'; return false; }
    if (!this.model.detalles.length || this.model.detalles.some(item => item.productoVarianteId <= 0 || item.cantidadSolicitada <= 0)) { this.error = 'Cada línea debe tener variante y cantidad positiva.'; return false; }
    return true;
  }

  private nuevoModelo(): TransferenciaInventarioFormValue {
    return { almacenOrigenId: 0, almacenDestinoId: 0, observaciones: '', detalles: [this.nuevoDetalle()] };
  }

  private nuevoDetalle(): TransferenciaInventarioDetalleInput {
    return { productoVarianteId: 0, ubicacionOrigenId: null, ubicacionDestinoId: null, cantidadSolicitada: 1 };
  }
}
