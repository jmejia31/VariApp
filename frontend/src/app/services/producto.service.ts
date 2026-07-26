import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedRequest, PagedResult } from '../core/models/api-response.model';
import { Producto, ProductoFormValue } from '../core/models/producto.model';

export interface ProductoPagedRequest extends PagedRequest {
  categoriaId?: number;
  colorId?: number;
  tallaId?: number;
  marcaId?: number;
  modeloId?: number;
  activo?: boolean;
  agotado?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductoService {
  private readonly apiUrl = `${environment.apiUrl}/productos`;

  constructor(private http: HttpClient) {}

  getPaged(request: ProductoPagedRequest): Observable<ApiResponse<PagedResult<Producto>>> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    if (request.categoriaId != null) params = params.set('categoriaId', request.categoriaId);
    if (request.colorId != null) params = params.set('colorId', request.colorId);
    if (request.tallaId != null) params = params.set('tallaId', request.tallaId);
    if (request.marcaId != null) params = params.set('marcaId', request.marcaId);
    if (request.modeloId != null) params = params.set('modeloId', request.modeloId);
    if (request.activo != null) params = params.set('activo', request.activo);
    if (request.agotado != null) params = params.set('agotado', request.agotado);

    return this.http.get<ApiResponse<PagedResult<Producto>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<Producto>> {
    return this.http.get<ApiResponse<Producto>>(`${this.apiUrl}/${id}`);
  }

  create(value: ProductoFormValue): Observable<ApiResponse<Producto>> {
    const formData = new FormData();
    this.appendCamposBase(formData, value);
    (value.imagenesNuevas ?? []).forEach((file) => formData.append('Imagenes', file));
    return this.http.post<ApiResponse<Producto>>(this.apiUrl, formData);
  }

  update(id: number, value: ProductoFormValue): Observable<ApiResponse<Producto>> {
    const formData = new FormData();
    this.appendCamposBase(formData, value);

    (value.imagenesNuevas ?? []).forEach((file) => formData.append('ImagenesNuevas', file));
    (value.imagenesAEliminarIds ?? []).forEach((imagenId) =>
      formData.append('ImagenesAEliminarIds', String(imagenId))
    );
    if (value.imagenPrincipalId != null) {
      formData.append('ImagenPrincipalId', String(value.imagenPrincipalId));
    }

    return this.http.put<ApiResponse<Producto>>(`${this.apiUrl}/${id}`, formData);
  }

  activar(id: number): Observable<ApiResponse<Producto>> {
    return this.http.patch<ApiResponse<Producto>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Producto>> {
    return this.http.patch<ApiResponse<Producto>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }

  descargarImagen(productoId: number, imagenId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${productoId}/imagenes/${imagenId}/descargar`,
      { responseType: 'blob' }
    );
  }

  descargarTodasLasImagenes(productoId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${productoId}/imagenes/descargar-todas`,
      { responseType: 'blob' }
    );
  }

  private appendCamposBase(formData: FormData, value: ProductoFormValue): void {
    formData.append('Nombre', value.nombre);
    formData.append('Marca', value.marca);
    formData.append('Modelo', value.modelo);
    if (value.descripcion) formData.append('Descripcion', value.descripcion);
    formData.append('Cantidad', String(value.cantidad));
    formData.append('Costo', String(value.costo));
    formData.append('Precio', String(value.precio));
    formData.append('UmbralStockBajo', String(value.umbralStockBajo));
    if (value.categoriaId != null) formData.append('CategoriaId', String(value.categoriaId));
    if (value.colorId != null) formData.append('ColorId', String(value.colorId));
    if (value.tallaId != null) formData.append('TallaId', String(value.tallaId));
    if (value.marcaId != null) formData.append('MarcaId', String(value.marcaId));
    if (value.modeloId != null) formData.append('ModeloId', String(value.modeloId));
  }
}
