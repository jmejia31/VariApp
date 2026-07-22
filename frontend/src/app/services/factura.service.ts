import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { EnlaceCompartir, Factura, HistorialEnvio } from '../core/models/factura.model';

@Injectable({ providedIn: 'root' })
export class FacturaService {
  private readonly apiUrl = `${environment.apiUrl}/facturas`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Factura[]>> {
    return this.http.get<ApiResponse<Factura[]>>(this.apiUrl);
  }

  getById(id: number): Observable<ApiResponse<Factura>> {
    return this.http.get<ApiResponse<Factura>>(`${this.apiUrl}/${id}`);
  }

  getByVenta(ventaId: number): Observable<ApiResponse<Factura>> {
    return this.http.get<ApiResponse<Factura>>(`${this.apiUrl}/venta/${ventaId}`);
  }

  /** Único PDF oficial utilizado por descarga e impresión. */
  descargarPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  prepararWhatsApp(id: number): Observable<ApiResponse<EnlaceCompartir>> {
    return this.http.post<ApiResponse<EnlaceCompartir>>(`${this.apiUrl}/${id}/compartir/whatsapp`, {});
  }

  revocarEnlaces(id: number): Observable<ApiResponse<{ enlacesRevocados: number }>> {
    return this.http.post<ApiResponse<{ enlacesRevocados: number }>>(`${this.apiUrl}/${id}/compartir/revocar`, {});
  }

  registrarIntentoEnvio(
    id: number,
    canal: 'WhatsApp' | 'Correo',
    destinatario: string,
    resultado = 'Iniciado',
    error?: string
  ): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.apiUrl}/${id}/compartir/registrar`, {
      canal,
      destinatario,
      resultado,
      error
    });
  }

  getHistorialEnvios(id: number): Observable<ApiResponse<HistorialEnvio[]>> {
    return this.http.get<ApiResponse<HistorialEnvio[]>>(`${this.apiUrl}/${id}/historial-envios`);
  }

  enviarPorCorreo(id: number, destinatario: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.apiUrl}/${id}/compartir/correo`, { destinatario });
  }
}