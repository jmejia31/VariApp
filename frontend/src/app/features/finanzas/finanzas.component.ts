import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { FinanzasService } from '../../services/finanzas.service';
import { FinanzasResumen, MovimientoFinanciero, RevisionFinanciera } from '../../core/models/finanzas.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-finanzas',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule
  ],
  templateUrl: './finanzas.component.html',
  styleUrl: './finanzas.component.scss'
})
export class FinanzasComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly resumen = signal<FinanzasResumen | null>(null);
  readonly movimientos = signal<MovimientoFinanciero[]>([]);
  readonly revisiones = signal<RevisionFinanciera[]>([]);
  readonly loading = signal(true);
  readonly mostrarFormMovimiento = signal(false);
  readonly mostrarFormRevision = signal(false);
  readonly esAdministrador = this.permisosRuntime.esAdministrador;
  readonly puedeCrearMovimiento = signal(false);
  readonly puedeAnularMovimiento = signal(false);

  readonly movimientoForm = this.fb.group({
    tipo: ['Egreso', Validators.required],
    categoria: ['GastoOperativo', Validators.required],
    concepto: ['', Validators.required],
    descripcion: [''],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    metodoPago: ['Efectivo']
  });

  readonly revisionForm = this.fb.group({
    fechaDesde: ['', Validators.required],
    fechaHasta: ['', Validators.required],
    estadoRevision: ['Revisado', Validators.required],
    observaciones: ['']
  });

  constructor(private finanzasService: FinanzasService, private dialog: MatDialog) {}

  ngOnInit(): void {
    this.puedeCrearMovimiento.set(this.permisosRuntime.puede('Finanzas', 'Crear'));
    this.puedeAnularMovimiento.set(this.permisosRuntime.puede('Finanzas', 'Anular'));
    this.cargarTodo();
  }

  cargarTodo(): void {
    this.loading.set(true);

    this.finanzasService.getResumen().subscribe({
      next: (res) => {
        this.resumen.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.finanzasService.getMovimientos().subscribe({
      next: (res) => this.movimientos.set(res.data),
      error: () => this.movimientos.set([])
    });

    if (this.esAdministrador()) {
      this.finanzasService.getRevisiones().subscribe({
        next: (res) => this.revisiones.set(res.data),
        error: () => this.revisiones.set([])
      });
    } else {
      this.revisiones.set([]);
      this.mostrarFormRevision.set(false);
    }
  }

  registrarMovimiento(): void {
    if (!this.puedeCrearMovimiento() || this.movimientoForm.invalid) return;

    this.finanzasService.registrarManual(this.movimientoForm.getRawValue() as any).subscribe(() => {
      this.movimientoForm.reset({ tipo: 'Egreso', categoria: 'GastoOperativo', metodoPago: 'Efectivo' });
      this.mostrarFormMovimiento.set(false);
      this.cargarTodo();
    });
  }

  anularMovimiento(m: MovimientoFinanciero): void {
    if (!this.puedeAnularMovimiento()) return;

    const ref = this.dialog.open(AnularDialogComponent, {
      data: { title: 'Anular movimiento', message: `¿Anular "${m.concepto}"?` }
    });
    ref.afterClosed().subscribe((motivo: string | undefined) => {
      if (!motivo) return;
      this.finanzasService.anularMovimiento(m.id, motivo).subscribe(() => this.cargarTodo());
    });
  }

  registrarRevision(): void {
    if (!this.esAdministrador() || !this.puedeCrearMovimiento() || this.revisionForm.invalid) return;

    this.finanzasService.registrarRevision(this.revisionForm.getRawValue() as any).subscribe(() => {
      this.revisionForm.reset({ estadoRevision: 'Revisado' });
      this.mostrarFormRevision.set(false);
      this.cargarTodo();
    });
  }
}
