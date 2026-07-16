import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FacturaService } from '../../services/factura.service';
import { Factura } from '../../core/models/factura.model';

@Component({
  selector: 'app-factura-view',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './factura-view.component.html',
  styleUrl: './factura-view.component.scss'
})
export class FacturaViewComponent implements OnInit {
  readonly factura = signal<Factura | null>(null);
  readonly loading = signal(true);
  readonly descargandoPdf = signal(false);

  constructor(private facturaService: FacturaService, private route: ActivatedRoute, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.facturaService.getById(id).subscribe({
      next: (res) => { this.factura.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  imprimir(): void {
    window.print();
  }

  /** Descarga el PDF real generado en backend (sección 13/14), no una
   * captura de la vista HTML. */
  descargarPdf(): void {
    const f = this.factura();
    if (!f) return;

    this.descargandoPdf.set(true);
    this.facturaService.descargarPdf(f.id).subscribe({
      next: (blob) => {
        this.descargandoPdf.set(false);
        const url = window.URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = `${f.numeroFactura}.pdf`;
        enlace.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.descargandoPdf.set(false);
        this.snackBar.open('No se pudo generar el PDF de la factura.', 'Cerrar', { duration: 5000 });
      }
    });
  }
}
