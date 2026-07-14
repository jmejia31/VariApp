import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { ActualizarPerfilValue, CambiarPasswordValue, Perfil } from '../core/models/perfil.model';

@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly apiUrl = `${environment.apiUrl}/perfil`;

  constructor(private http: HttpClient) {}

  get(): Observable<ApiResponse<Perfil>> {
    return this.http.get<ApiResponse<Perfil>>(this.apiUrl);
  }

  update(valor: ActualizarPerfilValue): Observable<ApiResponse<Perfil>> {
    return this.http.put<ApiResponse<Perfil>>(this.apiUrl, valor);
  }

  cambiarPassword(valor: CambiarPasswordValue): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.apiUrl}/password`, valor);
  }
}
