import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 907,
  nombreComercial: 'VariStore Home Audit',
  nombreVisibleSistema: 'VariStore Home Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Portada comercial de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 VariStore Home Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública de auditoría',
  piePaginaActivo: true,
  piePaginaTexto: 'Compra simple y segura',
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

async function abrirHome(page: Page): Promise<void> {
  await page.goto('/varistorehn');
  await expect(page.locator('.storefront')).toBeVisible();
  await expect(page.getByRole('heading', { name: /Todo lo que buscas/i })).toBeVisible();
}

async function activarBaseDatos(page: Page): Promise<void> {
  await page.getByRole('group', { name: 'Origen de datos' })
    .getByRole('button', { name: 'Base de datos' }).click();
}

test.describe('VariStoreHn Fase 7 — home comercial', () => {
  test.describe.configure({ retries: 0 });

  test('demo: portada comercial no duplica catálogo, filtros ni paginación', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await abrirHome(page);

    await expect(page.locator('app-varistorehn-header .skip-link')).toHaveAttribute('href', '#contenido-principal');
    await expect(page.locator('.category-card')).toHaveCount(6);
    await expect(page.locator('.featured-card')).toHaveCount(3);
    await expect(page.locator('.filters')).toHaveCount(0);
    await expect(page.locator('.catalog-toolbar')).toHaveCount(0);
    await expect(page.locator('.pagination')).toHaveCount(0);
    await expect(page.locator('article.product-card')).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Explorar productos/i })).toHaveAttribute('href', '/varistorehn/productos');
    await expect(page.getByRole('link', { name: 'Ver categorías', exact: true })).toHaveAttribute('href', '/varistorehn/categorias');
  });

  test('búsqueda del home navega al catálogo canónico y conserva el término', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirHome(page);

    const search = page.locator('app-varistorehn-header').getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await search.press('Enter');

    await expect(page).toHaveURL(/\/varistorehn\/productos\?q=Wireless(?:%20|\+)Studio/);
    await expect(page.getByRole('heading', { level: 1, name: 'Productos', exact: true })).toBeVisible();
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');
  });

  test('categorías del home abren la URL pública canónica por slug', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirHome(page);

    await page.getByRole('button', { name: 'Explorar Audio', exact: true }).click();
    await expect(page).toHaveURL(/\/varistorehn\/categoria\/demo-categoria-2$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Audio', exact: true })).toBeVisible();
  });

  test('destacados demo respetan la marca comercial y navegan al detalle real', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirHome(page);

    const destacados = page.locator('.featured-card');
    await expect(destacados).toHaveCount(3);
    await expect(destacados.nth(0)).toContainText('Laptop Pro 14');
    await destacados.nth(0).getByRole('button', { name: 'Ver Laptop Pro 14' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/producto\/demo-producto-1$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Laptop Pro 14', exact: true })).toBeVisible();
  });

  test('fuente real carga categorías reales, no descarga catálogo completo ni fabrica destacados', async ({ page }) => {
    await prepararEmpresa(page);
    let solicitudesProductos = 0;
    page.on('request', request => {
      if (/\/tienda\/productos(?:\?|$)/.test(request.url())) solicitudesProductos += 1;
    });
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: [
            { id: 71, slug: 'hogar-real-71', nombre: 'Hogar real', descripcion: 'Categoría desde BD', totalProductos: 4 },
            { id: 72, slug: 'oficina-real-72', nombre: 'Oficina real', descripcion: 'Categoría desde BD', totalProductos: null }
          ]
        })
      });
    });

    await abrirHome(page);
    await activarBaseDatos(page);

    await expect(page.locator('.category-card')).toHaveCount(2);
    await expect(page.getByRole('button', { name: 'Explorar Hogar real' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Explorar Oficina real' })).toBeVisible();
    await expect(page.locator('.featured-card')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Aún no hay productos marcados como destacados' })).toBeVisible();
    await expect(page.getByText('El home no elige productos arbitrarios', { exact: false })).toBeVisible();
    await expect.poll(() => solicitudesProductos).toBe(0);
  });

  test('error de categorías reales permanece visible y nunca cae silenciosamente a demo', async ({ page }) => {
    await prepararEmpresa(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Servicio no disponible' })
      });
    });

    await abrirHome(page);
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { name: 'No pudimos cargar las categorías' })).toBeVisible();
    await expect(page.locator('.category-card')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Explorar Computadoras' })).toHaveCount(0);
  });

  test('carrito ya hidratado conserva continuidad al volver al home y la portada refluye hasta 320px', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/varistorehn/productos');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    const catalogHeader = page.locator('app-varistorehn-header');
    await expect(catalogHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await catalogHeader.getByRole('link', { name: 'Inicio', exact: true }).click();

    await expect(page).toHaveURL(/\/varistorehn$/);
    const homeHeader = page.locator('app-varistorehn-header');
    await expect(homeHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await homeHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/carrito$/);
    await expect(page.locator('.cart-item')).toContainText('Laptop Pro 14');

    await page.goto('/varistorehn');
    for (const width of [760, 390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }

    await page.setViewportSize({ width: 320, height: 844 });
    const ctas = await page.locator('.hero-actions .button').evaluateAll(elements =>
      elements.map(element => Math.round(element.getBoundingClientRect().height))
    );
    expect(ctas.length).toBeGreaterThan(0);
    expect(ctas.every(height => height >= 44)).toBe(true);
  });
});
