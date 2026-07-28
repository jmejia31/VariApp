import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

let token = '';
let productoId = 0;
let varianteId = 0;

const nombres = {
  marca: `Marca Variantes ${suffix}`,
  modelo: `Modelo Variantes ${suffix}`,
  color: `Negro Variantes ${suffix}`,
  color2: `Azul Variantes ${suffix}`,
  producto: `Producto Variantes ${suffix}`,
  sku: `VAR-BLK-${suffix}`,
  sku2: `VAR-BLU-${suffix}`
};

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
  const data = await dataOf(response);
  expect(data?.token).toBeTruthy();
  return data.token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function crearCatalogo(
  request: APIRequestContext,
  ruta: string,
  data: Record<string, unknown>
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/${ruta}`, { headers: headers(), data });
  expect(response.status(), `${ruta}: ${await response.text()}`).toBe(201);
  return await dataOf(response);
}

test.describe('Fase 4 — variantes por color, SKU e inventario', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);
  });

  test('API crea múltiples colores, suma stock y conserva la variante en documentos', async ({ request }) => {
    const marca = await crearCatalogo(request, 'marcas', {
      nombre: nombres.marca,
      descripcion: 'Marca temporal para variantes',
      orden: 20
    });
    const modelo = await crearCatalogo(request, 'modelos', {
      nombre: nombres.modelo,
      descripcion: 'Modelo temporal para variantes',
      catalogoPadreId: marca.id,
      orden: 20
    });
    const color = await crearCatalogo(request, 'colores', {
      nombre: nombres.color,
      descripcion: 'Primer color temporal para variantes',
      codigoVisual: '#111827',
      orden: 20
    });
    const color2 = await crearCatalogo(request, 'colores', {
      nombre: nombres.color2,
      descripcion: 'Segundo color temporal para variantes',
      codigoVisual: '#2563EB',
      orden: 21
    });

    const productoResponse = await request.post(`${API_URL}/productos`, {
      headers: headers(),
      multipart: {
        Nombre: nombres.producto,
        Marca: nombres.marca,
        Modelo: nombres.modelo,
        MarcaId: String(marca.id),
        ModeloId: String(modelo.id),
        Cantidad: '10',
        Costo: '104',
        Precio: '220',
        UmbralStockBajo: '4',
        'Variantes[0].ColorId': String(color.id),
        'Variantes[0].Sku': nombres.sku.toLowerCase(),
        'Variantes[0].CodigoBarras': `7501${suffix.slice(-8)}`,
        'Variantes[0].Cantidad': '6',
        'Variantes[0].UmbralStockBajo': '2',
        'Variantes[0].Costo': '100',
        'Variantes[0].Precio': '220',
        'Variantes[0].Activo': 'true',
        'Variantes[1].ColorId': String(color2.id),
        'Variantes[1].Sku': nombres.sku2.toLowerCase(),
        'Variantes[1].CodigoBarras': `7502${suffix.slice(-8)}`,
        'Variantes[1].Cantidad': '4',
        'Variantes[1].UmbralStockBajo': '2',
        'Variantes[1].Costo': '110',
        'Variantes[1].Precio': '230',
        'Variantes[1].Activo': 'true'
      }
    });
    expect(productoResponse.status(), await productoResponse.text()).toBe(201);
    const producto = await dataOf(productoResponse);
    productoId = producto.id;
    expect(producto.cantidad).toBe(10);
    expect(producto.totalVariantes).toBe(2);
    expect(producto.variantes).toHaveLength(2);
    expect(producto.variantes.map((v: any) => v.cantidad).reduce((a: number, b: number) => a + b, 0)).toBe(10);

    const variante = producto.variantes.find((v: any) => v.colorId === color.id);
    expect(variante).toBeTruthy();
    varianteId = variante.id;
    expect(variante.sku).toBe(nombres.sku.toUpperCase());
    expect(variante.cantidad).toBe(6);

    const skuDuplicado = await request.post(`${API_URL}/productos/${productoId}/variantes`, {
      headers: headers(),
      data: {
        colorId: color.id,
        sku: nombres.sku,
        cantidad: 0,
        umbralStockBajo: 2,
        costo: 100,
        precio: 220
      }
    });
    expect(skuDuplicado.status()).toBe(400);

    const compraResponse = await request.post(`${API_URL}/compras`, {
      headers: headers(),
      data: {
        proveedorNombre: `Proveedor Variantes ${suffix}`,
        metodoPago: 'Efectivo',
        estadoPago: 'Pendiente',
        descuento: 0,
        impuesto: 0,
        detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 4, costoUnitario: 90 }]
      }
    });
    expect(compraResponse.status(), await compraResponse.text()).toBe(201);
    const compra = await dataOf(compraResponse);
    expect(compra.detalles[0].productoSku).toBe(nombres.sku.toUpperCase());
    expect(compra.detalles[0].productoColor).toBe(nombres.color);

    const confirmarCompra = await request.post(`${API_URL}/compras/${compra.id}/confirmar`, {
      headers: headers()
    });
    expect(confirmarCompra.status(), await confirmarCompra.text()).toBe(200);

    let productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(14);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(10);

    const envioResponse = await request.get(`${API_URL}/costos-envio/predeterminado`, { headers: headers() });
    expect(envioResponse.status(), await envioResponse.text()).toBe(200);
    const envio = await dataOf(envioResponse);

    const ventaResponse = await request.post(`${API_URL}/ventas`, {
      headers: headers(),
      data: {
        clienteNombre: `Cliente Variantes ${suffix}`,
        metodoPago: 'Efectivo',
        estadoPago: 'Pendiente',
        descuento: 0,
        impuesto: 0,
        costoEnvioId: envio.id,
        envioExonerado: false,
        detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 3, precioUnitario: 220 }]
      }
    });
    expect(ventaResponse.status(), await ventaResponse.text()).toBe(201);
    const venta = await dataOf(ventaResponse);
    expect(venta.detalles[0].productoSku).toBe(nombres.sku.toUpperCase());

    const confirmarVenta = await request.post(`${API_URL}/ventas/${venta.id}/confirmar`, {
      headers: headers()
    });
    expect(confirmarVenta.status(), await confirmarVenta.text()).toBe(200);
    const ventaConfirmada = await dataOf(confirmarVenta);
    expect(ventaConfirmada.facturaId).toBeTruthy();

    productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(11);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(7);

    const facturaResponse = await request.get(`${API_URL}/facturas/${ventaConfirmada.facturaId}`, {
      headers: headers()
    });
    expect(facturaResponse.status(), await facturaResponse.text()).toBe(200);
    const factura = await dataOf(facturaResponse);
    expect(factura.detalles[0].productoVarianteId).toBe(varianteId);
    expect(factura.detalles[0].varianteSku).toBe(nombres.sku.toUpperCase());
    expect(factura.detalles[0].varianteColor).toBe(nombres.color);

    const anularVenta = await request.post(`${API_URL}/ventas/${venta.id}/anular`, {
      headers: headers(),
      data: { motivoAnulacion: 'Reversión E2E de variante' }
    });
    expect(anularVenta.status(), await anularVenta.text()).toBe(200);

    productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(14);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(10);

    const anularCompra = await request.post(`${API_URL}/compras/${compra.id}/anular`, {
      headers: headers(),
      data: { motivoAnulacion: 'Reversión E2E de compra por variante' }
    });
    expect(anularCompra.status(), await anularCompra.text()).toBe(200);

    productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(10);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(6);
  });

  test('Formulario principal permite agregar colores y muestra la suma del stock', async ({ page }) => {
    await loginUi(page);

    await page.goto(`/productos/${productoId}/editar`);
    await expect(page.getByRole('heading', { name: 'Colores y existencias' })).toBeVisible();
    await expect(page.getByText('Stock total calculado')).toBeVisible();
    await expect(page.locator('.variant-card')).toHaveCount(2);
    await expect(page.locator('.stock-summary').getByText('10 unidades', { exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Agregar otro color' }).first().click();
    await expect(page.locator('.variant-card')).toHaveCount(3);
    await expect(page.locator('mat-select[formcontrolname="colorId"]')).toHaveCount(3);
    await expect(page.locator('input[formcontrolname="cantidad"]')).toHaveCount(3);
  });

  test('Angular exige la variante exacta en compra y venta', async ({ page }) => {
    await loginUi(page);

    await page.goto(`/productos/${productoId}/variantes`);
    await expect(page.getByRole('heading', { name: 'Variantes por color y SKU' })).toBeVisible();
    const fila = page.locator('table tr', { hasText: nombres.sku.toUpperCase() });
    await expect(fila).toBeVisible();
    await expect(fila.getByText(nombres.color)).toBeVisible();
    await expect(fila.getByText('6', { exact: true })).toBeVisible();

    await page.goto('/ventas/nueva');
    const productoVenta = page.locator('mat-select[formcontrolname="productoId"]').first();
    await productoVenta.click();
    await page.getByRole('option', { name: new RegExp(nombres.producto) }).click();
    const varianteVenta = page.locator('mat-select[formcontrolname="productoVarianteId"]').first();
    await expect(varianteVenta).toBeVisible();
    await varianteVenta.click();
    await expect(page.getByRole('option', { name: new RegExp(nombres.sku.toUpperCase()) })).toBeVisible();
    await page.keyboard.press('Escape');

    await page.goto('/compras/nueva');
    const productoCompra = page.locator('mat-select[formcontrolname="productoId"]').first();
    await productoCompra.click();
    await page.getByRole('option', { name: new RegExp(nombres.producto) }).click();
    const varianteCompra = page.locator('mat-select[formcontrolname="productoVarianteId"]').first();
    await expect(varianteCompra).toBeVisible();
    await varianteCompra.click();
    await expect(page.getByRole('option', { name: new RegExp(nombres.sku.toUpperCase()) })).toBeVisible();
  });
});
