import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { SucursalService } from '../../services/sucursal.service';
import { SucursalFormComponent } from './sucursal-form.component';

describe('SucursalFormComponent tenant ownership', () => {
  let fixture: ComponentFixture<SucursalFormComponent>;
  let component: SucursalFormComponent;
  const sucursalService = {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn()
  };
  const router = {
    navigate: vi.fn()
  };

  beforeEach(async () => {
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [SucursalFormComponent],
      providers: [
        { provide: SucursalService, useValue: sucursalService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } }
        }
      ]
    })
      .overrideComponent(SucursalFormComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(SucursalFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('requires an explicit EmpresaId greater than zero', () => {
    const empresaId = component.form.controls.empresaId;

    expect(empresaId.hasError('required')).toBe(true);

    empresaId.setValue(0);
    expect(empresaId.hasError('min')).toBe(true);

    empresaId.setValue(7);
    expect(empresaId.valid).toBe(true);
  });

  it('does not write a sucursal without tenant ownership', () => {
    component.form.patchValue({
      empresaId: null,
      codigo: 'TGU-01',
      nombre: 'Tegucigalpa',
      zonaHoraria: 'America/Tegucigalpa'
    });

    component.submit();

    expect(sucursalService.create).not.toHaveBeenCalled();
    expect(sucursalService.update).not.toHaveBeenCalled();
  });

  it('sends the explicit EmpresaId in the create contract', () => {
    sucursalService.create.mockReturnValue(of({ data: {} } as never));
    component.form.patchValue({
      empresaId: 7,
      codigo: 'TGU-01',
      nombre: 'Tegucigalpa',
      direccion: '',
      telefono: '',
      correo: '',
      zonaHoraria: 'America/Tegucigalpa'
    });

    component.submit();

    expect(sucursalService.create).toHaveBeenCalledWith(expect.objectContaining({ empresaId: 7 }));
    expect(router.navigate).toHaveBeenCalledWith(['/sucursales']);
  });
});
