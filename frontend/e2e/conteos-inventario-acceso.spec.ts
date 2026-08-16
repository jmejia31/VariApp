import { test, expect } from '@playwright/test';

test.describe('Conteos físicos - acceso', () => {
  test('redirige al login cuando no existe sesión', async ({ page }) => {
    await page.goto('/inventario/conteos');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega la ruta a un usuario autenticado sin MovimientosInventario:Ver', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('inventoryapp_token', 'e2e-token-sin-permiso-conteos');
      localStorage.setItem('inventoryapp_user', 'e2e-conteos-sin-permiso');
      localStorage.setItem('inventoryapp_nombre_completo', 'E2E Conteos Sin Permiso');
      localStorage.setItem('inventoryapp_rol', 'Operador');
      localStorage.setItem('inventoryapp_expira_en', '2099-12-31T23:59:59Z');
    });

    await page.route('**/permisos/mis-permisos', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Permisos cargados',
          data: {
            permisos: ['Dashboard:Ver'],
            esAdministrador: false
          }
        })
      });
    });

    await page.goto('/inventario/conteos');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/inventario\/conteos(?:\?|$)/);
  });
});
