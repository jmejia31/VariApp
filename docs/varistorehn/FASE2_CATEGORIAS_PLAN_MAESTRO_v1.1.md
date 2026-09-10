# VariStoreHn — Fase 2: categorías públicas

## Estado

**Implementada y validada en rama de integración. Cierre final sujeto a la recertificación post-merge en `Desarrollo`.**

Esta fase continúa el Plan Maestro v1.1 después de la recertificación de Fase 1. Su objetivo es dejar de inferir la navegación de categorías únicamente desde el texto de los productos y activar una página pública propia que consuma el contrato canónico de categorías.

## Alcance de Fase 2

### Ruta pública independiente

Se activa `/varistorehn/categorias` mediante `VaristorehnCategoriasComponent`, sin `authGuard`, `permisoGuard` ni componentes del CRUD administrativo.

La ruta reutiliza `VaristorehnHeaderComponent`; por tanto, identidad empresarial, tema, navegación móvil, búsqueda, WhatsApp y semántica accesible siguen la frontera pública establecida en Fase 1.

### Contrato canónico

La fuente real es `VaristorehnService.obtenerCategorias()`, que consume `GET /tienda/categorias`.

`mapearCategoriaTienda` convierte `CategoriaCatalogoPublico` a `CategoriaTienda`. La UI no genera slugs a partir del nombre y no calcula categorías reales recorriendo el texto de los productos.

El conteo conserva su semántica original:

- `cantidadProductos = 0`: la fuente confirmó que no hay productos;
- `cantidadProductos = null`: la fuente no entregó un conteo y la UI muestra que la cantidad no está disponible;
- nunca se transforma `null` en cero para aparentar precisión.

El backend actual entrega `TotalProductos = null` en el contrato público de categorías; la UI respeta ese significado y no fabrica un conteo local para sustituirlo.

### Home y filtros existentes

La sección de categorías del home y el listado de categorías del filtro pasan a consumir `CategoriaTienda` desde la frontera pública dedicada.

No se usa ya `new Set(productos.map(p => p.categoria))` como fuente de verdad para categorías. Tampoco se toma arbitrariamente la imagen de un producto para presentarla como imagen de la categoría. Mientras el contrato público no suministre una imagen de categoría, se utiliza una ilustración neutral gobernada por los tokens del tema.

### Estados de consulta

Tanto la página independiente como la sección del home distinguen explícitamente:

- `loading`;
- `empty`;
- `error`;
- `success`.

Un error de la API real no se sustituye silenciosamente con fixtures. Los datos de ejemplo existen únicamente en el modo de vista previa no productivo.

### Continuidad con búsqueda y carrito

El header de la página de categorías conserva la búsqueda: enviar una consulta vuelve al catálogo actual mediante `/varistorehn?q=...#catalogo`.

Seleccionar una categoría usa el `slug` recibido por el contrato público y vuelve al catálogo existente mediante `/varistorehn?categoria=<slug>#catalogo`. El home resuelve ese slug contra `CategoriaTienda` antes de aplicar el filtro por nombre, de modo que no fabrica slugs locales.

La extracción de un estado global del carrito sigue reservada a Fase 5. Hasta entonces, la página de categorías reconstruye únicamente el resumen visible del carrito a partir de las referencias persistidas y el catálogo actual. Si no puede validar ese resumen, muestra un estado desconocido en vez de inventar cero. Abrir el carrito vuelve al home, donde sigue viviendo el carrito real actual.

### Límite de fase

Fase 2 no activa:

- `/varistorehn/productos` como catálogo independiente: Fase 3;
- detalle de producto por slug: Fase 4;
- store global/persistente del carrito: Fase 5;
- checkout/pedido real: Fase 6.

Tampoco crea una segunda fuente de identidad, tema o configuración empresarial.

## Protección contra regresiones

La fase incorpora:

- `frontend/scripts/validate-varistorehn-fase2.mjs`, integrado al `npm run lint` normal;
- `frontend/e2e/varistorehn-fase2.spec.ts`;
- actualización de la regresión de Fase 1 para reconocer la nueva URL canónica de Categorías;
- `.github/workflows/varistorehn-fase2-regression.yml`, que ejecuta lint, build y pruebas de navegador de Fase 1 y Fase 2.

La suite de navegador verifica, entre otros puntos:

- render independiente y responsive;
- consumo de datos reales simulados desde `/tienda/categorias`;
- preservación de `null` como conteo desconocido;
- estados empty y error sin fallback silencioso;
- navegación mediante slug canónico;
- continuidad de búsqueda;
- continuidad del carrito existente entre páginas;
- ausencia de overflow en anchos representativos de desktop, tablet y móvil.

## Definition of Done

- [x] `/varistorehn/categorias` pública y sin guards administrativos.
- [x] Página standalone reutilizando el header público.
- [x] `CategoriaCatalogoPublico` mapeado explícitamente a `CategoriaTienda`.
- [x] Home y filtros dejan de inferir categorías desde el texto de productos.
- [x] Conteo `null` permanece desconocido.
- [x] Estados loading/empty/error/success visibles y accesibles.
- [x] Sin fallback de API real a fixtures.
- [x] Navegación al catálogo basada en slug canónico recibido.
- [x] Búsqueda del header conserva continuidad hacia el catálogo.
- [x] Resumen de carrito no inventa valores cuando no puede validarse.
- [x] Sin dependencias administrativas en la experiencia pública.
- [x] Tokens visuales del sistema y objetivos táctiles adecuados.
- [x] Regresión de Fase 1 sigue verde.
- [x] Lint/TypeScript, build y Playwright Fase 2 verdes.
- [ ] Validación post-merge sobre `Desarrollo`.

## Evidencia en rama

Candidato funcional validado: `9cf463a861aa788b62cf0f01f76bc963d2e55797`.

GitHub Actions run `34530528076`: **success**.

Resultado:

- `npm ci`: success;
- TypeScript/lint y guardas estáticas de Fases 1 y 2: success;
- build de producción: success;
- regresión Playwright Fase 1: **4/4**;
- Playwright Fase 2: **5/5**;
- ruta independiente y responsive: success;
- contrato real simulado y `null` preservado como desconocido: success;
- empty/error sin datos demo silenciosos: success;
- búsqueda, slug canónico y continuidad del carrito: success.

La evidencia definitiva de cierre se registrará después de integrar el PR y volver a ejecutar la regresión sobre el commit exacto resultante en `Desarrollo`.

## Observaciones no bloqueantes

El build conserva el warning previo de presupuesto CSS de `varistorehn.component.scss`: 17.10 kB frente al umbral de warning de 16.00 kB. Fase 2 no aumenta ese archivo y su extracción posterior sigue perteneciendo al roadmap.

`npm ci` también continúa reportando vulnerabilidades de dependencias preexistentes; se mantienen como deuda transversal separada y no se ocultan como parte del cierre de esta fase.
