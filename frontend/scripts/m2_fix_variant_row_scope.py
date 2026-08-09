from pathlib import Path

path = Path('frontend/e2e/fase4-variantes.spec.ts')
text = path.read_text(encoding='utf-8')
old = '''    const colores = page.locator('mat-select[formcontrolname="colorId"]');
    await colores.nth(0).click();
    await page.getByRole('option', { name: nombres.color, exact: true }).click();

    await page.locator('input[formcontrolname="cantidad"]').nth(0).fill('2');
    await page.locator('input[formcontrolname="costo"]').nth(0).fill('100');
    await page.locator('input[formcontrolname="precio"]').nth(0).fill('300');
    await page.locator('input[formcontrolname="umbralStockBajo"]').nth(0).fill('0');

    await page.getByRole('button', { name: 'Agregar variante' }).first().click();
    await expect(page.locator('.variant-card')).toHaveCount(2);

    await colores.nth(1).click();
    await page.getByRole('option', { name: nombres.color2, exact: true }).click();
    await page.locator('input[formcontrolname="cantidad"]').nth(1).fill('3');
    await page.locator('input[formcontrolname="costo"]').nth(1).fill('100');
    await page.locator('input[formcontrolname="precio"]').nth(1).fill('300');
    await page.locator('input[formcontrolname="umbralStockBajo"]').nth(1).fill('0');'''
new = '''    const variantes = page.locator('.variant-card');
    const primeraVariante = variantes.nth(0);
    await primeraVariante.locator('mat-select[formcontrolname="colorId"]').click();
    await page.getByRole('option', { name: nombres.color, exact: true }).click();

    await primeraVariante.locator('input[formcontrolname="cantidad"]').fill('2');
    await primeraVariante.locator('input[formcontrolname="costo"]').fill('100');
    await primeraVariante.locator('input[formcontrolname="precio"]').fill('300');
    await primeraVariante.locator('input[formcontrolname="umbralStockBajo"]').fill('0');

    await page.getByRole('button', { name: 'Agregar variante' }).first().click();
    await expect(variantes).toHaveCount(2);

    const segundaVariante = variantes.nth(1);
    await segundaVariante.locator('mat-select[formcontrolname="colorId"]').click();
    await page.getByRole('option', { name: nombres.color2, exact: true }).click();
    await segundaVariante.locator('input[formcontrolname="cantidad"]').fill('3');
    await segundaVariante.locator('input[formcontrolname="costo"]').fill('100');
    await segundaVariante.locator('input[formcontrolname="precio"]').fill('300');
    await segundaVariante.locator('input[formcontrolname="umbralStockBajo"]').fill('0');'''
if text.count(old) != 1:
    raise SystemExit('Bloque de filas de variante no único o no encontrado')
path.write_text(text.replace(old, new, 1), encoding='utf-8')
