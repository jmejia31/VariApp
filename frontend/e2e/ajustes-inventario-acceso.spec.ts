import { expect, test } from '@playwright/test';

test.describe('Ajustes de inventario - control de acceso', () => {
  test('redirige a login cuando se intenta abrir el módulo sin sesión autenticada', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega el módulo a un usuario autenticado sin permiso Inventario:Ver', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('inventoryapp_token', 'e2e-token-sin-permiso-inventario');
      localStorage.setItem('inventoryapp_user', 'e2e-sin-inventario');
      localStorage.setItem('inventoryapp_nombre_completo', 'E2E Sin Inventario');
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
            esAdministrador: false,
            permisos: ['Dashboard:Ver']
          }
        })
      });
    });

    await page.goto('/inventario/ajustes');

    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/inventario\/ajustes(?:\?|$)/);
  });
});
