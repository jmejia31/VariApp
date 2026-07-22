import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { UsuarioService } from '../../services/usuario.service';
import { RolService } from '../../services/rol.service';
import { Rol } from '../../core/models/rol.model';

@Component({
  selector: 'app-usuario-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatButtonModule,
    MatFormFieldModule, MatIconModule, MatInputModule,
    MatProgressSpinnerModule, MatSelectModule
  ],
  templateUrl: './usuario-form.component.html',
  styleUrl: './usuario-form.component.scss'
})
export class UsuarioFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly roles = signal<Rol[]>([]);
  readonly nombreUsuario = signal('');
  private usuarioId = 0;

  readonly form = this.fb.group({
    nombreCompleto: ['', [Validators.required, Validators.maxLength(150)]],
    rolId: [null as number | null, Validators.required],
    nuevaPassword: ['', [Validators.minLength(8), Validators.maxLength(128)]]
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.usuarioId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(this.usuarioId) || this.usuarioId <= 0) {
      this.router.navigate(['/usuarios']);
      return;
    }

    forkJoin({
      usuario: this.usuarioService.getById(this.usuarioId),
      roles: this.rolService.getAll(false)
    }).subscribe({
      next: ({ usuario, roles }) => {
        const u = usuario.data;
        this.roles.set(roles.data.filter(r => r.activo));
        this.nombreUsuario.set(u.nombreUsuario);
        this.form.patchValue({
          nombreCompleto: u.nombreCompleto,
          rolId: u.rolId ?? null,
          nuevaPassword: ''
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar el usuario.');
      }
    });
  }

  guardar(): void {
    if (this.form.invalid || this.saving()) return;

    const valor = this.form.getRawValue();
    const rol = this.roles().find(r => r.id === valor.rolId);
    if (!rol) {
      this.errorMessage.set('Selecciona un rol activo.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.usuarioService.update(this.usuarioId, {
      nombreCompleto: valor.nombreCompleto!.trim(),
      rol: rol.esAdministrador ? 'Administrador' : 'Vendedor',
      rolId: rol.id,
      nuevaPassword: valor.nuevaPassword?.trim() || undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Usuario actualizado correctamente.', 'Cerrar', { duration: 4000 });
        this.router.navigate(['/usuarios', this.usuarioId]);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo actualizar el usuario.');
      }
    });
  }
}
