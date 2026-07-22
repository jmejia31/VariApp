import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormControl, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsuarioService } from '../../services/usuario.service';
import { RolService } from '../../services/rol.service';
import { Usuario } from '../../core/models/usuario.model';
import { Rol } from '../../core/models/rol.model';
import { PagedResult } from '../../core/models/api-response.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AuthService } from '../../core/auth/auth.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatSelectModule,
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
  readonly buscador = new FormControl('');

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeAsignarRol = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  readonly form = this.fb.group({
    nombreUsuario: ['', Validators.required],
    nombreCompleto: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    rolId: [null as number | null, Validators.required]
  });

  constructor(
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private permisosRuntime: PermisosRuntimeService,
    private auth: AuthService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeAsignarRol.set(this.permisosRuntime.puede('Usuarios', 'AsignarRol'));
    this.puedeCrear.set(
      this.permisosRuntime.puede('Usuarios', 'Crear') && this.puedeAsignarRol()
    );
    this.puedeEditar.set(this.permisosRuntime.puede('Usuarios', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Usuarios', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Usuarios', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Usuarios', 'EliminarLogico'));

    this.cargar();

    if (this.puedeCrear()) {
      this.rolService.getAll().subscribe({
        next: (res) => this.roles.set(res.data.filter((r) => r.activo)),
        error: () => this.roles.set([])
      });
    }

    this.buscador.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => this.cargar());
  }

  esUsuarioActual(u: Usuario): boolean {
    return this.auth.nombreUsuario() === u.nombreUsuario;
  }

  puedeCambiarEstado(usuario: Usuario): boolean {
    if (this.esUsuarioActual(usuario)) return false;
    return usuario.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  puedeBloquearUsuario(usuario: Usuario): boolean {
    if (this.esUsuarioActual(usuario)) return false;
    return usuario.bloqueado ? this.puedeActivar() : this.puedeDesactivar();
  }

  cargar(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.usuarioService.getPaged({ page: 1, pageSize: 100, search: this.buscador.value || undefined }).subscribe({
      next: (res) => {
        const usuarios = this.extraerUsuarios(res.data);
        if (usuarios.length > 0 || this.buscador.value) {
          this.usuarios.set(usuarios);
          this.loading.set(false);
          return;
        }

        this.cargarListadoLegado();
      },
      error: () => this.cargarListadoLegado()
    });
  }

  private cargarListadoLegado(): void {
    this.usuarioService.getAll().subscribe({
      next: (res) => {
        const termino = (this.buscador.value || '').trim().toLowerCase();
        const usuarios = this.extraerUsuarios(res.data).filter((u) => {
          if (!termino) return true;
          return u.nombreUsuario.toLowerCase().includes(termino) || u.nombreCompleto.toLowerCase().includes(termino);
        });

        this.usuarios.set(usuarios);
        this.loading.set(false);
      },
      error: (err) => {
        this.usuarios.set([]);
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar la lista de usuarios.');
        this.snackBar.open(this.errorMessage()!, 'Cerrar', { duration: 6000 });
      }
    });
  }

  private extraerUsuarios(data: PagedResult<Usuario> | Usuario[] | null | undefined): Usuario[] {
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.items)) return data.items;
    return [];
  }

  crear(): void {
    if (!this.puedeCrear() || this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const valor = this.form.getRawValue();
    this.usuarioService.create({
      nombreUsuario: valor.nombreUsuario!,
      nombreCompleto: valor.nombreCompleto!,
      password: valor.password!,
      rol: 'Vendedor',
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

  async toggleEstado(usuario: Usuario): Promise<void> {
    if (!this.puedeCambiarEstado(usuario)) return;

    const activar = !usuario.activo;
    const confirmado = await this.alerts.confirmar({
      titulo: activar ? 'Activar usuario' : 'Desactivar usuario',
      mensaje: `Se ${activar ? 'habilitará' : 'deshabilitará'} el acceso de "${usuario.nombreCompleto}".`,
      detalle: activar ? 'Podrá ingresar según los permisos de su rol.' : 'No podrá iniciar nuevas sesiones.',
      tipo: activar ? 'info' : 'advertencia',
      confirmarTexto: activar ? 'Activar' : 'Desactivar'
    });
    if (!confirmado) return;

    this.usuarioService.updateEstado(usuario.id, activar).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 })
    });
  }

  async bloquear(usuario: Usuario): Promise<void> {
    if (!this.puedeBloquearUsuario(usuario) || usuario.bloqueado) return;

    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Bloquear usuario',
      mensaje: `Indica por qué se bloqueará a "${usuario.nombreCompleto}".`,
      tipo: 'advertencia',
      confirmarTexto: 'Bloquear',
      entrada: { etiqueta: 'Motivo del bloqueo', requerida: true }
    });
    if (!motivo) return;

    this.usuarioService.bloquear(usuario.id, motivo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo bloquear al usuario.', 'Cerrar', { duration: 5000 })
    });
  }

  async desbloquear(usuario: Usuario): Promise<void> {
    if (!this.puedeBloquearUsuario(usuario) || !usuario.bloqueado) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Desbloquear usuario',
      mensaje: `Se restablecerá el acceso de "${usuario.nombreCompleto}".`,
      confirmarTexto: 'Desbloquear'
    });
    if (!confirmado) return;

    this.usuarioService.desbloquear(usuario.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo desbloquear al usuario.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(usuario: Usuario): Promise<void> {
    if (!this.puedeEliminar() || this.esUsuarioActual(usuario)) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar usuario',
      mensaje: `Se eliminará lógicamente a "${usuario.nombreCompleto}".`,
      detalle: 'Sus registros históricos se conservarán.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar usuario'
    });
    if (!confirmado) return;

    this.usuarioService.eliminar(usuario.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar al usuario.', 'Cerrar', { duration: 5000 })
    });
  }
}
