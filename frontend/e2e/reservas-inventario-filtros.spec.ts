import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test('filtra reservas usando el catálogo activo de almacenes', async ({ page }) => {
  await login(page);
  const consultas: string[] = [];
  await page.route('**/almacenes/activos', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: [{ id: 11, codigo: 'BOD-11', nombre: 'Bodega Central', activo: true }] }) }));
  await page.route('**/reservas-inventario**', async route => {
    consultas.push(route.request().url());
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: { items: [], totalCount: 0, page: 1, pageSize: 20 } }) });
  });

  await page.goto('/inventario/reservas');
  const almacen = page.getByLabel('Almacén');
  await expect(almacen).toBeVisible();
  await almacen.locator('.mat-mdc-select-trigger').click();
  await expect(almacen).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('option', { name: 'BOD-11 · Bodega Central' }).click();
  await expect(almacen).toHaveAttribute('aria-expanded', 'false');
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
  await page.getByRole('button', { name: 'Filtrar', exact: true }).click();

  await expect.poll(() => consultas.some(url => new URL(url).searchParams.get('almacenId') === '11')).toBeTruthy();
});
