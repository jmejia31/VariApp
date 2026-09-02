import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import {
  CreateCuentaBancariaDto,
  CuentaBancaria,
  CuentaBancariaQueryFilter,
  EstadoCuentaBancaria
} from '../models/cuenta-bancaria';
import { CuentaBancariaPage } from '../models/cuenta-bancaria-page';
import { CuentaBancariaService } from './cuenta-bancaria.service';

describe('CuentaBancariaService', () => {
  let service: CuentaBancariaService;
  let httpTestingController: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/cuentas-bancarias`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CuentaBancariaService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CuentaBancariaService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('serializa filtros y devuelve la página canónica', () => {
    const filter: CuentaBancariaQueryFilter = {
      page: 1,
      pageSize: 10,
      bancoId: 5,
      estado: EstadoCuentaBancaria.Activa,
      moneda: 'USD',
      searchTerm: 'test'
    };
    const response: CuentaBancariaPage<CuentaBancaria> = {
      items: [
        {
          id: 1,
          bancoId: 5,
          nombre: 'Test',
          numeroCuenta: '123',
          moneda: 'USD',
          saldoInicial: 100,
          estado: EstadoCuentaBancaria.Activa
        }
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1
    };

    service.getAll(filter).subscribe(result => expect(result).toEqual(response));

    const req = httpTestingController.expectOne(request =>
      request.url === apiUrl &&
      request.params.get('page') === '1' &&
      request.params.get('pageSize') === '10' &&
      request.params.get('bancoId') === '5' &&
      request.params.get('estado') === '1' &&
      request.params.get('moneda') === 'USD' &&
      request.params.get('searchTerm') === 'test'
    );
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('obtiene cuentas activas', () => {
    const response: CuentaBancaria[] = [
      {
        id: 1,
        bancoId: 2,
        nombre: 'Active',
        numeroCuenta: '456',
        moneda: 'HNL',
        saldoInicial: 0,
        estado: EstadoCuentaBancaria.Activa
      }
    ];

    service.getActivas().subscribe(result => expect(result).toEqual(response));

    const req = httpTestingController.expectOne(`${apiUrl}/activas`);
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('obtiene una cuenta por id', () => {
    const response: CuentaBancaria = {
      id: 3,
      bancoId: 1,
      nombre: 'ById',
      numeroCuenta: '789',
      moneda: 'EUR',
      saldoInicial: 50,
      estado: EstadoCuentaBancaria.Inactiva
    };

    service.getById(3).subscribe(result => expect(result).toEqual(response));

    const req = httpTestingController.expectOne(`${apiUrl}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('crea una cuenta', () => {
    const dto: CreateCuentaBancariaDto = {
      bancoId: 4,
      nombre: 'New',
      numeroCuenta: '000',
      moneda: 'USD',
      saldoInicial: 1000
    };
    const response: CuentaBancaria = {
      ...dto,
      id: 10,
      estado: EstadoCuentaBancaria.Activa
    };

    service.create(dto).subscribe(result => expect(result).toEqual(response));

    const req = httpTestingController.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(response);
  });

  it('activa una cuenta', () => {
    service.activar(5).subscribe();
    const req = httpTestingController.expectOne(`${apiUrl}/5/activar`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });

  it('desactiva una cuenta', () => {
    service.desactivar(6).subscribe();
    const req = httpTestingController.expectOne(`${apiUrl}/6/desactivar`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });
});
