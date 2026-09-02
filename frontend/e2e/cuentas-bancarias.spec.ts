import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

test.describe('Cuentas Bancarias E2E y Accesibilidad N4.2.E', () => {
  test.beforeEach(async ({ page }) => {
    await loginUi(page);
  });

  test('valida renderizado y estado de accesibilidad para interacciones clave', async ({ page }) => {
    await page.goto('/cuentas-bancarias');

    const heading = page.getByRole('heading', { name: 'Cuentas Bancarias' });
    await expect(heading).toBeVisible({ timeout: 5000 });

    const nuevaCuentaBtn = page.getByRole('button', { name: 'Nueva Cuenta' });
    await expect(nuevaCuentaBtn).toHaveAttribute('aria-expanded', 'false');

    await nuevaCuentaBtn.click();

    await expect(nuevaCuentaBtn).toHaveAttribute('aria-expanded', 'true');
    await expect(page.getByRole('heading', { name: 'Registrar Nueva Cuenta' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Nombre de la cuenta' })).toBeVisible();
    await expect(page.getByRole('search', { name: 'Filtros de búsqueda' })).toBeVisible();
    await expect(page.getByRole('table', { name: 'Tabla de cuentas bancarias' })).toBeVisible();
  });
});
