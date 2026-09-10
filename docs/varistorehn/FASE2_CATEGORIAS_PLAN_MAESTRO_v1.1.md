# VariStoreHn — Fase 2: categorías públicas

## Estado

**En implementación.**

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

- [ ] `/varistorehn/categorias` pública y sin guards administrativos.
- [ ] Página standalone reutilizando el header público.
- [ ] `CategoriaCatalogoPublico` mapeado explícitamente a `CategoriaTienda`.
- [ ] Home y filtros dejan de inferir categorías desde el texto de productos.
- [ ] Conteo `null` permanece desconocido.
- [ ] Estados loading/empty/error/success visibles y accesibles.
- [ ] Sin fallback de API real a fixtures.
- [ ] Navegación al catálogo basada en slug canónico recibido.
- [ ] Búsqueda del header conserva continuidad hacia el catálogo.
- [ ] Resumen de carrito no inventa valores cuando no puede validarse.
- [ ] Sin dependencias administrativas en la experiencia pública.
- [ ] Tokens visuales del sistema y objetivos táctiles adecuados.
- [ ] Regresión de Fase 1 sigue verde.
- [ ] Lint/TypeScript, build y Playwright Fase 2 verdes.
- [ ] Validación post-merge sobre `Desarrollo`.

## Evidencia

Se completará únicamente después de ejecutar CI y validar el commit integrado en `Desarrollo`.
