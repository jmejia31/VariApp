import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, provideRoutes } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { ALMACENES_ROUTES } from './features/almacenes/almacenes.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideRoutes(ALMACENES_ROUTES),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
