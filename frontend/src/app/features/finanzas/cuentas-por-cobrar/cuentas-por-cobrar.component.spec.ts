import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { environment } from '../../../../environments/environment';
import { Factura } from '../../../core/models/factura.model';
import { CuentasPorCobrarComponent } from './cuentas-por-cobrar.component';

describe('CuentasPorCobrarComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CuentasPorCobrarComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNoopAnimations()]
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('carga y muestra la proyección read-only de facturas pendientes', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();

    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    expect(request.request.method).toBe('GET');

    const factura = {
      numeroFactura: 'FAC-001',
      clienteNombre: 'Cliente QA',
      fechaVencimiento: '2026-08-30T00:00:00Z',
      estado: 'Emitida',
      total: 1000,
      totalPagado: 250,
      saldoPendiente: 750,
      moneda: 'HNL'
    } as unknown as Factura;

    request.flush({ success: true, data: [factura], message: '', errors: [] });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('FAC-001');
    expect(text).toContain('Cliente QA');
    expect(text).toContain('750.00 HNL');
  });

  it('maneja el error 403 Forbidden correctamente', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();

    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    request.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.items().length).toBe(0);
    expect(component.error()).toBe('No tiene permiso Facturacion/Ver para consultar esta información.');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('No tiene permiso Facturacion/Ver para consultar esta información.');
  });

  it('maneja un error de contrato cuando la API responde success=false', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();

    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    request.flush({ success: false, message: 'Custom API Error', data: [], errors: [] });
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.items().length).toBe(0);
    expect(component.error()).toBe('Custom API Error');
  });
});
