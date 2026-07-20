import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const sessionActivity = inject(SessionActivityService);

  const token = authService.getToken();
  if (token && authService.isTokenExpired()) {
    sessionActivity.cerrarPor401();
    return throwError(() => ({ status: 401, message: 'Token expirado' }));
  }

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error) => {
      if (error.status === 401) {
        sessionActivity.cerrarPor401();
      }
      return throwError(() => error);
    })
  );
};
