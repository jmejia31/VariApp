import { Injectable, Component, Inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';

export interface AppAlertData {
  titulo: string;
  mensaje: string;
  detalle?: string;
  tipo?: 'info' | 'advertencia' | 'peligro';
  confirmarTexto?: string;
  cancelarTexto?: string;
  entrada?: { etiqueta: string; valor?: string; requerida?: boolean };
}

@Component({
  selector: 'app-alert-dialog',
  standalone: true,
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  template: `
    <section class="app-alert" [class]="'app-alert app-alert--' + (data.tipo || 'info')">
      <header><mat-icon>{{ icono }}</mat-icon><div><h2>{{ data.titulo }}</h2><p>{{ data.mensaje }}</p></div></header>
      @if (data.detalle) { <div class="app-alert__detail">{{ data.detalle }}</div> }
      @if (data.entrada) {
        <mat-form-field appearance="outline">
          <mat-label>{{ data.entrada.etiqueta }}</mat-label>
          <input matInput [(ngModel)]="valor" [required]="!!data.entrada.requerida" autofocus>
        </mat-form-field>
      }
      <footer>
        <button mat-button type="button" (click)="ref.close(null)">{{ data.cancelarTexto || 'Cancelar' }}</button>
        <button mat-flat-button type="button" [disabled]="!!data.entrada?.requerida && !valor.trim()" (click)="confirmar()">
          {{ data.confirmarTexto || 'Confirmar' }}
        </button>
      </footer>
    </section>
  `,
  styles: [`
    .app-alert{padding:24px;max-width:480px}.app-alert header{display:flex;gap:14px;align-items:flex-start}
    .app-alert h2{margin:0 0 6px;font-size:20px}.app-alert p{margin:0;line-height:1.5;color:var(--color-texto-secundario)}
    .app-alert mat-icon{color:var(--color-informacion);margin-top:2px}.app-alert--advertencia mat-icon{color:var(--color-advertencia)}
    .app-alert--peligro mat-icon{color:var(--color-error)}.app-alert__detail{margin:18px 0;padding:12px;background:var(--color-fondo-principal);border-radius:6px}
    mat-form-field{display:block;margin-top:20px}footer{display:flex;justify-content:flex-end;gap:8px;margin-top:22px}
    footer button[mat-flat-button]{background:var(--color-botones-principales);color:#fff}
  `]
})
export class AppAlertDialogComponent {
  valor: string;

  constructor(@Inject(MAT_DIALOG_DATA) public data: AppAlertData, public ref: MatDialogRef<AppAlertDialogComponent>) {
    this.valor = data.entrada?.valor ?? '';
  }
  get icono(): string { return this.data.tipo === 'peligro' ? 'delete_forever' : this.data.tipo === 'advertencia' ? 'warning' : 'info'; }
  confirmar(): void { this.ref.close(this.data.entrada ? this.valor.trim() : true); }
}

@Injectable({ providedIn: 'root' })
export class AppAlertService {
  constructor(private dialog: MatDialog) {}

  async confirmar(data: AppAlertData): Promise<boolean> {
    const result = await firstValueFrom(this.dialog.open(AppAlertDialogComponent, { data, width: 'min(92vw, 520px)', autoFocus: false }).afterClosed());
    return result === true;
  }

  async solicitarTexto(data: AppAlertData): Promise<string | null> {
    const result = await firstValueFrom(this.dialog.open(AppAlertDialogComponent, { data, width: 'min(92vw, 520px)', autoFocus: false }).afterClosed());
    return typeof result === 'string' && result ? result : null;
  }
}
