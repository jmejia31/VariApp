import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Factura, FacturaPago, RegistrarFacturaPago } from '../../core/models/factura.model';
import { FacturaService } from '../../services/factura.service';

@Component({
  selector: 'app-factura-pagos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './factura-pagos.component.html',
  styleUrl: './factura-pagos.component.scss'
})
export class FacturaPagosComponent implements OnInit {
  readonly factura = signal<Factura | null>(null);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly anulandoId = signal<number | null>(null);

  readonly metodosPago = ['Efectivo', 'Transferencia', 'Tarjeta', 'Otro'];

  pago: RegistrarFacturaPago = {
    monto: 0,
    metodoPago: 'Efectivo',
    referencia: '',
    observaciones: ''
  };

  constructor(
    private readonly route: ActivatedRoute,
    private readonly facturaService: FacturaService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.cargando.set(true);
    this.facturaService.getById(id).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        this.pago.monto = Math.max(0, respuesta.data.saldoPendiente);
        this.cargando.set(false);
      },
      error: (error) => {
        this.cargando.set(false);
        this.snackBar.open(error.error?.message ?? 'No se pudo cargar la factura.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  registrarPago(): void {
    const factura = this.factura();
    if (!factura || this.guardando() || this.pago.monto <= 0) return;

    this.guardando.set(true);
    this.facturaService.registrarPago(factura.id, {
      ...this.pago,
      referencia: this.pago.referencia?.trim() || undefined,
      observaciones: this.pago.observaciones?.trim() || undefined
    }).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        this.pago = {
          monto: Math.max(0, respuesta.data.saldoPendiente),
          metodoPago: 'Efectivo',
          referencia: '',
          observaciones: ''
        };
        this.guardando.set(false);
        this.snackBar.open('Pago registrado correctamente.', 'Cerrar', { duration: 3500 });
      },
      error: (error) => {
        this.guardando.set(false);
        this.snackBar.open(error.error?.message ?? 'No se pudo registrar el pago.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  anularPago(pago: FacturaPago): void {
    const factura = this.factura();
    if (!factura || pago.anulado || this.anulandoId() !== null) return;

    const motivo = window.prompt('Indique el motivo de anulación del pago:')?.trim();
    if (!motivo) return;

    this.anulandoId.set(pago.id);
    this.facturaService.anularPago(factura.id, pago.id, motivo).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        this.pago.monto = Math.max(0, respuesta.data.saldoPendiente);
        this.anulandoId.set(null);
        this.snackBar.open('Pago anulado y saldo recalculado.', 'Cerrar', { duration: 3500 });
      },
      error: (error) => {
        this.anulandoId.set(null);
        this.snackBar.open(error.error?.message ?? 'No se pudo anular el pago.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  puedeRegistrar(): boolean {
    const factura = this.factura();
    return !!factura
      && !['Anulada', 'Cancelada'].includes(factura.estado)
      && factura.saldoPendiente > 0
      && this.pago.monto > 0
      && this.pago.monto <= factura.saldoPendiente
      && !this.guardando();
  }
}
