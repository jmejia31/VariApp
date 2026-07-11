import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { MisPermisos, PermisoMatrizItem } from '../core/models/permiso.model';

@Injectable({ providedIn: 'root' })
export class PermisoService {
  private readonly apiUrl = `${environment.apiUrl}/permisos`;

  constructor(private http: HttpClient) {}

  getMatriz(): Observable<ApiResponse<PermisoMatrizItem[]>> {
    return this.http.get<ApiResponse<PermisoMatrizItem[]>>(`${this.apiUrl}/matriz`);
  }

  updateMatriz(permisos: PermisoMatrizItem[]): Observable<ApiResponse<PermisoMatrizItem[]>> {
    return this.http.put<ApiResponse<PermisoMatrizItem[]>>(`${this.apiUrl}/matriz`, { permisos });
  }

  getMisPermisos(): Observable<ApiResponse<MisPermisos>> {
    return this.http.get<ApiResponse<MisPermisos>>(`${this.apiUrl}/mis-permisos`);
  }
}
