import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../core/models/api-response.model';

export interface ProductoCatalogoPublico {
  id: number;
  nombre: string;
  descripcion?: string;
  categoriaNombre?: string;
  marcaNombre?: string;
  modeloNombre?: string;
  precio: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  imagenPrincipalUrl?: string;
  imagenes: Array<{ url: string; orden: number; esPrincipal: boolean }>;
  modelos: Array<ModeloCatalogoPublico>;
}

export interface ModeloCatalogoPublico {
  modeloId?: number;
  modeloNombre?: string;
  marcaNombre?: string;
  precio: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  imagenes: Array<{ url: string; orden: number; esPrincipal: boolean }>;
}

@Injectable({ providedIn: 'root' })
export class VaristorehnService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/tienda/productos`;

  obtenerProductos(page = 1, pageSize = 48): Observable<ApiResponse<PagedResult<ProductoCatalogoPublico>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<ProductoCatalogoPublico>>>(this.url, { params });
  }
}
