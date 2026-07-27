import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  EnlaceCompartir,
  Factura,
  FacturaFormatoCodigo,
  FacturaFormatoPdf,
  HistorialEnvio
} from '../core/models/factura.model';

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

  getFormatosPdf(): Observable<ApiResponse<FacturaFormatoPdf[]>> {
    return this.http.get<ApiResponse<FacturaFormatoPdf[]>>(`${this.apiUrl}/formatos-pdf`);
  }

  /**
   * Genera el PDF oficial con un perfil explícito. A4 permanece como valor
   * predeterminado para mantener compatibilidad con enlaces y correo.
   */
  descargarPdf(id: number, formato: FacturaFormatoCodigo = 'a4'): Observable<Blob> {
    const params = new HttpParams().set('formato', formato);
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { params, responseType: 'blob' });
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
