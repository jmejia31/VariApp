import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const repoDir = path.resolve(frontendDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  routes,
  paths,
  config,
  cartHtml,
  checkoutTs,
  checkoutHtml,
  checkoutScss,
  checkoutRules,
  pedidoTs,
  pedidoHtml,
  pedidoScss,
  pedidoService,
  storefrontService,
  models,
  backendController,
  backendDto
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.paths.ts'),
  readFeature('varistorehn.config.ts'),
  readFeature('varistorehn-carrito.component.html'),
  readFeature('varistorehn-checkout.component.ts'),
  readFeature('varistorehn-checkout.component.html'),
  readFeature('varistorehn-checkout.component.scss'),
  readFeature('varistorehn-checkout.rules.ts'),
  readFeature('varistorehn-pedido.component.ts'),
  readFeature('varistorehn-pedido.component.html'),
  readFeature('varistorehn-pedido.component.scss'),
  readFeature('varistorehn-pedido.service.ts'),
  readFeature('varistorehn.service.ts'),
  readFeature('varistorehn.models.ts'),
  readFile(path.join(repoDir, 'backend/src/API/Controllers/TiendaController.cs'), 'utf8'),
  readFile(path.join(repoDir, 'backend/src/Application/DTOs/TiendaCheckoutDto.cs'), 'utf8')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };
const routeLine = pathValue => routes.split('\n').find(line => line.includes(`path: '${pathValue}'`)) || '';

const checkoutRoute = routeLine('varistorehn/checkout');
const pedidoRoute = routeLine('varistorehn/pedido/:id');
expect(checkoutRoute.includes('VaristorehnCheckoutComponent'), 'Debe existir la página pública /varistorehn/checkout.');
expect(pedidoRoute.includes('VaristorehnPedidoComponent'), 'Debe existir la página pública /varistorehn/pedido/:id.');
expect(!checkoutRoute.includes('authGuard') && !checkoutRoute.includes('permisoGuard'), 'Checkout no debe exigir autenticación administrativa.');
expect(!pedidoRoute.includes('authGuard') && !pedidoRoute.includes('permisoGuard'), 'Confirmación pública no debe exigir autenticación administrativa.');
expect(paths.includes("checkout: '/varistorehn/checkout'"), 'Las rutas canónicas deben declarar checkout.');
expect(paths.includes("pedido: (id: string | number)"), 'Las rutas canónicas deben construir la referencia de pedido.');
expect(cartHtml.includes('[href]="enlaces.checkout"') && cartHtml.includes('Continuar al checkout'), 'El carrito no vacío debe conectar con checkout.');

expect(config.includes("export type ModoCarrito = 'whatsapp' | 'tarjeta' | 'ambos'"), 'La configuración debe conservar los tres modos comerciales.');
expect(config.includes('endpointCheckoutTarjeta: null'), 'Tarjeta debe permanecer fail-closed por defecto.');
expect(config.includes('origenesCheckoutPermitidos: []'), 'La allowlist de pago debe estar vacía por defecto.');

for (const required of [
  'this.servicio.validarCheckout(this.referenciasCheckout())',
  'this.carrito.hidratar(productos',
  'Validators.required',
  'Validators.email',
  'Validators.maxLength(600)',
  "this.config.modoCarrito !== 'tarjeta'",
  "this.config.modoCarrito !== 'whatsapp'",
  'urlCheckoutPermitida(respuesta.checkoutUrl, this.config.origenesCheckoutPermitidos)',
  "this.guardarRecibo(validado, this.utilizarDatosBaseDatos() ? 'whatsapp-preparado' : 'demo')",
  'this.document.defaultView?.location.assign(segura)'
]) {
  expect(checkoutTs.includes(required), `Checkout debe contener la salvaguarda: ${required}.`);
}
expect(!checkoutTs.includes('localStorage'), 'Checkout no debe guardar datos del comprador en localStorage.');
expect(!checkoutTs.includes('numeroTarjeta') && !checkoutTs.includes('cvv') && !checkoutTs.includes('pinTarjeta'), 'Checkout no debe capturar credenciales de tarjeta.');
expect(checkoutTs.includes("if (!endpoint || !this.tarjetaConfigurada())"), 'Tarjeta debe bloquearse si falta endpoint/origen seguro.');

for (const required of [
  'Confirma tus datos y tu forma de compra',
  'Nombre completo',
  'Teléfono',
  'Correo electrónico',
  'Notas para el comercio',
  'Total validado',
  'Preparar pedido por WhatsApp',
  'Tarjeta no disponible',
  'Nunca ingreses número de tarjeta, CVV o PIN'
]) {
  expect(checkoutHtml.includes(required), `La UI de checkout debe incluir: ${required}.`);
}
expect(!checkoutHtml.includes('Fase 6'), 'La UI pública no debe exponer lenguaje interno del roadmap.');
expect(!/formControlName\s*=\s*["'](?:tarjeta|numeroTarjeta|cardNumber|cvv|cvc|pin)["']/i.test(checkoutHtml), 'No debe existir ningún campo de captura de tarjeta.');
expect(!/type\s*=\s*["']password["']/i.test(checkoutHtml), 'Checkout no debe capturar secretos de pago.');

expect(checkoutRules.includes("destino.protocol === 'https:'"), 'La redirección de tarjeta debe exigir HTTPS.');
expect(checkoutRules.includes('permitidos.has(destino.origin)'), 'La redirección de tarjeta debe exigir origen allowlisted.');
expect(checkoutRules.includes('encodeURIComponent') === false, 'Las reglas puras deben devolver contenido, no abrir URLs ni manipular el navegador.');

expect(storefrontService.includes("private readonly urlValidarCheckout = `${this.urlTienda}/checkout/validar`"), 'El cliente HTTP debe apuntar a /tienda/checkout/validar.');
expect(storefrontService.includes('this.http.post<ApiResponse<CheckoutValidado>>'), 'La revalidación debe usar POST tipado.');
expect(storefrontService.includes("if (!ruta || /^https?:/i.test(ruta) || ruta.includes('..'))"), 'El endpoint configurable de tarjeta debe aceptar solo rutas backend relativas seguras.');
expect(storefrontService.includes('checkoutValidado(data)'), 'El cliente debe validar estructuralmente la respuesta de checkout.');

const checkoutItemBlock = models.match(/export interface CheckoutItemRequest \{([\s\S]*?)\n\}/)?.[1] || '';
expect(checkoutItemBlock.includes('productoId') && checkoutItemBlock.includes('modeloId') && checkoutItemBlock.includes('unidades'), 'El request frontend debe contener solo referencias de producto/modelo/cantidad.');
expect(!/precio|total|stock/i.test(checkoutItemBlock), 'El request frontend no debe enviar precio, total ni stock como autoridad.');

const backendItemBlock = backendDto.match(/public sealed class CheckoutTiendaItemRequestDto\s*\{([\s\S]*?)\n\}/)?.[1] || '';
expect(backendItemBlock.includes('ProductoId') && backendItemBlock.includes('ModeloId') && backendItemBlock.includes('Unidades'), 'El DTO backend debe aceptar únicamente referencias mínimas.');
expect(!/Precio|Total|Stock/i.test(backendItemBlock), 'El DTO backend no debe aceptar precio, total ni stock del cliente.');
for (const required of [
  '[AllowAnonymous]',
  '[HttpPost("checkout/validar")]',
  '_productoService.GetByIdAsync(solicitud.ProductoId)',
  'producto.Variantes.Where(variante => variante.Activo)',
  'if (stock < solicitud.Unidades)',
  'Total = precio * solicitud.Unidades',
  'var subtotal = lineas.Sum(linea => linea.Total)',
  'Guid.NewGuid().ToString("N")',
  'TimeSpan.FromMinutes(10)'
]) {
  expect(backendController.includes(required), `Backend debe conservar la validación autoritativa: ${required}.`);
}
expect(!backendController.includes('IPedidoVentaService') && !backendController.includes('ICotizacionService'), 'La validación pública no debe saltarse permisos reutilizando servicios administrativos de pedido/cotización.');

expect(pedidoService.includes('sessionStorage.setItem'), 'El recibo UX debe persistirse solo en sessionStorage.');
expect(!pedidoService.includes('localStorage'), 'El recibo UX no debe usar localStorage.');
expect(!/nombre|telefono|correo|notas/i.test(pedidoService), 'El recibo persistido no debe incorporar PII del comprador.');
expect(pedidoService.includes('MAX_VIGENCIA_MS'), 'El recibo debe tener caducidad limitada.');
expect(pedidoHtml.includes('no contiene ni almacena tus datos de contacto ni información de tarjetas'), 'La confirmación debe explicar el límite de datos persistidos.');
expect(pedidoHtml.includes('No encontramos una confirmación vigente en esta sesión'), 'La ruta de pedido debe manejar referencias ausentes/expiradas sin inventar estado.');
expect(!pedidoTs.includes('localStorage') && !pedidoTs.includes('sessionStorage'), 'El componente de pedido debe delegar persistencia al servicio dedicado.');

for (const [name, scss] of [['checkout', checkoutScss], ['pedido', pedidoScss]]) {
  expect(scss.includes('var(--color-bg)') && scss.includes('var(--color-surface)') && scss.includes('var(--color-primary)'), `${name} debe consumir tokens de tema de empresa.`);
  expect(!/#[0-9a-f]{3,8}\b/i.test(scss), `${name} no debe introducir hex propios.`);
  expect(!/\brgb(?:a)?\s*\(/i.test(scss), `${name} no debe introducir RGB propios.`);
  expect(!/\bhsl(?:a)?\s*\(/i.test(scss), `${name} no debe introducir HSL propios.`);
  expect(/min-height\s*:\s*44px/.test(scss), `${name} debe conservar touch targets de al menos 44 px.`);
  expect(scss.includes('@media'), `${name} debe tener tratamiento responsive explícito.`);
}

if (failures.length) {
  console.error('Fase 6 — validación de checkout/pedido FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 6 — checkout/pedido: rutas públicas, revalidación server-side, WhatsApp, tarjeta fail-closed, recibo efímero, tema y privacidad aprobados.');
