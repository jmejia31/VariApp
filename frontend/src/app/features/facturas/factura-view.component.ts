import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FacturaService } from '../../services/factura.service';
import {
  EnlaceCompartir,
  Factura,
  FacturaFormatoCodigo,
  FacturaFormatoPdf,
  HistorialEnvio
} from '../../core/models/factura.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

const FORMATO_STORAGE_KEY = 'variapp_factura_formato_pdf';
const FORMATOS_FALLBACK: FacturaFormatoPdf[] = [
  { codigo: 'a4', nombre: 'A4', descripcion: '210 × 297 mm', anchoMm: 210, altoMm: 297, esContinuo: false, usoRecomendado: 'Impresoras convencionales y archivo digital' },
  { codigo: 'carta', nombre: 'Carta', descripcion: '8.5 × 11 pulgadas', anchoMm: 215.9, altoMm: 279.4, esContinuo: false, usoRecomendado: 'Impresoras de oficina' },
  { codigo: 'legal', nombre: 'Legal', descripcion: '8.5 × 14 pulgadas', anchoMm: 215.9, altoMm: 355.6, esContinuo: false, usoRecomendado: 'Documentos extensos en papel legal' },
  { codigo: 'oficio', nombre: 'Oficio', descripcion: '8.5 × 13 pulgadas', anchoMm: 215.9, altoMm: 330.2, esContinuo: false, usoRecomendado: 'Impresoras configuradas para papel oficio' },
  { codigo: 'a5', nombre: 'A5', descripcion: '148 × 210 mm', anchoMm: 148, altoMm: 210, esContinuo: false, usoRecomendado: 'Comprobantes compactos' },
  { codigo: 'pos58', nombre: 'POS 58 mm', descripcion: 'Rollo continuo de 58 mm', anchoMm: 58, esContinuo: true, usoRecomendado: 'Impresoras térmicas móviles y handheld' },
  { codigo: 'pos80', nombre: 'POS 80 mm', descripcion: 'Rollo continuo de 80 mm', anchoMm: 80, esContinuo: true, usoRecomendado: 'Impresoras térmicas POS e industriales' }
];

@Component({
  selector: 'app-factura-view',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule
  ],
  templateUrl: './factura-view.component.html',
  styleUrl: './factura-view.component.scss'
})
export class FacturaViewComponent implements OnInit {
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly defaultLogoUrl = 'assets/varistorehn-logo.png';
  readonly factura = signal<Factura | null>(null);
  readonly loading = signal(true);
  readonly formatosPdf = signal<FacturaFormatoPdf[]>(FORMATOS_FALLBACK);
  formatoSeleccionado: FacturaFormatoCodigo = 'a4';

  readonly descargandoPdf = signal(false);
  readonly imprimiendoPdf = signal(false);
  readonly revocandoEnlaces = signal(false);
  readonly puedeExportar = signal(false);
  readonly puedeImprimir = signal(false);
  readonly puedeCompartir = signal(false);

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

  constructor(
    private facturaService: FacturaService,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeExportar.set(this.permisosRuntime.puede('Facturacion', 'Exportar'));
    this.puedeImprimir.set(this.permisosRuntime.puede('Facturacion', 'Imprimir'));
    this.puedeCompartir.set(this.permisosRuntime.puede('Facturacion', 'Compartir'));
    this.restaurarFormatoPreferido();

    this.facturaService.getFormatosPdf().subscribe({
      next: (res) => {
        if (res.data?.length) {
          this.formatosPdf.set(res.data);
          if (!res.data.some((x) => x.codigo === this.formatoSeleccionado)) {
            this.formatoSeleccionado = 'a4';
          }
        }
      }
    });

    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.facturaService.getById(id).subscribe({
      next: (res) => { this.factura.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  formatoActual(): FacturaFormatoPdf {
    return this.formatosPdf().find((x) => x.codigo === this.formatoSeleccionado)
      ?? FORMATOS_FALLBACK[0];
  }

  esTermico(): boolean {
    return this.formatoActual().esContinuo;
  }

  onFormatoChange(codigo: FacturaFormatoCodigo): void {
    this.formatoSeleccionado = codigo;
    try {
      window.localStorage.setItem(FORMATO_STORAGE_KEY, codigo);
    } catch {
      // La selección sigue activa durante la sesión aunque el navegador bloquee almacenamiento.
    }
  }

  /** Abre el PDF seleccionado; el visor del navegador ofrece el diálogo de impresión. */
  imprimir(): void {
    if (!this.puedeImprimir() || this.imprimiendoPdf()) return;

    const factura = this.factura();
    if (!factura) return;

    const formato = this.formatoActual();
    const ventanaPdf = window.open('', '_blank');
    if (!ventanaPdf) {
      this.snackBar.open('El navegador bloqueó la ventana de impresión. Habilita las ventanas emergentes.', 'Cerrar', { duration: 6000 });
      return;
    }

    ventanaPdf.opener = null;
    ventanaPdf.document.title = `Preparando ${factura.numeroFactura} — ${formato.nombre}`;
    ventanaPdf.document.body.textContent = `Preparando PDF ${formato.nombre} para impresión...`;
    this.imprimiendoPdf.set(true);

    this.facturaService.descargarPdf(factura.id, formato.codigo).subscribe({
      next: (blob) => {
        this.imprimiendoPdf.set(false);
        const url = window.URL.createObjectURL(blob);
        ventanaPdf.location.href = url;
        window.setTimeout(() => window.URL.revokeObjectURL(url), 120_000);
      },
      error: () => {
        this.imprimiendoPdf.set(false);
        ventanaPdf.close();
        this.snackBar.open(`No se pudo abrir el PDF ${formato.nombre} para imprimir.`, 'Cerrar', { duration: 5000 });
      }
    });
  }

  descargarPdf(): void {
    if (!this.puedeExportar() || this.descargandoPdf()) return;

    const factura = this.factura();
    if (!factura) return;

    const formato = this.formatoActual();
    this.descargandoPdf.set(true);
    this.facturaService.descargarPdf(factura.id, formato.codigo).subscribe({
      next: (blob) => {
        this.descargandoPdf.set(false);
        const url = window.URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = `${factura.numeroFactura}-${formato.codigo}.pdf`;
        enlace.rel = 'noopener';
        enlace.click();
        window.setTimeout(() => window.URL.revokeObjectURL(url), 1000);
      },
      error: () => {
        this.descargandoPdf.set(false);
        this.snackBar.open(`No se pudo generar el PDF ${formato.nombre} de la factura.`, 'Cerrar', { duration: 5000 });
      }
    });
  }

  toggleWhatsApp(): void {
    if (!this.puedeCompartir()) return;

    if (this.mostrarPanelWhatsApp()) {
      this.mostrarPanelWhatsApp.set(false);
      return;
    }

    const factura = this.factura();
    if (!factura) return;

    const enlaceActual = this.enlaceCompartir();
    if (enlaceActual && new Date(enlaceActual.fechaExpiracion).getTime() > Date.now()) {
      this.mostrarPanelWhatsApp.set(true);
      return;
    }

    this.preparandoWhatsApp.set(true);
    this.facturaService.prepararWhatsApp(factura.id).subscribe({
      next: (res) => {
        this.preparandoWhatsApp.set(false);
        this.enlaceCompartir.set(res.data);
        this.telefonoEditable = res.data.telefonoSugerido;
        this.mensajeEditable = res.data.mensajeWhatsApp;
        this.mostrarPanelWhatsApp.set(true);
        this.snackBar.open('Enlace temporal A4 creado. Los enlaces anteriores fueron revocados.', 'Cerrar', { duration: 4500 });
      },
      error: (err) => {
        this.preparandoWhatsApp.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudo preparar el envío por WhatsApp.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  revocarEnlaces(): void {
    if (!this.puedeCompartir() || this.revocandoEnlaces()) return;

    const factura = this.factura();
    if (!factura) return;

    this.revocandoEnlaces.set(true);
    this.facturaService.revocarEnlaces(factura.id).subscribe({
      next: (res) => {
        this.revocandoEnlaces.set(false);
        this.enlaceCompartir.set(null);
        this.mostrarPanelWhatsApp.set(false);
        const cantidad = res.data.enlacesRevocados;
        this.snackBar.open(
          cantidad > 0 ? 'Los enlaces públicos fueron revocados.' : 'No había enlaces públicos vigentes.',
          'Cerrar',
          { duration: 4500 }
        );
      },
      error: (err) => {
        this.revocandoEnlaces.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudieron revocar los enlaces.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  telefonoValido(): boolean {
    const soloDigitos = this.telefonoEditable.replace(/\D/g, '');
    return soloDigitos.length >= 10 && soloDigitos.length <= 15;
  }

  abrirWhatsApp(): void {
    if (!this.puedeCompartir()) return;

    const factura = this.factura();
    if (!factura || !this.telefonoValido() || !this.mensajeEditable.trim()) return;

    const numero = this.telefonoEditable.replace(/\D/g, '');
    const url = `https://wa.me/${numero}?text=${encodeURIComponent(this.mensajeEditable.trim())}`;

    this.facturaService
      .registrarIntentoEnvio(factura.id, 'WhatsApp', numero, 'Iniciado')
      .subscribe();

    window.open(url, '_blank', 'noopener,noreferrer');
    this.mostrarPanelWhatsApp.set(false);
  }

  toggleHistorial(): void {
    if (!this.puedeCompartir()) return;

    if (this.mostrarHistorial()) {
      this.mostrarHistorial.set(false);
      return;
    }

    const factura = this.factura();
    if (!factura) return;

    this.facturaService.getHistorialEnvios(factura.id).subscribe({
      next: (res) => {
        this.historial.set(res.data);
        this.mostrarHistorial.set(true);
      },
      error: () => this.snackBar.open('No se pudo cargar el historial de envíos.', 'Cerrar', { duration: 5000 })
    });
  }

  toggleCorreo(): void {
    if (!this.puedeCompartir()) return;

    if (this.mostrarPanelCorreo()) {
      this.mostrarPanelCorreo.set(false);
      return;
    }

    const factura = this.factura();
    if (!factura) return;

    this.correoEditable = factura.clienteCorreo || '';
    this.mostrarPanelCorreo.set(true);
  }

  correoValido(): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.correoEditable.trim());
  }

  enviarCorreo(): void {
    if (!this.puedeCompartir()) return;

    const factura = this.factura();
    if (!factura || !this.correoValido()) return;

    this.enviandoCorreo.set(true);
    this.facturaService.enviarPorCorreo(factura.id, this.correoEditable.trim()).subscribe({
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

  private restaurarFormatoPreferido(): void {
    try {
      const guardado = window.localStorage.getItem(FORMATO_STORAGE_KEY) as FacturaFormatoCodigo | null;
      if (guardado && FORMATOS_FALLBACK.some((x) => x.codigo === guardado)) {
        this.formatoSeleccionado = guardado;
      }
    } catch {
      this.formatoSeleccionado = 'a4';
    }
  }
}
