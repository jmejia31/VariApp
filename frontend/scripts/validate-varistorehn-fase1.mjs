import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');

const read = (name) => readFile(path.join(featureDir, name), 'utf8');
const [
  headerTs,
  headerHtml,
  headerScss,
  storefrontTs,
  storefrontHtml,
  storefrontScss,
  storefrontResponsiveScss
] = await Promise.all([
  read('varistorehn-header.component.ts'),
  read('varistorehn-header.component.html'),
  read('varistorehn-header.component.scss'),
  read('varistorehn.component.ts'),
  read('varistorehn.component.html'),
  read('varistorehn.component.scss'),
  read('varistorehn.responsive.scss')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(headerTs.includes('VARISTOREHN_PATHS'), 'El header debe construir navegación desde VARISTOREHN_PATHS.');
expect(headerTs.includes('EmpresaIdentidadService'), 'El header debe consumir la identidad empresarial compartida.');
expect(headerTs.includes('showModal()'), 'El menú móvil debe usar un diálogo modal nativo para contener el foco.');
expect(headerTs.includes('telefonoWhatsapp'), 'WhatsApp debe normalizarse con la regla compartida del escaparate.');
expect(headerHtml.includes('role="search"'), 'El buscador debe conservar semántica role=search.');
expect(headerHtml.includes('aria-controls="varistorehn-menu-movil"'), 'El disparador móvil debe declarar aria-controls.');
expect(headerHtml.includes('[attr.aria-expanded]="menuAbierto()"'), 'El disparador móvil debe exponer aria-expanded real.');
expect(headerHtml.includes('[totalUnidades]') === false, 'El header no debe intentar enlazar inputs a sí mismo.');
expect(headerHtml.includes('{{ totalUnidades }}'), 'El contador visual del carrito debe usar el total real recibido.');
expect(headerHtml.includes('enlaceWhatsapp()'), 'Debe existir una acción secundaria de WhatsApp cuando esté configurada.');
expect(headerScss.includes('min-height: 44px'), 'Los controles móviles deben conservar objetivos táctiles de al menos 44px.');
expect(!/#[0-9a-f]{3,8}\b/i.test(headerScss), 'El header no debe introducir colores hexadecimales fuera del tema.');
expect(storefrontTs.includes('VaristorehnHeaderComponent'), 'El escaparate debe importar el header público reutilizable.');
expect(storefrontHtml.includes('<app-varistorehn-header'), 'El escaparate debe delegar su cabecera al componente público.');
expect(!storefrontHtml.includes('<header class="store-header">'), 'La cabecera monolítica anterior debe dejar de vivir en el escaparate.');
expect(storefrontHtml.includes('(busquedaActualizada)="buscar($event)"'), 'La búsqueda del header debe seguir filtrando el catálogo actual.');
expect(storefrontHtml.includes('(carritoSolicitado)="abrirCarrito()"'), 'El carrito del header debe abrir el estado real existente.');

const staleHeaderSelectors = [
  '.skip-link',
  '.utility-bar',
  '.utility-inner',
  '.store-header',
  '.header-main',
  '.brand-mark',
  '.search-box',
  '.cart-trigger',
  '.cart-icon',
  '.store-nav'
];

for (const [name, content] of [
  ['varistorehn.component.scss', storefrontScss],
  ['varistorehn.responsive.scss', storefrontResponsiveScss]
]) {
  for (const selector of staleHeaderSelectors) {
    expect(
      !content.includes(selector),
      `${name} no debe conservar estilos residuales del header extraído (${selector}).`
    );
  }
}

for (const [name, content] of [['header.ts', headerTs], ['header.html', headerHtml]]) {
  expect(!content.includes('authGuard'), `${name} no debe depender de authGuard.`);
  expect(!content.includes('permisoGuard'), `${name} no debe depender de permisoGuard.`);
  expect(!content.includes('ProductosListComponent'), `${name} no debe reutilizar componentes administrativos de productos.`);
  expect(!content.includes('CategoriasListComponent'), `${name} no debe reutilizar componentes administrativos de categorías.`);
}

if (failures.length) {
  console.error('Fase 1 — validación de navegación/header FALLÓ:');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 1 — navegación/header: guardas estáticas aprobadas, incluida la extracción completa de estilos.');
