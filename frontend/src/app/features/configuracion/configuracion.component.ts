import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EmpresaConfiguracionService } from '../../services/empresa-configuracion.service';
import { TemaVisualService } from '../../services/tema-visual.service';
import { ThemeApplierService } from '../../services/theme-applier.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CAMPOS_TEMA, TemaVisual } from '../../core/models/tema-visual.model';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, MatFormFieldModule, MatInputModule,
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

  // --- Tema visual (sección 16) ---
  readonly campos = CAMPOS_TEMA;
  readonly loadingTema = signal(true);
  readonly guardandoTema = signal(false);
  readonly restaurandoTema = signal(false);
  readonly errorTema = signal<string | null>(null);
  tema: TemaVisual = this.temaVacio();
  private temaOriginal: TemaVisual = this.temaVacio();

  constructor(
    private empresaService: EmpresaConfiguracionService,
    private temaVisualService: TemaVisualService,
    private themeApplier: ThemeApplierService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar
  ) {}

  private temaVacio(): TemaVisual {
    return Object.fromEntries(this.campos?.map((c) => [c.clave, '']) ?? []) as unknown as TemaVisual;
  }

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

    this.temaVisualService.get().subscribe({
      next: (res) => {
        this.tema = { ...res.data };
        this.temaOriginal = { ...res.data };
        this.loadingTema.set(false);
      },
      error: () => this.loadingTema.set(false)
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

  /** Vista previa inmediata (sección 16: "vista previa") sin persistir
   * todavía — solo aplica al DOM local, un refresh sin guardar la revierte. */
  previsualizar(): void {
    this.themeApplier.aplicar(this.tema);
  }

  guardarTema(): void {
    if (!confirm('¿Aplicar estos colores a todo el sistema para todos los usuarios?')) return;

    this.guardandoTema.set(true);
    this.errorTema.set(null);
    this.temaVisualService.update(this.tema).subscribe({
      next: (res) => {
        this.guardandoTema.set(false);
        this.tema = { ...res.data };
        this.temaOriginal = { ...res.data };
        this.themeApplier.aplicar(res.data);
        this.snackBar.open('Tema visual actualizado para todo el sistema.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.guardandoTema.set(false);
        this.errorTema.set(err.error?.message ?? 'No se pudo guardar el tema.');
      }
    });
  }

  cancelarTema(): void {
    this.tema = { ...this.temaOriginal };
    this.themeApplier.aplicar(this.tema);
  }

  restaurarTema(): void {
    if (!confirm('¿Restaurar los colores predeterminados de fábrica para todo el sistema?')) return;

    this.restaurandoTema.set(true);
    this.temaVisualService.restaurar().subscribe({
      next: (res) => {
        this.restaurandoTema.set(false);
        this.tema = { ...res.data };
        this.temaOriginal = { ...res.data };
        this.themeApplier.aplicar(res.data);
        this.snackBar.open('Tema visual restaurado a los valores predeterminados.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.restaurandoTema.set(false);
        this.errorTema.set(err.error?.message ?? 'No se pudo restaurar el tema.');
      }
    });
  }
}
