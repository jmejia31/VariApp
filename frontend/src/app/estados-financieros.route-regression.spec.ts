import { authGuard } from './core/guards/auth.guard';
import { permisoGuard } from './core/guards/permiso.guard';
import { routes } from './app.routes';
import { EstadosFinancierosComponent } from './features/estados-financieros/estados-financieros.component';

describe('N4.10.G.8 Estados Financieros route regression', () => {
  const route = routes.find(candidate => candidate.path === 'estados-financieros');

  it('keeps the canonical estados-financieros path wired to its standalone component', async () => {
    expect(route).withContext('Missing /estados-financieros route').toBeDefined();
    expect(route?.loadComponent).withContext('Route must lazy-load EstadosFinancierosComponent').toBeDefined();

    const component = await route!.loadComponent!();
    expect(component).toBe(EstadosFinancierosComponent);
  });

  it('keeps authentication and Finanzas/Ver permission metadata fail-closed', () => {
    expect(route).withContext('Missing /estados-financieros route').toBeDefined();
    expect(route?.canActivate).toEqual([authGuard, permisoGuard]);
    expect(route?.data).toEqual(jasmine.objectContaining({ modulo: 'Finanzas', accion: 'Ver' }));
  });
});
