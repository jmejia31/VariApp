import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CompraService } from '../../services/compra.service';
import { Compra } from '../../core/models/compra.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-compra-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule],
  templateUrl: './compra-detail.component.html',
  styleUrl: './compra-detail.component.scss'
})
export class CompraDetailComponent implements OnInit {
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly compra = signal<Compra | null>(null);
  readonly loading = signal(true);
  readonly procesando = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeConfirmar = signal(false);
  readonly puedeAnular = signal(false);
  readonly puedeEliminar = signal(false);
  private compraId!: number;

  constructor(
    private compraService: CompraService,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.puedeEditar.set(this.permisosRuntime.puede('Compras', 'Editar'));
    this.puedeConfirmar.set(this.permisosRuntime.puede('Compras', 'Confirmar'));
    this.puedeAnular.set(this.permisosRuntime.puede('Compras', 'Anular'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Compras', 'EliminarLogico'));

    this.compraId = Number(this.route.snapshot.paramMap.get('id'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.compraService.getById(this.compraId).subscribe({
      next: (res) => { this.compra.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  confirmar(): void {
    if (!this.puedeConfirmar() || this.procesando()) return;

    this.procesando.set(true);
    this.compraService.confirmar(this.compraId).subscribe({
      next: (res) => { this.compra.set(res.data); this.procesando.set(false); },
      error: () => this.procesando.set(false)
    });
  }

  anular(): void {
    if (!this.puedeAnular() || this.procesando()) return;

    const ref = this.dialog.open(AnularDialogComponent, {
      data: { title: 'Anular compra', message: 'Esta acción revertirá el stock de los productos. Escribe el motivo:' }
    });

    ref.afterClosed().subscribe((motivo: string | undefined) => {
      if (!motivo) return;
      this.procesando.set(true);
      this.compraService.anular(this.compraId, motivo).subscribe({
        next: (res) => { this.compra.set(res.data); this.procesando.set(false); },
        error: () => this.procesando.set(false)
      });
    });
  }

  eliminarBorrador(): void {
    if (!this.puedeEliminar() || this.procesando()) return;

    this.procesando.set(true);
    this.compraService.deleteBorrador(this.compraId).subscribe({
      next: () => this.router.navigate(['/compras']),
      error: () => this.procesando.set(false)
    });
  }
}
