import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LoginRequest, LoginResponse } from '../models/auth.model';

const TOKEN_KEY = 'inventoryapp_token';
const USER_KEY = 'inventoryapp_user';
const NOMBRE_COMPLETO_KEY = 'inventoryapp_nombre_completo';
const ROL_KEY = 'inventoryapp_rol';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  // Signal en memoria; se hidrata desde storage al iniciar el servicio.
  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private readonly _nombreUsuario = signal<string | null>(localStorage.getItem(USER_KEY));
  private readonly _nombreCompleto = signal<string | null>(localStorage.getItem(NOMBRE_COMPLETO_KEY));
  private readonly _rol = signal<'Administrador' | 'Vendedor' | null>(
    (localStorage.getItem(ROL_KEY) as 'Administrador' | 'Vendedor' | null)
  );

  readonly isAuthenticated = computed(() => !!this._token());
  readonly nombreUsuario = computed(() => this._nombreUsuario());
  readonly nombreCompleto = computed(() => this._nombreCompleto());
  readonly rol = computed(() => this._rol());
  readonly esAdministrador = computed(() => this._rol() === 'Administrador');

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, request).pipe(
      tap((res) => {
        if (res.success) {
          this._token.set(res.data.token);
          this._nombreUsuario.set(res.data.nombreUsuario);
          this._nombreCompleto.set(res.data.nombreCompleto);
          this._rol.set(res.data.rol);
          localStorage.setItem(TOKEN_KEY, res.data.token);
          localStorage.setItem(USER_KEY, res.data.nombreUsuario);
          localStorage.setItem(NOMBRE_COMPLETO_KEY, res.data.nombreCompleto);
          localStorage.setItem(ROL_KEY, res.data.rol);
        }
      })
    );
  }

  logout(): void {
    this._token.set(null);
    this._nombreUsuario.set(null);
    this._nombreCompleto.set(null);
    this._rol.set(null);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(NOMBRE_COMPLETO_KEY);
    localStorage.removeItem(ROL_KEY);
  }

  getToken(): string | null {
    return this._token();
  }
}
