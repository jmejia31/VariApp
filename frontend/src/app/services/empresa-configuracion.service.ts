import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { ActualizarEmpresaConfiguracionValue, EmpresaConfiguracion } from '../core/models/empresa-configuracion.model';

@Injectable({ providedIn: 'root' })
export class EmpresaConfiguracionService {
  private readonly apiUrl = `${environment.apiUrl}/empresa-configuracion`;

  constructor(private http: HttpClient) {}

  get(): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.get<ApiResponse<EmpresaConfiguracion>>(this.apiUrl);
  }

  getPublica(): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.get<ApiResponse<EmpresaConfiguracion>>(`${this.apiUrl}/publica`);
  }

  update(valor: ActualizarEmpresaConfiguracionValue): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.put<ApiResponse<EmpresaConfiguracion>>(this.apiUrl, valor);
  }

  updateLogo(file: File): Observable<ApiResponse<EmpresaConfiguracion>> {
    const formData = new FormData();
    formData.append('logo', file);
    return this.http.post<ApiResponse<EmpresaConfiguracion>>(`${this.apiUrl}/logo`, formData);
  }

  restaurarLogo(): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.delete<ApiResponse<EmpresaConfiguracion>>(`${this.apiUrl}/logo`);
  }
}
