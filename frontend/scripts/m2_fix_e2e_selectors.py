from pathlib import Path


def replace_once(path: str, old: str, new: str):
    p = Path(path)
    text = p.read_text(encoding='utf-8')
    if text.count(old) != 1:
        raise SystemExit(f'Ancla no única/no encontrada en {path}: {old!r}')
    p.write_text(text.replace(old, new, 1), encoding='utf-8')

# El generador multidimensional añade controles con los mismos formControlName.
# La prueba debe validar los selectores de la fila física, no asumir unicidad global.
replace_once(
    'frontend/e2e/catalogos-mantenimientos.spec.ts',
    '''    await page.goto('/productos/nuevo');\n    await expect(page.locator('mat-select[formcontrolname="marcaId"]')).toBeVisible();\n    await expect(page.locator('mat-select[formcontrolname="modeloId"]')).toBeVisible();\n    await expect(page.locator('mat-select[formcontrolname="colorId"]')).toBeVisible();\n    await expect(page.locator('mat-select[formcontrolname="tallaId"]')).toBeVisible();''',
    '''    await page.goto('/productos/nuevo');\n    const primeraVariante = page.locator('.variant-card').first();\n    await expect(primeraVariante).toBeVisible();\n    await expect(primeraVariante.locator('mat-select[formcontrolname="marcaId"]')).toBeVisible();\n    await expect(primeraVariante.locator('mat-select[formcontrolname="modeloId"]')).toBeVisible();\n    await expect(primeraVariante.locator('mat-select[formcontrolname="colorId"]')).toBeVisible();\n    await expect(primeraVariante.locator('mat-select[formcontrolname="tallaId"]')).toBeVisible();''')

# Terminología anterior quedó obsoleta por el requisito aprobado M2:
# "Variantes y existencias" / "Agregar variante".
p = Path('frontend/e2e/fase4-variantes.spec.ts')
text = p.read_text(encoding='utf-8')
replacements = {
    'Fase 4 — variantes por color, SKU e inventario': 'Fase 4 — variantes multidimensionales, SKU e inventario',
    "name: 'Agregar otro color'": "name: 'Agregar variante'",
    "name: 'Colores y existencias'": "name: 'Variantes y existencias'",
    "name: 'Variantes por color y SKU'": "name: 'Variantes y existencias'",
}
for old, new in replacements.items():
    if old not in text:
        raise SystemExit(f'Ancla no encontrada en fase4-variantes: {old!r}')
    text = text.replace(old, new)
p.write_text(text, encoding='utf-8')
