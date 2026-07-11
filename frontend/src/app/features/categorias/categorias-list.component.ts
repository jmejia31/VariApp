import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CategoriaService } from '../../services/categoria.service';
import { Categoria } from '../../core/models/categoria.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

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

  constructor(private categoriaService: CategoriaService, private permisosRuntime: PermisosRuntimeService) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Categorias', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Categorias', 'Editar'));
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
}
