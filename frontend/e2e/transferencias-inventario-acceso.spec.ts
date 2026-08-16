import { test, expect } from '@playwright/test';

test.describe('Transferencias de inventario - acceso', () => {
  test('redirige al login cuando no existe sesión', async ({ page }) => {
    await page.goto('/inventario/transferencias');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega la ruta a un usuario autenticado sin MovimientosInventario:Ver', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('token', 'e2e-token');
      localStorage.setItem('usuario', JSON.stringify({
        id: 999,
        nombreUsuario: 'e2e-transferencias-sin-permiso',
        nombreCompleto: 'E2E Transferencias Sin Permiso',
        rol: 'Operador'
      }));
    });

    await page.route('**/permisos/mis-permisos', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data: {
            permisos: ['Dashboard:Ver'],
            esAdministrador: false
          }
        })
      });
    });

    await page.goto('/inventario/transferencias');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });
});
