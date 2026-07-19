import { Injectable } from '@angular/core';
import { TemaVisualService } from './tema-visual.service';
import { CAMPOS_TEMA, TemaVisual } from '../core/models/tema-visual.model';

/** Aplica el tema visual global a las variables CSS del documento en tiempo
 * de ejecución (sección 16: persistencia tras recargar/cerrar sesión/
 * reiniciar frontend — el tema vive en la base de datos, no en localStorage,
 * así que sobrevive a todo eso por diseño). Se invoca desde AppComponent,
 * que se instancia siempre, autenticado o no (cubre también la pantalla de
 * login, sección 16: "pantallas de autenticación cuando corresponda"). */
@Injectable({ providedIn: 'root' })
export class ThemeApplierService {
  constructor(private temaVisualService: TemaVisualService) {}

  aplicarTemaGuardado(): void {
    this.temaVisualService.get().subscribe({
      next: (res) => this.aplicar(res.data),
      error: () => {
        // Si el tema no se pudo cargar (backend caído, primera carga, etc.)
        // la app sigue funcionando con los valores por defecto ya presentes
        // en styles.scss — nunca se rompe la interfaz por esto.
      }
    });
  }

  aplicar(tema: TemaVisual): void {
    const root = document.documentElement;
    for (const campo of CAMPOS_TEMA) {
      const valor = tema[campo.clave];
      if (valor) root.style.setProperty(campo.variableCss, valor as string);
    }
  }
}
