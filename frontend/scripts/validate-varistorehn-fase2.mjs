import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const readFeature = (name) => readFile(path.join(featureDir, name), 'utf8');

const [
  appRoutes,
  models,
  service,
  categoryRules,
  categoriesTs,
  categoriesHtml,
  categoriesScss,
  storefrontTs,
  storefrontHtml,
  headerTs,
  headerHtml
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.models.ts'),
  readFeature('varistorehn.service.ts'),
  readFeature('varistorehn-categorias.catalog.ts'),
  readFeature('varistorehn-categorias.component.ts'),
  readFeature('varistorehn-categorias.component.html'),
  readFeature('varistorehn-categorias.component.scss'),
  readFeature('varistorehn.component.ts'),
  readFeature('varistorehn.component.html'),
  readFeature('varistorehn-header.component.ts'),
  readFeature('varistorehn-header.component.html')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const routeLine = appRoutes.split('\n').find(line => line.includes("path: 'varistorehn/categorias'")) || '';
expect(Boolean(routeLine), 'Debe existir la ruta pública /varistorehn/categorias.');
expect(routeLine.includes('VaristorehnCategoriasComponent'), 'La ruta de categorías debe cargar su página pública independiente.');
expect(!routeLine.includes('authGuard') && !routeLine.includes('permisoGuard'), 'La ruta pública de categorías no debe requerir guards administrativos.');

expect(models.includes('export interface CategoriaTienda'), 'Fase 2 debe conservar CategoriaTienda como modelo visual canónico.');
expect(models.includes('cantidadProductos: number | null'), 'El conteo de CategoriaTienda debe preservar null como desconocido.');
expect(service.includes('obtenerCategorias()'), 'La página pública debe usar la frontera HTTP de categorías ya definida.');
expect(categoryRules.includes('mapearCategoriaTienda'), 'Debe existir un mapeo explícito del DTO público a CategoriaTienda.');
expect(categoryRules.includes('cantidadProductos: cantidad'), 'El mapeo debe conservar el conteo público sin fabricarlo.');
expect(categoryRules.includes('cantidad !== null'), 'El mapeo debe validar el conteo solo cuando la fuente lo conoce.');
expect(categoryRules.includes('crearCategoriasTiendaEjemplo'), 'Los fixtures de preview deben estar separados de la fuente real.');

for (const required of ['CategoriaTienda', 'EstadoConsultaPublica', 'VaristorehnService', 'obtenerCategorias()', 'mapearCategoriaTienda', 'restaurarCarrito', 'VARISTOREHN_PATHS']) {
  expect(categoriesTs.includes(required), `La página de categorías debe integrar ${required}.`);
}
expect(categoriesTs.includes("this.error.set('No pudimos cargar las categorías"), 'La página debe exponer un error real cuando falle la fuente de categorías.');
expect(!categoriesTs.includes('CategoriasListComponent'), 'La página pública no debe reutilizar el CRUD administrativo de categorías.');
expect(!categoriesTs.includes('authGuard') && !categoriesTs.includes('permisoGuard'), 'La página pública no debe depender de guards administrativos.');
expect(!/#[0-9a-f]{3,8}\b/i.test(categoriesScss), 'La página de categorías no debe introducir colores hexadecimales fuera del tema.');
expect(categoriesScss.includes('min-height: 44px'), 'Los controles de Fase 2 deben conservar objetivos táctiles de al menos 44px.');

for (const state of ['loading', 'error', 'empty']) {
  expect(categoriesHtml.includes(`estado() === '${state}'`), `La página de categorías debe representar el estado ${state}.`);
}
expect(categoriesHtml.includes("[attr.aria-busy]=\"estado() === 'loading'\""), 'La carga de categorías debe exponerse con aria-busy.');
expect(categoriesHtml.includes('Cantidad no disponible') || categoriesTs.includes('Cantidad no disponible'), 'Un conteo null debe mostrarse como desconocido, no como cero.');
expect(categoriesHtml.includes('destinoSaltar="#contenido-categorias"'), 'El header reutilizado debe tener un destino de salto válido en la página de categorías.');
expect(categoriesHtml.includes('[href]="rutaExplorar(categoria)"'), 'Cada categoría debe llevar al catálogo usando su slug canónico.');

expect(storefrontTs.includes('categoriasTienda'), 'El home debe consumir el estado canónico de categorías de Fase 2.');
expect(storefrontTs.includes('cargarCategorias()'), 'El home debe cargar categorías mediante la fuente pública dedicada.');
expect(storefrontTs.includes('this.servicio.obtenerCategorias()'), 'El home no debe inferir categorías reales desde productos.');
expect(!storefrontTs.includes('new Set(this.productos().map(p => p.categoria))'), 'Debe eliminarse la inferencia de categorías basada en texto de producto.');
expect(storefrontHtml.includes('categoriasTienda()'), 'La sección visual y los filtros del home deben consumir CategoriaTienda.');
expect(storefrontHtml.includes('estadoCategorias()'), 'La sección de categorías del home debe representar estados explícitos.');
expect(!storefrontHtml.includes('categoria.producto'), 'La imagen de categoría no debe inventarse tomando un producto representativo.');
expect(!storefrontHtml.includes('categoria.cantidad'), 'El conteo visual debe provenir de CategoriaTienda, no de un cálculo local de productos.');

expect(headerTs.includes('enlaces.categorias: VARISTOREHN_PATHS.categorias'), 'El enlace principal de Categorías debe apuntar a la ruta pública canónica de Fase 2.');
expect(headerHtml.includes('[href]="destinoSaltar"'), 'El skip link del header debe ser reutilizable fuera del home.');
expect(headerTs.includes('totalUnidades: number | null'), 'El resumen de carrito compartido debe poder expresar un valor desconocido sin inventar cero.');

if (failures.length) {
  console.error('Fase 2 — validación de categorías FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 2 — categorías: contrato público, estados, ruta, navegación y separación admin aprobados.');
