import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LoginRequest, LoginResponse } from '../models/auth.model';

const TOKEN_KEY = 'inventoryapp_token';
const USER_KEY = 'inventoryapp_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  // Signal en memoria; se hidrata desde storage al iniciar el servicio.
  private readonly _token = signal<string | null>(this.readToken());
  private readonly _nombreUsuario = signal<string | null>(localStorage.getItem(USER_KEY));

  readonly isAuthenticated = computed(() => !!this._token());
  readonly nombreUsuario = computed(() => this._nombreUsuario());

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, request).pipe(
      tap((res) => {
        if (res.success) {
          this._token.set(res.data.token);
          this._nombreUsuario.set(res.data.nombreUsuario);
          localStorage.setItem(TOKEN_KEY, res.data.token);
          localStorage.setItem(USER_KEY, res.data.nombreUsuario);
        }
      })
    );
  }

  logout(): void {
    this._token.set(null);
    this._nombreUsuario.set(null);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  getToken(): string | null {
    return this._token();
  }

  private readToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }
}
