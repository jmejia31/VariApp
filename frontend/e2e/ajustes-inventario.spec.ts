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

test.describe('N0.7.E - Ajustes de inventario', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeEach(async ({ page }) => {
    await loginUi(page);
  });

  test('expone listado y navegación de ajustes', async ({ page }) => {
    await page.goto('/inventario/ajustes');
    await expect(page.getByRole('heading', { name: 'Ajustes de inventario' })).toBeVisible();

    const rows = page.locator('tbody tr');
    if ((await rows.count()) === 0) {
      await expect(page.getByText(/No hay ajustes para los filtros seleccionados/i)).toBeVisible();
      return;
    }

    const firstRow = rows.first();
    const ver = firstRow.getByRole('button', { name: /^Ver$/i });
    await ver.click();
    await expect(page).toHaveURL(/\/inventario\/ajustes\/\d+$/);
  });

  test('creación expone formulario y detalles dinámicos', async ({ page }) => {
    await page.goto('/inventario/ajustes/nuevo');

    await expect(page.getByRole('heading', { name: 'Nuevo ajuste' })).toBeVisible();
    await expect(page.locator('form')).toBeVisible();

    const details = page.locator('article.detail');
    await expect(details).toHaveCount(1);

    await page.getByRole('button', { name: /Agregar detalle/i }).click();
    await expect(details).toHaveCount(2);

    await page.getByRole('button', { name: 'Eliminar detalle' }).last().click();
    await expect(details).toHaveCount(1);
  });

  test('edición sólo aparece para borradores y abre la ruta correcta', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    const borrador = page.locator('tbody tr').filter({ hasText: 'Borrador' }).first();
    if ((await borrador.count()) === 0) return;

    await borrador.getByRole('button', { name: /^Editar$/i }).click();
    await expect(page).toHaveURL(/\/inventario\/ajustes\/\d+\/editar$/);
    await expect(page.getByRole('heading', { name: 'Editar borrador' })).toBeVisible();
  });

  test('confirmación usa confirmación explícita antes de aplicar inventario', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    const borrador = page.locator('tbody tr').filter({ hasText: 'Borrador' }).first();
    if ((await borrador.count()) === 0) return;

    page.once('dialog', async (dialog) => {
      expect(dialog.type()).toBe('confirm');
      expect(dialog.message()).toMatch(/Confirmar el ajuste/i);
      await dialog.dismiss();
    });

    await borrador.getByRole('button', { name: /^Confirmar$/i }).click();
    await expect(borrador).toContainText('Borrador');
  });

  test('anulación exige motivo y conserva el ajuste si se cancela', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    const confirmado = page.locator('tbody tr').filter({ hasText: 'Confirmado' }).first();
    if ((await confirmado.count()) === 0) return;

    page.once('dialog', async (dialog) => {
      expect(dialog.type()).toBe('prompt');
      expect(dialog.message()).toMatch(/Motivo obligatorio para anular/i);
      await dialog.dismiss();
    });

    await confirmado.getByRole('button', { name: /^Anular$/i }).click();
    await expect(confirmado).toContainText('Confirmado');
  });
});
