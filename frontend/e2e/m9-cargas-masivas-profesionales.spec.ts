import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;
let token = '';

function headers(): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  return (await dataOf(response)).token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

test.describe('M9 — cargas masivas profesionales', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);
  });

  test('publica contrato versionado y rechaza plantillas obsoletas', async ({ request }) => {
    const configResponse = await request.get(`${API_URL}/cargas-masivas/configuracion`, { headers: headers() });
    expect(configResponse.status(), await configResponse.text()).toBe(200);
    const config = await dataOf(configResponse);

    expect(config.versionPlantillaActual).toBe('M9.1');
    expect(config.tamanoLoteProcesamiento).toBe(250);
    expect(config.maximoFilasVistaPrevia).toBe(200);
    expect(config.etapasProceso).toEqual(['Carga', 'Lectura', 'Validacion', 'VistaPrevia', 'Confirmacion']);
    expect(config.tipos.every((tipo: any) => tipo.versionPlantilla === 'M9.1')).toBe(true);

    const vigente = await request.get(`${API_URL}/cargas-masivas/plantillas/VariantesInventario?formato=csv&version=M9.1`, {
      headers: headers()
    });
    expect(vigente.status(), await vigente.text()).toBe(200);
    expect(vigente.headers()['content-disposition']).toContain('vM9-1');

    const obsoleta = await request.get(`${API_URL}/cargas-masivas/plantillas/Clientes?formato=csv&version=M8`, {
      headers: headers()
    });
    expect(obsoleta.status()).toBe(400);
    const rechazo = await obsoleta.json();
    expect(rechazo.success ?? rechazo.Success).toBe(false);
    expect(rechazo.message ?? rechazo.Message).toContain('no está vigente');
  });

  test('expone progreso por etapas y conteos correctos/error/omitidos', async ({ request }) => {
    const contenido = [
      'Nombre,Telefono,IdentidadORTN,Correo,Direccion,Activo',
      `Cliente M9 ${suffix},9999-9001,08011990${suffix.slice(-5)},correo-invalido,Tegucigalpa,Si`
    ].join('\n');

    const validacion = await request.post(`${API_URL}/cargas-masivas/validar`, {
      headers: headers(),
      multipart: {
        tipo: 'Clientes',
        archivo: {
          name: `m9-progreso-${suffix}.csv`,
          mimeType: 'text/csv',
          buffer: Buffer.from(`\uFEFF${contenido}`, 'utf8')
        }
      }
    });
    expect(validacion.status(), await validacion.text()).toBe(200);
    const carga = await dataOf(validacion);
    expect(carga.estado).toBe('ConErrores');

    const progresoResponse = await request.get(`${API_URL}/cargas-masivas/${carga.id}/progreso`, { headers: headers() });
    expect(progresoResponse.status(), await progresoResponse.text()).toBe(200);
    const progreso = await dataOf(progresoResponse);

    expect(progreso.versionPlantilla).toBe('M9.1');
    expect(progreso.etapaActual).toBe('Correccion');
    expect(progreso.filasConError).toBeGreaterThan(0);
    expect(progreso.filasOmitidas).toBe(0);
    expect(progreso.etapas.map((etapa: any) => etapa.codigo)).toEqual([
      'Carga', 'Lectura', 'Validacion', 'VistaPrevia', 'Confirmacion'
    ]);
    expect(progreso.etapas.find((etapa: any) => etapa.codigo === 'Validacion')?.estado).toBe('Error');
  });

  test('la UI presenta versión, lote y flujo profesional', async ({ page }) => {
    await loginUi(page);
    await page.goto('/cargas-masivas');

    await expect(page.getByRole('heading', { name: 'Cargas masivas profesionales' })).toBeVisible();
    await expect(page.getByText(/Plantilla vigente M9\.1/)).toBeVisible();
    await expect(page.getByText(/lote operativo 250 filas/)).toBeVisible();
    await expect(page.getByText('Sin cambios parciales')).toBeVisible();
    await expect(page.getByRole('button', { name: /Plantilla Excel/ })).toBeVisible();
  });
});
