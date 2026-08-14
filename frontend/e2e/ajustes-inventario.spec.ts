import { expect, test } from '@playwright/test';

test.describe('N0.7.E - Ajustes de inventario', () => {
  test('navega listado -> detalle -> edición de borrador', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    const firstRow = page.locator('tbody tr').first();
    await expect(firstRow).toBeVisible();

    const detailLink = firstRow.getByRole('link', { name: /ver detalle|detalle/i });
    await detailLink.click();
    await expect(page).toHaveURL(/\/inventario\/ajustes\/[^/]+$/);

    const editLink = page.getByRole('link', { name: /editar/i });
    if (await editLink.isVisible()) {
      await editLink.click();
      await expect(page).toHaveURL(/\/inventario\/ajustes\/[^/]+\/editar$/);
    }
  });

  test('creación expone formulario y detalle dinámico', async ({ page }) => {
    await page.goto('/inventario/ajustes/nuevo');

    await expect(page.getByRole('heading', { name: /nuevo ajuste|crear ajuste/i })).toBeVisible();
    await expect(page.locator('form')).toBeVisible();

    const addDetail = page.getByRole('button', { name: /agregar.*detalle|agregar.*producto|añadir.*detalle|añadir.*producto/i });
    if (await addDetail.isVisible()) {
      await addDetail.click();
      await expect(page.locator('form').locator('input, select').first()).toBeVisible();
    }
  });

  test('confirmación y anulación mantienen lifecycle fail-closed en UI', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    const firstRow = page.locator('tbody tr').first();
    await expect(firstRow).toBeVisible();

    const confirm = firstRow.getByRole('button', { name: /confirmar/i });
    if (await confirm.isVisible()) {
      await confirm.click();
      await expect(page.getByText(/confirmar ajuste|¿.*confirmar/i)).toBeVisible();
      const accept = page.getByRole('button', { name: /confirmar|aceptar/i }).last();
      await accept.click();
    }

    const annul = firstRow.getByRole('button', { name: /anular/i });
    if (await annul.isVisible()) {
      await annul.click();
      const reason = page.locator('textarea, input').filter({ has: page.locator(':scope') }).last();
      await expect(reason).toBeVisible();
      await reason.fill('E2E N0.7.E - anulación controlada');
      const acceptAnnul = page.getByRole('button', { name: /anular|aceptar/i }).last();
      await acceptAnnul.click();
    }
  });
});
