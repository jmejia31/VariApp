import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PermisoService } from '../../services/permiso.service';
import { PermisoMatrizItem } from '../../core/models/permiso.model';

@Component({
  selector: 'app-permisos-matrix',
  standalone: true,
  imports: [CommonModule, MatCheckboxModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './permisos-matrix.component.html',
  styleUrl: './permisos-matrix.component.scss'
})
export class PermisosMatrixComponent implements OnInit {
  readonly items = signal<PermisoMatrizItem[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);

  readonly modulos = computed(() => [...new Set(this.items().map((i) => i.modulo))]);

  constructor(private permisoService: PermisoService, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.permisoService.getMatriz().subscribe({
      next: (res) => {
        this.items.set(res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  accionesDelModulo(modulo: string): PermisoMatrizItem[] {
    return this.items().filter((i) => i.modulo === modulo);
  }

  toggle(item: PermisoMatrizItem): void {
    this.items.set(this.items().map((i) => (i === item ? { ...i, permitido: !i.permitido } : i)));
  }

  guardar(): void {
    this.saving.set(true);
    this.permisoService.updateMatriz(this.items()).subscribe({
      next: (res) => {
        this.items.set(res.data ?? []);
        this.saving.set(false);
        this.snackBar.open('Matriz de permisos actualizada. El vendedor debe volver a iniciar sesión para verla reflejada.', 'Cerrar', { duration: 6000 });
      },
      error: () => this.saving.set(false)
    });
  }
}
