import { expect, Page, test } from '@playwright/test';

const empresa = {
  id: 910,
  nombreComercial: 'VariStore Fase 10',
  nombreVisibleSistema: 'VariStore Fase 10',
  eslogan: 'Compra cómoda desde cualquier pantalla',
  descripcionSistema: 'Auditoría responsive, UX y accesibilidad',
  mensajeLogin: 'Administración',
  copyright: '© 2026 VariStore',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Tienda pública',
  piePaginaActivo: true,
  piePaginaTexto: 'Tienda pública',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

const imagenData = (texto: string) => `data:image/svg+xml,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600"><rect width="800" height="600" fill="white"/><text x="400" y="300" text-anchor="middle" font-size="48">${texto}</text></svg>`
)}`;

async function preparar(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: empresa })
  }));
}

async function sinOverflowHorizontal(page: Page, ruta: string, width: number): Promise<void> {
  await page.setViewportSize({ width, height: 844 });
  await page.goto(ruta);
  await expect(page.locator('.storefront')).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, `overflow horizontal en ${ruta} a ${width}px`).toBeLessThanOrEqual(0);
}

test.describe('VariStoreHN Fase 10 — responsive, UX y accesibilidad', () => {
  test.describe.configure({ retries: 0 });

  test('flujo público no desborda en anchos móviles comunes', async ({ page }) => {
    await preparar(page);
    for (const width of [320, 360, 390, 430]) {
      await sinOverflowHorizontal(page, '/varistorehn', width);
      await sinOverflowHorizontal(page, '/varistorehn/productos', width);
      await sinOverflowHorizontal(page, '/varistorehn/producto/demo-producto-1', width);
      await sinOverflowHorizontal(page, '/varistorehn/carrito', width);
    }
  });

  test('detalle móvil ofrece feedback visible, CTA seguro y targets táctiles', async ({ page }) => {
    await preparar(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/varistorehn/producto/demo-producto-1');

    const agregar = page.getByRole('button', { name: 'Agregar al carrito', exact: true });
    await expect(agregar).toBeEnabled();
    await agregar.click();

    await expect(page.locator('.feedback-toast')).toContainText(/unidad agregada|unidades agregadas/);
    await expect(page.locator('.mobile-buy-bar')).toBeVisible();

    const alturas = await page.locator('button, .button').evaluateAll(elements =>
      elements.filter(element => {
        const style = getComputedStyle(element);
        const rect = element.getBoundingClientRect();
        return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
      }).map(element => Math.round(element.getBoundingClientRect().height))
    );
    expect(alturas.length).toBeGreaterThan(0);
    expect(Math.min(...alturas)).toBeGreaterThanOrEqual(38);

    const bar = await page.locator('.mobile-buy-bar').boundingBox();
    expect(bar).not.toBeNull();
    expect((bar?.y || 0) + (bar?.height || 0)).toBeLessThanOrEqual(845);
  });

  test('fullscreen abre, permite swipe táctil, cierra y devuelve foco', async ({ page }) => {
    await preparar(page);
    const imagenes = [imagenData('Imagen uno'), imagenData('Imagen dos')];
    const detalle = {
      id: 9101, slug: 'producto-fase10', nombre: 'Producto Fase 10', descripcion: 'Detalle accesible.',
      categoriaId: 21, categoriaNombre: 'Tecnología', marcaNombre: 'Marca F10', precio: 1500,
      precioOferta: null, ofertaActiva: false, ofertaNombre: null, ahorro: 0, porcentajeAhorro: 0,
      cantidadDisponible: 5, estaAgotado: false, estadoDisponibilidad: 'Disponible', sku: 'F10-9101',
      activo: true, esDestacado: false, imagenes: imagenes.map((url, i) => ({ url, orden: i + 1, esPrincipal: i === 0 })),
      modelos: [{
        productoVarianteId: 91010, modeloId: 910100, modeloNombre: 'Base', marcaNombre: 'Marca F10',
        sku: 'F10-9101-A', precio: 1500, precioOferta: null, ofertaActiva: false, ofertaNombre: null,
        ahorro: 0, porcentajeAhorro: 0, cantidadDisponible: 5, estaAgotado: false,
        estadoDisponibilidad: 'Disponible', imagenes: imagenes.map((url, i) => ({ url, orden: i + 1, esPrincipal: i === 0 }))
      }]
    };

    await page.route('**/tienda/productos?*', route => route.fulfill({
      status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { items: [detalle], page: 1, pageSize: 96, totalCount: 1 } })
    }));
    await page.route('**/tienda/productos/*', route => route.fulfill({
      status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: detalle })
    }));
    await page.route('**/tienda/categorias', route => route.fulfill({
      status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: [{ id: 21, slug: 'tecnologia-21', nombre: 'Tecnología', descripcion: '', totalProductos: 1 }] })
    }));

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/varistorehn/producto/producto-fase10');
    await page.getByRole('group', { name: 'Origen de datos' }).getByRole('button', { name: 'Base de datos' }).click();
    await expect(page.getByRole('heading', { level: 1, name: 'Producto Fase 10' })).toBeVisible();

    const principal = page.locator('.main-image-button');
    await expect(principal).toBeVisible();
    await principal.click();
    const dialogo = page.locator('dialog.lightbox');
    await expect(dialogo).toBeVisible();

    const contador = dialogo.locator('.lightbox-controls span');
    await expect(contador).toHaveText('1 / 2');
    const stage = dialogo.locator('.lightbox-stage');
    await stage.dispatchEvent('pointerdown', { pointerId: 1, pointerType: 'touch', clientX: 310, clientY: 220, isPrimary: true });
    await stage.dispatchEvent('pointerup', { pointerId: 1, pointerType: 'touch', clientX: 170, clientY: 224, isPrimary: true });
    await expect(contador).toHaveText('2 / 2');

    await dialogo.getByRole('button', { name: 'Cerrar imagen ampliada' }).click();
    await expect(dialogo).not.toBeVisible();
    await expect(principal).toBeFocused();

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(0);
  });

  test('checkout móvil asocia errores a campos y conserva navegación por teclado', async ({ page }) => {
    await preparar(page);
    await page.setViewportSize({ width: 390, height: 844 });

    await page.goto('/varistorehn/producto/demo-producto-1');
    await page.getByRole('button', { name: 'Agregar al carrito', exact: true }).click();
    await page.goto('/varistorehn/checkout');

    await expect(page.getByRole('heading', { name: 'Confirma tus datos y tu forma de compra' })).toBeVisible();

    const accion = page.getByRole('button', { name: /Simular continuación segura|Preparar pedido por WhatsApp/ }).first();
    await expect(accion).toBeVisible();
    await accion.click();

    const nombre = page.locator('input[formcontrolname="nombre"]');
    const telefono = page.locator('input[formcontrolname="telefono"]');
    await expect(nombre).toHaveAttribute('aria-invalid', 'true');
    await expect(telefono).toHaveAttribute('aria-invalid', 'true');
    await expect(nombre).toHaveAttribute('aria-describedby', /nombre-error/);
    await expect(telefono).toHaveAttribute('aria-describedby', /telefono-error/);

    await nombre.focus();
    await expect(nombre).toBeFocused();
    await page.keyboard.press('Tab');
    const activeTag = await page.evaluate(() => document.activeElement?.tagName);
    expect(activeTag).toBeTruthy();

    const controles = await page.locator('.checkout-page input, .checkout-page textarea, .checkout-page button, .checkout-page .button').evaluateAll(elements =>
      elements.filter(element => {
        const rect = element.getBoundingClientRect();
        return rect.width > 0 && rect.height > 0;
      }).map(element => Math.round(element.getBoundingClientRect().height))
    );
    expect(controles.every(height => height >= 44)).toBe(true);
  });
});
