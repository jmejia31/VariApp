import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../core/models/api-response.model';
import { ProductoCatalogoPublico, ReferenciaCarrito } from './varistorehn.catalog';

export type { ModeloCatalogoPublico, ProductoCatalogoPublico } from './varistorehn.catalog';

@Injectable({ providedIn: 'root' })
export class VaristorehnService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/tienda/productos`;

  obtenerProductos(page = 1, pageSize = 48): Observable<ApiResponse<PagedResult<ProductoCatalogoPublico>>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<ProductoCatalogoPublico>>>(this.url, { params });
  }

  /** Read every API page so search/categories never silently stop at item 48. */
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

  /** Integration boundary only: a server must reprice, validate stock and create the payment session. */
  crearCheckoutTarjeta(endpoint: string, items: ReferenciaCarrito[], idempotencyKey: string): Observable<string> {
    if (!/^\/[a-zA-Z0-9/_-]+$/.test(endpoint) || endpoint.startsWith('//')) {
      return throwError(() => new Error('Endpoint de pago no válido.'));
    }
    return this.http.post<ApiResponse<{ checkoutUrl: string }>>(`${environment.apiUrl}${endpoint}`, {
      items: items.map(({ productoId, modeloClave, unidades }) => ({ productoId, modeloClave, cantidad: unidades }))
    }, { headers: { 'Idempotency-Key': idempotencyKey } }).pipe(map(res => {
      if (!res.success || typeof res.data?.checkoutUrl !== 'string') throw new Error('No se pudo iniciar el pago.');
      return res.data.checkoutUrl;
    }));
  }
}
