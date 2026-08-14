import { expect, test } from '@playwright/test';

test.describe('Ajustes de inventario - control de acceso', () => {
  test('redirige a login cuando se intenta abrir el módulo sin sesión autenticada', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });
});
