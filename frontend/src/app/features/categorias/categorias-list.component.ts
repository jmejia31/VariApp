import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CategoriaService } from '../../services/categoria.service';
import { Categoria } from '../../core/models/categoria.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-categorias-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './categorias-list.component.html',
  styleUrl: './categorias-list.component.scss'
})
export class CategoriasListComponent implements OnInit {
  readonly categorias = signal<Categoria[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private categoriaService: CategoriaService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Categorias', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Categorias', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Categorias', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.categoriaService.getAll().subscribe({
      next: (res) => {
        this.categorias.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  toggleActiva(categoria: Categoria): void {
    this.categoriaService.update(categoria.id, {
      nombre: categoria.nombre,
      descripcion: categoria.descripcion,
      activa: !categoria.activa
    }).subscribe(() => this.cargar());
  }

  async eliminar(categoria: Categoria): Promise<void> {
    const confirmado = await this.alerts.confirmar({ titulo: 'Eliminar categoría', mensaje: `Se ocultará "${categoria.nombre}" sin borrar el historial relacionado.`, tipo: 'peligro', confirmarTexto: 'Eliminar' });
    if (!confirmado) return;

    this.categoriaService.delete(categoria.id).subscribe({
      next: () => {
        this.snackBar.open('Categoria eliminada correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar la categoria.', 'Cerrar', { duration: 5000 })
    });
  }
}
