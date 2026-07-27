from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding='utf-8')


def write(path: str, content: str) -> None:
    Path(path).write_text(content, encoding='utf-8')


def replace_once(path: str, old: str, new: str) -> None:
    text = read(path)
    if old not in text:
        raise RuntimeError(f'No se encontró el bloque esperado en {path}: {old[:120]!r}')
    write(path, text.replace(old, new, 1))


# --- Productos: listado resiliente ---
replace_once(
    'frontend/src/app/features/productos/productos-list.component.html',
    '''                <div class="thumb-wrapper">
                  @if (p.imagenPrincipalUrl) {
                    <img [src]="p.imagenPrincipalUrl" class="thumb" [alt]="p.nombre">
                    @if (p.totalImagenes > 1) {
                      <span class="img-count" [attr.aria-label]="(p.totalImagenes - 1) + ' imágenes adicionales'">+{{ p.totalImagenes - 1 }}</span>
                    }
                  } @else {
                    <div class="thumb placeholder" aria-label="Producto sin imagen"><mat-icon>image</mat-icon></div>
                  }
                </div>''',
    '''                <div class="thumb-wrapper">
                  <app-producto-imagen
                    variant="thumbnail"
                    [src]="p.imagenPrincipalUrl"
                    [alt]="'Imagen principal de ' + p.nombre"
                    [fallbackLabel]="p.nombre + ' no tiene imagen disponible'"
                    loading="lazy">
                  </app-producto-imagen>
                  @if (p.totalImagenes > 1) {
                    <span class="img-count" [attr.aria-label]="(p.totalImagenes - 1) + ' imágenes adicionales'">+{{ p.totalImagenes - 1 }}</span>
                  }
                </div>'''
)
replace_once(
    'frontend/src/app/features/productos/productos-list.component.html',
    '''            <div class="thumb-wrapper">
              @if (p.imagenPrincipalUrl) {
                <img [src]="p.imagenPrincipalUrl" class="thumb" [alt]="p.nombre">
                @if (p.totalImagenes > 1) {
                  <span class="img-count">+{{ p.totalImagenes - 1 }}</span>
                }
              } @else {
                <div class="thumb placeholder" aria-label="Producto sin imagen"><mat-icon>image</mat-icon></div>
              }
            </div>''',
    '''            <div class="thumb-wrapper">
              <app-producto-imagen
                variant="card"
                [src]="p.imagenPrincipalUrl"
                [alt]="'Imagen principal de ' + p.nombre"
                [fallbackLabel]="p.nombre + ' no tiene imagen disponible'"
                loading="lazy">
              </app-producto-imagen>
              @if (p.totalImagenes > 1) {
                <span class="img-count" [attr.aria-label]="(p.totalImagenes - 1) + ' imágenes adicionales'">+{{ p.totalImagenes - 1 }}</span>
              }
            </div>'''
)
replace_once(
    'frontend/src/app/features/productos/productos-list.component.scss',
    '''.thumb {
  width: 44px;
  height: 44px;
  display: block;
  border-radius: 9px;
  object-fit: cover;

  &.placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--color-bg);
    color: var(--color-text-muted);
  }
}
''',
    ''
)
replace_once(
    'frontend/src/app/features/productos/productos-list.component.scss',
    '''  .producto-card-top .thumb {
    width: 64px;
    height: 64px;
  }
''',
    ''
)
replace_once(
    'frontend/src/app/features/productos/productos-list.component.scss',
    '''  .producto-card-top .thumb {
    width: 58px;
    height: 58px;
  }
''',
    '''  .producto-card-top app-producto-imagen {
    width: 58px;
    height: 58px;
  }
'''
)
replace_once(
    'frontend/src/app/features/productos/productos-list.component.scss',
    '''  .producto-card-top .thumb {
    width: 54px;
    height: 54px;
  }
''',
    '''  .producto-card-top app-producto-imagen {
    width: 54px;
    height: 54px;
  }
'''
)

# --- Producto: formulario con fallback ---
replace_once(
    'frontend/src/app/features/productos/producto-form.component.html',
    '''            <img [src]="img.url" [alt]="img.esPrincipal ? 'Foto principal del producto' : 'Foto adicional del producto'">''',
    '''            <app-producto-imagen
              variant="gallery"
              [src]="img.url"
              [alt]="(img.esPrincipal ? 'Foto principal' : 'Foto adicional ' + (i + 1)) + ' de ' + (form.controls.nombre.value || 'producto')"
              [fallbackLabel]="'No se pudo mostrar la fotografía ' + (i + 1)"
              loading="lazy">
            </app-producto-imagen>'''
)
replace_once(
    'frontend/src/app/features/productos/producto-form.component.scss',
    '''  img {
    width: 100%;
    height: 100%;
    display: block;
    object-fit: cover;
  }
''',
    '''  app-producto-imagen {
    width: 100%;
    height: 100%;
  }
'''
)

# --- Producto: detalle, imagen principal, galería y ampliación accesible ---
write('frontend/src/app/features/productos/producto-detail.component.html', '''<div class="header-row">
  <a routerLink="/productos" mat-icon-button title="Volver" aria-label="Volver a productos"><mat-icon>arrow_back</mat-icon></a>
  <h1 class="page-title">Detalle de producto</h1>
</div>

@if (loading()) {
  <div class="loading" aria-live="polite"><mat-spinner></mat-spinner></div>
} @else if (notFound()) {
  <p class="empty">El producto no existe o fue eliminado.</p>
} @else if (producto(); as p) {
  <article class="card detail-card">
    <div class="product-overview">
      <div class="hero-media">
        <app-producto-imagen
          variant="hero"
          [src]="p.imagenPrincipalUrl"
          [alt]="'Imagen principal de ' + p.nombre"
          [fallbackLabel]="p.nombre + ' no tiene imagen principal disponible'"
          fallbackText="Producto sin imagen"
          loading="eager"
          [priority]="true">
        </app-producto-imagen>
        @if (p.totalImagenes > 1) {
          <span class="image-total">{{ p.totalImagenes }} imágenes</span>
        }
      </div>

      <div class="overview-copy">
        <div class="detail-header">
          <div>
            <h2>{{ p.nombre }}</h2>
            <p class="subtitle">{{ p.marca }} · {{ p.modelo }}</p>
            @if (p.estaAgotado) { <span class="badge danger">Agotado</span> }
          </div>
          @if (puedeEditar()) {
            <a class="action-edit" [routerLink]="['/productos', p.id, 'editar']" mat-button><mat-icon>edit</mat-icon> Editar</a>
          }
        </div>

        <div class="detail-grid">
          <div><span class="label">Existencias</span><span>{{ p.cantidad }}</span></div>
          <div><span class="label">Precio</span><span>L. {{ p.precio | number:'1.2-2' }}</span></div>
          <div><span class="label">Costo</span><span>L. {{ p.costo | number:'1.2-2' }}</span></div>
          <div><span class="label">Categoría</span><span>{{ p.categoriaNombre || 'Sin categoría' }}</span></div>
          <div>
            <span class="label">Color</span>
            <span>
              @if (p.colorNombre) {
                <i class="select-swatch" [style.background]="p.colorCodigoVisual || '#64748B'"></i>{{ p.colorNombre }}
              } @else { Sin color }
            </span>
          </div>
          <div><span class="label">Talla o tamaño</span><span>{{ p.tallaNombre || 'Sin talla' }}</span></div>
          <div><span class="label">Marca administrada</span><span>{{ p.marcaNombre || p.marca }}</span></div>
          <div><span class="label">Modelo administrado</span><span>{{ p.modeloNombre || p.modelo }}</span></div>
          @if (p.descripcion) {
            <div class="full-width"><span class="label">Descripción</span><span>{{ p.descripcion }}</span></div>
          }
        </div>
      </div>
    </div>

    <div class="section-heading">
      <div>
        <h3>Galería de imágenes</h3>
        <p>Selecciona una miniatura para ampliarla.</p>
      </div>
      @if (puedeExportar() && p.totalImagenes > 0) {
        <button mat-button type="button" (click)="descargarTodas()" [disabled]="descargando() === -1">
          @if (descargando() === -1) {
            <mat-spinner diameter="16"></mat-spinner>
          } @else {
            <mat-icon>archive</mat-icon>
          }
          Descargar todas
        </button>
      }
    </div>

    @if (p.imagenes.length === 0) {
      <div class="empty-imagenes" role="status"><mat-icon>image_not_supported</mat-icon><span>Este producto no tiene imágenes cargadas.</span></div>
    } @else {
      <div class="galeria" aria-label="Galería de {{ p.nombre }}">
        @for (img of p.imagenes; track img.id; let i = $index) {
          <article class="galeria-item" [class.principal]="img.esPrincipal">
            <button
              class="gallery-trigger"
              type="button"
              (click)="ampliar(img)"
              [attr.aria-label]="'Ampliar imagen ' + (i + 1) + ' de ' + p.nombre">
              <app-producto-imagen
                variant="gallery"
                [src]="img.url"
                [alt]="(img.esPrincipal ? 'Imagen principal' : 'Imagen adicional ' + (i + 1)) + ' de ' + p.nombre"
                [fallbackLabel]="'No se pudo cargar la imagen ' + (i + 1) + ' de ' + p.nombre"
                loading="lazy">
              </app-producto-imagen>
            </button>
            @if (img.esPrincipal) { <span class="badge principal">Principal</span> }
            @if (puedeExportar()) {
              <button mat-icon-button class="btn-descargar action-view" type="button" title="Descargar" [attr.aria-label]="'Descargar imagen ' + (i + 1) + ' de ' + p.nombre" [disabled]="descargando() === img.id" (click)="descargarImagen(img)">
                @if (descargando() === img.id) {
                  <mat-spinner diameter="18"></mat-spinner>
                } @else {
                  <mat-icon>download</mat-icon>
                }
              </button>
            }
          </article>
        }
      </div>
    }
  </article>

  @if (imagenAmpliada(); as img) {
    <div class="lightbox" role="dialog" aria-modal="true" [attr.aria-label]="'Imagen ampliada de ' + p.nombre" (click)="cerrarAmpliada()">
      <div class="lightbox-media" (click)="$event.stopPropagation()">
        <app-producto-imagen
          variant="lightbox"
          [src]="img.url"
          [alt]="'Imagen ampliada de ' + p.nombre"
          [fallbackLabel]="'No se pudo cargar la imagen ampliada de ' + p.nombre"
          loading="eager"
          [priority]="true">
        </app-producto-imagen>
      </div>
      <button mat-icon-button class="cerrar" type="button" aria-label="Cerrar imagen ampliada" (click)="$event.stopPropagation(); cerrarAmpliada()"><mat-icon>close</mat-icon></button>
    </div>
  }
}
''')

write('frontend/src/app/features/productos/producto-detail.component.scss', ''':host { display: block; min-width: 0; }
.header-row { display: flex; align-items: center; gap: 8px; margin-bottom: 20px; }
.page-title { margin: 0; font-size: clamp(22px, 2.2vw, 30px); }
.loading { display: flex; justify-content: center; padding: 48px 0; }
.empty { text-align: center; color: var(--color-text-muted); padding: 32px 0; }
.detail-card { padding: clamp(16px, 2.4vw, 28px); }

.product-overview {
  display: grid;
  grid-template-columns: minmax(240px, 360px) minmax(0, 1fr);
  gap: clamp(22px, 3vw, 40px);
  align-items: start;
  margin-bottom: 30px;
  padding-bottom: 28px;
  border-bottom: 1px solid var(--color-border);
}
.hero-media { position: relative; min-width: 0; }
.hero-media app-producto-imagen { margin-inline: auto; }
.image-total {
  position: absolute; right: 10px; bottom: 10px; padding: 5px 9px;
  border-radius: 999px; background: rgb(2 6 23 / 78%); color: white;
  font-size: 11px; font-weight: 700;
}
.overview-copy { min-width: 0; }
.detail-header {
  display: flex; justify-content: space-between; align-items: flex-start; gap: 14px;
  margin-bottom: 22px;
  h2 { margin: 0; font-size: clamp(22px, 2.4vw, 32px); line-height: 1.2; overflow-wrap: anywhere; }
  .subtitle { margin: 6px 0 0; font-size: 14px; color: var(--color-text-muted); overflow-wrap: anywhere; }
  a[mat-button] { flex: 0 0 auto; text-decoration: none; }
}
.detail-grid {
  display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px 22px;
  .full-width { grid-column: 1 / -1; }
  > div { min-width: 0; display: flex; flex-direction: column; gap: 3px; }
  .label { font-size: 11px; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: .03em; }
  span:last-child { overflow-wrap: anywhere; }
}
.badge.danger { display: inline-flex; margin-top: 8px; padding: 4px 9px; border-radius: 999px; background: color-mix(in srgb, var(--color-danger) 10%, var(--color-surface)); color: var(--color-danger); font-size: 11px; font-weight: 700; }

.section-heading {
  display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; margin: 0 0 14px;
  h3 { margin: 0; font-size: 17px; }
  p { margin: 4px 0 0; color: var(--color-text-muted); font-size: 12px; }
  button { flex: 0 0 auto; }
}
.empty-imagenes {
  min-height: 150px; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px;
  padding: 28px; border: 1px dashed var(--color-border); border-radius: 12px;
  background: color-mix(in srgb, var(--color-bg) 72%, var(--color-surface)); color: var(--color-text-muted); text-align: center;
}
.galeria { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 14px; }
.galeria-item {
  position: relative; min-width: 0; aspect-ratio: 1; border: 2px solid transparent; border-radius: 12px; overflow: hidden;
  background: var(--color-bg);
  &.principal { border-color: var(--color-primary); }
  .badge.principal { position: absolute; top: 7px; left: 7px; z-index: 2; padding: 3px 8px; border-radius: 999px; background: var(--color-primary); color: var(--color-on-primary); font-size: 10px; font-weight: 800; pointer-events: none; }
  .btn-descargar { position: absolute; right: 6px; bottom: 6px; z-index: 2; background: color-mix(in srgb, var(--color-surface) 92%, transparent); box-shadow: 0 2px 8px rgb(15 23 42 / 18%); }
}
.gallery-trigger {
  width: 100%; height: 100%; min-width: 0; min-height: 0; display: block; padding: 0; border: 0; background: transparent; cursor: zoom-in;
  &:focus-visible { outline: 3px solid var(--color-primary); outline-offset: -3px; }
}
.gallery-trigger app-producto-imagen { width: 100%; height: 100%; }

.lightbox {
  position: fixed; inset: 0; z-index: 2000; display: flex; align-items: center; justify-content: center;
  padding: 24px; background: rgb(0 0 0 / 88%);
}
.lightbox-media { max-width: 100%; max-height: 100%; }
.lightbox .cerrar { position: absolute; top: max(12px, env(safe-area-inset-top)); right: max(12px, env(safe-area-inset-right)); background: rgb(255 255 255 / 16%); color: white; }

@media (max-width: 820px) {
  .product-overview { grid-template-columns: 1fr; }
  .hero-media { width: min(100%, 420px); margin-inline: auto; }
}
@media (max-width: 600px) {
  .detail-header, .section-heading { flex-direction: column; align-items: stretch; }
  .detail-header a[mat-button], .section-heading button { width: 100%; }
  .detail-grid { grid-template-columns: 1fr; }
  .detail-grid .full-width { grid-column: auto; }
  .galeria { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
  .lightbox { padding: 12px; }
}
''')

# --- Formularios de Compra y Venta: miniatura del producto seleccionado ---
for html_path in [
    'frontend/src/app/features/compras/compra-form.component.html',
    'frontend/src/app/features/ventas/venta-form.component.html'
]:
    replace_once(
        html_path,
        '''        <div class="detalle-row" [formGroupName]="i">
          <mat-form-field''',
        '''        <div class="detalle-row" [formGroupName]="i">
          <div class="col-imagen">
            @if (productoSeleccionado(grupo); as producto) {
              <app-producto-imagen
                variant="line"
                [src]="producto.imagenPrincipalUrl"
                [alt]="'Imagen de ' + producto.nombre"
                [fallbackLabel]="producto.nombre + ' no tiene imagen disponible'"
                loading="lazy">
              </app-producto-imagen>
            } @else {
              <app-producto-imagen variant="line" [src]="null" fallbackLabel="Selecciona un producto para mostrar su imagen" fallbackText="Selecciona"></app-producto-imagen>
            }
          </div>
          <mat-form-field'''
    )

for scss_path in [
    'frontend/src/app/features/compras/compra-form.component.scss',
    'frontend/src/app/features/ventas/venta-form.component.scss'
]:
    replace_once(
        scss_path,
        '  grid-template-columns: minmax(220px, 3fr) minmax(90px, 1fr) minmax(110px, 1fr) minmax(90px, auto) auto;',
        '  grid-template-columns: 52px minmax(220px, 3fr) minmax(90px, 1fr) minmax(110px, 1fr) minmax(90px, auto) auto;'
    )
    replace_once(
        scss_path,
        '  .col-producto, .col-cantidad, .col-costo { width: 100%; min-width: 0; }',
        '''  .col-imagen { align-self: start; padding-top: 2px; }
  .col-producto, .col-cantidad, .col-costo { width: 100%; min-width: 0; }'''
    )
    replace_once(
        scss_path,
        '''    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
    grid-template-areas:
      "producto producto producto"
      "cantidad costo eliminar";''',
        '''    grid-template-columns: 52px minmax(0, 1fr) minmax(0, 1fr) auto;
    grid-template-areas:
      "imagen producto producto producto"
      "imagen cantidad costo eliminar";'''
    )
    replace_once(
        scss_path,
        '  .col-producto { grid-area: producto; }',
        '''  .col-imagen { grid-area: imagen; }
  .col-producto { grid-area: producto; }'''
    )
    replace_once(
        scss_path,
        '''    grid-template-columns: 1fr auto;
    grid-template-areas:
      "producto producto"
      "cantidad eliminar"
      "costo costo";''',
        '''    grid-template-columns: 52px minmax(0, 1fr) auto;
    grid-template-areas:
      "imagen producto producto"
      "imagen cantidad eliminar"
      ". costo costo";'''
    )

# --- Detalles de Compra y Venta ---
replace_once(
    'frontend/src/app/features/compras/compra-detail.component.html',
    '<tr><th>Producto</th><th>Marca</th><th>Modelo</th><th>Cantidad</th><th>Costo unit.</th><th>Importe</th></tr>',
    '<tr><th><span class="sr-only">Imagen</span></th><th>Producto</th><th>Marca</th><th>Modelo</th><th>Cantidad</th><th>Costo unit.</th><th>Importe</th></tr>'
)
replace_once(
    'frontend/src/app/features/compras/compra-detail.component.html',
    '''            <tr>
              <td>{{ d.productoNombre }}</td>''',
    '''            <tr>
              <td class="producto-imagen-cell"><app-producto-imagen variant="thumbnail" [src]="d.productoImagenPrincipalUrl" [alt]="'Imagen de ' + d.productoNombre" [fallbackLabel]="d.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen></td>
              <td>{{ d.productoNombre }}</td>'''
)
replace_once(
    'frontend/src/app/features/ventas/venta-detail.component.html',
    '<tr><th>Producto</th><th>Marca</th><th>Modelo</th><th>Cantidad</th><th>Precio unit.</th><th>Subtotal</th></tr>',
    '<tr><th><span class="sr-only">Imagen</span></th><th>Producto</th><th>Marca</th><th>Modelo</th><th>Cantidad</th><th>Precio unit.</th><th>Subtotal</th></tr>'
)
replace_once(
    'frontend/src/app/features/ventas/venta-detail.component.html',
    '''            <tr>
              <td>{{ d.productoNombre }}</td>''',
    '''            <tr>
              <td class="producto-imagen-cell"><app-producto-imagen variant="thumbnail" [src]="d.productoImagenPrincipalUrl" [alt]="'Imagen de ' + d.productoNombre" [fallbackLabel]="d.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen></td>
              <td>{{ d.productoNombre }}</td>'''
)

for scss_path in [
    'frontend/src/app/features/compras/compra-detail.component.scss',
    'frontend/src/app/features/ventas/venta-detail.component.scss'
]:
    text = read(scss_path)
    text += '''

.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
.producto-imagen-cell { width: 60px; min-width: 60px; }
.table th:first-child { width: 60px; }
'''
    text = text.replace('.table { min-width: 560px; }', '.table { min-width: 630px; }')
    write(scss_path, text)

# --- Listados de Compra y Venta: resumen visual de la primera línea ---
replace_once(
    'frontend/src/app/features/compras/compras-list.component.html',
    '''          <th>Número</th>
          <th>Fecha</th>''',
    '''          <th><span class="sr-only">Producto</span></th>
          <th>Número</th>
          <th>Fecha</th>'''
)
replace_once(
    'frontend/src/app/features/compras/compras-list.component.html',
    '''          <tr>
            <td>{{ c.numeroCompra }}</td>''',
    '''          <tr>
            <td class="transaction-image-cell">
              @if (c.detalles[0]; as detalle) {
                <app-producto-imagen variant="thumbnail" [src]="detalle.productoImagenPrincipalUrl" [alt]="'Imagen de ' + detalle.productoNombre" [fallbackLabel]="detalle.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen>
              } @else {
                <app-producto-imagen variant="thumbnail" [src]="null" fallbackLabel="Compra sin productos"></app-producto-imagen>
              }
            </td>
            <td>{{ c.numeroCompra }}</td>'''
)
replace_once('frontend/src/app/features/compras/compras-list.component.html', 'colspan="7"', 'colspan="8"')
replace_once(
    'frontend/src/app/features/compras/compras-list.component.html',
    '''        <a [routerLink]="['/compras', c.id]" class="item-card compra-card">
          <div class="item-card-header">''',
    '''        <a [routerLink]="['/compras', c.id]" class="item-card compra-card">
          <div class="transaction-card-layout">
            @if (c.detalles[0]; as detalle) {
              <app-producto-imagen variant="card" [src]="detalle.productoImagenPrincipalUrl" [alt]="'Imagen de ' + detalle.productoNombre" [fallbackLabel]="detalle.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen>
            } @else {
              <app-producto-imagen variant="card" [src]="null" fallbackLabel="Compra sin productos"></app-producto-imagen>
            }
            <div class="transaction-card-copy">
          <div class="item-card-header">'''
)
replace_once(
    'frontend/src/app/features/compras/compras-list.component.html',
    '''          <div class="item-card-meta">
            <span>L. {{ c.total | number:'1.2-2' }}</span>
            <span>{{ c.creadoPorNombreUsuario || '—' }}</span>
          </div>
        </a>''',
    '''          <p class="transaction-product">{{ c.detalles[0]?.productoNombre || 'Sin productos' }}<span>@if (c.detalles.length > 1) { +{{ c.detalles.length - 1 }} más }</span></p>
          <div class="item-card-meta">
            <span>L. {{ c.total | number:'1.2-2' }}</span>
            <span>{{ c.creadoPorNombreUsuario || '—' }}</span>
          </div>
            </div>
          </div>
        </a>'''
)

replace_once(
    'frontend/src/app/features/ventas/ventas-list.component.html',
    '''          <th>Número</th>
          <th>Fecha</th>''',
    '''          <th><span class="sr-only">Producto</span></th>
          <th>Número</th>
          <th>Fecha</th>'''
)
replace_once(
    'frontend/src/app/features/ventas/ventas-list.component.html',
    '''          <tr>
            <td>{{ v.numeroVenta }}</td>''',
    '''          <tr>
            <td class="transaction-image-cell">
              @if (v.detalles[0]; as detalle) {
                <app-producto-imagen variant="thumbnail" [src]="detalle.productoImagenPrincipalUrl" [alt]="'Imagen de ' + detalle.productoNombre" [fallbackLabel]="detalle.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen>
              } @else {
                <app-producto-imagen variant="thumbnail" [src]="null" fallbackLabel="Venta sin productos"></app-producto-imagen>
              }
            </td>
            <td>{{ v.numeroVenta }}</td>'''
)
replace_once('frontend/src/app/features/ventas/ventas-list.component.html', 'colspan="8"', 'colspan="9"')
replace_once(
    'frontend/src/app/features/ventas/ventas-list.component.html',
    '''        <a [routerLink]="['/ventas', v.id]" class="item-card venta-card">
          <div class="item-card-header">''',
    '''        <a [routerLink]="['/ventas', v.id]" class="item-card venta-card">
          <div class="transaction-card-layout">
            @if (v.detalles[0]; as detalle) {
              <app-producto-imagen variant="card" [src]="detalle.productoImagenPrincipalUrl" [alt]="'Imagen de ' + detalle.productoNombre" [fallbackLabel]="detalle.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen>
            } @else {
              <app-producto-imagen variant="card" [src]="null" fallbackLabel="Venta sin productos"></app-producto-imagen>
            }
            <div class="transaction-card-copy">
          <div class="item-card-header">'''
)
replace_once(
    'frontend/src/app/features/ventas/ventas-list.component.html',
    '''          <div class="item-card-meta">
            <span>L. {{ v.total | number:'1.2-2' }}</span>
            <span>{{ v.confirmadoPorNombreUsuario || v.creadoPorNombreUsuario || '—' }}</span>
          </div>
        </a>''',
    '''          <p class="transaction-product">{{ v.detalles[0]?.productoNombre || 'Sin productos' }}<span>@if (v.detalles.length > 1) { +{{ v.detalles.length - 1 }} más }</span></p>
          <div class="item-card-meta">
            <span>L. {{ v.total | number:'1.2-2' }}</span>
            <span>{{ v.confirmadoPorNombreUsuario || v.creadoPorNombreUsuario || '—' }}</span>
          </div>
            </div>
          </div>
        </a>'''
)

for scss_path in [
    'frontend/src/app/features/compras/compras-list.component.scss',
    'frontend/src/app/features/ventas/ventas-list.component.scss'
]:
    text = read(scss_path)
    text += '''

.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
.transaction-image-cell { width: 60px; min-width: 60px; }
.transaction-card-layout { min-width: 0; display: grid; grid-template-columns: 64px minmax(0, 1fr); gap: 12px; align-items: start; }
.transaction-card-copy { min-width: 0; }
.transaction-product { margin: 0 0 6px; color: var(--color-text); font-size: 12px; line-height: 1.35; overflow-wrap: anywhere; }
.transaction-product span { margin-left: 5px; color: var(--color-text-muted); }
'''
    write(scss_path, text)

# --- Historial de inventario ---
replace_once(
    'frontend/src/app/features/inventario/movimientos-list.component.html',
    '<tr><th scope="col">Fecha</th><th scope="col">Producto</th>',
    '<tr><th scope="col"><span class="sr-only">Imagen</span></th><th scope="col">Fecha</th><th scope="col">Producto</th>'
)
replace_once(
    'frontend/src/app/features/inventario/movimientos-list.component.html',
    '''            <tr>
              <td>{{ m.fecha | date:'dd/MM/yyyy HH:mm' }}</td>''',
    '''            <tr>
              <td class="producto-imagen-cell"><app-producto-imagen variant="thumbnail" [src]="m.productoImagenPrincipalUrl" [alt]="'Imagen de ' + m.productoNombre" [fallbackLabel]="m.productoNombre + ' no tiene imagen disponible'" loading="lazy"></app-producto-imagen></td>
              <td>{{ m.fecha | date:'dd/MM/yyyy HH:mm' }}</td>'''
)
replace_once('frontend/src/app/features/inventario/movimientos-list.component.html', 'colspan="8"', 'colspan="9"')
replace_once(
    'frontend/src/app/features/inventario/movimientos-list.component.scss',
    '  min-width: 760px;',
    '  min-width: 830px;'
)
replace_once(
    'frontend/src/app/features/inventario/movimientos-list.component.scss',
    '''.table td:first-child,
.table td:nth-child(4),
.table td:nth-child(5),
.table td:nth-child(6) {''',
    '''.table td:nth-child(2),
.table td:nth-child(5),
.table td:nth-child(6),
.table td:nth-child(7) {'''
)
write(
    'frontend/src/app/features/inventario/movimientos-list.component.scss',
    read('frontend/src/app/features/inventario/movimientos-list.component.scss') + '\n.producto-imagen-cell { width: 60px; min-width: 60px; }\n'
)

# --- Prueba E2E de imágenes y fallbacks ---
write('frontend/e2e/fase5-imagenes.spec.ts', '''import { test, expect, Page, Route } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const IMAGE_OK = '/fase5/imagen-ok.svg';
const IMAGE_BROKEN = '/fase5/imagen-rota.svg';

const producto = {
  id: 501,
  nombre: 'Producto visual Fase 5',
  marca: 'Marca accesible',
  modelo: 'Modelo responsive',
  descripcion: 'Producto controlado para certificar imágenes, fallbacks y galería.',
  cantidad: 12,
  costo: 100,
  precio: 150,
  umbralStockBajo: 5,
  tieneStockBajo: false,
  estaAgotado: false,
  estadoInventario: 'Disponible',
  activo: true,
  imagenPrincipalUrl: IMAGE_OK,
  imagenes: [
    { id: 901, url: IMAGE_OK, orden: 0, esPrincipal: true },
    { id: 902, url: IMAGE_BROKEN, orden: 1, esPrincipal: false }
  ],
  totalImagenes: 2,
  fechaCreacion: '2026-07-27T00:00:00Z',
  fechaActualizacion: '2026-07-27T00:00:00Z'
};

const productoSinImagen = { ...producto, id: 502, nombre: 'Producto sin fotografía', imagenPrincipalUrl: null, imagenes: [], totalImagenes: 0 };
const detalleCompra = { id: 801, productoId: 501, productoNombre: producto.nombre, productoMarca: producto.marca, productoModelo: producto.modelo, productoImagenPrincipalUrl: IMAGE_OK, cantidad: 2, costoUnitario: 100, subtotal: 200 };
const detalleVenta = { id: 802, productoId: 501, productoNombre: producto.nombre, productoMarca: producto.marca, productoModelo: producto.modelo, productoImagenPrincipalUrl: IMAGE_BROKEN, cantidad: 1, precioUnitario: 150, subtotal: 150, utilidadBruta: 50 };
const compra = { id: 601, numeroCompra: 'COM-F5-001', fecha: '2026-07-27T00:00:00Z', proveedorNombre: 'Proveedor visual', estado: 'Borrador', estadoPago: 'Pendiente', metodoPago: 'Efectivo', subtotal: 200, descuento: 0, impuesto: 0, total: 200, detalles: [detalleCompra], impuestosAplicados: [], fechaCreacion: '2026-07-27T00:00:00Z' };
const venta = { id: 701, numeroVenta: 'VEN-F5-001', fecha: '2026-07-27T00:00:00Z', clienteNombre: 'Cliente visual', estado: 'Borrador', estadoPago: 'Pendiente', metodoPago: 'Efectivo', subtotal: 150, descuento: 0, impuesto: 0, total: 150, costoTotal: 100, utilidadBruta: 50, detalles: [detalleVenta], descuentosAplicados: [], impuestosAplicados: [], fechaCreacion: '2026-07-27T00:00:00Z' };

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

function json(route: Route, data: unknown): Promise<void> {
  return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data }) });
}

async function installFixtures(page: Page): Promise<void> {
  await page.route('**/fase5/imagen-ok.svg', (route) => route.fulfill({
    status: 200,
    contentType: 'image/svg+xml',
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="240" height="240" viewBox="0 0 240 240"><rect width="240" height="240" rx="24" fill="#e2e8f0"/><path d="M40 170l50-55 35 35 25-25 50 45H40z" fill="#475569"/><circle cx="88" cy="78" r="20" fill="#64748b"/></svg>'
  }));
  await page.route('**/fase5/imagen-rota.svg', (route) => route.abort('failed'));

  await page.route(/\/api\/productos\/501(?:\?.*)?$/, (route) => json(route, producto));
  await page.route(/\/api\/productos(?:\?.*)?$/, (route) => json(route, { items: [producto, productoSinImagen], page: 1, pageSize: 200, totalCount: 2, totalPages: 1 }));
  await page.route(/\/api\/compras\/601\/documentos(?:\?.*)?$/, (route) => json(route, []));
  await page.route(/\/api\/compras\/601(?:\?.*)?$/, (route) => json(route, compra));
  await page.route(/\/api\/compras(?:\?.*)?$/, (route) => json(route, { items: [compra], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 }));
  await page.route(/\/api\/ventas\/701(?:\?.*)?$/, (route) => json(route, venta));
  await page.route(/\/api\/ventas(?:\?.*)?$/, (route) => json(route, { items: [venta], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 }));
  await page.route(/\/api\/inventario\/movimientos(?:\?.*)?$/, (route) => json(route, [{ id: 1001, productoId: 501, productoNombre: producto.nombre, productoImagenPrincipalUrl: IMAGE_OK, tipo: 'Entrada', cantidad: 2, stockAnterior: 10, stockNuevo: 12, referenciaTipo: 'Compra', referenciaId: 601, fecha: '2026-07-27T00:00:00Z' }]));
}

async function expectNoOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
}

test.describe('Fase 5 - tratamiento integral de imágenes', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeEach(async ({ page }) => {
    await login(page);
    await installFixtures(page);
  });

  test('Productos usa carga eficiente, texto alternativo y fallback accesible', async ({ page }) => {
    await page.goto('/productos');
    const loaded = page.locator('app-producto-imagen img').first();
    await expect(loaded).toBeVisible();
    await expect(loaded).toHaveAttribute('loading', 'lazy');
    await expect(loaded).toHaveAttribute('decoding', 'async');
    await expect(loaded).toHaveAttribute('alt', /Imagen principal de Producto visual Fase 5/);
    await expect(loaded).toHaveAttribute('width', /\d+/);
    await expect(loaded).toHaveAttribute('height', /\d+/);
    await expect(page.getByRole('img', { name: /Producto sin fotografía no tiene imagen disponible/ }).first()).toBeVisible();
    await expectNoOverflow(page);
  });

  test('Detalle prioriza la imagen principal y la galería es operable con teclado', async ({ page }) => {
    await page.goto('/productos/501');
    const hero = page.locator('app-producto-imagen[data-variant="hero"] img');
    await expect(hero).toBeVisible();
    await expect(hero).toHaveAttribute('loading', 'eager');
    await expect(hero).toHaveAttribute('fetchpriority', 'high');

    const secondTrigger = page.getByRole('button', { name: /Ampliar imagen 2/ });
    await secondTrigger.focus();
    await page.keyboard.press('Enter');
    await expect(page.getByRole('dialog', { name: /Imagen ampliada/ })).toBeVisible();
    await expect(page.getByRole('img', { name: /No se pudo cargar la imagen ampliada/ })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog')).toBeHidden();
    await expectNoOverflow(page);
  });

  test('Compras y Ventas muestran imágenes o fallback en listas, formularios y detalles', async ({ page }) => {
    await page.goto('/compras');
    await expect(page.locator('app-producto-imagen img').first()).toBeVisible();
    await page.goto('/compras/601');
    await expect(page.locator('.producto-imagen-cell app-producto-imagen img')).toBeVisible();

    await page.goto('/compras/nueva');
    await page.locator('.col-producto mat-select').first().click();
    await page.getByRole('option', { name: /Producto visual Fase 5/ }).click();
    await expect(page.locator('.col-imagen app-producto-imagen img')).toBeVisible();

    await page.goto('/ventas');
    await expect(page.getByRole('img', { name: /Producto visual Fase 5 no tiene imagen disponible/ }).first()).toBeVisible();
    await page.goto('/ventas/701');
    await expect(page.getByRole('img', { name: /Producto visual Fase 5 no tiene imagen disponible/ }).first()).toBeVisible();

    await page.goto('/ventas/nueva');
    await page.locator('.col-producto mat-select').first().click();
    await page.getByRole('option', { name: /Producto visual Fase 5/ }).click();
    await expect(page.locator('.col-imagen app-producto-imagen img')).toBeVisible();
    await expectNoOverflow(page);
  });

  test('El historial de inventario conserva la miniatura principal', async ({ page }) => {
    await page.goto('/inventario/movimientos');
    const image = page.locator('.producto-imagen-cell app-producto-imagen img');
    await expect(image).toBeVisible();
    await expect(image).toHaveAttribute('alt', /Imagen de Producto visual Fase 5/);
    await expectNoOverflow(page);
  });
});
''')

# Integrar la prueba en la aceptación permanente.
replace_once(
    '.github/workflows/catalogos-aceptacion.yml',
    '''            e2e/fase4-responsive.spec.ts \\
            e2e/phase7-admin-role.spec.ts''',
    '''            e2e/fase4-responsive.spec.ts \\
            e2e/fase5-imagenes.spec.ts \\
            e2e/phase7-admin-role.spec.ts'''
)

# Actualizar checkpoint de la fase actual.
write('.github/checkpoints/phase-5', '''Checkpoint de certificación de la Fase 5 — tratamiento integral de imágenes.
Fecha: 2026-07-27
Rama exclusiva: Desarrollo
Cobertura: Productos, formularios, Compras, Ventas, detalles e historial de inventario.
Verificaciones: imagen principal, fallback accesible, error de carga, alt descriptivo, dimensiones, lazy/eager loading, prioridad, galería con teclado, lightbox y responsive.
Producción y main permanecen congeladas.
''')

print('Interfaz, estilos, E2E y checkpoint de Fase 5 actualizados.')
