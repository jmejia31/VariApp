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
import { RolService } from '../../services/rol.service';
import { Usuario } from '../../core/models/usuario.model';
import { Rol } from '../../core/models/rol.model';

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
  readonly roles = signal<Rol[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly mostrarFormulario = signal(false);

  form = this.fb.group({
    nombreUsuario: ['', Validators.required],
    nombreCompleto: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    rolId: [null as number | null, Validators.required]
  });

  constructor(private usuarioService: UsuarioService, private rolService: RolService) {}

  ngOnInit(): void {
    this.cargar();
    // Solo roles activos pueden asignarse a un usuario nuevo.
    this.rolService.getAll().subscribe((res) => this.roles.set(res.data.filter((r) => r.activo)));
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

    const valor = this.form.getRawValue();
    this.usuarioService.create({
      nombreUsuario: valor.nombreUsuario!,
      nombreCompleto: valor.nombreCompleto!,
      password: valor.password!,
      rol: 'Vendedor', // fallback legado; el backend prioriza rolId cuando se envía
      rolId: valor.rolId!
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.mostrarFormulario.set(false);
        this.form.reset();
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
