import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { telefonoWhatsapp } from './varistorehn.catalog';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { IconoTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn-header',
  standalone: true,
  imports: [CommonModule, IconoTiendaComponent],
  templateUrl: './varistorehn-header.component.html',
  styleUrl: './varistorehn-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnHeaderComponent {
  readonly identidad = inject(EmpresaIdentidadService);

  @Input() busqueda = '';
  @Input() categorias: readonly string[] = [];
  @Input() categoriaActiva = '';
  @Input() totalUnidades = 0;
  @Input() subtotal = 0;
  @Input() permiteWhatsapp = true;

  @Output() readonly busquedaActualizada = new EventEmitter<string>();
  @Output() readonly buscarSolicitado = new EventEmitter<void>();
  @Output() readonly categoriaSeleccionada = new EventEmitter<string>();
  @Output() readonly carritoSolicitado = new EventEmitter<void>();

  @ViewChild('menuMovil') private menuMovil?: ElementRef<HTMLDialogElement>;
  @ViewChild('botonMenu') private botonMenu?: ElementRef<HTMLButtonElement>;
  @ViewChild('primerEnlaceMovil') private primerEnlaceMovil?: ElementRef<HTMLAnchorElement>;

  readonly menuAbierto = signal(false);
  private readonly logoFallido = signal<string | null>(null);
  private restaurarFocoAlCerrar = true;
  readonly telefono = computed(() => telefonoWhatsapp(this.identidad.config().whatsApp));
  readonly mostrarLogo = computed(() => {
    const logo = this.identidad.logoUrl();
    return Boolean(logo && this.logoFallido() !== logo);
  });
  readonly enlaceWhatsapp = computed(() => this.telefono() ? `https://wa.me/${this.telefono()}` : '');

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: `${VARISTOREHN_PATHS.inicio}#catalogo`,
    categorias: `${VARISTOREHN_PATHS.inicio}#categories-title`,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  actualizarBusqueda(texto: string): void {
    this.busquedaActualizada.emit(texto);
  }

  enviarBusqueda(evento: Event): void {
    evento.preventDefault();
    this.buscarSolicitado.emit();
  }

  seleccionarCategoria(nombre: string): void {
    if (this.menuAbierto()) this.cerrarMenuMovil(false);
    this.categoriaSeleccionada.emit(nombre);
  }

  solicitarCarrito(): void {
    if (this.menuAbierto()) this.cerrarMenuMovil(false);
    this.carritoSolicitado.emit();
  }

  abrirMenuMovil(): void {
    const dialogo = this.menuMovil?.nativeElement;
    if (!dialogo || dialogo.open) return;

    this.restaurarFocoAlCerrar = true;
    dialogo.showModal();
    this.menuAbierto.set(true);
    queueMicrotask(() => this.primerEnlaceMovil?.nativeElement.focus());
  }

  cerrarMenuMovil(devolverFoco = true): void {
    const dialogo = this.menuMovil?.nativeElement;
    const estabaAbierto = Boolean(dialogo?.open || this.menuAbierto());
    if (!estabaAbierto) return;

    this.restaurarFocoAlCerrar = devolverFoco;
    if (dialogo?.open) dialogo.close();
    else this.menuAbierto.set(false);
  }

  cerrarDesdeFondo(evento: MouseEvent): void {
    if (evento.target === this.menuMovil?.nativeElement) this.cerrarMenuMovil(true);
  }

  alCerrarDialogo(): void {
    const restaurarFoco = this.restaurarFocoAlCerrar;
    this.restaurarFocoAlCerrar = true;
    this.menuAbierto.set(false);
    if (restaurarFoco) queueMicrotask(() => this.botonMenu?.nativeElement.focus());
  }

  reportarErrorLogo(url: string): void {
    this.logoFallido.set(url);
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', {
        style: 'currency',
        currency: this.identidad.config().moneda || 'HNL'
      }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }
}