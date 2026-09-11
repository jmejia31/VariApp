# VariStoreHn — Plan Maestro v1.1 — Fase 3

## Estado

**EN IMPLEMENTACIÓN / PENDIENTE DE CERTIFICACIÓN.**

Tracking: #3342.

La Fase 3 convierte el catálogo/grid en una página pública independiente y canónica en `/varistorehn/productos`. La fase se considera terminada únicamente después de lint/TypeScript, build de producción, regresiones Fase 1–3 en navegador real, comparación contra `Desarrollo` y un gate post-merge sobre el commit exacto de integración.

## Base comprobada antes de iniciar

Antes de abrir esta fase se revalidó Fase 2 (#3332). Su commit certificado `d215f0f574cb05e7dfefe1a6ebe239c0e948cedf` permanecía en la ascendencia de `Desarrollo` y los commits posteriores solo modificaban CI/backend, no VariStoreHn. Por tanto Fase 3 parte de una Fase 2 íntegra y no la reconstruye.

## Alcance funcional

- Ruta pública `/varistorehn/productos`, sin `authGuard` ni `permisoGuard`.
- `VaristorehnProductosComponent` standalone y desacoplado del CRUD administrativo.
- Reutilización del `VaristorehnHeaderComponent` de Fase 1.
- Fuente real exclusiva a través de `VaristorehnService.obtenerCatalogo()`, que lee todas las páginas de `GET /tienda/productos` antes del filtrado local.
- `ProductoCatalogoPublico` se transforma a `ProductoTienda` mediante `mapearProducto`.
- Categorías del filtro provienen de la frontera pública `GET /tienda/categorias` y se transforman a `CategoriaTienda`.
- Fixtures demo únicamente en preview/no producción; un error real permanece error y no se sustituye por demo.
- Estados explícitos `loading`, `empty`, `error` y `success`.
- Búsqueda inmediata por nombre, descripción, categoría, marca, modelo y SKU usando `filtrarProductos`.
- Deep link por `?q=...`.
- Filtro canónico de categoría mediante `?categoria=<slug>`; el slug se resuelve contra `CategoriaTienda` y el filtrado usa su nombre canónico.
- Filtro “Solo disponibles”.
- Precio máximo.
- Ordenamiento: Recomendados, menor precio, mayor precio y nombre A–Z.
- Paginación local de 12 resultados sobre el catálogo público completo.
- Tarjetas con imagen real/fallback local, categoría, descripción, stock, disponibilidad, marca/modelo, SKU y precio.
- Selector de modelo cuando existe más de una variante pública.
- Agregar al carrito conserva `ReferenciaCarrito` (`productoId`, `modeloClave`, `unidades`) y restaura contra precio/stock actuales; no persiste precio ni datos de pago.
- El resumen del carrito del header se actualiza con unidades y subtotal reales.
- El enlace principal “Productos” del header usa `VARISTOREHN_PATHS.productos`.
- Búsquedas desde páginas de categorías continúan hacia `/varistorehn/productos`.
- “Ver productos de esta categoría” continúa hacia `/varistorehn/productos?categoria=<slug>`.
- Responsive con sidebar/filtros adaptativos y objetivos táctiles principales de al menos 44 px.
- Colores exclusivamente mediante los tokens existentes del tema (`--color-*`); no se añade paleta propia.

## Compatibilidad transitoria

El home `/varistorehn` conserva por ahora su grid histórico para no adelantar una refactorización destructiva ni romper flujos previos. La navegación pública canónica de “Productos”, las búsquedas desde categorías y los enlaces por categoría pasan a la nueva página independiente. La retirada o simplificación del grid embebido del home deberá hacerse solo cuando el roadmap lo asigne y con regresión propia.

El botón de carrito de la página independiente continúa abriendo el carrito existente del home mediante `/varistorehn?carrito=1`. La extracción de un store/ruta global de carrito corresponde a Fase 5.

## Protección permanente prevista

- `frontend/scripts/validate-varistorehn-fase3.mjs` integrado a `npm run lint`.
- `frontend/e2e/varistorehn-fase3.spec.ts` con navegador real.
- `.github/workflows/varistorehn-fase3-regression.yml` ejecutando:
  - `npm ci`;
  - TypeScript/lint y guardas Fase 1–3;
  - build de producción;
  - Playwright Fase 1;
  - Playwright Fase 2;
  - Playwright Fase 3;
  - artefactos de evidencia.

## Matriz de aceptación

| Riesgo | Evidencia requerida |
| --- | --- |
| Ruta todavía apunta al home | `/varistorehn/productos` renderiza `VaristorehnProductosComponent` |
| Dependencia del CRUD | Guardia impide `ProductosListComponent`, `authGuard` y `permisoGuard` |
| Catálogo real cortado a una página HTTP | Playwright publica un producto en la página HTTP 2 y la búsqueda debe encontrarlo |
| Error real ocultado por demo | 503 debe renderizar error sin ningún fixture demo |
| Catálogo real vacío falseado | respuesta vacía debe renderizar `empty` y cero tarjetas |
| Slug de categoría perdido | deep link `?categoria=<slug>` debe resolver nombre y filtrar |
| Filtros no funcionales | Playwright cubre texto, categoría, disponibilidad, precio y orden |
| Carrito duplicado/inseguro | persistencia contiene solo referencias y restaura en el home |
| Ruptura de Fase 1/2 | workflows ejecutan las tres suites en la misma revisión |
| Regresión responsive | navegador valida ausencia de overflow y objetivos >=44 px |
| Paleta paralela | guardia rechaza hex/RGB/HSL y aliases de tema inexistentes |

## Fuera de alcance

- `/varistorehn/producto/:slug` y navegación de detalle: **Fase 4**.
- Store/ruta global de carrito: **Fase 5**.
- Checkout y creación de pedido real: **Fase 6**.
- Promociones/destacados comerciales no respaldados por una fuente de verdad: fases posteriores según roadmap.

## Cierre

Este documento se actualizará a **COMPLETADA / REAUDITADA / HARDENED** únicamente después de la certificación pre-merge y post-merge. Hasta entonces #3342 debe permanecer abierto.
