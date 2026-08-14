import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SucursalFormValue } from '../../core/models/sucursal.model';
import { SucursalService } from '../../services/sucursal.service';

@Component({
  selector: 'app-sucursal-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './sucursal-form.component.html',
  styleUrl: './sucursal-form.component.scss'
})
export class SucursalFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly zonasSugeridas = [
    'America/Tegucigalpa',
    'America/Guatemala',
    'America/El_Salvador',
    'America/Managua',
    'America/Costa_Rica',
    'America/Panama',
    'America/Mexico_City',
    'America/New_York'
  ];

  private sucursalId: number | null = null;

  readonly form = this.fb.group({
    empresaId: this.fb.control<number | null>(null, [Validators.min(1)]),
    codigo: ['', [Validators.required, Validators.maxLength(40)]],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    direccion: ['', [Validators.maxLength(500)]],
    telefono: ['', [Validators.maxLength(50)]],
    correo: ['', [Validators.email, Validators.maxLength(254)]],
    zonaHoraria: ['America/Tegucigalpa', [Validators.required, Validators.maxLength(100)]]
  });

  constructor(
    private sucursalService: SucursalService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    const id = Number(idParam);
    if (!Number.isInteger(id) || id <= 0) {
      this.errorMessage.set('El identificador de la sucursal no es válido.');
      this.form.disable();
      return;
    }

    this.isEdit.set(true);
    this.sucursalId = id;
    this.cargarSucursal(id);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving() || this.loading()) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    const value: SucursalFormValue = {
      empresaId: raw.empresaId && raw.empresaId > 0 ? raw.empresaId : null,
      codigo: raw.codigo?.trim() ?? '',
      nombre: raw.nombre?.trim() ?? '',
      direccion: this.opcional(raw.direccion),
      telefono: this.opcional(raw.telefono),
      correo: this.opcional(raw.correo),
      zonaHoraria: raw.zonaHoraria?.trim() || 'America/Tegucigalpa'
    };

    const request$ = this.isEdit()
      ? this.sucursalService.update(this.sucursalId!, value)
      : this.sucursalService.create(value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/sucursales']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la sucursal.');
      }
    });
  }

  private cargarSucursal(id: number): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.form.disable();

    this.sucursalService.getById(id).subscribe({
      next: (res) => {
        const sucursal = res.data;
        this.form.patchValue({
          empresaId: sucursal.empresaId ?? null,
          codigo: sucursal.codigo,
          nombre: sucursal.nombre,
          direccion: sucursal.direccion ?? '',
          telefono: sucursal.telefono ?? '',
          correo: sucursal.correo ?? '',
          zonaHoraria: sucursal.zonaHoraria
        });
        this.form.enable();
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar la sucursal.');
      }
    });
  }

  private opcional(value: string | null | undefined): string | null {
    const limpio = value?.trim();
    return limpio ? limpio : null;
  }
}
