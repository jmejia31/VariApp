import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  EstadoFinanciero,
  TipoEstadoFinanciero,
} from '../../core/models/estado-financiero.model';
import { EstadoFinancieroService } from '../../services/estado-financiero.service';

@Component({
  selector: 'app-estados-financieros',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './estados-financieros.component.html',
  styleUrl: './estados-financieros.component.scss',
})
export class EstadosFinancierosComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(EstadoFinancieroService);

  readonly tipos = [
    [TipoEstadoFinanciero.BalanceGeneral, 'Balance general'],
    [TipoEstadoFinanciero.EstadoResultados, 'Estado de resultados'],
    [TipoEstadoFinanciero.BalanceComprobacion, 'Balance de comprobación'],
    [TipoEstadoFinanciero.LibroDiario, 'Libro diario'],
    [TipoEstadoFinanciero.LibroMayor, 'Libro mayor'],
    [TipoEstadoFinanciero.FlujoEfectivo, 'Flujo de efectivo'],
  ] as const;

  readonly form = this.fb.group({
    tipo: [TipoEstadoFinanciero.BalanceGeneral, Validators.required],
    modo: ['periodo' as 'periodo' | 'rango', Validators.required],
    periodoContableId: [null as number | null],
    fechaDesde: [''],
    fechaHasta: [''],
  });

  loading = false;
  error = '';
  resultado: EstadoFinanciero | null = null;

  cambiarModo(): void {
    this.error = '';
    this.resultado = null;
    if (this.form.controls.modo.value === 'periodo') {
      this.form.patchValue({ fechaDesde: '', fechaHasta: '' });
    } else {
      this.form.patchValue({ periodoContableId: null });
    }
  }

  generar(): void {
    this.error = '';
    this.resultado = null;
    const value = this.form.getRawValue();

    if (value.modo === 'periodo') {
      if (!value.periodoContableId || value.periodoContableId <= 0) {
        this.error = 'Seleccione un período contable válido.';
        return;
      }
    } else {
      if (!value.fechaDesde || !value.fechaHasta) {
        this.error = 'Indique el rango completo de fechas.';
        return;
      }
      if (value.fechaDesde > value.fechaHasta) {
        this.error = 'La fecha final no puede ser anterior a la inicial.';
        return;
      }
    }

    this.loading = true;
    this.service.generar(value.tipo!, value.modo === 'periodo'
      ? { periodoContableId: value.periodoContableId! }
      : { fechaDesde: value.fechaDesde!, fechaHasta: value.fechaHasta! })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.error = response.message || 'No fue posible generar el estado financiero.';
            return;
          }
          this.resultado = response.data;
        },
        error: () => {
          this.error = 'No fue posible generar el estado financiero. Intente nuevamente.';
        },
      });
  }

  trackLinea(_: number, linea: { cuentaContableId: number }): number {
    return linea.cuentaContableId;
  }
}
