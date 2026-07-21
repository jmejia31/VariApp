import { Injectable, computed, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { EmpresaConfiguracion } from '../core/models/empresa-configuracion.model';
import { EmpresaConfiguracionService } from './empresa-configuracion.service';

const DEFAULT_CONFIG: EmpresaConfiguracion = {
  id: 0,
  nombreComercial: 'VariStorehn',
  eslogan: 'Eleva tu mundo digital',
  nombreVisibleSistema: 'VariStorehn',
  descripcionSistema: 'Administrativo',
  mensajeLogin: 'Inicia sesión para administrar VariStorehn',
  copyright: '© 2026 VariStorehn. Todos los derechos reservados.',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  piePaginaActivo: true,
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy'
};

@Injectable({ providedIn: 'root' })
export class EmpresaIdentidadService {
  private readonly _config = signal<EmpresaConfiguracion>(DEFAULT_CONFIG);
  private cargada = false;

  readonly config = this._config.asReadonly();
  readonly nombreSistema = computed(() => this._config().nombreVisibleSistema || this._config().nombreComercial || 'VariStorehn');
  readonly descripcionSistema = computed(() => this.normalizarDescripcion(this._config().encabezadoTexto || this._config().descripcionSistema));
  readonly logoUrl = computed(() => this._config().logoUrl || 'assets/varistorehn-logo.png');
  readonly mensajeLogin = computed(() => this._config().mensajeLogin || `Inicia sesión para administrar ${this.nombreSistema()}`);
  readonly mostrarCopyright = computed(() => this._config().mostrarCopyright);
  readonly copyright = computed(() => {
    const actual = this._config();
    if (!actual.usarAnioAutomaticoCopyright) return actual.copyright;
    return actual.copyright.replace(/\b20\d{2}\b/, String(new Date().getFullYear()));
  });

  constructor(private empresaService: EmpresaConfiguracionService) {}

  cargar(force = false) {
    if (this.cargada && !force) return of(this._config());
    return this.empresaService.getPublica().pipe(
      tap((res) => {
        this._config.set({ ...DEFAULT_CONFIG, ...res.data });
        this.cargada = true;
      }),
      catchError(() => {
        this.cargada = true;
        return of(this._config());
      })
    );
  }

  refrescarDespuesDeGuardar(config: EmpresaConfiguracion): void {
    this._config.set({ ...DEFAULT_CONFIG, ...config });
    this.cargada = true;
  }

  private normalizarDescripcion(value?: string | null): string {
    const texto = value?.trim();
    if (!texto || texto === 'Gestión de Inventario' || texto === 'Gestion de Inventario') {
      return 'Administrativo';
    }

    return texto;
  }
}
