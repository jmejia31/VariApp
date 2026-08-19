import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: { permisos, esAdministrador: false }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test.describe('Órdenes de compra - error y recuperación N2.2.G.2', () => {
  test('falla cerrado, no conserva filas stale y permite reintentar la consulta', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);

    await page.route('**/proveedores/activos', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: '', errors: [], data: [] })
    }));

    let intento = 0;
    await page.route('**/ordenes-compra?**', async route => {
      intento++;
      if (intento === 1) {
        await route.fulfill({
          status: 503,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            type: 'about:blank',
            title: 'Servicio temporalmente no disponible',
            status: 503,
            detail: 'Fallo causal simulado para QA'
          })
        });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: '',
          errors: [],
          data: {
            items: [{
              id: 91,
              numeroOrden: 'OC-000091',
              estado: 1,
              solicitudCompraId: null,
              proveedorId: 9,
              proveedorNombre: 'Proveedor recuperado',
              moneda: 'HNL',
              condicionesCompra: null,
              fechaEsperadaUtc: null,
              observaciones: null,
              subtotal: 500,
              descuento: 0,
              impuesto: 0,
              total: 500,
              fechaEnvioAprobacionUtc: null,
              fechaAprobacionUtc: null,
              fechaCancelacionUtc: null,
              detalles: []
            }],
            page: 1,
            pageSize: 10,
            totalCount: 1,
            totalPages: 1
          }
        })
      });
    });

    await page.goto('/ordenes-compra');

    const alerta = page.getByRole('alert');
    await expect(alerta).toContainText('No fue posible cargar las órdenes de compra. Intenta nuevamente.');
    await expect(page.getByText('OC-000091', { exact: true })).toHaveCount(0);

    await alerta.getByRole('button', { name: 'Reintentar' }).click();

    await expect(page.getByText('OC-000091', { exact: true })).toBeVisible();
    await expect(page.getByText('Proveedor recuperado', { exact: true })).toBeVisible();
    await expect(page.getByRole('alert')).toHaveCount(0);
    expect(intento).toBe(2);
  });
});
