import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedRequest, PagedResult } from '../core/models/api-response.model';
import { Compra, CompraFormValue } from '../core/models/compra.model';

@Injectable({ providedIn: 'root' })
export class CompraService {
  private readonly apiUrl = `${environment.apiUrl}/compras`;

  constructor(private http: HttpClient) {}

  getPaged(request: PagedRequest): Observable<ApiResponse<PagedResult<Compra>>> {
    let params = new HttpParams().set('page', request.page).set('pageSize', request.pageSize);
    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    return this.http.get<ApiResponse<PagedResult<Compra>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<Compra>> {
    return this.http.get<ApiResponse<Compra>>(`${this.apiUrl}/${id}`);
  }

  create(value: CompraFormValue): Observable<ApiResponse<Compra>> {
    return this.http.post<ApiResponse<Compra>>(this.apiUrl, value);
  }

  update(id: number, value: CompraFormValue): Observable<ApiResponse<Compra>> {
    return this.http.put<ApiResponse<Compra>>(`${this.apiUrl}/${id}`, value);
  }

  confirmar(id: number): Observable<ApiResponse<Compra>> {
    return this.http.post<ApiResponse<Compra>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  anular(id: number, motivoAnulacion: string): Observable<ApiResponse<Compra>> {
    return this.http.post<ApiResponse<Compra>>(`${this.apiUrl}/${id}/anular`, { motivoAnulacion });
  }

  deleteBorrador(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
