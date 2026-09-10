import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FormBuilder } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EmpresaAdministracionCardComponent } from './empresa-administracion-card.component';

describe('N6.1.E Empresa administration UX', () => {
  let httpMock: HttpTestingController;
  const permisos = { puede: vi.fn(() => true) };
  const snackBar = { open: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FormBuilder,
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: MatSnackBar, useValue: snackBar }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function createComponent(): EmpresaAdministracionCardComponent {
    return TestBed.runInInjectionContext(() => new EmpresaAdministracionCardComponent());
  }

  it('loads the list and settles the loading state', () => {
    const component = createComponent();
    component.ngOnInit();

    const req = httpMock.expectOne(`${environment.apiUrl}/empresas`);
    expect(req.request.method).toBe('GET');
    req.flush({ data: [{ id: 1, nombre: 'Empresa Uno', activa: true, fechaCreacion: '', fechaActualizacion: '' }] });

    expect(component.loading()).toBe(false);
    expect(component.empresas()).toHaveLength(1);
    expect(permisos.puede).toHaveBeenCalledWith('Configuracion', 'Crear');
    expect(permisos.puede).toHaveBeenCalledWith('Configuracion', 'Desactivar');
  });

  it('preserves an explicit inactive filter', () => {
    const component = createComponent();
    component.cambiarEstado('inactivas');

    const req = httpMock.expectOne(request => request.url === `${environment.apiUrl}/empresas`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('activa')).toBe('false');
    req.flush({ data: [] });

    expect(component.loading()).toBe(false);
    expect(component.empresas()).toEqual([]);
  });

  it('rejects a whitespace-only company name without sending a write', () => {
    const component = createComponent();
    component.nueva();
    component.form.setValue({ nombre: '   ' });
    component.guardar();

    expect(component.form.controls.nombre.hasError('whitespace')).toBe(true);
    httpMock.expectNone(`${environment.apiUrl}/empresas`);
  });

  it('uses the canonical state endpoint and updates the local row', () => {
    const component = createComponent();
    component.empresas.set([{ id: 7, nombre: 'Empresa Siete', activa: true, fechaCreacion: '', fechaActualizacion: '' }]);
    component.cambiarActivo(component.empresas()[0], false);

    const req = httpMock.expectOne(`${environment.apiUrl}/empresas/7/desactivar`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush({ data: { id: 7, nombre: 'Empresa Siete', activa: false, fechaCreacion: '', fechaActualizacion: '' } });

    expect(component.empresas()[0].activa).toBe(false);
    expect(component.operandoId()).toBeNull();
  });
});
