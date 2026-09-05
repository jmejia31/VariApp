import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Factura, FacturaPago, RegistrarFacturaPago } from '../../core/models/factura.model';
import { BancoLookup, MetodoPago } from '../../core/models/metodo-pago.model';
import { FacturaService } from '../../services/factura.service';
import { MetodoPagoService } from '../../services/metodo-pago.service';

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
  readonly cargandoCatalogos = signal(true);
  readonly guardando = signal(false);
  readonly anulandoId = signal<number | null>(null);
  readonly metodosPago = signal<MetodoPago[]>([]);
  readonly bancos = signal<BancoLookup[]>([]);

  pago: RegistrarFacturaPago = this.nuevoPago();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly facturaService: FacturaService,
    private readonly metodoPagoService: MetodoPagoService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.cargarCatalogos();
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

  cargarCatalogos(): void {
    this.cargandoCatalogos.set(true);
    forkJoin({
      metodos: this.metodoPagoService.getActivos(),
      bancos: this.metodoPagoService.getBancosActivos()
    }).subscribe({
      next: ({ metodos, bancos }) => {
        const activos = [...metodos.data]
          .filter((metodo) => metodo.activo)
          .sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo));
        this.metodosPago.set(activos);
        this.bancos.set([...bancos.data].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        this.normalizarMetodoPago();
        this.cargandoCatalogos.set(false);
      },
      error: () => {
        this.cargandoCatalogos.set(false);
        this.snackBar.open('No se pudieron cargar los métodos de pago y bancos activos.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  metodoSeleccionado(): MetodoPago | undefined {
    const valor = this.pago.metodoPago?.trim().toLocaleLowerCase('es');
    return this.metodosPago().find((metodo) =>
      metodo.codigo.toLocaleLowerCase('es') === valor
      || metodo.nombre.toLocaleLowerCase('es') === valor
    );
  }

  onMetodoPagoChange(): void {
    const metodo = this.metodoSeleccionado();
    if (!metodo?.requiereBanco) this.pago.bancoId = undefined;
  }

  cambioEstimado(): number {
    const factura = this.factura();
    const metodo = this.metodoSeleccionado();
    if (!factura || !metodo?.permiteCambio || this.pago.monto <= factura.saldoPendiente) return 0;
    return Math.round((this.pago.monto - factura.saldoPendiente) * 100) / 100;
  }

  registrarPago(): void {
    const factura = this.factura();
    if (!factura || !this.puedeRegistrar()) return;

    this.guardando.set(true);
    this.facturaService.registrarPago(factura.id, {
      ...this.pago,
      bancoId: this.pago.bancoId || undefined,
      referencia: this.pago.referencia?.trim() || undefined,
      observaciones: this.pago.observaciones?.trim() || undefined
    }).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        this.pago = this.nuevoPago(Math.max(0, respuesta.data.saldoPendiente));
        this.normalizarMetodoPago();
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
    const metodo = this.metodoSeleccionado();
    if (!factura || !metodo || this.cargandoCatalogos() || this.guardando()) return false;
    if (['Anulada', 'Cancelada'].includes(factura.estado) || factura.saldoPendiente <= 0 || this.pago.monto <= 0) return false;
    if (this.pago.monto > factura.saldoPendiente && !metodo.permiteCambio) return false;
    if (metodo.requiereReferencia && !this.pago.referencia?.trim()) return false;
    if (metodo.requiereBanco && (!this.pago.bancoId || this.pago.bancoId <= 0)) return false;
    return true;
  }

  private normalizarMetodoPago(): void {
    const activos = this.metodosPago();
    if (activos.length === 0) {
      this.pago.metodoPago = '';
      return;
    }

    const actual = this.metodoSeleccionado();
    if (!actual) this.pago.metodoPago = activos[0].codigo;
    this.onMetodoPagoChange();
  }

  private nuevoPago(monto = 0): RegistrarFacturaPago {
    return {
      monto,
      metodoPago: '',
      bancoId: undefined,
      referencia: '',
      observaciones: ''
    };
  }
}
