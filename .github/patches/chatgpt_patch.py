from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, content: str) -> None:
    (ROOT / path).write_text(content, encoding="utf-8")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: se esperaba 1 coincidencia y se encontraron {count}")
    return text.replace(old, new, 1)


# Modelos del contrato de ajuste.
model_path = "frontend/src/app/core/models/producto.model.ts"
model = read(model_path)
if "export interface AjusteStockRequest" not in model:
    model += '''

export interface AjusteStockRequest {
  cantidadActualEsperada: number;
  cantidadNueva: number;
  motivo: string;
}

export interface AjusteStockResultado {
  productoId: number;
  productoVarianteId?: number;
  cantidadAnterior: number;
  cantidadNueva: number;
  diferencia: number;
  motivo: string;
}
'''
write(model_path, model)

# Cliente HTTP.
service_path = "frontend/src/app/services/producto.service.ts"
service = read(service_path)
service = replace_once(
    service,
    "import { Producto, ProductoFormValue, ProductoVariante, ProductoVarianteFormValue } from '../core/models/producto.model';",
    "import { AjusteStockRequest, AjusteStockResultado, Producto, ProductoFormValue, ProductoVariante, ProductoVarianteFormValue } from '../core/models/producto.model';",
    "importar contratos de ajuste")
service = replace_once(
    service,
    '''  eliminarVariante(productoId: number, varianteId: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}`);
  }

  activar(id: number): Observable<ApiResponse<Producto>> {''',
    '''  eliminarVariante(productoId: number, varianteId: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}`);
  }

  ajustarStockProducto(
    productoId: number,
    request: AjusteStockRequest
  ): Observable<ApiResponse<AjusteStockResultado>> {
    return this.http.post<ApiResponse<AjusteStockResultado>>(
      `${this.apiUrl}/${productoId}/ajustes-stock`,
      request
    );
  }

  ajustarStockVariante(
    productoId: number,
    varianteId: number,
    request: AjusteStockRequest
  ): Observable<ApiResponse<AjusteStockResultado>> {
    return this.http.post<ApiResponse<AjusteStockResultado>>(
      `${this.apiUrl}/${productoId}/variantes/${varianteId}/ajustes-stock`,
      request
    );
  }

  activar(id: number): Observable<ApiResponse<Producto>> {''',
    "métodos HTTP de ajuste")
write(service_path, service)

# Detalle de producto: ajuste de producto simple.
detail_ts_path = "frontend/src/app/features/productos/producto-detail.component.ts"
detail_ts = read(detail_ts_path)
detail_ts = replace_once(
    detail_ts,
    '''  readonly puedeExportar = signal(false);
  readonly puedeEditar = signal(false);''',
    '''  readonly puedeExportar = signal(false);
  readonly puedeEditar = signal(false);
  readonly ajustandoStock = signal(false);''',
    "signal ajuste producto")
detail_ts = replace_once(
    detail_ts,
    '''    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.productoService.getById(id).subscribe({
      next: (res) => { this.producto.set(res.data); this.loading.set(false); },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }

  ampliar(imagen: ProductoImagen): void {''',
    '''    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.cargarProducto(id);
  }

  private cargarProducto(id: number): void {
    this.loading.set(true);
    this.productoService.getById(id).subscribe({
      next: (res) => {
        this.producto.set(res.data);
        this.notFound.set(false);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  ajustarStockProducto(): void {
    const producto = this.producto();
    if (!producto || producto.usaVariantes || this.ajustandoStock()) return;

    const cantidadTexto = window.prompt(
      `Stock actual: ${producto.cantidad}. Ingresa la nueva cantidad:`,
      String(producto.cantidad)
    );
    if (cantidadTexto === null) return;

    const cantidadNueva = Number(cantidadTexto.trim());
    if (!Number.isInteger(cantidadNueva) || cantidadNueva < 0) {
      this.snackBar.open('La nueva cantidad debe ser un entero mayor o igual que cero.', 'Cerrar', { duration: 5000 });
      return;
    }

    const motivo = window.prompt('Motivo obligatorio del ajuste:')?.trim();
    if (!motivo) {
      this.snackBar.open('El motivo del ajuste es obligatorio.', 'Cerrar', { duration: 5000 });
      return;
    }

    this.ajustandoStock.set(true);
    this.productoService.ajustarStockProducto(producto.id, {
      cantidadActualEsperada: producto.cantidad,
      cantidadNueva,
      motivo
    }).subscribe({
      next: () => {
        this.ajustandoStock.set(false);
        this.snackBar.open('Inventario ajustado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargarProducto(producto.id);
      },
      error: (err) => {
        this.ajustandoStock.set(false);
        this.snackBar.open(
          err.error?.message ?? 'No se pudo ajustar el inventario.',
          'Cerrar',
          { duration: 6000 }
        );
        this.cargarProducto(producto.id);
      }
    });
  }

  ampliar(imagen: ProductoImagen): void {''',
    "método ajuste producto simple")
write(detail_ts_path, detail_ts)

detail_html_path = "frontend/src/app/features/productos/producto-detail.component.html"
detail_html = read(detail_html_path)
detail_html = replace_once(
    detail_html,
    '''            <div class="header-actions">
              <a class="action-edit" [routerLink]="['/productos', p.id, 'variantes']" mat-button><mat-icon>tune</mat-icon> Variantes</a>
              <a class="action-edit" [routerLink]="['/productos', p.id, 'editar']" mat-button><mat-icon>edit</mat-icon> Editar</a>
            </div>''',
    '''            <div class="header-actions">
              @if (!p.usaVariantes) {
                <button class="action-edit" mat-button type="button" (click)="ajustarStockProducto()" [disabled]="ajustandoStock()">
                  <mat-icon>inventory</mat-icon>
                  {{ ajustandoStock() ? 'Ajustando...' : 'Ajustar inventario' }}
                </button>
              }
              <a class="action-edit" [routerLink]="['/productos', p.id, 'variantes']" mat-button><mat-icon>tune</mat-icon> Variantes</a>
              <a class="action-edit" [routerLink]="['/productos', p.id, 'editar']" mat-button><mat-icon>edit</mat-icon> Editar</a>
            </div>''',
    "botón ajuste producto")
write(detail_html_path, detail_html)

# Mantenimiento de variantes: cantidad de solo lectura y acción separada.
variants_ts_path = "frontend/src/app/features/productos/producto-variantes.component.ts"
variants_ts = read(variants_ts_path)
variants_ts = replace_once(
    variants_ts,
    '''  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);''',
    '''  readonly saving = signal(false);
  readonly ajustandoId = signal<number | null>(null);
  readonly errorMessage = signal<string | null>(null);''',
    "signal ajuste variante")
variants_ts = replace_once(
    variants_ts,
    '''    this.form.setValue({
      colorId: variante.colorId,
      sku: variante.sku,
      codigoBarras: variante.codigoBarras ?? '',
      cantidad: variante.cantidad,
      umbralStockBajo: variante.umbralStockBajo,
      costo: variante.costo,
      precio: variante.precio
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });''',
    '''    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.setValue({
      colorId: variante.colorId,
      sku: variante.sku,
      codigoBarras: variante.codigoBarras ?? '',
      cantidad: variante.cantidad,
      umbralStockBajo: variante.umbralStockBajo,
      costo: variante.costo,
      precio: variante.precio
    });
    this.form.controls.cantidad.disable({ emitEvent: false });
    window.scrollTo({ top: 0, behavior: 'smooth' });''',
    "bloquear cantidad variante")
variants_ts = replace_once(
    variants_ts,
    '''  cancelar(): void {
    this.editandoId.set(null);
    this.form.reset({ colorId: 0, sku: '', codigoBarras: '', cantidad: 0, umbralStockBajo: 5, costo: 0, precio: 0 });
  }

  guardar(): void {''',
    '''  cancelar(): void {
    this.editandoId.set(null);
    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.reset({ colorId: 0, sku: '', codigoBarras: '', cantidad: 0, umbralStockBajo: 5, costo: 0, precio: 0 });
  }

  ajustarStock(variante: ProductoVariante): void {
    if (this.ajustandoId() !== null) return;

    const cantidadTexto = window.prompt(
      `Stock actual de ${variante.sku}: ${variante.cantidad}. Ingresa la nueva cantidad:`,
      String(variante.cantidad)
    );
    if (cantidadTexto === null) return;

    const cantidadNueva = Number(cantidadTexto.trim());
    if (!Number.isInteger(cantidadNueva) || cantidadNueva < 0) {
      this.errorMessage.set('La nueva cantidad debe ser un entero mayor o igual que cero.');
      return;
    }

    const motivo = window.prompt('Motivo obligatorio del ajuste:')?.trim();
    if (!motivo) {
      this.errorMessage.set('El motivo del ajuste es obligatorio.');
      return;
    }

    this.ajustandoId.set(variante.id);
    this.errorMessage.set(null);
    this.productoService.ajustarStockVariante(this.productoId, variante.id, {
      cantidadActualEsperada: variante.cantidad,
      cantidadNueva,
      motivo
    }).subscribe({
      next: () => {
        this.ajustandoId.set(null);
        this.snackBar.open('Inventario de la variante ajustado correctamente.', 'Cerrar', { duration: 3500 });
        this.cancelar();
        this.cargar();
      },
      error: (err) => {
        this.ajustandoId.set(null);
        this.errorMessage.set(err.error?.message ?? 'No se pudo ajustar el inventario de la variante.');
        this.cargar();
      }
    });
  }

  guardar(): void {''',
    "método ajuste variante")
write(variants_ts_path, variants_ts)

variants_html_path = "frontend/src/app/features/productos/producto-variantes.component.html"
variants_html = read(variants_html_path)
variants_html = replace_once(
    variants_html,
    '''    <mat-form-field appearance="outline">
      <mat-label>Existencias</mat-label>
      <input matInput type="number" min="0" formControlName="cantidad">
    </mat-form-field>''',
    '''    <mat-form-field appearance="outline">
      <mat-label>Existencias</mat-label>
      <input matInput type="number" min="0" formControlName="cantidad">
      @if (editandoId()) {
        <mat-hint>Usa “Ajustar stock” para modificar existencias con control de concurrencia.</mat-hint>
      }
    </mat-form-field>''',
    "hint stock variante")
variants_html = replace_once(
    variants_html,
    '''          <td mat-cell *matCellDef="let v" class="actions">
            <button mat-icon-button type="button" title="Editar" (click)="editar(v)"><mat-icon>edit</mat-icon></button>
            <button mat-icon-button type="button" [title]="v.activo ? 'Desactivar' : 'Activar'" (click)="cambiarEstado(v)"><mat-icon>{{ v.activo ? 'toggle_on' : 'toggle_off' }}</mat-icon></button>
            <button mat-icon-button type="button" title="Eliminar" [disabled]="v.cantidad !== 0" (click)="eliminar(v)"><mat-icon>delete</mat-icon></button>
          </td>''',
    '''          <td mat-cell *matCellDef="let v" class="actions">
            <button mat-icon-button type="button" title="Ajustar stock" [disabled]="ajustandoId() !== null" (click)="ajustarStock(v)"><mat-icon>inventory</mat-icon></button>
            <button mat-icon-button type="button" title="Editar metadatos" (click)="editar(v)"><mat-icon>edit</mat-icon></button>
            <button mat-icon-button type="button" [title]="v.activo ? 'Desactivar' : 'Activar'" (click)="cambiarEstado(v)"><mat-icon>{{ v.activo ? 'toggle_on' : 'toggle_off' }}</mat-icon></button>
            <button mat-icon-button type="button" title="Eliminar" [disabled]="v.cantidad !== 0" (click)="eliminar(v)"><mat-icon>delete</mat-icon></button>
          </td>''',
    "acción ajuste variante")
write(variants_html_path, variants_html)

# Formulario general: variantes y cantidades de solo lectura en edición.
form_ts_path = "frontend/src/app/features/productos/producto-form.component.ts"
form_ts = read(form_ts_path)
form_ts = replace_once(
    form_ts,
    "  private productoId: number | null = null;",
    "  productoId: number | null = null;",
    "exponer id para enlace")
form_ts = replace_once(
    form_ts,
    '''        this.cargarModelos(marcaId, () => {''',
    '''        if (this.isEdit()) {
          this.variantes.controls.forEach((control) => control.disable({ emitEvent: false }));
        }

        this.cargarModelos(marcaId, () => {''',
    "deshabilitar variantes en edición")
write(form_ts_path, form_ts)

form_html_path = "frontend/src/app/features/productos/producto-form.component.html"
form_html = read(form_html_path)
form_html = replace_once(
    form_html,
    '''          <h2 id="producto-variantes-title">Colores y existencias</h2>
          <p>Agrega una fila por cada color disponible. La cantidad de cada fila alimenta el inventario de esa variante.</p>
        </div>
        <button mat-stroked-button color="primary" type="button" (click)="agregarVariante()">
          <mat-icon>add</mat-icon>
          Agregar otro color
        </button>''',
    '''          <h2 id="producto-variantes-title">Colores y existencias</h2>
          @if (isEdit()) {
            <p>Las variantes y el stock se administran por separado para impedir sobrescrituras de inventario.</p>
          } @else {
            <p>Agrega una fila por cada color disponible. La cantidad inicial alimenta el inventario de esa variante.</p>
          }
        </div>
        @if (isEdit() && productoId) {
          <a mat-stroked-button color="primary" [routerLink]="['/productos', productoId, 'variantes']">
            <mat-icon>tune</mat-icon>
            Administrar variantes
          </a>
        } @else {
          <button mat-stroked-button color="primary" type="button" (click)="agregarVariante()">
            <mat-icon>add</mat-icon>
            Agregar otro color
          </button>
        }''',
    "encabezado variantes formulario")
form_html = replace_once(
    form_html,
    '''              <button mat-icon-button type="button" color="warn" [attr.aria-label]="'Quitar color ' + (i + 1)" title="Quitar color" (click)="quitarVariante(i)">
                <mat-icon>delete</mat-icon>
              </button>''',
    '''              @if (!isEdit()) {
                <button mat-icon-button type="button" color="warn" [attr.aria-label]="'Quitar color ' + (i + 1)" title="Quitar color" (click)="quitarVariante(i)">
                  <mat-icon>delete</mat-icon>
                </button>
              }''',
    "ocultar quitar en edición")
form_html = replace_once(
    form_html,
    '''                <mat-hint>Unidades disponibles únicamente en este color.</mat-hint>''',
    '''                <mat-hint>{{ isEdit() ? 'Solo lectura. Usa Administrar variantes → Ajustar stock.' : 'Cantidad inicial disponible en este color.' }}</mat-hint>''',
    "hint cantidad producto")
form_html = replace_once(
    form_html,
    '''      <button class="add-color-bottom" mat-stroked-button color="primary" type="button" (click)="agregarVariante()">
        <mat-icon>add_circle</mat-icon>
        Agregar otro color
      </button>''',
    '''      @if (!isEdit()) {
        <button class="add-color-bottom" mat-stroked-button color="primary" type="button" (click)="agregarVariante()">
          <mat-icon>add_circle</mat-icon>
          Agregar otro color
        </button>
      }''',
    "ocultar agregar inferior")
write(form_html_path, form_html)

print("Frontend de ajustes formales conectado.")
