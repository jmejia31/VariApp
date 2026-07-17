import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FacturaService } from '../../services/factura.service';
import { EnlaceCompartir, Factura, HistorialEnvio } from '../../core/models/factura.model';

@Component({
  selector: 'app-factura-view',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule
  ],
  templateUrl: './factura-view.component.html',
  styleUrl: './factura-view.component.scss'
})
export class FacturaViewComponent implements OnInit {
  readonly factura = signal<Factura | null>(null);
  readonly loading = signal(true);
  readonly descargandoPdf = signal(false);

  readonly mostrarPanelWhatsApp = signal(false);
  readonly preparandoWhatsApp = signal(false);
  readonly enlaceCompartir = signal<EnlaceCompartir | null>(null);
  telefonoEditable = '';
  mensajeEditable = '';

  readonly historial = signal<HistorialEnvio[]>([]);
  readonly mostrarHistorial = signal(false);

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

  /** Abre (o cierra) el panel de WhatsApp — la opción PRINCIPAL de envío
   * (sección 14: "WhatsApp debe ser la opción principal de envío"). */
  toggleWhatsApp(): void {
    if (this.mostrarPanelWhatsApp()) {
      this.mostrarPanelWhatsApp.set(false);
      return;
    }

    const f = this.factura();
    if (!f) return;

    this.preparandoWhatsApp.set(true);
    this.facturaService.prepararWhatsApp(f.id).subscribe({
      next: (res) => {
        this.preparandoWhatsApp.set(false);
        this.enlaceCompartir.set(res.data);
        this.telefonoEditable = res.data.telefonoSugerido;
        this.mensajeEditable = res.data.mensajeWhatsApp;
        this.mostrarPanelWhatsApp.set(true);
      },
      error: (err) => {
        this.preparandoWhatsApp.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudo preparar el envío por WhatsApp.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  /** Sección 14, puntos 3-4: valida formato básico con código de país antes
   * de habilitar el envío. No pretende validar exhaustivamente todos los
   * formatos internacionales, solo descarta números claramente incompletos. */
  telefonoValido(): boolean {
    const soloDigitos = this.telefonoEditable.replace(/\D/g, '');
    return soloDigitos.length >= 10; // código de país (1-3 dígitos) + número local
  }

  abrirWhatsApp(): void {
    const f = this.factura();
    if (!f || !this.telefonoValido()) return;

    const numero = this.telefonoEditable.replace(/\D/g, '');
    const url = `https://wa.me/${numero}?text=${encodeURIComponent(this.mensajeEditable)}`;

    // Se registra el intento ANTES de abrir la pestaña (sección 14/18:
    // "registrar el intento"). No hay forma de confirmar que el mensaje
    // llegó realmente sin una API oficial de WhatsApp — se registra la
    // apertura del flujo, no una "entrega confirmada" que sería simulada.
    this.facturaService.registrarIntentoEnvio(f.id, 'WhatsApp', this.telefonoEditable, 'Iniciado').subscribe();

    window.open(url, '_blank');
    this.mostrarPanelWhatsApp.set(false);
  }

  toggleHistorial(): void {
    if (this.mostrarHistorial()) {
      this.mostrarHistorial.set(false);
      return;
    }

    const f = this.factura();
    if (!f) return;

    this.facturaService.getHistorialEnvios(f.id).subscribe({
      next: (res) => {
        this.historial.set(res.data);
        this.mostrarHistorial.set(true);
      },
      error: () => this.snackBar.open('No se pudo cargar el historial de envíos.', 'Cerrar', { duration: 5000 })
    });
  }
}
