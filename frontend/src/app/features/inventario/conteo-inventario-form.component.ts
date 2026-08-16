import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { ConteoInventarioFormValue, TipoConteoInventario } from '../../core/models/conteo-inventario.model';
import { ConteoInventarioService } from '../../services/conteo-inventario.service';

@Component({
  selector: 'app-conteo-inventario-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="conteo-form-title">
      <header><button mat-icon-button type="button" aria-label="Volver" (click)="volver()"><mat-icon>arrow_back</mat-icon></button><div><p class="eyebrow">Conteos físicos</p><h1 id="conteo-form-title">{{ id ? 'Editar borrador' : 'Nuevo conteo' }}</h1><p>Define el alcance físico antes de iniciar el conteo.</p></div></header>
      <form #form="ngForm" (ngSubmit)="guardar()" class="card" novalidate>
        <div class="grid">
          <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><mat-select required name="tipo" [(ngModel)]="model.tipo" (selectionChange)="sincronizarTipo()"><mat-option *ngFor="let item of tipos" [value]="item.value">{{ item.label }}</mat-option></mat-select></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Almacén ID</mat-label><input matInput required min="1" type="number" name="almacenId" [(ngModel)]="model.almacenId" /></mat-form-field>
          <mat-form-field *ngIf="requiereUbicacion" appearance="outline"><mat-label>Ubicación ID</mat-label><input matInput required min="1" type="number" name="ubicacionAlmacenId" [(ngModel)]="model.ubicacionAlmacenId" /></mat-form-field>
          <mat-form-field *ngIf="requiereCategoria" appearance="outline"><mat-label>Categoría ID</mat-label><input matInput required min="1" type="number" name="categoriaId" [(ngModel)]="model.categoriaId" /></mat-form-field>
        </div>
        <mat-checkbox name="esCiego" [(ngModel)]="model.esCiego">Ocultar stock esperado durante la captura (conteo ciego)</mat-checkbox>
        <mat-form-field appearance="outline" class="full"><mat-label>Variantes específicas</mat-label><input matInput name="variantes" [(ngModel)]="variantesTexto" placeholder="Ej. 101, 102, 103" /><mat-hint>Opcional para alcances automáticos; si se especifican, usa IDs separados por coma.</mat-hint></mat-form-field>
        <mat-form-field appearance="outline" class="full"><mat-label>Observaciones</mat-label><textarea matInput rows="4" name="observaciones" [(ngModel)]="model.observaciones"></textarea></mat-form-field>
        <div *ngIf="error" class="error" role="alert">{{ error }}</div>
        <div class="actions"><button mat-button type="button" [disabled]="saving" (click)="volver()">Cancelar</button><button mat-flat-button color="primary" type="submit" [disabled]="saving || form.invalid"><mat-spinner *ngIf="saving" diameter="20"></mat-spinner><span *ngIf="!saving">{{ id ? 'Guardar cambios' : 'Crear conteo' }}</span></button></div>
      </form>
    </section>
  `,
  styles: [`
    .page{max-width:980px;margin:0 auto;padding:24px;display:grid;gap:20px}header{display:flex;align-items:flex-start;gap:12px}header h1{margin:0}header p{margin:5px 0;color:#667085}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)!important}.card{display:grid;gap:18px;padding:24px;border:1px solid #e4e7ec;border-radius:14px;background:var(--surface,#fff)}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.full{width:100%}.actions{display:flex;justify-content:flex-end;gap:8px}.error{padding:12px;border-radius:8px;background:#fef3f2;color:#b42318}@media(max-width:640px){.page{padding:16px}.grid{grid-template-columns:1fr}.card{padding:16px}}
  `]
})
export class ConteoInventarioFormComponent implements OnInit {
  readonly tipos = [
    { value: TipoConteoInventario.General, label: 'General' },
    { value: TipoConteoInventario.Ciclico, label: 'Cíclico' },
    { value: TipoConteoInventario.PorUbicacion, label: 'Por ubicación' },
    { value: TipoConteoInventario.PorCategoria, label: 'Por categoría' },
    { value: TipoConteoInventario.Ciego, label: 'Ciego' }
  ];
  id: number | null = null;
  saving = false;
  error = '';
  variantesTexto = '';
  model: ConteoInventarioFormValue = { tipo: TipoConteoInventario.General, almacenId: 0, esCiego: false, productoVarianteIds: [], observaciones: '' };

  constructor(private readonly service: ConteoInventarioService, private readonly route: ActivatedRoute, private readonly router: Router) {}

  ngOnInit(): void {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    this.id = Number.isInteger(value) && value > 0 ? value : null;
    if (!this.id) return;
    this.service.getById(this.id).subscribe({ next: response => {
      if (!response.success) { this.error = response.message || 'No se pudo cargar el conteo.'; return; }
      const item = response.data;
      this.model = { tipo: item.tipo, almacenId: item.almacenId, ubicacionAlmacenId: item.ubicacionAlmacenId, categoriaId: item.categoriaId, esCiego: item.esCiego, observaciones: item.observaciones, productoVarianteIds: item.detalles.map(x => x.productoVarianteId) };
      this.variantesTexto = this.model.productoVarianteIds.join(', ');
    }, error: () => this.error = 'No se pudo cargar el conteo.' });
  }

  get requiereUbicacion(): boolean { return this.model.tipo === TipoConteoInventario.PorUbicacion; }
  get requiereCategoria(): boolean { return this.model.tipo === TipoConteoInventario.PorCategoria; }
  sincronizarTipo(): void { this.model.esCiego = this.model.tipo === TipoConteoInventario.Ciego || this.model.esCiego; if (!this.requiereUbicacion) this.model.ubicacionAlmacenId = null; if (!this.requiereCategoria) this.model.categoriaId = null; }

  guardar(): void {
    this.error = '';
    if (!this.model.almacenId || this.model.almacenId < 1) { this.error = 'Selecciona un almacén válido.'; return; }
    const ids = this.variantesTexto.split(',').map(x => Number(x.trim())).filter(x => Number.isInteger(x) && x > 0);
    const invalidos = this.variantesTexto.split(',').map(x => x.trim()).filter(Boolean).length !== ids.length;
    if (invalidos) { this.error = 'Las variantes deben ser IDs numéricos positivos separados por coma.'; return; }
    const value: ConteoInventarioFormValue = { ...this.model, productoVarianteIds: [...new Set(ids)] };
    this.saving = true;
    const request = this.id ? this.service.update(this.id, value) : this.service.create(value);
    request.pipe(finalize(() => this.saving = false)).subscribe({ next: response => { if (!response.success) { this.error = response.message || 'No se pudo guardar el conteo.'; return; } void this.router.navigate(['/inventario/conteos', response.data.id]); }, error: err => this.error = err?.error?.message || 'No se pudo guardar el conteo.' });
  }
  volver(): void { void this.router.navigate(['/inventario/conteos']); }
}
