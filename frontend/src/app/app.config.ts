import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { ALMACENES_ROUTES } from './features/almacenes/almacenes.routes';
import { UBICACIONES_ALMACEN_ROUTES } from './features/ubicaciones-almacen/ubicaciones-almacen.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter([...UBICACIONES_ALMACEN_ROUTES, ...ALMACENES_ROUTES, ...routes]),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
