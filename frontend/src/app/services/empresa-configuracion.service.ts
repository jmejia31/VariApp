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

  update(valor: ActualizarEmpresaConfiguracionValue): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.put<ApiResponse<EmpresaConfiguracion>>(this.apiUrl, valor);
  }
}
