import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { PermisosRuntimeService } from '../auth/permisos-runtime.service';

export const permisoGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const permisosRuntime = inject(PermisosRuntimeService);
  const router = inject(Router);

  const modulo = route.data?.['modulo'] as string | undefined;
  const accion = route.data?.['accion'] as string | undefined;

  if (!modulo || !accion) {
    return true;
  }

  if (authService.esAdministrador()) {
    return true;
  }

  if (permisosRuntime.cargado()) {
    return permisosRuntime.puede(modulo, accion) ? true : router.createUrlTree(['/dashboard']);
  }

  return permisosRuntime.cargar().pipe(
    map((ok) => ok && permisosRuntime.puede(modulo, accion) ? true : router.createUrlTree(['/dashboard']))
  );
};
