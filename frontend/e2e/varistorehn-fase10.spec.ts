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

    const principal = page.locator('.main-image-button');
    await expect(principal).toBeVisible();

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
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/varistorehn/producto/demo-producto-1');

    const principal = page.locator('.main-image-button');
    await principal.click();
    const dialogo = page.locator('dialog.lightbox');
    await expect(dialogo).toBeVisible();

    const contador = dialogo.locator('.lightbox-controls span');
    const antes = await contador.textContent();
    const stage = dialogo.locator('.lightbox-stage');
    const box = await stage.boundingBox();
    if (box) {
      await stage.dispatchEvent('pointerdown', { pointerType: 'touch', clientX: box.x + box.width * .8, clientY: box.y + box.height * .5 });
      await stage.dispatchEvent('pointerup', { pointerType: 'touch', clientX: box.x + box.width * .2, clientY: box.y + box.height * .5 });
    }
    const total = Number((antes || '1 / 1').split('/')[1]?.trim() || '1');
    if (total > 1) await expect(contador).not.toHaveText(antes || '');

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
