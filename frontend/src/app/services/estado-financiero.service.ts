import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  EstadoFinanciero,
  EstadoFinancieroFiltro,
  TipoEstadoFinanciero,
} from '../core/models/estado-financiero.model';

@Injectable({ providedIn: 'root' })
export class EstadoFinancieroService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/estados-financieros`;

  generar(
    tipo: TipoEstadoFinanciero,
    filtro: EstadoFinancieroFiltro,
  ): Observable<ApiResponse<EstadoFinanciero>> {
    let params = new HttpParams();
    if (filtro.periodoContableId != null) {
      params = params.set('periodoContableId', filtro.periodoContableId.toString());
    }
    if (filtro.fechaDesde) params = params.set('fechaDesde', filtro.fechaDesde);
    if (filtro.fechaHasta) params = params.set('fechaHasta', filtro.fechaHasta);

    return this.http.get<ApiResponse<EstadoFinanciero>>(`${this.apiUrl}/${tipo}`, { params });
  }
}
