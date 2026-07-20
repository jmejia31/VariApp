import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { PermisosRuntimeService } from './permisos-runtime.service';

const INACTIVITY_LIMIT_MS = 30 * 60 * 1000;
const SESSION_MESSAGE_KEY = 'inventoryapp_session_message';
const LAST_ACTIVITY_KEY = 'inventoryapp_last_activity';

@Injectable({ providedIn: 'root' })
export class SessionActivityService {
  private iniciado = false;
  private cerrando = false;
  private intervalId?: number;
  private readonly eventos = ['click', 'keydown', 'pointerdown', 'touchstart', 'scroll'];

  constructor(
    private authService: AuthService,
    private permisosRuntime: PermisosRuntimeService,
    private router: Router,
    private zone: NgZone
  ) {}

  iniciar(): void {
    if (this.iniciado) return;
    this.iniciado = true;
    this.marcarActividad();

    this.zone.runOutsideAngular(() => {
      this.eventos.forEach((evento) => window.addEventListener(evento, this.onActividad, { passive: true }));
      this.intervalId = window.setInterval(() => this.verificar(), 15_000);
    });
  }

  detener(): void {
    if (!this.iniciado) return;
    this.iniciado = false;
    this.eventos.forEach((evento) => window.removeEventListener(evento, this.onActividad));
    if (this.intervalId) window.clearInterval(this.intervalId);
    this.intervalId = undefined;
  }

  cerrarPor401(): void {
    this.cerrarSesion('Tu sesion expiro. Inicia sesion nuevamente.');
  }

  cerrarManual(): void {
    this.cerrarSesion(undefined);
  }

  tomarMensajePendiente(): string | null {
    const mensaje = sessionStorage.getItem(SESSION_MESSAGE_KEY);
    sessionStorage.removeItem(SESSION_MESSAGE_KEY);
    return mensaje;
  }

  private readonly onActividad = () => {
    if (this.authService.isAuthenticated()) this.marcarActividad();
  };

  private marcarActividad(): void {
    localStorage.setItem(LAST_ACTIVITY_KEY, String(Date.now()));
  }

  private verificar(): void {
    if (!this.authService.getToken()) return;
    if (this.authService.isTokenExpired()) {
      this.zone.run(() => this.cerrarSesion('Tu sesion expiro. Inicia sesion nuevamente.'));
      return;
    }

    const lastActivity = Number(localStorage.getItem(LAST_ACTIVITY_KEY) || Date.now());
    if (Date.now() - lastActivity >= INACTIVITY_LIMIT_MS) {
      this.zone.run(() => this.cerrarSesion('Tu sesion expiro por inactividad.'));
    }
  }

  private cerrarSesion(mensaje?: string): void {
    if (this.cerrando) return;
    this.cerrando = true;
    if (mensaje) sessionStorage.setItem(SESSION_MESSAGE_KEY, mensaje);
    this.authService.logout();
    this.permisosRuntime.limpiar();
    localStorage.removeItem(LAST_ACTIVITY_KEY);
    this.router.navigate(['/login']).finally(() => {
      this.cerrando = false;
    });
  }
}
