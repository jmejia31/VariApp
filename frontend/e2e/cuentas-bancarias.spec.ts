import { expect, Page, test, APIRequestContext, APIResponse } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'MISSING_SECRET_IN_ENV';

let adminToken = '';

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });

  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  expect(data?.token).toBeTruthy();
  return data.token;
}

async function prepararSesion(page: Page, token: string): Promise<void> {
  await page.addInitScript((t) => {
    localStorage.setItem('inventoryapp_token', t);
    localStorage.setItem('inventoryapp_user', 'e2e_admin');
    localStorage.setItem('inventoryapp_rol', 'Administrador');
  }, token);
}

test.describe('Cuentas Bancarias E2E y Accesibilidad N4.2.E', () => {
  test.beforeAll(async ({ request }) => {
    adminToken = await loginApi(request);
  });

  test('valida renderizado y estado de accesibilidad para interacciones clave', async ({ page }) => {
    await prepararSesion(page, adminToken);
    await page.goto('/cuentas-bancarias');

    const heading = page.getByRole('heading', { name: 'Cuentas Bancarias' });
    await expect(heading).toBeVisible({ timeout: 5000 });

    const nuevaCuentaBtn = page.getByRole('button', { name: 'Nueva Cuenta' });
    await expect(nuevaCuentaBtn).toHaveAttribute('aria-expanded', 'false');

    await nuevaCuentaBtn.click();

    await expect(nuevaCuentaBtn).toHaveAttribute('aria-expanded', 'true');
    await expect(page.getByRole('heading', { name: 'Registrar Nueva Cuenta' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Nombre de la cuenta' })).toBeVisible();
  });
});
