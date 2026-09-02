import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import {
  CreateCuentaBancariaDto,
  CuentaBancaria,
  CuentaBancariaQueryFilter,
  EstadoCuentaBancaria,
  UpdateCuentaBancariaDto
} from '../../core/models/cuenta-bancaria';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { CuentaBancariaService } from '../../core/services/cuenta-bancaria.service';

interface CuentasBancariasNavigationState {
  search: string;
  estadoFilter: EstadoCuentaBancaria | null;
}

@Component({
  selector: 'app-cuentas-bancarias',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './cuentas-bancarias.component.html',
  styleUrls: ['./cuentas-bancarias.component.scss']
})
export class CuentasBancariasComponent implements OnInit {
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly cuentaService = inject(CuentaBancariaService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly route = inject(ActivatedRoute);
  private readonly navigationState = inject(ListNavigationStateService);
  private readonly searchSubject = new Subject<string>();

  readonly cuentas = signal<CuentaBancaria[]>([]);
  readonly mostrarFormulario = signal(false);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly cuentaEnEdicion = signal<CuentaBancaria | null>(null);
  readonly columnasMostradas = ['nombre', 'numeroCuenta', 'moneda', 'estado', 'acciones'];
  readonly errorMessage = signal<string | null>(null);

  search = '';
  estadoFilter: EstadoCuentaBancaria | null = null;

  private readonly navigationDefaults: CuentasBancariasNavigationState = {
    search: '',
    estadoFilter: null
  };

  readonly formulario = this.fb.group({
    bancoId: [0, [Validators.required, Validators.min(1)]],
    nombre: ['', Validators.required],
    numeroCuenta: ['', Validators.required],
    moneda: ['HNL', Validators.required],
    saldoInicial: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.searchSubject.pipe(debounceTime(250)).subscribe(() => this.aplicarFiltros());
    effect(() => {
      if (!this.mostrarFormulario()) {
        this.cuentaEnEdicion.set(null);
        this.formulario.reset({ moneda: 'HNL', saldoInicial: 0, bancoId: 0, nombre: '', numeroCuenta: '' });
        this.errorMessage.set(null);
      }
    }, { allowSignalWrites: true });
  }

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('Finanzas', 'Crear'));
    this.puedeEditar.set(this.permisos.puede('Finanzas', 'Editar'));
    this.puedeActivar.set(this.permisos.puede('Finanzas', 'Activar'));
    this.puedeDesactivar.set(this.permisos.puede('Finanzas', 'Desactivar'));

    const state = this.navigationState.restore<CuentasBancariasNavigationState>(
      'cuentas-bancarias',
      this.route,
      this.navigationDefaults
    );
    this.search = state.search;
    this.estadoFilter = state.estadoFilter;

    this.cargarCuentas();
  }

  cargarCuentas(): void {
    this.errorMessage.set(null);
    this.loading.set(true);
    const filtros: CuentaBancariaQueryFilter = {
      page: 1,
      pageSize: 50,
      searchTerm: this.search || undefined,
      estado: this.estadoFilter ?? undefined
    };

    this.cuentaService.getAll(filtros).subscribe({
      next: (res) => {
        this.cuentas.set(res.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al cargar las cuentas.'));
      }
    });
    this.persistirEstado();
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  aplicarFiltros(): void {
    this.cargarCuentas();
  }

  limpiarFiltros(): void {
    this.search = '';
    this.estadoFilter = null;
    this.navigationState.clear('cuentas-bancarias');
    this.aplicarFiltros();
  }

  guardarCuenta(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    if (this.submitting()) return;

    this.errorMessage.set(null);
    this.submitting.set(true);
    const formValue = this.formulario.getRawValue();
    const cuentaEditada = this.cuentaEnEdicion();

    if (cuentaEditada) {
      const dto: UpdateCuentaBancariaDto = { nombre: formValue.nombre };
      this.cuentaService.update(cuentaEditada.id, dto)
        .pipe(finalize(() => this.submitting.set(false)))
        .subscribe({
          next: () => {
            this.mostrarFormulario.set(false);
            this.cuentaEnEdicion.set(null);
            this.formulario.reset({ moneda: 'HNL', saldoInicial: 0, bancoId: 0, nombre: '', numeroCuenta: '' });
            this.cargarCuentas();
          },
          error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al actualizar la cuenta.'))
        });
      return;
    }

    const dto: CreateCuentaBancariaDto = formValue;
    this.cuentaService.create(dto)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.mostrarFormulario.set(false);
          this.formulario.reset({ moneda: 'HNL', saldoInicial: 0, bancoId: 0, nombre: '', numeroCuenta: '' });
          this.cargarCuentas();
        },
        error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al crear la cuenta.'))
      });
  }

  editar(cuenta: CuentaBancaria): void {
    if (!this.puedeEditar()) return;
    this.cuentaEnEdicion.set(cuenta);
    this.formulario.patchValue({
      nombre: cuenta.nombre,
      numeroCuenta: cuenta.numeroCuenta,
      moneda: cuenta.moneda,
      saldoInicial: cuenta.saldoInicial,
      bancoId: cuenta.bancoId
    });
    this.mostrarFormulario.set(true);
  }

  activar(cuenta: CuentaBancaria): void {
    if (!this.puedeActivar()) return;
    this.errorMessage.set(null);
    this.cuentaService.activar(cuenta.id).subscribe({
      next: () => this.cargarCuentas(),
      error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al activar la cuenta.'))
    });
  }

  desactivar(cuenta: CuentaBancaria): void {
    if (!this.puedeDesactivar()) return;
    this.errorMessage.set(null);
    this.cuentaService.desactivar(cuenta.id).subscribe({
      next: () => this.cargarCuentas(),
      error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al desactivar la cuenta.'))
    });
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse && typeof err.error?.detail === 'string' && err.error.detail.trim().length > 0) {
      return err.error.detail.trim();
    }
    return fallback;
  }

  private persistirEstado(): void {
    const state: CuentasBancariasNavigationState = {
      search: this.search,
      estadoFilter: this.estadoFilter
    };
    this.navigationState.persist('cuentas-bancarias', this.route, state, this.navigationDefaults);
  }
}
