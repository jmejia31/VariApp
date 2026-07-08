import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedRequest, PagedResult } from '../core/models/api-response.model';
import { Producto, ProductoFormValue } from '../core/models/producto.model';

@Injectable({ providedIn: 'root' })
export class ProductoService {
  private readonly apiUrl = `${environment.apiUrl}/productos`;

  constructor(private http: HttpClient) {}

  getPaged(request: PagedRequest): Observable<ApiResponse<PagedResult<Producto>>> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);

    return this.http.get<ApiResponse<PagedResult<Producto>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<Producto>> {
    return this.http.get<ApiResponse<Producto>>(`${this.apiUrl}/${id}`);
  }

  create(value: ProductoFormValue): Observable<ApiResponse<Producto>> {
    return this.http.post<ApiResponse<Producto>>(this.apiUrl, this.toFormData(value));
  }

  update(id: number, value: ProductoFormValue): Observable<ApiResponse<Producto>> {
    return this.http.put<ApiResponse<Producto>>(`${this.apiUrl}/${id}`, this.toFormData(value));
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }

  private toFormData(value: ProductoFormValue): FormData {
    const formData = new FormData();
    formData.append('nombre', value.nombre);
    formData.append('marca', value.marca);
    formData.append('modelo', value.modelo);
    if (value.descripcion) formData.append('descripcion', value.descripcion);
    formData.append('cantidad', String(value.cantidad));
    formData.append('costo', String(value.costo));
    formData.append('precio', String(value.precio));
    formData.append('umbralStockBajo', String(value.umbralStockBajo));
    if (value.imagen) formData.append('imagen', value.imagen);
    if (value.eliminarImagen) formData.append('eliminarImagen', 'true');
    return formData;
  }
}
