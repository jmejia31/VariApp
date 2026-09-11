import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';
import { TenantContextService } from '../auth/tenant-context.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const sessionActivity = inject(SessionActivityService);
  const tenantContext = inject(TenantContextService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    // Autenticación por sí sola no autoriza el shell ERP. Cada navegación
    // protegida exige un contexto tenant materializado por el backend.
    if (tenantContext.tieneContextoVerificado()) return true;
    return router.parseUrl('/login');
  }

  if (authService.getToken() && authService.isTokenExpired()) {
    tenantContext.limpiar();
    sessionActivity.cerrarPor401();
    return false;
  }

  tenantContext.limpiar();
  return router.parseUrl('/login');
};
