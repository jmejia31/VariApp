import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { MovimientoInventario } from '../core/models/movimiento-inventario.model';

@Injectable({ providedIn: 'root' })
export class MovimientoInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/inventario/movimientos`;

  constructor(private http: HttpClient) {}

  getFiltered(productoId?: number, tipo?: string): Observable<ApiResponse<MovimientoInventario[]>> {
    let params = new HttpParams();
    if (productoId) params = params.set('productoId', productoId);
    if (tipo) params = params.set('tipo', tipo);
    return this.http.get<ApiResponse<MovimientoInventario[]>>(this.apiUrl, { params });
  }
}
