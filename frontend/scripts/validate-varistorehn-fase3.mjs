import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  appRoutes,
  paths,
  models,
  service,
  catalog,
  productsTs,
  productsHtml,
  productsScss,
  headerTs,
  categoriesTs,
  categoryTs
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.paths.ts'),
  readFeature('varistorehn.models.ts'),
  readFeature('varistorehn.service.ts'),
  readFeature('varistorehn.catalog.ts'),
  readFeature('varistorehn-productos.component.ts'),
  readFeature('varistorehn-productos.component.html'),
  readFeature('varistorehn-productos.component.scss'),
  readFeature('varistorehn-header.component.ts'),
  readFeature('varistorehn-categorias.component.ts'),
  readFeature('varistorehn-categoria.component.ts')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const productsRouteLine = appRoutes.split('\n').find(line => line.includes("path: 'varistorehn/productos'")) || '';
expect(Boolean(productsRouteLine), 'Debe existir la ruta pública /varistorehn/productos.');
expect(productsRouteLine.includes('VaristorehnProductosComponent'), 'La ruta /varistorehn/productos debe cargar su componente público independiente.');
expect(!productsRouteLine.includes('authGuard') && !productsRouteLine.includes('permisoGuard'), 'La ruta pública de productos no debe usar guards administrativos.');
expect(!appRoutes.includes("path: 'varistorehn/producto/:slug'"), 'Fase 3 no debe activar todavía la ruta de detalle asignada a Fase 4.');

expect(paths.includes("productos: '/varistorehn/productos'"), 'El mapa canónico debe conservar VARISTOREHN_PATHS.productos.');
expect(models.includes('export interface ProductoTienda'), 'Fase 3 debe usar ProductoTienda como modelo visual canónico.');
expect(models.includes("export type OrdenCatalogo = 'destacados' | 'precio-asc' | 'precio-desc' | 'nombre'"), 'El orden canónico del catálogo debe conservar sus cuatro opciones.');
expect(service.includes('obtenerCatalogo()'), 'La página de productos debe consumir la frontera pública del catálogo.');
expect(service.includes('expand(datos => datos.page < datos.totalPages'), 'La frontera pública debe leer todas las páginas HTTP antes de filtrar localmente.');
expect(catalog.includes('export function filtrarProductos'), 'Los filtros deben reutilizar la regla pura canónica.');
expect(catalog.includes('export function referenciasCarrito'), 'La persistencia debe conservar el formato canónico de referencias.');
expect(catalog.includes('export function restaurarCarrito'), 'El carrito debe restaurarse contra catálogo/precio/stock actuales.');

for (const required of [
  'ProductoTienda',
  'EstadoConsultaPublica',
  'filtrarProductos',
  'this.servicio.obtenerCatalogo()',
  'this.servicio.obtenerCategorias()',
  'mapearCategoriaTienda',
  'referenciasCarrito',
  'restaurarCarrito',
  'VARISTOREHN_PATHS.productos',
  'this.route.queryParamMap'
]) {
  expect(productsTs.includes(required), `La página independiente de productos debe integrar ${required}.`);
}

expect(productsTs.includes("params.get('q')"), 'La búsqueda profunda debe hidratarse desde el query param q.');
expect(productsTs.includes("params.get('categoria')"), 'El filtro de categoría debe hidratarse desde el slug público del query param categoria.');
expect(productsTs.includes("No se sustituyeron los datos reales por ejemplos"), 'Una falla del catálogo real no debe caer silenciosamente a fixtures.');
expect(productsTs.includes("const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos()"), 'Demo y fuente real deben estar separados explícitamente.');
expect(productsTs.includes('cargarCatalogo(): void'), 'La acción de reintento del template debe ser pública y comprobable por Angular.');
expect(!productsTs.includes('private cargarCatalogo(): void'), 'El template no debe depender de un método privado.');
expect(!productsTs.includes('ProductosListComponent'), 'El catálogo público no debe reutilizar el CRUD administrativo de productos.');
expect(!productsTs.includes('authGuard') && !productsTs.includes('permisoGuard'), 'El componente público no debe depender de guards administrativos.');
expect(!productsTs.includes('obtenerProductoPorSlug('), 'Fase 3 no debe adelantar la carga del detalle de Fase 4.');
expect(!productsTs.includes('crearCheckoutTarjeta('), 'Fase 3 no debe adelantar checkout de Fase 6.');

for (const state of ['loading', 'error', 'empty']) {
  expect(productsHtml.includes(`estadoCatalogo() === '${state}'`), `La plantilla de catálogo debe representar el estado ${state}.`);
}
expect(productsHtml.includes('[attr.aria-busy]="estadoCatalogo() === \'loading\'"'), 'El catálogo debe exponer la carga mediante aria-busy.');
expect(productsHtml.includes('No encontramos coincidencias'), 'Debe distinguir catálogo vacío de filtros sin coincidencias.');
expect(productsHtml.includes('Solo disponibles'), 'Debe existir filtro de disponibilidad.');
expect(productsHtml.includes('Precio máximo'), 'Debe existir filtro de precio máximo.');
for (const order of ['destacados', 'precio-asc', 'precio-desc', 'nombre']) {
  expect(productsHtml.includes(`value="${order}"`), `Debe existir la opción de orden ${order}.`);
}
expect(productsHtml.includes('Paginación del catálogo'), 'La página independiente debe exponer paginación accesible.');
expect(productsHtml.includes("[attr.aria-label]=\"'Modelo de ' + producto.nombre\""), 'Las variantes deben poder seleccionarse con control etiquetado.');
expect(productsHtml.includes("[attr.aria-label]=\"'Agregar ' + producto.nombre\""), 'Cada tarjeta disponible debe ofrecer una acción explícita de agregar.');
expect(productsHtml.includes('destinoSaltar="#catalogo-productos"'), 'El header compartido debe saltar al contenido real de Fase 3.');
expect(!productsHtml.includes('/varistorehn/producto/'), 'Las tarjetas de Fase 3 no deben fingir un detalle antes de Fase 4.');

expect(!/#[0-9a-f]{3,8}\b/i.test(productsScss), 'Fase 3 no debe introducir colores hexadecimales fuera del tema global.');
expect(!/\brgb(?:a)?\s*\(/i.test(productsScss), 'Fase 3 no debe introducir colores RGB paralelos al tema.');
expect(!/\bhsl(?:a)?\s*\(/i.test(productsScss), 'Fase 3 no debe introducir colores HSL paralelos al tema.');
expect(productsScss.includes('var(--color-bg)'), 'La página debe heredar el fondo canónico del tema.');
expect(productsScss.includes('var(--color-text-muted)'), 'La página debe usar el texto secundario canónico del tema.');
expect(productsScss.includes('min-height: 44px'), 'Los controles principales deben conservar objetivos táctiles de al menos 44px.');
expect(!productsScss.includes('--color-background') && !productsScss.includes('--color-text-secondary'), 'Fase 3 no debe reintroducir aliases de tema inexistentes.');

expect(headerTs.includes('productos: VARISTOREHN_PATHS.productos'), 'El enlace Productos del header debe usar la ruta canónica independiente.');
expect(categoriesTs.includes("this.router.navigate(['/varistorehn/productos']"), 'La búsqueda desde el listado de categorías debe continuar al catálogo independiente.');
expect(categoriesTs.includes('productos: VARISTOREHN_PATHS.productos'), 'El footer/listado de categorías debe enlazar el catálogo independiente.');
expect(categoryTs.includes('`${VARISTOREHN_PATHS.productos}?categoria=${encodeURIComponent(slug)}`'), 'La categoría individual debe continuar al catálogo con su slug público.');
expect(categoryTs.includes("this.router.navigate(['/varistorehn/productos']"), 'La búsqueda desde categoría individual debe continuar al catálogo independiente.');
expect(categoryTs.includes('productos: VARISTOREHN_PATHS.productos'), 'La categoría individual debe exponer Productos como ruta canónica.');

if (failures.length) {
  console.error('Fase 3 — validación de catálogo público FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 3 — catálogo independiente: ruta, datos, filtros, carrito, navegación, tema y límites aprobados.');
