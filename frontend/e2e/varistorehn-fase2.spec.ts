import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 901,
  nombreComercial: 'VariStore Audit',
  nombreVisibleSistema: 'VariStore Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 VariStore Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública de auditoría',
  piePaginaActivo: true,
  piePaginaTexto: 'Pie de auditoría',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

async function prepararEmpresa(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: empresaBase })
    });
  });
}

async function mockCatalogoVacio(page: Page): Promise<void> {
  await page.route('**/tienda/productos?*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: { items: [], page: 1, pageSize: 96, totalCount: 0 }
      })
    });
  });
}

async function activarBaseDatos(page: Page): Promise<void> {
  await page.getByRole('group', { name: 'Origen de datos' })
    .getByRole('button', { name: 'Base de datos' }).click();
}

test.describe('VariStoreHn Fase 2 — categorías públicas', () => {
  test.describe.configure({ retries: 0 });

  test('ruta independiente reutiliza header, fixtures explícitos y responde sin overflow', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/varistorehn/categorias');

    await expect(page.getByRole('heading', { level: 1, name: 'Categorías', exact: true })).toBeVisible();
    await expect(page.locator('.categories-grid .category-card')).toHaveCount(6);
    await expect(page.locator('app-varistorehn-header .skip-link')).toHaveAttribute('href', '#contenido-categorias');
    await expect(page.locator('app-varistorehn-header').getByRole('link', { name: 'Categorías', exact: true }).first())
      .toHaveAttribute('href', '/varistorehn/categorias');
    await expect(page.getByText(/Modo demostración:/)).toContainText('fixtures de vista previa');

    const target = page.getByRole('link', { name: 'Explorar categoría Computadoras', exact: true });
    const alto = await target.evaluate(element => Math.round(element.getBoundingClientRect().height));
    expect(alto).toBeGreaterThanOrEqual(44);

    for (const width of [1366, 1000, 760, 390, 320]) {
      await page.setViewportSize({ width, height: 900 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }
  });

  test('fuente real consume CategoriaTienda y conserva conteo desconocido como desconocido', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: [
            { id: 21, slug: 'audio-y-video-21', nombre: 'Audio y Vídeo', descripcion: 'Sonido e imagen para tu espacio.', totalProductos: null },
            { id: 22, slug: 'computadoras-22', nombre: 'Computadoras', descripcion: 'Equipos para trabajo y estudio.', totalProductos: 2 }
          ]
        })
      });
    });

    await page.goto('/varistorehn/categorias');
    await activarBaseDatos(page);

    const cards = page.locator('.categories-grid .category-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.nth(0)).toContainText('Audio y Vídeo');
    await expect(cards.nth(0)).toContainText('Sonido e imagen para tu espacio.');
    await expect(cards.nth(0)).toContainText('Cantidad no disponible');
    await expect(cards.nth(0)).not.toContainText('0 productos');
    await expect(cards.nth(1)).toContainText('2 productos');
    await expect(page.getByRole('link', { name: 'Explorar categoría Audio y Vídeo', exact: true }))
      .toHaveAttribute('href', '/varistorehn?categoria=audio-y-video-21#catalogo');
    await expect(page.getByText('Datos de la tienda', { exact: true }).last()).toBeVisible();
  });

  test('fuente real vacía representa empty sin fabricar categorías', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });

    await page.goto('/varistorehn/categorias');
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { name: 'Aún no hay categorías públicas disponibles' })).toBeVisible();
    await expect(page.locator('.category-card')).toHaveCount(0);
    await expect(page.getByText('Computadoras', { exact: true })).toHaveCount(0);
  });

  test('error de categorías reales no cae silenciosamente a datos demo', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Servicio no disponible' })
      });
    });

    await page.goto('/varistorehn/categorias');
    await activarBaseDatos(page);

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('No pudimos cargar las categorías');
    await expect(alert).toContainText('No se sustituyeron los datos reales por ejemplos');
    await expect(alert.getByRole('button', { name: 'Intentar de nuevo' })).toBeVisible();
    await expect(page.locator('.category-card')).toHaveCount(0);
  });

  test('búsqueda, filtro canónico y carrito conservan continuidad entre categorías y home', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/varistorehn');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    await page.locator('dialog.cart-dialog').getByRole('button', { name: 'Cerrar carrito' }).click();

    await page.locator('app-varistorehn-header').getByRole('link', { name: 'Categorías', exact: true }).first().click();
    await expect(page).toHaveURL(/\/varistorehn\/categorias$/);
    const categoriesHeader = page.locator('app-varistorehn-header');
    await expect(categoriesHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(categoriesHeader.locator('.cart-copy small')).toContainText('18,490');

    const search = categoriesHeader.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await search.press('Enter');
    await expect(page).toHaveURL(/\/varistorehn\?q=Wireless(?:%20|\+)Studio#catalogo$/);
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');

    await page.goto('/varistorehn/categorias');
    await page.getByRole('link', { name: 'Explorar categoría Audio', exact: true }).click();
    await expect(page).toHaveURL(/\/varistorehn\?categoria=demo-categoria-2#catalogo$/);
    await expect(page.locator('#catalog-title')).toHaveText('Audio');
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await page.goto('/varistorehn/categorias');
    await page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/varistorehn\?carrito=1$/);
    const carrito = page.locator('dialog.cart-dialog');
    await expect(carrito).toBeVisible();
    await expect(carrito).toContainText('Laptop Pro 14');
  });
});
