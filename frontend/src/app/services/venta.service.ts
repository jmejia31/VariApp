import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedRequest, PagedResult } from '../core/models/api-response.model';
import { ResultadoCalculo, Venta, VentaDetalleInput, VentaFormValue } from '../core/models/venta.model';

@Injectable({ providedIn: 'root' })
export class VentaService {
  private readonly apiUrl = `${environment.apiUrl}/ventas`;

  constructor(private http: HttpClient) {}

  getPaged(request: PagedRequest): Observable<ApiResponse<PagedResult<Venta>>> {
    let params = new HttpParams().set('page', request.page).set('pageSize', request.pageSize);
    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    return this.http.get<ApiResponse<PagedResult<Venta>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<Venta>> {
    return this.http.get<ApiResponse<Venta>>(`${this.apiUrl}/${id}`);
  }

  create(value: VentaFormValue): Observable<ApiResponse<Venta>> {
    return this.http.post<ApiResponse<Venta>>(this.apiUrl, value);
  }

  update(id: number, value: VentaFormValue): Observable<ApiResponse<Venta>> {
    return this.http.put<ApiResponse<Venta>>(`${this.apiUrl}/${id}`, value);
  }

  confirmar(id: number): Observable<ApiResponse<Venta>> {
    return this.http.post<ApiResponse<Venta>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  anular(id: number, motivoAnulacion: string): Observable<ApiResponse<Venta>> {
    return this.http.post<ApiResponse<Venta>>(`${this.apiUrl}/${id}/anular`, { motivoAnulacion });
  }

  deleteBorrador(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }

  /** Vista previa: calcula descuentos/impuestos reales sin guardar nada. */
  calcular(clienteId: number | null, codigoPromocional: string | null, detalles: VentaDetalleInput[]): Observable<ApiResponse<ResultadoCalculo>> {
    return this.http.post<ApiResponse<ResultadoCalculo>>(`${this.apiUrl}/calcular`, {
      clienteId, codigoPromocional, detalles
    });
  }
}
