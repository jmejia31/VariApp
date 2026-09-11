import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../core/models/api-response.model';
import { CategoriaCatalogoPublico, ProductoCatalogoPublico } from './varistorehn.models';

export type {
  CategoriaCatalogoPublico,
  ModeloCatalogoPublico,
  ProductoCatalogoPublico
} from './varistorehn.models';

@Injectable({ providedIn: 'root' })
export class VaristorehnService {
  private readonly http = inject(HttpClient);
  private readonly urlProductos = `${environment.apiUrl}/tienda/productos`;
  private readonly urlCategorias = `${environment.apiUrl}/tienda/categorias`;

  obtenerProductos(page = 1, pageSize = 48): Observable<ApiResponse<PagedResult<ProductoCatalogoPublico>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<ProductoCatalogoPublico>>>(this.urlProductos, { params });
  }

  /** Lee todas las paginas para que busqueda/categorias no se corten silenciosamente. */
  obtenerCatalogo(): Observable<ProductoCatalogoPublico[]> {
    const leer = (pagina: number) => this.obtenerProductos(pagina, 96).pipe(map(res => {
      const datos = res.data;
      if (!res.success || !datos || !Array.isArray(datos.items) || datos.page !== pagina
        || !Number.isSafeInteger(datos.totalCount) || datos.totalCount < 0
        || !Number.isSafeInteger(datos.pageSize) || datos.pageSize <= 0) {
        throw new Error('Respuesta de catálogo no válida.');
      }
      const paginas = Math.ceil(datos.totalCount / datos.pageSize);
      return { ...datos, totalPages: paginas };
    }));
    return leer(1).pipe(
      expand(datos => datos.page < datos.totalPages ? leer(datos.page + 1) : EMPTY),
      reduce((todos, pagina) => {
        pagina.items.forEach(item => todos.set(item.id, item));
        return todos;
      }, new Map<number, ProductoCatalogoPublico>()),
      map(todos => [...todos.values()])
    );
  }

  obtenerProductoPorSlug(slug: string): Observable<ProductoCatalogoPublico> {
    const seguro = this.slugSeguro(slug);
    if (!seguro) return throwError(() => new Error('Slug de producto no válido.'));
    return this.http.get<ApiResponse<ProductoCatalogoPublico>>(`${this.urlProductos}/${encodeURIComponent(seguro)}`).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error('Producto no encontrado.');
        return res.data;
      })
    );
  }

  obtenerCategorias(): Observable<CategoriaCatalogoPublico[]> {
    return this.http.get<ApiResponse<CategoriaCatalogoPublico[]>>(this.urlCategorias).pipe(map(res => {
      if (!res.success || !Array.isArray(res.data)) throw new Error('Respuesta de categorías no válida.');
      return res.data;
    }));
  }

  obtenerCategoriaPorSlug(slug: string): Observable<CategoriaCatalogoPublico> {
    const seguro = this.slugSeguro(slug);
    if (!seguro) return throwError(() => new Error('Slug de categoría no válido.'));
    return this.http.get<ApiResponse<CategoriaCatalogoPublico>>(`${this.urlCategorias}/${encodeURIComponent(seguro)}`).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error('Categoría no encontrada.');
        return res.data;
      })
    );
  }

  private slugSeguro(slug: string): string {
    const valor = slug.trim();
    return valor.length > 0 && valor.length <= 180 && /^[a-zA-Z0-9áéíóúüñÁÉÍÓÚÜÑ-]+$/.test(valor) ? valor : '';
  }
}
