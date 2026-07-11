import { Injectable, signal } from '@angular/core';
import { catchError, map, of } from 'rxjs';
import { PermisoService } from '../../services/permiso.service';

@Injectable({ providedIn: 'root' })
export class PermisosRuntimeService {
  private readonly _permisos = signal<Set<string>>(new Set());
  private readonly _esAdministrador = signal(false);
  private readonly _cargado = signal(false);

  readonly esAdministrador = this._esAdministrador.asReadonly();
  readonly cargado = this._cargado.asReadonly();

  constructor(private permisoService: PermisoService) {}

  cargar() {
    return this.permisoService.getMisPermisos().pipe(
      map((res) => {
        this._permisos.set(new Set(res.data.permisos));
        this._esAdministrador.set(res.data.esAdministrador);
        this._cargado.set(true);
        return true;
      }),
      catchError(() => {
        this._cargado.set(true);
        return of(false);
      })
    );
  }

  limpiar(): void {
    this._permisos.set(new Set());
    this._esAdministrador.set(false);
    this._cargado.set(false);
  }

  puede(modulo: string, accion: string): boolean {
    if (this._esAdministrador()) return true;
    return this._permisos().has(`${modulo}:${accion}`);
  }
}
