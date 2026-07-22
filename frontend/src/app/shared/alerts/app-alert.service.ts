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
      <header>
        <span class="app-alert__icon" aria-hidden="true"><mat-icon>{{ icono }}</mat-icon></span>
        <div>
          <h2>{{ data.titulo }}</h2>
          <p>{{ data.mensaje }}</p>
        </div>
      </header>

      @if (data.detalle) {
        <div class="app-alert__detail">{{ data.detalle }}</div>
      }

      @if (data.entrada) {
        <mat-form-field appearance="outline">
          <mat-label>{{ data.entrada.etiqueta }}</mat-label>
          <input
            matInput
            [(ngModel)]="valor"
            [required]="!!data.entrada.requerida"
            maxlength="300"
            autocomplete="off"
            autofocus>
          <mat-hint align="end">{{ valor.length }}/300</mat-hint>
        </mat-form-field>
      }

      <footer>
        <button mat-button type="button" (click)="ref.close(null)">{{ data.cancelarTexto || 'Cancelar' }}</button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="!!data.entrada?.requerida && !valor.trim()"
          (click)="confirmar()">
          {{ data.confirmarTexto || 'Confirmar' }}
        </button>
      </footer>
    </section>
  `,
  styles: [`
    .app-alert {
      width: min(100%, 520px);
      padding: clamp(18px, 4vw, 26px);
      color: var(--color-text);
    }

    .app-alert header {
      display: flex;
      gap: 14px;
      align-items: flex-start;
    }

    .app-alert__icon {
      width: 42px;
      height: 42px;
      flex: 0 0 42px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background: color-mix(in srgb, var(--color-info) 12%, var(--color-surface));
      color: var(--color-info);
    }

    .app-alert--advertencia .app-alert__icon {
      background: color-mix(in srgb, var(--color-warning) 13%, var(--color-surface));
      color: var(--color-warning);
    }

    .app-alert--peligro .app-alert__icon {
      background: color-mix(in srgb, var(--color-danger) 11%, var(--color-surface));
      color: var(--color-danger);
    }

    .app-alert h2 {
      margin: 0 0 6px;
      font-size: 20px;
      line-height: 1.2;
    }

    .app-alert p {
      margin: 0;
      color: var(--color-text-muted);
      line-height: 1.55;
      overflow-wrap: anywhere;
    }

    .app-alert__detail {
      margin: 18px 0 0;
      padding: 12px 14px;
      border: 1px solid var(--color-border);
      border-radius: 9px;
      background: var(--color-bg);
      color: var(--color-text-muted);
      line-height: 1.5;
      overflow-wrap: anywhere;
    }

    mat-form-field {
      display: block;
      width: 100%;
      margin-top: 20px;
    }

    footer {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-top: 22px;
    }

    @media (max-width: 480px) {
      .app-alert header {
        gap: 10px;
      }

      .app-alert__icon {
        width: 36px;
        height: 36px;
        flex-basis: 36px;
      }

      .app-alert h2 {
        font-size: 18px;
      }

      footer {
        flex-direction: column-reverse;
      }

      footer button {
        width: 100%;
      }
    }
  `]
})
export class AppAlertDialogComponent {
  valor: string;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: AppAlertData,
    public ref: MatDialogRef<AppAlertDialogComponent>
  ) {
    this.valor = data.entrada?.valor ?? '';
  }

  get icono(): string {
    return this.data.tipo === 'peligro'
      ? 'delete_forever'
      : this.data.tipo === 'advertencia'
        ? 'warning'
        : 'info';
  }

  confirmar(): void {
    this.ref.close(this.data.entrada ? this.valor.trim() : true);
  }
}

@Injectable({ providedIn: 'root' })
export class AppAlertService {
  constructor(private dialog: MatDialog) {}

  async confirmar(data: AppAlertData): Promise<boolean> {
    const result = await firstValueFrom(this.dialog.open(AppAlertDialogComponent, {
      data,
      width: 'min(94vw, 540px)',
      maxWidth: '94vw',
      autoFocus: false,
      restoreFocus: true
    }).afterClosed());
    return result === true;
  }

  async solicitarTexto(data: AppAlertData): Promise<string | null> {
    const result = await firstValueFrom(this.dialog.open(AppAlertDialogComponent, {
      data,
      width: 'min(94vw, 540px)',
      maxWidth: '94vw',
      autoFocus: false,
      restoreFocus: true
    }).afterClosed());
    return typeof result === 'string' && result ? result : null;
  }
}