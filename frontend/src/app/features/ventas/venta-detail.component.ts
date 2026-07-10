import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { VentaService } from '../../services/venta.service';
import { Venta } from '../../core/models/venta.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';

@Component({
  selector: 'app-venta-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule],
  templateUrl: './venta-detail.component.html',
  styleUrl: './venta-detail.component.scss'
})
export class VentaDetailComponent implements OnInit {
  readonly venta = signal<Venta | null>(null);
  readonly loading = signal(true);
  readonly procesando = signal(false);
  private ventaId!: number;

  constructor(
    private ventaService: VentaService,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.ventaId = Number(this.route.snapshot.paramMap.get('id'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.ventaService.getById(this.ventaId).subscribe({
      next: (res) => { this.venta.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  confirmar(): void {
    this.procesando.set(true);
    this.ventaService.confirmar(this.ventaId).subscribe({
      next: (res) => {
        this.venta.set(res.data);
        this.procesando.set(false);
        if (res.data.facturaId) this.router.navigate(['/facturas', res.data.facturaId]);
      },
      error: () => this.procesando.set(false)
    });
  }

  anular(): void {
    const ref = this.dialog.open(AnularDialogComponent, {
      data: { title: 'Anular venta', message: 'Esta acción revertirá el stock y anulará la factura. Escribe el motivo:' }
    });

    ref.afterClosed().subscribe((motivo: string | undefined) => {
      if (!motivo) return;
      this.procesando.set(true);
      this.ventaService.anular(this.ventaId, motivo).subscribe({
        next: (res) => { this.venta.set(res.data); this.procesando.set(false); },
        error: () => this.procesando.set(false)
      });
    });
  }

  eliminarBorrador(): void {
    this.ventaService.deleteBorrador(this.ventaId).subscribe(() => this.router.navigate(['/ventas']));
  }
}
