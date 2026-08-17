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

test.describe('Reservas de inventario - formulario', () => {
  test('crea un borrador con clave física y cantidad', async ({ page }) => {
    await login(page);
    let payload: unknown;
    await page.route('**/reservas-inventario', async route => {
      if (route.request().method() !== 'POST') { await route.continue(); return; }
      payload = route.request().postDataJSON();
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Reserva creada correctamente.', data: { id: 901, numero: 'RES-E2E-901', ventaId: null, estado: 'Borrador', fechaCreacion: '2026-08-17T10:00:00Z', fechaExpiracion: null, fechaActivacion: null, fechaConsumo: null, fechaLiberacion: null, fechaExpiracionAplicada: null, fechaCancelacion: null, motivoLiberacion: null, motivoCancelacion: null, detalles: [{ id: 1, productoVarianteId: 15, almacenId: 3, ubicacionAlmacenId: 8, cantidadReservada: 4, cantidadConsumida: 0 }] } }) });
    });

    await page.goto('/inventario/reservas/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva reserva' })).toBeVisible();
    await page.getByLabel('Variante').fill('15');
    await page.getByLabel('Almacén').fill('3');
    await page.getByLabel('Ubicación').fill('8');
    await page.getByLabel('Cantidad').fill('4');
    await page.getByRole('button', { name: 'Guardar reserva' }).click();

    await expect(page).toHaveURL(/\/inventario\/reservas\/901$/);
    expect(payload).toMatchObject({ detalles: [{ productoVarianteId: 15, almacenId: 3, ubicacionAlmacenId: 8, cantidad: 4 }] });
  });
});
