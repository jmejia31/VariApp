import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EmpresaConfiguracionService } from '../../services/empresa-configuracion.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.scss'
})
export class ConfiguracionComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly puedeEditar = signal(false);
  readonly logoUrl = signal<string | undefined>(undefined);

  form = this.fb.group({
    nombreComercial: ['', Validators.required],
    eslogan: [''],
    rtn: [''],
    telefono: [''],
    correo: ['', Validators.email],
    direccion: ['']
  });

  constructor(
    private empresaService: EmpresaConfiguracionService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeEditar.set(this.permisosRuntime.puede('Configuracion', 'Editar'));
    if (!this.puedeEditar()) this.form.disable();

    this.empresaService.get().subscribe({
      next: (res) => {
        this.logoUrl.set(res.data.logoUrl);
        this.form.patchValue({
          nombreComercial: res.data.nombreComercial,
          eslogan: res.data.eslogan,
          rtn: res.data.rtn,
          telefono: res.data.telefono,
          correo: res.data.correo,
          direccion: res.data.direccion
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  guardar(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const v = this.form.getRawValue();
    this.empresaService.update({
      nombreComercial: v.nombreComercial!,
      eslogan: v.eslogan || '',
      rtn: v.rtn || undefined,
      telefono: v.telefono || undefined,
      correo: v.correo || undefined,
      direccion: v.direccion || undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set('Configuración guardada correctamente.');
        this.snackBar.open('Configuración de empresa actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la configuración.');
      }
    });
  }
}
