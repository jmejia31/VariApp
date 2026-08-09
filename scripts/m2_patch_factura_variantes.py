from pathlib import Path


def replace_once(path: str, old: str, new: str):
    p = Path(path)
    text = p.read_text(encoding='utf-8')
    if text.count(old) != 1:
        raise SystemExit(f'Ancla no única/no encontrada en {path}: {old[:80]!r}')
    p.write_text(text.replace(old, new, 1), encoding='utf-8')

replace_once(
    'backend/src/Application/Services/FacturaService.cs',
    '''            VarianteColor = d.VarianteColor,\n            VarianteSku = d.VarianteSku,''',
    '''            VarianteColor = d.VarianteColor,\n            VarianteTalla = d.VarianteTalla,\n            VarianteSku = d.VarianteSku,''')

replace_once(
    'backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs',
    '''                            var nombreProducto = compacto\n                                ? ConstruirProductoCompacto(detalle)\n                                : detalle.ProductoNombre;''',
    '''                            var nombreProducto = compacto\n                                ? ConstruirProductoCompacto(detalle)\n                                : ConstruirProductoPapel(detalle);''')

replace_once(
    'backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs',
    '''    private static string ConstruirProductoCompacto(FacturaDetalleDto detalle)\n    {\n        var clasificacion = string.Join(" ", new[] { detalle.ProductoMarca, detalle.ProductoModelo }\n            .Where(x => !string.IsNullOrWhiteSpace(x)));\n        return string.IsNullOrWhiteSpace(clasificacion)\n            ? detalle.ProductoNombre\n            : $"{detalle.ProductoNombre} · {clasificacion}";\n    }''',
    '''    private static string ConstruirProductoCompacto(FacturaDetalleDto detalle)\n    {\n        var clasificacion = string.Join(" · ", new[]\n        {\n            detalle.ProductoMarca,\n            detalle.ProductoModelo,\n            detalle.VarianteColor,\n            detalle.VarianteTalla,\n            detalle.VarianteSku\n        }.Where(x => !string.IsNullOrWhiteSpace(x)));\n        return string.IsNullOrWhiteSpace(clasificacion)\n            ? detalle.ProductoNombre\n            : $"{detalle.ProductoNombre} · {clasificacion}";\n    }\n\n    private static string ConstruirProductoPapel(FacturaDetalleDto detalle)\n    {\n        var variante = string.Join(" · ", new[]\n        {\n            detalle.VarianteColor,\n            detalle.VarianteTalla,\n            detalle.VarianteSku\n        }.Where(x => !string.IsNullOrWhiteSpace(x)));\n        return string.IsNullOrWhiteSpace(variante)\n            ? detalle.ProductoNombre\n            : $"{detalle.ProductoNombre} · {variante}";\n    }''')

replace_once(
    'frontend/src/app/features/facturas/factura-view.component.html',
    '''                  {{ d.productoNombre }}\n                  <span class="clasificacion-termica">{{ d.productoMarca }} {{ d.productoModelo }}</span>''',
    '''                  {{ d.productoNombre }}\n                  @if (d.varianteColor || d.varianteTalla || d.varianteSku) {\n                    <small class="variante-detalle"> · {{ d.varianteColor || '' }}{{ d.varianteColor && d.varianteTalla ? ' · ' : '' }}{{ d.varianteTalla || '' }}{{ (d.varianteColor || d.varianteTalla) && d.varianteSku ? ' · ' : '' }}{{ d.varianteSku || '' }}</small>\n                  }\n                  <span class="clasificacion-termica">{{ d.productoMarca }} · {{ d.productoModelo }}@if (d.varianteColor) { · {{ d.varianteColor }}}@if (d.varianteTalla) { · {{ d.varianteTalla }}}@if (d.varianteSku) { · {{ d.varianteSku }}}</span>''')
