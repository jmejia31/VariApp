import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const rootDir = path.resolve(frontendDir, '..');
const readFrontend = name => readFile(path.join(frontendDir, name), 'utf8');
const readRoot = name => readFile(path.join(rootDir, name), 'utf8');

const [routes, models, catalog, productsTs, productsHtml, detailTs, detailHtml, cartHtml, headerHtml, controller, promoService] = await Promise.all([
  readFrontend('src/app/app.routes.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn.models.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn.catalog.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-productos.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-productos.component.html'),
  readFrontend('src/app/features/varistorehn/varistorehn-producto.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-producto.component.html'),
  readFrontend('src/app/features/varistorehn/varistorehn-carrito.component.html'),
  readFrontend('src/app/features/varistorehn/varistorehn-header.component.html'),
  readRoot('backend/src/API/Controllers/TiendaController.cs'),
  readRoot('backend/src/Application/Services/PromocionPublicaService.cs')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(routes.includes("path: 'varistorehn/ofertas'"), 'Debe existir la ruta pública /varistorehn/ofertas.');
expect(headerHtml.includes('[routerLink]="enlaces.ofertas"'), 'La navegación global debe enlazar Ofertas.');
for (const token of ['precioOferta', 'ofertaActiva', 'ofertaNombre', 'ahorro', 'porcentajeAhorro', 'estadoDisponibilidad']) {
  expect(models.includes(token), `El contrato público debe incluir ${token}.`);
}
expect(models.includes('soloOfertas: boolean'), 'Fase 8 debe poder filtrar ofertas una vez existe autoridad en Fase 9.');
expect(catalog.includes('modelo.precioOferta') && catalog.includes('modelo.ofertaActiva'), 'El precio promocional debe resolverse por variante.');
expect(catalog.includes("return 'Agotado'") && catalog.includes("return 'Últimas unidades'") && catalog.includes("return 'Disponible'"),
  'Los tres estados públicos de inventario deben ser canónicos.');
expect(productsTs.includes('soloOfertasPagina') && productsHtml.includes('PROMOCIONES VIGENTES'), 'La página Ofertas debe reutilizar el catálogo con filtro autoritativo.');
expect(productsHtml.includes('Solo productos en oferta'), 'El catálogo debe exponer el filtro de oferta ahora que aplica.');
expect(productsHtml.includes('Ahorras') && productsHtml.includes('discount-badge'), 'El catálogo debe mostrar ahorro y badge solo en oferta.');
expect(detailHtml.includes('Ahorras') && detailTs.includes('ofertaNombre'), 'El detalle debe mostrar el mismo precio promocional y ahorro.');
expect(cartHtml.includes('item.ofertaActiva') && cartHtml.includes('item.precioNormal'), 'El carrito debe distinguir precio normal y promocional.');
expect(!productsTs.includes('modelo.stock <= 3') && !detailTs.includes('stock <= 3'), 'La UI no debe mantener un umbral de stock hardcodeado.');
expect(promoService.includes('GetVigentesConRelacionesAsync'), 'La promoción pública debe reutilizar la autoridad existente de Descuentos.');
expect(promoService.includes('FechaInicio') && promoService.includes('FechaFin'), 'La vigencia debe validarse en backend.');
expect(promoService.includes('TipoDescuento.Porcentaje'), 'Solo reglas reproducibles por unidad deben convertirse en precio público.');
expect(controller.includes('precioVigente = oferta?.PrecioOferta ?? precio'), 'Checkout debe recalcular el mismo precio promocional.');
expect(controller.includes('stock <= 0 || stock < solicitud.Unidades'), 'Checkout debe bloquear explícitamente stock 0.');

if (failures.length) {
  console.error('Fase 9 — validación de ofertas e inventario FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}
console.info('Fase 9 — autoridad de promociones, /ofertas, ahorro, stock y checkout aprobados.');
