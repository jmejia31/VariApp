import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { SessionActivityService } from '../../core/auth/session-activity.service';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly permisosRuntime = inject(PermisosRuntimeService);
  private readonly sessionActivity = inject(SessionActivityService);
  readonly identidad = inject(EmpresaIdentidadService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hidePassword = signal(true);

  readonly form = this.fb.group({
    nombreUsuario: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required, Validators.maxLength(128)]]
  });

  constructor() {
    if (this.authService.isAuthenticated()) {
      this.router.navigateByUrl('/dashboard', { replaceUrl: true });
      return;
    }

    this.identidad.cargar().subscribe();
    const mensaje = this.sessionActivity.tomarMensajePendiente();
    if (mensaje) this.errorMessage.set(mensaje);
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const valor = this.form.getRawValue();
    this.authService.login({
      nombreUsuario: valor.nombreUsuario!.trim(),
      password: valor.password!
    }).subscribe({
      next: () => {
        this.permisosRuntime.cargar().subscribe({
          next: () => {
            this.loading.set(false);
            this.sessionActivity.iniciar();
            const ruta = this.permisosRuntime.rutaInicialPermitida() ?? '/perfil';
            this.router.navigateByUrl(ruta, { replaceUrl: true });
          },
          error: () => {
            this.loading.set(false);
            this.sessionActivity.iniciar();
            this.router.navigateByUrl('/perfil', { replaceUrl: true });
          }
        });
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo iniciar sesión. Verifica tus credenciales e intenta nuevamente.');
      }
    });
  }
}