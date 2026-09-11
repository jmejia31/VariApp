import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const sessionActivity = inject(SessionActivityService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  if (authService.getToken() && authService.isTokenExpired()) {
    sessionActivity.cerrarPor401();
    return false;
  }

  router.navigate(['/login']);
  return false;
};
