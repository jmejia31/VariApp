import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  routes,
  paths,
  headerHtml,
  categoriesHtml,
  productsHtml,
  productHtml,
  cartService,
  cartHtml,
  checkoutTs,
  checkoutHtml,
  config,
  homeResponsive,
  cartScss,
  checkoutScss
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.paths.ts'),
  readFeature('varistorehn-header.component.html'),
  readFeature('varistorehn-categorias.component.html'),
  readFeature('varistorehn-productos.component.html'),
  readFeature('varistorehn-producto.component.html'),
  readFeature('varistorehn-carrito.service.ts'),
  readFeature('varistorehn-carrito.component.html'),
  readFeature('varistorehn-checkout.component.ts'),
  readFeature('varistorehn-checkout.component.html'),
  readFeature('varistorehn.config.ts'),
  readFeature('varistorehn.responsive.scss'),
  readFeature('varistorehn-carrito.component.scss'),
  readFeature('varistorehn-checkout.component.scss')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };
const routeLine = route => routes.split('\n').find(line => line.includes(`path: '${route}'`)) || '';

const publicRoutes = [
  ['varistorehn', 'escaparate'],
  ['varistorehn/categorias', 'categorías'],
  ['varistorehn/categoria/:slug', 'categoría'],
  ['varistorehn/productos', 'catálogo'],
  ['varistorehn/producto/:slug', 'detalle'],
  ['varistorehn/carrito', 'carrito'],
  ['varistorehn/checkout', 'checkout'],
  ['varistorehn/pedido/:id', 'cierre de pedido']
];

for (const [route, label] of publicRoutes) {
  const line = routeLine(route);
  expect(Boolean(line), `MVP debe conservar la ruta pública de ${label}: /${route}.`);
  expect(!line.includes('authGuard') && !line.includes('permisoGuard') && !line.includes('canActivate'),
    `La ruta MVP de ${label} no debe exigir login administrativo.`);
}

for (const token of [
  "inicio: '/varistorehn'",
  "productos: '/varistorehn/productos'",
  "categorias: '/varistorehn/categorias'",
  "carrito: '/varistorehn/carrito'",
  "checkout: '/varistorehn/checkout'",
  'producto: (slug: string)',
  'categoria: (slug: string)',
  'pedido: (id: string | number)'
]) {
  expect(paths.includes(token), `Rutas canónicas MVP deben conservar: ${token}.`);
}

expect(headerHtml.includes('Navegación de tienda'), 'MVP debe conservar navegación pública en el header.');
expect(headerHtml.includes('Productos') && headerHtml.includes('Categorías') && headerHtml.includes('Mi carrito'),
  'Header MVP debe exponer catálogo, categorías y carrito.');
expect(!headerHtml.includes('\\n'), 'Header MVP no debe renderizar escapes literales \\n.');

expect(categoriesHtml.includes('categories-grid') && categoriesHtml.includes('categoria.nombre'),
  'MVP debe conservar listado público de categorías.');
expect(productsHtml.includes('CATÁLOGO PÚBLICO') && productsHtml.includes('product-grid'),
  'MVP debe conservar catálogo público.');
expect(productsHtml.includes('rutaProducto') || productsHtml.includes('/varistorehn/producto'),
  'Catálogo debe enlazar al detalle de producto.');

expect(productHtml.includes('price-block') && productHtml.includes('precioActual()'),
  'Detalle MVP debe mostrar precio vigente.');
expect(productHtml.includes('availability') && productHtml.includes('textoDisponibilidad()'),
  'Detalle MVP debe mostrar disponibilidad/stock.');
expect(productHtml.includes('Agregar al carrito') && productHtml.includes('[disabled]="!puedeAgregar()"'),
  'Detalle MVP debe permitir agregar al carrito y bloquear cuando no hay disponibilidad.');

expect(cartService.includes('localStorage.setItem') && cartService.includes('localStorage.getItem'),
  'Carrito MVP debe persistir entre recargas.');
expect(cartService.includes('referenciasCarrito') && cartService.includes('restaurarCarrito'),
  'Carrito persistente debe rehidratar referencias contra catálogo vigente.');
expect(cartHtml.includes('Continuar al checkout'), 'Carrito MVP debe conectar con checkout.');

expect(checkoutHtml.includes('Confirma tus datos y tu forma de compra'), 'MVP debe conservar checkout público.');
expect(checkoutTs.includes('prepararWhatsapp()') && checkoutTs.includes('confirmarSalidaWhatsapp'),
  'MVP debe permitir cierre por WhatsApp.');
expect(checkoutTs.includes("this.identidad.config().nombreComercial || 'VariStoreHN'"),
  'WhatsApp MVP debe usar marca comercial pública.');
expect(!checkoutTs.includes('VaristorehnCuentaService'), 'Cuenta de cliente no debe ser requisito del checkout MVP.');
expect(config.includes('endpointCheckoutTarjeta: null'), 'Pasarela debe seguir siendo opcional/fail-closed por defecto.');
expect(config.includes("export type ModoCarrito = 'whatsapp' | 'tarjeta' | 'ambos'"),
  'La operación debe poder cerrar inicialmente por WhatsApp sin exigir pasarela.');

for (const [name, scss] of [
  ['home', homeResponsive],
  ['carrito', cartScss],
  ['checkout', checkoutScss]
]) {
  expect(scss.includes('@media'), `Responsive básico MVP debe cubrir ${name}.`);
}
expect(homeResponsive.includes('max-width: 600px') || homeResponsive.includes('max-width:600px'),
  'Home MVP debe incluir un breakpoint móvil común.');
expect(cartScss.includes('max-width:680px') || cartScss.includes('max-width: 680px'),
  'Carrito MVP debe reflowar en móvil.');
expect(checkoutScss.includes('max-width: 600px') || checkoutScss.includes('max-width:600px'),
  'Checkout MVP debe reflowar en móvil.');

if (failures.length) {
  console.error('MVP Fases 0–6 — auditoría de alcance FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('MVP Fases 0–6 — header, categorías, catálogo, detalle, precio/stock, carrito persistente, checkout, WhatsApp y responsive aprobados.');
