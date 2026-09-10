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
  formatoFecha: 'dd/MM/yyyy'
};

async function prepararTienda(page: Page, whatsApp = '9876-5432'): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: { ...empresaBase, whatsApp }
      })
    });
  });

  await page.goto('/varistorehn');
  await expect(page.locator('.storefront')).toBeVisible();
  await expect(page.getByRole('heading', { name: /Todo lo que buscas/i })).toBeVisible();
}

async function esperarCatalogo(page: Page): Promise<void> {
  await expect(page.getByRole('status').filter({ hasText: /productos encontrados/i })).toBeVisible();
  await expect(page.locator('.product-card').first()).toBeVisible();
}

test.describe('VariStoreHn Fase 1 — navegación y header', () => {
  test.describe.configure({ retries: 0 });

  test('desktop: identidad, navegación, búsqueda, categorías, carrito y WhatsApp usan estado real', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await prepararTienda(page);
    await esperarCatalogo(page);

    const header = page.locator('app-varistorehn-header');
    await expect(header.getByText('VariStore Audit', { exact: true }).first()).toBeVisible();
    await expect(header.getByText('Compra simple y segura', { exact: true })).toBeVisible();

    await expect(header.getByRole('link', { name: 'Inicio', exact: true })).toHaveAttribute('href', '/varistorehn');
    await expect(header.getByRole('link', { name: 'Productos', exact: true })).toHaveAttribute('href', '/varistorehn#catalogo');
    await expect(header.getByRole('link', { name: 'Categorías', exact: true })).toHaveAttribute('href', '/varistorehn#categories-title');

    const whatsapp = header.getByRole('link', { name: 'Contactar por WhatsApp' });
    await expect(whatsapp).toBeVisible();
    await expect(whatsapp).toHaveAttribute('href', 'https://wa.me/50498765432');

    await expect(header.getByRole('button', { name: 'Abrir carrito con 0 unidades' })).toBeVisible();

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await expect(laptop).toBeVisible();
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();

    const carrito = page.locator('dialog.cart-dialog');
    await expect(carrito).toBeVisible();
    await expect(carrito.getByRole('heading', { name: /Mi carrito/i })).toContainText('1 unidades');
    await carrito.getByRole('button', { name: 'Cerrar carrito' }).click();
    await expect(carrito).not.toBeVisible();

    await expect(header.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(header.locator('.cart-copy small')).toContainText('18,490');

    const search = header.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');

    await search.press('Enter');
    const catalogTop = await page.locator('#catalogo').evaluate(element => element.getBoundingClientRect().top);
    expect(catalogTop).toBeLessThan(220);

    await search.fill('');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
    const nav = header.locator('nav.store-nav');
    const audio = nav.getByRole('button', { name: 'Audio', exact: true });
    await audio.click();
    await expect(audio).toHaveAttribute('aria-pressed', 'true');
    await expect(page.locator('#catalog-title')).toHaveText('Audio');
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await nav.getByRole('button', { name: /Todas/ }).click();
    await expect(page.locator('#catalog-title')).toHaveText('Todos los productos');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
  });

  test('desktop: la navegación opera también sobre productos obtenidos del endpoint público', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    let peticionesCatalogo = 0;

    await page.route('http://localhost:5005/tienda/productos**', async route => {
      peticionesCatalogo += 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: {
            items: [
              {
                id: 701,
                slug: 'teclado-http-audit-701',
                nombre: 'Teclado HTTP Audit',
                descripcion: 'Producto servido por la API pública de auditoría.',
                categoriaId: 71,
                categoriaNombre: 'Periféricos',
                marcaNombre: 'Audit',
                modeloNombre: 'Base',
                precio: 1250,
                precioOferta: null,
                cantidadDisponible: 4,
                estaAgotado: false,
                sku: 'AUD-701',
                activo: true,
                esDestacado: false,
                fechaCreacion: '2026-09-10T00:00:00Z',
                imagenPrincipalUrl: null,
                imagenes: [],
                modelos: []
              },
              {
                id: 702,
                slug: 'audifonos-http-audit-702',
                nombre: 'Audífonos HTTP Audit',
                descripcion: 'Segundo producto servido por la API pública.',
                categoriaId: 72,
                categoriaNombre: 'Audio',
                marcaNombre: 'Audit',
                modeloNombre: 'Base',
                precio: 990,
                precioOferta: null,
                cantidadDisponible: 3,
                estaAgotado: false,
                sku: 'AUD-702',
                activo: true,
                esDestacado: false,
                fechaCreacion: '2026-09-10T00:00:00Z',
                imagenPrincipalUrl: null,
                imagenes: [],
                modelos: []
              }
            ],
            page: 1,
            pageSize: 96,
            totalCount: 2
          }
        })
      });
    });

    await prepararTienda(page);
    await page.getByRole('button', { name: 'Base de datos' }).click();
    await expect(page.getByRole('status').filter({ hasText: '2 productos encontrados' })).toBeVisible();
    expect(peticionesCatalogo).toBe(1);
    await expect(page.locator('article.product-card')).toHaveCount(2);
    await expect(page.locator('article.product-card')).not.toContainText('Laptop Pro 14');

    const header = page.locator('app-varistorehn-header');
    const search = header.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Audífonos HTTP');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await expect(page.locator('article.product-card')).toContainText('Audífonos HTTP Audit');

    await search.fill('');
    const audio = header.locator('nav.store-nav').getByRole('button', { name: 'Audio', exact: true });
    await audio.click();
    await expect(audio).toHaveAttribute('aria-pressed', 'true');
    await expect(page.locator('#catalog-title')).toHaveText('Audio');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();

    await header.locator('nav.store-nav').getByRole('button', { name: /Todas/ }).click();
    const teclado = page.locator('article.product-card').filter({ hasText: 'Teclado HTTP Audit' });
    await teclado.getByRole('button', { name: 'Agregar Teclado HTTP Audit' }).click();
    const carrito = page.locator('dialog.cart-dialog');
    await expect(carrito).toBeVisible();
    await carrito.getByRole('button', { name: 'Cerrar carrito' }).click();
    await expect(header.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(header.locator('.cart-copy small')).toContainText('1,250');
  });

  test('tablet: WhatsApp permanece accesible y sin overflow en toda la franja previa al menú móvil', async ({ page }) => {
    await page.setViewportSize({ width: 900, height: 900 });
    await prepararTienda(page);
    await esperarCatalogo(page);

    const header = page.locator('app-varistorehn-header');
    for (const width of [1120, 900, 761]) {
      await page.setViewportSize({ width, height: 900 });
      await expect(header.getByRole('link', { name: 'Contactar por WhatsApp' })).toBeVisible();
      await expect(header.getByRole('button', { name: 'Abrir navegación' })).toBeHidden();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }
  });

  test('móvil: drawer modal, foco, Escape, categoría, modo de compra y reflow funcionan', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await prepararTienda(page);
    await esperarCatalogo(page);

    const header = page.locator('app-varistorehn-header');
    const toggle = header.getByRole('button', { name: 'Abrir navegación' });
    const dialog = header.locator('#varistorehn-menu-movil');

    await expect(toggle).toBeVisible();
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-expanded', 'true');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('nav a').first()).toBeFocused();

    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    await expect(toggle).toBeFocused();

    await toggle.click();
    const categoriaAudio = dialog.locator('.mobile-categories').getByRole('button', { name: 'Audio', exact: true });
    await categoriaAudio.click();
    await expect(dialog).not.toBeVisible();
    await expect(page.locator('#catalog-title')).toHaveText('Audio');
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await toggle.click();
    await expect(dialog.locator('a.mobile-whatsapp')).toBeVisible();
    await dialog.getByRole('button', { name: 'Cerrar navegación' }).click();

    await page.locator('.preview-panel select').selectOption('tarjeta');
    await toggle.click();
    await expect(dialog.locator('a.mobile-whatsapp')).toHaveCount(0);
    await dialog.getByRole('button', { name: 'Cerrar navegación' }).click();

    const targetAudit = await page.evaluate(() => {
      const selectors = ['.search-box button', '.cart-trigger', '.mobile-menu-trigger'];
      return selectors.map(selector => {
        const element = document.querySelector<HTMLElement>(selector);
        if (!element) return { selector, width: 0, height: 0 };
        const rect = element.getBoundingClientRect();
        return { selector, width: Math.round(rect.width), height: Math.round(rect.height) };
      });
    });
    expect(targetAudit.every(item => item.width >= 44 && item.height >= 44), JSON.stringify(targetAudit)).toBe(true);

    for (const width of [760, 390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      await expect(header.getByRole('button', { name: 'Abrir navegación' })).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }
  });

  test('WhatsApp inválido no genera una acción rota', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await prepararTienda(page, 'https://wa.me/50499999999');
    await esperarCatalogo(page);

    const header = page.locator('app-varistorehn-header');
    await expect(header.getByRole('link', { name: 'Contactar por WhatsApp' })).toHaveCount(0);
  });
});
