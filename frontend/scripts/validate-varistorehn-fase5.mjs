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
  cartService,
  cartTs,
  cartHtml,
  cartScss,
  productHtml,
  homeTs,
  productsTs,
  productTs,
  categoriesTs,
  categoryTs
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.paths.ts'),
  readFeature('varistorehn-carrito.service.ts'),
  readFeature('varistorehn-carrito.component.ts'),
  readFeature('varistorehn-carrito.component.html'),
  readFeature('varistorehn-carrito.component.scss'),
  readFeature('varistorehn-producto.component.html'),
  readFeature('varistorehn.component.ts'),
  readFeature('varistorehn-productos.component.ts'),
  readFeature('varistorehn-producto.component.ts'),
  readFeature('varistorehn-categorias.component.ts'),
  readFeature('varistorehn-categoria.component.ts')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const cartRoute = routes.split('\n').find(line => line.includes("path: 'varistorehn/carrito'")) || '';
expect(Boolean(cartRoute), 'Debe existir /varistorehn/carrito.');
expect(cartRoute.includes('VaristorehnCarritoComponent'), 'La ruta /varistorehn/carrito debe cargar su página pública.');
expect(!cartRoute.includes('authGuard') && !cartRoute.includes('permisoGuard'), 'El carrito público no debe usar guards administrativos.');
expect(paths.includes("carrito: '/varistorehn/carrito'"), 'VARISTOREHN_PATHS debe conservar la ruta canónica del carrito.');

for (const required of [
  "@Injectable({ providedIn: 'root' })",
  'readonly items = this._items.asReadonly()',
  'readonly totalUnidades = computed',
  'readonly subtotal = computed',
  'readonly total = computed',
  'restaurarCarrito(originales, productos)',
  'referenciasCarrito(restaurados)',
  'varistorehn:carrito:v2:',
  'incrementar(clave: string)',
  'disminuir(clave: string)',
  'establecerCantidad(clave: string, unidades: number)',
  'quitar(clave: string)',
  'vaciar(): void',
  'Math.max(1, Math.min(item.stock',
  'this.document.defaultView?.localStorage.setItem'
]) {
  expect(cartService.includes(required), `El store global debe contener ${required}.`);
}

expect(!/precio\s*:\s*item\.precio/.test(cartService), 'La persistencia no debe serializar precios como autoridad.');
expect(!cartService.includes('JSON.stringify(this._items'), 'localStorage nunca debe guardar ItemCarrito completo.');
expect(cartService.includes('JSON.stringify(referencias)'), 'localStorage debe guardar referencias mínimas.');

for (const [name, source] of [
  ['home', homeTs],
  ['catálogo', productsTs],
  ['detalle', productTs],
  ['categorías', categoriesTs],
  ['categoría', categoryTs],
  ['carrito', cartTs]
]) {
  expect(source.includes('VaristorehnCarritoService'), `${name} debe consumir el carrito global.`);
  expect(!source.includes('localStorage'), `${name} no debe mantener un localStorage de carrito paralelo.`);
}

for (const source of [productsTs, productTs, categoriesTs, categoryTs]) {
  expect(source.includes('VARISTOREHN_PATHS.carrito'), 'Las páginas públicas independientes deben navegar al carrito canónico.');
}

for (const required of [
  'Mi carrito',
  'TU CARRITO ESTÁ VACÍO',
  'Explorar productos',
  'Vaciar carrito',
  'Precio unitario',
  'Cantidad',
  'Total de línea',
  'Subtotal',
  'Total del carrito',
  'carrito.total()',
  'min="1"',
  '[max]="item.stock"',
  '[disabled]="item.unidades <= 1"',
  '[disabled]="item.unidades >= item.stock"',
  'No se puede continuar a checkout con un carrito vacío',
  'Checkout se habilita en la Fase 6'
]) {
  expect(cartHtml.includes(required), `La página de carrito debe incluir ${required}.`);
}
expect(!cartHtml.includes('[href]="enlaces.checkout"'), 'Fase 5 no debe enlazar un checkout aún inexistente.');
expect(!cartTs.includes('crearCheckoutTarjeta'), 'La página de carrito no debe adelantar lógica de pago de Fase 6.');
expect(!cartTs.includes('authGuard') && !cartTs.includes('permisoGuard'), 'El carrito público no debe importar guards administrativos.');

expect(cartScss.includes('var(--color-bg)') && cartScss.includes('var(--color-surface)') && cartScss.includes('var(--color-primary)'), 'El carrito debe usar tokens canónicos del tema.');
expect(!/#[0-9a-f]{3,8}\b/i.test(cartScss), 'Fase 5 no debe introducir colores hexadecimales propios.');
expect(!/\brgb(?:a)?\s*\(/i.test(cartScss), 'Fase 5 no debe introducir colores RGB propios.');
expect(!/\bhsl(?:a)?\s*\(/i.test(cartScss), 'Fase 5 no debe introducir colores HSL propios.');
expect(/min-height\s*:\s*44px/.test(cartScss), 'Los controles del carrito deben conservar touch targets de al menos 44 px.');
expect(cartScss.includes('@media(max-width:390px)'), 'El carrito debe incluir validación responsive para teléfonos estrechos.');

expect(productTs.includes('stockRestante'), 'El detalle debe descontar lo ya agregado al validar una nueva cantidad.');
expect(productTs.includes('this.carritoStore.unidadesDe'), 'El stock restante del detalle debe salir del carrito global.');
expect(productHtml.includes('[max]="stockRestante()"'), 'El input del detalle debe mostrar el stock realmente restante, no el stock total.');
expect(productHtml.includes('cantidad() >= stockRestante()'), 'El botón + del detalle debe bloquearse en el stock restante.');
expect(homeTs.includes('location.assign(VARISTOREHN_PATHS.producto(producto.slug))'), 'El home debe llevar Ver producto al detalle canónico por slug.');
expect(!homeTs.includes('detalleDialog?.nativeElement.showModal'), 'El home no debe abrir un modal como detalle principal.');

if (failures.length) {
  console.error('Fase 5 — validación de carrito global FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 5 — carrito global: ruta, store único, persistencia mínima, stock, subtotal/total, tema y límites aprobados.');
