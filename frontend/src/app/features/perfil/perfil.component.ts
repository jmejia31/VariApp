import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PerfilService } from '../../services/perfil.service';
import { Perfil } from '../../core/models/perfil.model';

function passwordsIguales(control: AbstractControl): ValidationErrors | null {
  const nueva = control.get('passwordNueva')?.value;
  const confirmacion = control.get('passwordConfirmacion')?.value;
  return nueva && confirmacion && nueva !== confirmacion ? { noCoincide: true } : null;
}

@Component({
  selector: 'app-perfil',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './perfil.component.html',
  styleUrl: './perfil.component.scss'
})
export class PerfilComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly cambiandoPassword = signal(false);
  readonly perfil = signal<Perfil | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly errorPassword = signal<string | null>(null);
  readonly successPassword = signal<string | null>(null);

  formPerfil = this.fb.group({
    nombreCompleto: ['', Validators.required]
  });

  formPassword = this.fb.group({
    passwordActual: ['', Validators.required],
    passwordNueva: ['', [Validators.required, Validators.minLength(8)]],
    passwordConfirmacion: ['', Validators.required]
  }, { validators: passwordsIguales });

  constructor(private perfilService: PerfilService, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.perfilService.get().subscribe({
      next: (res) => {
        this.perfil.set(res.data);
        this.formPerfil.patchValue({ nombreCompleto: res.data.nombreCompleto });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  guardarPerfil(): void {
    if (this.formPerfil.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.perfilService.update({ nombreCompleto: this.formPerfil.value.nombreCompleto! }).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.perfil.set(res.data);
        this.successMessage.set('Perfil actualizado correctamente.');
        this.snackBar.open('Perfil actualizado.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo actualizar el perfil.');
      }
    });
  }

  cambiarPassword(): void {
    if (this.formPassword.invalid) return;

    this.cambiandoPassword.set(true);
    this.errorPassword.set(null);
    this.successPassword.set(null);

    const v = this.formPassword.value;
    this.perfilService.cambiarPassword({
      passwordActual: v.passwordActual!,
      passwordNueva: v.passwordNueva!
    }).subscribe({
      next: () => {
        this.cambiandoPassword.set(false);
        this.successPassword.set('Contraseña actualizada correctamente.');
        this.formPassword.reset();
        this.snackBar.open('Contraseña actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.cambiandoPassword.set(false);
        this.errorPassword.set(err.error?.message ?? 'No se pudo cambiar la contraseña.');
      }
    });
  }
}
