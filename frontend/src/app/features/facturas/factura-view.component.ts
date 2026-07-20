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

  readonly mostrarPanelCorreo = signal(false);
  readonly enviandoCorreo = signal(false);
  correoEditable = '';

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

  telefonoValido(): boolean {
    const soloDigitos = this.telefonoEditable.replace(/\D/g, '');
    return soloDigitos.length >= 10;
  }

  abrirWhatsApp(): void {
    const f = this.factura();
    if (!f || !this.telefonoValido()) return;

    const numero = this.telefonoEditable.replace(/\D/g, '');
    const url = `https://wa.me/${numero}?text=${encodeURIComponent(this.mensajeEditable)}`;

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

  toggleCorreo(): void {
    if (this.mostrarPanelCorreo()) {
      this.mostrarPanelCorreo.set(false);
      return;
    }

    const f = this.factura();
    if (!f) return;

    this.correoEditable = f.clienteCorreo || '';
    this.mostrarPanelCorreo.set(true);
  }

  correoValido(): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.correoEditable.trim());
  }

  enviarCorreo(): void {
    const f = this.factura();
    if (!f || !this.correoValido()) return;

    this.enviandoCorreo.set(true);
    this.facturaService.enviarPorCorreo(f.id, this.correoEditable.trim()).subscribe({
      next: (res) => {
        this.enviandoCorreo.set(false);
        this.mostrarPanelCorreo.set(false);
        this.snackBar.open(res.message || 'Correo enviado correctamente.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.enviandoCorreo.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudo enviar el correo.', 'Cerrar', { duration: 6000 });
      }
    });
  }
}
