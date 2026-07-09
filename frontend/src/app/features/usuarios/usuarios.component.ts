import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UsuarioService } from '../../services/usuario.service';
import { Usuario } from '../../core/models/usuario.model';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatSlideToggleModule, MatProgressSpinnerModule
  ],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly usuarios = signal<Usuario[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly mostrarFormulario = signal(false);

  form = this.fb.group({
    nombreUsuario: ['', Validators.required],
    nombreCompleto: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    rol: ['Vendedor' as 'Administrador' | 'Vendedor', Validators.required]
  });

  constructor(private usuarioService: UsuarioService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.usuarioService.getAll().subscribe({
      next: (res) => {
        this.usuarios.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  crear(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    this.usuarioService.create(this.form.getRawValue() as any).subscribe({
      next: () => {
        this.saving.set(false);
        this.mostrarFormulario.set(false);
        this.form.reset({ rol: 'Vendedor' });
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo crear el usuario.');
      }
    });
  }

  toggleEstado(usuario: Usuario): void {
    this.usuarioService.updateEstado(usuario.id, !usuario.activo).subscribe(() => this.cargar());
  }
}
