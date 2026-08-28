import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  CajaFlujoShellComponent,
  CajaSesionVista,
  CajaVista,
  MovimientoCajaUi
} from './caja-flujo-shell.component';

describe('CajaFlujoShellComponent', () => {
  let fixture: ComponentFixture<CajaFlujoShellComponent>;
  let component: CajaFlujoShellComponent;

  const cajaActiva: CajaVista = {
    id: 1,
    nombre: 'Caja principal',
    estado: 2,
    sesionActivaId: null
  };

  const sesionOperaciones: CajaSesionVista = {
    id: 10,
    cajaId: 1,
    usuarioId: 99,
    fechaApertura: '2026-08-28T08:00:00',
    fechaCierre: null,
    estado: 2,
    fondoInicial: 1000,
    totalIngresos: 250,
    totalRetiros: 0,
    totalDepositos: 0,
    saldoEsperado: null,
    saldoContado: null,
    diferencia: null,
    observacionesArqueo: null,
    movimientos: []
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CajaFlujoShellComponent, NoopAnimationsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(CajaFlujoShellComponent);
    component = fixture.componentInstance;
    component.caja = cajaActiva;
    fixture.detectChanges();
  });

  it('bloquea acciones operativas cuando no existe permiso UI', () => {
    component.puedeOperar = false;
    component.fondoInicial = 500;
    const aperturaSpy = spyOn(component.aperturaSolicitada, 'emit');
    const accionSpy = spyOn(component.accion, 'emit');

    component.solicitarApertura();

    expect(aperturaSpy).not.toHaveBeenCalled();
    expect(accionSpy).not.toHaveBeenCalled();
  });

  it('emite el fondo inicial validado al solicitar apertura', () => {
    component.puedeOperar = true;
    component.fondoInicial = 500;
    const aperturaSpy = spyOn(component.aperturaSolicitada, 'emit');
    const accionSpy = spyOn(component.accion, 'emit');

    component.solicitarApertura();

    expect(aperturaSpy).toHaveBeenCalledOnceWith({ fondoInicial: 500 });
    expect(accionSpy).toHaveBeenCalledOnceWith('ABRIR');
  });

  it('rechaza movimientos con monto no positivo', () => {
    component.puedeOperar = true;
    component.tipoMovimientoSeleccionado = 1;
    component.montoMovimiento = 0;
    const movimientoSpy = spyOn(component.movimientoSolicitado, 'emit');

    component.solicitarMovimiento();

    expect(movimientoSpy).not.toHaveBeenCalled();
  });

  it('emite tipo, monto y referencia normalizada para un movimiento válido', () => {
    component.puedeOperar = true;
    component.sesion = sesionOperaciones;
    component.tipoMovimientoSeleccionado = 3;
    component.montoMovimiento = 120.5;
    component.referenciaMovimiento = '  depósito #42  ';
    const movimientoSpy = spyOn(component.movimientoSolicitado, 'emit');

    component.solicitarMovimiento();

    const esperado: MovimientoCajaUi = { tipo: 3, monto: 120.5, referencia: 'depósito #42' };
    expect(movimientoSpy).toHaveBeenCalledOnceWith(esperado);
  });

  it('acepta saldo contado cero y normaliza observaciones de arqueo', () => {
    component.puedeOperar = true;
    component.saldoContado = 0;
    component.observacionesArqueo = '  sin efectivo  ';
    const arqueoSpy = spyOn(component.arqueoSolicitado, 'emit');

    component.solicitarArqueo();

    expect(arqueoSpy).toHaveBeenCalledOnceWith({
      saldoContado: 0,
      observacionesArqueo: 'sin efectivo'
    });
  });
});
