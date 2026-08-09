from pathlib import Path

path = Path('frontend/e2e/fase4-variantes.spec.ts')
text = path.read_text(encoding='utf-8')
old = '''    await page.locator('mat-select[formcontrolname="marcaId"]').click();
    await page.getByRole('option', { name: nombres.marca, exact: true }).click();

    await page.locator('mat-select[formcontrolname="modeloId"]').click();
    await page.getByRole('option', { name: nombres.modelo, exact: true }).click();'''
new = '''    const datosFamilia = page.locator('.data-section');
    await datosFamilia.locator('mat-select[formcontrolname="marcaId"]').click();
    await page.getByRole('option', { name: nombres.marca, exact: true }).click();

    await datosFamilia.locator('mat-select[formcontrolname="modeloId"]').click();
    await page.getByRole('option', { name: nombres.modelo, exact: true }).click();'''
if text.count(old) != 1:
    raise SystemExit('Ancla Marca/Modelo no única o no encontrada')
path.write_text(text.replace(old, new, 1), encoding='utf-8')
