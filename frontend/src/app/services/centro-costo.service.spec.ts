import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { TipoCentroCosto } from '../core/models/centro-costo.model';
import { CentroCostoService } from './centro-costo.service';

describe('N4.11.E CentroCostoService contract', () => {
  let service: CentroCostoService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [CentroCostoService] });
    service = TestBed.inject(CentroCostoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('serializes search filters and pagination without dropping false values', () => {
    service.buscar({
      termino: '  CC-01  ',
      tipo: TipoCentroCosto.Sucursal,
      sucursalId: 7,
      activo: false,
      pagina: 2,
      tamanoPagina: 25
    }).subscribe();

    const req = httpMock.expectOne(request => request.url === `${environment.apiUrl}/centros-costo`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('termino')).toBe('CC-01');
    expect(req.request.params.get('tipo')).toBe(String(TipoCentroCosto.Sucursal));
    expect(req.request.params.get('sucursalId')).toBe('7');
    expect(req.request.params.get('activo')).toBe('false');
    expect(req.request.params.get('pagina')).toBe('2');
    expect(req.request.params.get('tamanoPagina')).toBe('25');
    req.flush({ data: { items: [], total: 0, pagina: 2, tamanoPagina: 25 } });
  });

  it('uses the canonical activate/deactivate endpoints', () => {
    service.cambiarEstado(9, true).subscribe();
    const activar = httpMock.expectOne(`${environment.apiUrl}/centros-costo/9/activar`);
    expect(activar.request.method).toBe('PATCH');
    expect(activar.request.body).toEqual({});
    activar.flush({ data: { id: 9, activo: true } });

    service.cambiarEstado(9, false).subscribe();
    const desactivar = httpMock.expectOne(`${environment.apiUrl}/centros-costo/9/desactivar`);
    expect(desactivar.request.method).toBe('PATCH');
    expect(desactivar.request.body).toEqual({});
    desactivar.flush({ data: { id: 9, activo: false } });
  });

  it('targets the canonical delete endpoint', () => {
    service.delete(11).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/centros-costo/11`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ data: {} });
  });
});
