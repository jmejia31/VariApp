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

function respuestaListado() {
  return {
    success: true,
    message: '',
    errors: [],
    data: {
      items: [{
        id: 41,
        numeroRecepcion: 'RC-2026-000041',
        ordenCompraId: 17,
        numeroOrdenCompra: 'OC-2026-000017',
        estado: 1,
        observaciones: null,
        fechaRecepcionUtc: null,
        recibidaPorUsuarioId: null,
        recibidaPorNombreSnapshot: null,
        fechaAnulacionUtc: null,
        anuladaPorUsuarioId: null,
        motivoAnulacion: null,
        cantidadRecibidaTotal: 8,
        cantidadAceptadaTotal: 7,
        cantidadDanadaTotal: 1,
        cantidadFaltanteTotal: 2,
        cantidadSobranteTotal: 0,
        detalles: []
      }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    }
  };
}

test.describe('Recepción de mercancía - shell y acceso', () => {
  test('deniega la ruta a un usuario autenticado sin Compras:Ver', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver']);

    await page.goto('/recepciones-compra');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });

  test('lista recepciones y aplica filtros contra el contrato paginado', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);

    const urls: string[] = [];
    await page.route('**/recepciones-compra**', async route => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }
      urls.push(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(respuestaListado())
      });
    });

    await page.goto('/recepciones-compra');
    await expect(page.getByRole('heading', { name: 'Recepción de mercancía' })).toBeVisible();
    await expect(page.getByText('RC-2026-000041')).toBeVisible();
    await expect(page.getByText('OC-2026-000017')).toBeVisible();
    await expect(page.getByText('Borrador', { exact: true })).toBeVisible();

    await page.locator('input[name="ordenCompraId"]').fill('17');
    await page.locator('mat-select[name="estado"]').click();
    await page.getByRole('option', { name: 'Borrador', exact: true }).click();
    await page.getByRole('button', { name: 'Filtrar' }).click();

    await expect.poll(() => urls.length).toBeGreaterThan(1);
    const filtrada = new URL(urls.at(-1)!);
    expect(filtrada.searchParams.get('ordenCompraId')).toBe('17');
    expect(filtrada.searchParams.get('estado')).toBe('Borrador');
    expect(filtrada.searchParams.get('page')).toBe('1');
    expect(filtrada.searchParams.get('pageSize')).toBe('20');
  });
});
