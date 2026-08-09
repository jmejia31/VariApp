from pathlib import Path

TS_PATH = Path('frontend/src/app/features/productos/producto-form.component.ts')
HTML_PATH = Path('frontend/src/app/features/productos/producto-form.component.html')

ts = TS_PATH.read_text(encoding='utf-8')

old_import = "import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';"
new_import = old_import + "\nimport { ProductoCombinationGeneratorComponent } from './producto-combination-generator.component';"
if ts.count(old_import) != 1:
    raise SystemExit('Import ancla no único/no encontrado')
ts = ts.replace(old_import, new_import, 1)

old_component_imports = "MatProgressSpinnerModule, ProductoImagenComponent\n  ],"
new_component_imports = "MatProgressSpinnerModule, ProductoImagenComponent, ProductoCombinationGeneratorComponent\n  ],"
if ts.count(old_component_imports) != 1:
    raise SystemExit('Imports @Component ancla no único/no encontrado')
ts = ts.replace(old_component_imports, new_component_imports, 1)

anchor = "  quitarVariante(index: number): void {"
if ts.count(anchor) != 1:
    raise SystemExit('Método quitarVariante ancla no único/no encontrado')
addition = r'''  get combinacionesActuales(): string[] {
    return this.variantes.getRawValue().map(variante => this.claveCombinacion({
      marcaId: this.normalizarId(variante.marcaId),
      modeloId: this.normalizarId(variante.modeloId),
      colorId: this.normalizarId(variante.colorId),
      tallaId: this.normalizarId(variante.tallaId),
      cantidad: Number(variante.cantidad ?? 0),
      umbralStockBajo: Number(variante.umbralStockBajo ?? 0),
      costo: Number(variante.costo ?? 0),
      precio: Number(variante.precio ?? 0)
    }));
  }

  agregarCombinacionesGeneradas(generadas: ProductoVarianteFormValue[]): void {
    if (generadas.length === 0) return;

    const inicial = this.variantes.length === 1 ? this.variantes.at(0).getRawValue() : null;
    const esFilaInicialVacia = inicial && !inicial.id &&
      !this.normalizarId(inicial.marcaId) && !this.normalizarId(inicial.modeloId) &&
      !this.normalizarId(inicial.colorId) && !this.normalizarId(inicial.tallaId) &&
      !String(inicial.sku ?? '').trim() && !String(inicial.codigoBarras ?? '').trim() &&
      Number(inicial.cantidad ?? 0) === 0 && Number(inicial.costo ?? 0) === 0 && Number(inicial.precio ?? 0) === 0;
    if (esFilaInicialVacia) this.variantes.clear();

    const existentes = new Set(this.combinacionesActuales);
    let agregadas = 0;
    let omitidas = 0;
    for (const variante of generadas) {
      const clave = this.claveCombinacion(variante);
      if (existentes.has(clave)) {
        omitidas++;
        continue;
      }
      this.variantes.push(this.crearVarianteGroup(variante));
      existentes.add(clave);
      agregadas++;
    }

    this.errorMessage.set(omitidas > 0
      ? `${agregadas} combinación(es) agregada(s); ${omitidas} duplicada(s) fueron omitidas.`
      : null);
  }

  private claveCombinacion(variante: Pick<ProductoVarianteFormValue, 'marcaId' | 'modeloId' | 'colorId' | 'tallaId'>): string {
    return `${variante.marcaId ?? 0}:${variante.modeloId ?? 0}:${variante.colorId ?? 0}:${variante.tallaId ?? 0}`;
  }

'''
ts = ts.replace(anchor, addition + anchor, 1)
TS_PATH.write_text(ts, encoding='utf-8')

html = HTML_PATH.read_text(encoding='utf-8')
html_anchor = '    <div formArrayName="variantes" class="variants-list">'
if html.count(html_anchor) != 1:
    raise SystemExit('HTML ancla no único/no encontrado')
widget = '''    @if (!isEdit()) {
      <app-producto-combination-generator
        [marcas]="marcas()"
        [modelos]="modelosTodos()"
        [colores]="colores()"
        [tallas]="tallas()"
        [combinacionesExistentes]="combinacionesActuales"
        (combinacionesConfirmadas)="agregarCombinacionesGeneradas($event)">
      </app-producto-combination-generator>
    }
'''
html = html.replace(html_anchor, widget + html_anchor, 1)
HTML_PATH.write_text(html, encoding='utf-8')
