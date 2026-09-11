import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const repoDir = path.resolve(frontendDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  appRoutes,
  paths,
  service,
  catalog,
  productTs,
  productHtml,
  productScss,
  productsHtml,
  backendController
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.paths.ts'),
  readFeature('varistorehn.service.ts'),
  readFeature('varistorehn.catalog.ts'),
  readFeature('varistorehn-producto.component.ts'),
  readFeature('varistorehn-producto.component.html'),
  readFeature('varistorehn-producto.component.scss'),
  readFeature('varistorehn-productos.component.html'),
  readFile(path.join(repoDir, 'backend/src/API/Controllers/TiendaController.cs'), 'utf8')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const productRouteLine = appRoutes.split('\n').find(line => line.includes("path: 'varistorehn/producto/:slug'")) || '';
expect(Boolean(productRouteLine), 'Debe existir la ruta pública /varistorehn/producto/:slug.');
expect(productRouteLine.includes('VaristorehnProductoComponent'), 'La ruta de detalle debe cargar VaristorehnProductoComponent.');
expect(!productRouteLine.includes('authGuard') && !productRouteLine.includes('permisoGuard'), 'El detalle público no debe usar guards administrativos.');
expect(paths.includes("producto: (slug: string) => `/varistorehn/producto/${encodeURIComponent(slug)}`"), 'El mapa canónico debe definir el detalle por slug.');
expect(service.includes('obtenerProductoPorSlug(slug: string)'), 'El detalle debe usar el endpoint público de producto por slug.');

for (const required of [
  'this.servicio.obtenerProductoPorSlug(slug)',
  'mapearProducto',
  'VARISTOREHN_PATHS.producto(producto.slug)',
  'replaceUrl: true',
  'precioVenta',
  'referenciasCarrito',
  'restaurarCarrito',
  'varistorehn:carrito:v2:',
  'telefonoWhatsapp',
  'crearCatalogoEjemplo',
  'this.servicio.obtenerCatalogo()',
  'this.servicio.obtenerCategorias()',
  'reiniciarCantidad()',
  'cancelarSwipe()'
]) {
  expect(productTs.includes(required), `El detalle público debe integrar ${required}.`);
}

expect(productTs.includes("type EstadoProductoPublico = 'loading' | 'error' | 'not-found' | 'success'"), 'El detalle debe modelar loading/error/not-found/success.');
expect(productTs.includes("this.error.set('No pudimos cargar este producto"), 'Una falla real debe quedar visible y no sustituirse por demo.');
expect(productTs.includes("if (!this.utilizarDatosBaseDatos())"), 'Demo y fuente real deben estar separados de forma explícita.');
expect(productTs.includes('this.stockSeleccionado()'), 'La cantidad debe depender del stock de la variante seleccionada.');
expect(productTs.includes('Math.min(this.stockSeleccionado()'), 'La cantidad debe quedar acotada al stock.');
expect(productTs.includes('Math.min(this.cantidad(), this.stockRestante())'), 'Agregar al carrito debe respetar el stock restante frente al carrito existente.');
expect(!productTs.includes('ProductosListComponent'), 'El detalle público no debe reutilizar el CRUD administrativo.');
expect(!productTs.includes('authGuard') && !productTs.includes('permisoGuard'), 'El componente público no debe importar guards administrativos.');
expect(!productTs.includes('crearCheckoutTarjeta('), 'Fase 4 no debe adelantar el checkout de Fase 6.');

for (const required of [
  'Migas de pan',
  'Galería de imágenes del producto',
  'Miniaturas del producto',
  'pointerdown',
  'pointerup',
  'image-position',
  'lightbox',
  'Cerrar imagen ampliada',
  'SKU',
  'Precio del producto',
  'Cantidad a agregar',
  '[max]="stockSeleccionado()"',
  'Agregar al carrito',
  'Comprar por WhatsApp',
  'Descripción',
  'Características',
  'Productos relacionados',
  'mobile-buy-bar',
  "estado() === 'loading'",
  "estado() === 'error'",
  "estado() === 'not-found'"
]) {
  expect(productHtml.includes(required), `La plantilla de detalle debe contener ${required}.`);
}

expect(productHtml.includes('<dialog #lightbox'), 'La única ampliación modal permitida debe ser el lightbox de imágenes.');
expect(productHtml.indexOf('class="product-layout"') < productHtml.indexOf('<dialog #lightbox'), 'El contenido comercial debe vivir en la página antes del lightbox, no dentro del diálogo.');
expect(!productHtml.includes('class="detail-dialog"'), 'El detalle principal no puede reutilizar el modal legado del home.');
expect(productsHtml.includes('Ver producto'), 'Las tarjetas del catálogo deben ofrecer Ver producto.');
expect(productsHtml.includes("'/varistorehn/producto/' + producto.slug"), 'Ver producto debe navegar por slug a la página independiente.');
expect(productsHtml.includes('producto.precioOferta'), 'El catálogo debe poder mostrar el mismo precio promocional que el detalle cuando aplique.');

expect(catalog.includes('export function precioVenta'), 'Debe existir una regla única de precio efectivo para detalle y carrito.');
expect(catalog.includes('precio: precioVenta(producto, modelo)'), 'El carrito debe capturar el precio efectivo centralizado, no un precio divergente.');

expect(productScss.includes('object-fit: contain'), 'Las imágenes deben preservar proporción con object-fit: contain.');
expect(productScss.includes('touch-action: pan-y'), 'La galería móvil debe permitir swipe horizontal sin romper el scroll vertical.');
expect(productScss.includes('env(safe-area-inset-bottom)'), 'El CTA móvil fijo debe respetar el safe area inferior.');
expect(productScss.includes('.mobile-buy-bar'), 'Debe existir el CTA móvil fijo de compra.');
expect(productScss.includes('min-height: 44px') || productScss.includes('height: 44px'), 'Los controles principales deben conservar objetivos táctiles de al menos 44px.');
expect(productScss.includes('var(--color-bg)') && productScss.includes('var(--color-surface)') && productScss.includes('var(--color-primary)'), 'El detalle debe heredar los tokens canónicos del tema.');
expect(!/#[0-9a-f]{3,8}\b/i.test(productScss), 'Fase 4 no debe introducir colores hexadecimales paralelos al tema.');
expect(!/\brgb(?:a)?\s*\(/i.test(productScss), 'Fase 4 no debe introducir colores RGB paralelos al tema.');
expect(!/\bhsl(?:a)?\s*\(/i.test(productScss), 'Fase 4 no debe introducir colores HSL paralelos al tema.');
expect(!productScss.includes('--color-background') && !productScss.includes('--color-text-secondary'), 'Fase 4 no debe usar aliases de tema inexistentes.');

expect(backendController.includes('[AllowAnonymous]'), 'El controlador público de tienda debe mantenerse anónimo.');
expect(/\[HttpGet\("productos\/\{slug\}"\)\][\s\S]{0,180}GetProducto\(string slug\)/.test(backendController), 'El backend debe exponer producto público por slug.');
expect(/producto\s+is\s+null\s+\|\|\s+!producto\.Activo/.test(backendController), 'Producto inexistente o inactivo debe resolverse como no disponible desde la fuente pública.');

if (failures.length) {
  console.error('Fase 4 — validación de detalle público FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 4 — detalle público: ruta, galería, stock, precio, carrito, WhatsApp, tema y límites aprobados.');
