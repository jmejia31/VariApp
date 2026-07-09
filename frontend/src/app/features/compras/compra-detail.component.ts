import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CompraService } from '../../services/compra.service';
import { Compra } from '../../core/models/compra.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';

@Component({
  selector: 'app-compra-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule],
  templateUrl: './compra-detail.component.html',
  styleUrl: './compra-detail.component.scss'
})
export class CompraDetailComponent implements OnInit {
  readonly compra = signal<Compra | null>(null);
  readonly loading = signal(true);
  readonly procesando = signal(false);
  private compraId!: number;

  constructor(
    private compraService: CompraService,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
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
    this.procesando.set(true);
    this.compraService.confirmar(this.compraId).subscribe({
      next: (res) => { this.compra.set(res.data); this.procesando.set(false); },
      error: () => this.procesando.set(false)
    });
  }

  anular(): void {
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
    this.compraService.deleteBorrador(this.compraId).subscribe(() => this.router.navigate(['/compras']));
  }
}
