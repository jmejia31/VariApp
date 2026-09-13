import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { SuscripcionSaaSService } from './suscripcion-saas.service';

describe('SuscripcionSaaSService', () => {
  let service: SuscripcionSaaSService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SuscripcionSaaSService]
    });
    service = TestBed.inject(SuscripcionSaaSService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('consulta el entitlement normalizado dentro del tenant verificado', () => {
    const response = {
      data: {
        moduloClave: 'INVENTARIO',
        habilitado: true,
        motivo: 1,
        planId: 10,
        planCodigo: 'PRO'
      }
    };

    service.obtenerEntitlementModulo(17, ' inventario ').subscribe(res => expect(res).toEqual(response as any));

    const req = httpMock.expectOne(
      `${environment.apiUrl}/saas/tenants/17/modulos/INVENTARIO/entitlement`
    );
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('falla cerrado antes de HTTP cuando falta la clave del módulo', () => {
    expect(() => service.obtenerEntitlementModulo(17, '   ')).toThrowError('La clave del módulo es obligatoria.');
  });

  it('falla antes de HTTP cuando el tenant no es válido', () => {
    expect(() => service.obtenerEntitlementModulo(0, 'VENTAS')).toThrowError('Se requiere un tenant verificado.');
  });
});
