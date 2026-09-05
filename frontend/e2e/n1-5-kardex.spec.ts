import { test, expect, Page } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

test.describe('ERP-N1.5 — Kardex empresarial', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('UI usa consulta paginada, filtros empresariales y no desborda en móvil', async ({ page }) => {
    await loginUi(page);

    const cargaInicial = page.waitForResponse(response =>
      response.url().includes('/inventario/movimientos/paged') && response.request().method() === 'GET'
    );
    await page.goto('/inventario/movimientos');
    expect((await cargaInicial).status()).toBe(200);

    await expect(page.getByRole('heading', { name: 'Kardex de inventario', exact: true })).toBeVisible();
    await expect(page.getByText('Filtros del Kardex', { exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Producto', exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Variante', exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Almacén', exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Ubicación', exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Tipo de movimiento', exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Causa', exact: true })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Documento origen', exact: true })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Correlation ID', exact: true })).toBeVisible();

    await page.getByRole('textbox', { name: 'Correlation ID', exact: true }).fill('venta:999999:confirmar');
    const filtrada = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.endsWith('/inventario/movimientos/paged') &&
        url.searchParams.get('correlationId') === 'venta:999999:confirmar' &&
        url.searchParams.get('page') === '1';
    });
    await page.getByRole('button', { name: 'Aplicar filtros' }).click();
    expect((await filtrada).status()).toBe(200);

    const fechas = page.locator('input[type="date"]');
    await fechas.nth(0).fill('2026-08-16');
    await fechas.nth(1).fill('2026-08-15');
    const rangoInvalido = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.endsWith('/inventario/movimientos/paged') &&
        url.searchParams.get('desde') === '2026-08-16' &&
        url.searchParams.get('hasta') === '2026-08-15';
    });
    await page.getByRole('button', { name: 'Aplicar filtros' }).click();
    expect((await rangoInvalido).status()).toBe(400);
    await expect(page.getByRole('alert')).toContainText('fecha inicial', { ignoreCase: true });

    await page.setViewportSize({ width: 390, height: 844 });
    const recargaMovil = page.waitForResponse(response =>
      response.url().includes('/inventario/movimientos/paged') && response.request().method() === 'GET'
    );
    await page.getByRole('button', { name: 'Limpiar' }).click();
    expect((await recargaMovil).status()).toBe(200);

    const layout = await page.evaluate(() => ({
      viewport: document.documentElement.clientWidth,
      documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      tableScrollWidth: (document.querySelector('.table-scroll') as HTMLElement | null)?.clientWidth ?? 0,
      tableContentWidth: (document.querySelector('.table') as HTMLElement | null)?.scrollWidth ?? 0
    }));
    expect(layout.documentWidth - layout.viewport).toBeLessThanOrEqual(1);
    expect(layout.tableScrollWidth).toBeGreaterThan(0);
    expect(layout.tableContentWidth).toBeGreaterThan(layout.tableScrollWidth);
  });
});
