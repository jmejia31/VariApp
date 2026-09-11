# VariStoreHn — Plan Maestro v1.1 — Fase 3

## Estado

**COMPLETADA / REAUDITADA / HARDENED / INTEGRADA EN `Desarrollo`.**

Tracking: #3342. Implementación: PR #3343.

La Fase 3 convierte el catálogo/grid en una página pública independiente y canónica en `/varistorehn/productos`. El cierre se realizó únicamente después de lint/TypeScript, build de producción, regresiones Fase 1–3 en navegador real, control de concurrencia y un gate post-merge sobre el commit exacto de integración.

## Base comprobada antes de iniciar

Antes de abrir esta fase se revalidó Fase 2 (#3332). Su commit certificado `d215f0f574cb05e7dfefe1a6ebe239c0e948cedf` permanecía en la ascendencia de `Desarrollo` y los commits posteriores solo modificaban CI/backend, no VariStoreHn. Por tanto Fase 3 partió de una Fase 2 íntegra y no la reconstruyó.

## Alcance funcional acreditado

- [x] Ruta pública `/varistorehn/productos`, sin `authGuard` ni `permisoGuard`.
- [x] `VaristorehnProductosComponent` standalone y desacoplado del CRUD administrativo.
- [x] Reutilización del `VaristorehnHeaderComponent` de Fase 1.
- [x] Fuente real exclusiva a través de `VaristorehnService.obtenerCatalogo()`, que lee todas las páginas de `GET /tienda/productos` antes del filtrado local.
- [x] `ProductoCatalogoPublico` se transforma a `ProductoTienda` mediante `mapearProducto`.
- [x] Categorías del filtro provienen de la frontera pública `GET /tienda/categorias` y se transforman a `CategoriaTienda`.
- [x] Fixtures demo únicamente en preview/no producción; un error real permanece error y no se sustituye por demo.
- [x] Estados explícitos `loading`, `empty`, `error` y `success`.
- [x] Búsqueda inmediata por nombre, descripción, categoría, marca, modelo y SKU usando `filtrarProductos`.
- [x] Deep link por `?q=...`.
- [x] Filtro canónico de categoría mediante `?categoria=<slug>`; el slug se resuelve contra `CategoriaTienda` y el filtrado usa su nombre canónico.
- [x] Filtro “Solo disponibles”.
- [x] Precio máximo.
- [x] Ordenamiento: Recomendados, menor precio, mayor precio y nombre A–Z.
- [x] Paginación local de 12 resultados sobre el catálogo público completo.
- [x] Tarjetas con imagen real/fallback local, categoría, descripción, stock, disponibilidad, marca/modelo, SKU y precio.
- [x] Selector de modelo cuando existe más de una variante pública.
- [x] Agregar al carrito conserva `ReferenciaCarrito` (`productoId`, `modeloClave`, `unidades`) y restaura contra precio/stock actuales; no persiste precio ni datos de pago.
- [x] El resumen del carrito del header se actualiza con unidades y subtotal reales.
- [x] El enlace principal “Productos” del header usa `VARISTOREHN_PATHS.productos`.
- [x] Búsquedas desde páginas de categorías continúan hacia `/varistorehn/productos`.
- [x] “Ver productos de esta categoría” continúa hacia `/varistorehn/productos?categoria=<slug>`.
- [x] Responsive con sidebar/filtros adaptativos, ausencia de overflow en los viewports auditados y objetivos táctiles principales de al menos 44 px.
- [x] Colores exclusivamente mediante los tokens existentes del tema (`--color-*`); no se añade paleta propia.

## Compatibilidad transitoria

El home `/varistorehn` conserva por ahora su grid histórico para no adelantar una refactorización destructiva ni romper flujos previos. La navegación pública canónica de “Productos”, las búsquedas desde categorías y los enlaces por categoría pasan a la nueva página independiente. La retirada o simplificación del grid embebido del home deberá hacerse solo cuando el roadmap lo asigne y con regresión propia.

El botón de carrito de la página independiente continúa abriendo el carrito existente del home mediante `/varistorehn?carrito=1`. La extracción de un store/ruta global de carrito corresponde a Fase 5.

## Protección permanente

- `frontend/scripts/validate-varistorehn-fase3.mjs` integrado a `npm run lint`.
- `frontend/e2e/varistorehn-fase3.spec.ts` con 8 escenarios de navegador real.
- `.github/workflows/varistorehn-fase3-regression.yml` ejecuta:
  - `npm ci`;
  - TypeScript/lint y guardas Fase 1–3;
  - build de producción;
  - Playwright Fase 1;
  - Playwright Fase 2;
  - Playwright Fase 3;
  - artefactos de evidencia.

## Matriz de aceptación certificada

| Riesgo | Evidencia |
| --- | --- |
| Ruta todavía apunta al home | `/varistorehn/productos` renderiza `VaristorehnProductosComponent` |
| Dependencia del CRUD | Guardia impide `ProductosListComponent`, `authGuard` y `permisoGuard` en la página pública |
| Catálogo real cortado a una página HTTP | Playwright publica un producto en la página HTTP 2 y la búsqueda lo encuentra después de leer ambas páginas |
| Error real ocultado por demo | 503 renderiza error y cero fixtures demo |
| Catálogo real vacío falseado | respuesta vacía renderiza `empty` y cero tarjetas |
| Slug de categoría perdido | deep link `?categoria=<slug>` resuelve nombre y filtra |
| Filtros no funcionales | Playwright cubre texto, categoría, disponibilidad, precio y orden |
| Carrito duplicado/inseguro | persistencia contiene solo referencias y se restaura en el home |
| Ruptura de Fase 1/2 | workflow post-merge ejecutó Fase 1, Fase 2 y Fase 3 sobre el mismo SHA |
| Regresión responsive | navegador validó ausencia de overflow y objetivos >=44 px |
| Paleta paralela | guardia rechaza hex/RGB/HSL y aliases de tema inexistentes |

## Evidencia de integración

### PR #3343

Head pre-merge: `519f79c9b5796ccb655f07cc08c9852beaaf017f`.

- workflow integral Fase 3 PR `34553728771`: **success**;
- TypeScript/lint + guardas Fase 1–3: **success**;
- build producción: **success**;
- Playwright Fase 1: **4/4**;
- Playwright Fase 2: **8/8**;
- Playwright Fase 3: **8/8**;
- artefacto pre-merge: `10181783607`.

El workflow independiente de Fase 2 tuvo un primer intento fallido únicamente porque Angular no pudo resolver `fonts.googleapis.com` durante el inlining. El mismo job se reintentó sin cambios de código y terminó **success**, por lo que se registró como fallo transitorio de red y no como regresión funcional.

### Merge

PR #3343 fusionado mediante squash.

Commit funcional integrado en `Desarrollo`: `2dc89a8887570a6484ccc5862be3443f42eeece4`.

### Gate post-merge exacto

Workflow `VariStoreHn Fase 3 - regresion catalogo`, run `34554231445`: **success** sobre `2dc89a8887570a6484ccc5862be3443f42eeece4`.

- checkout del SHA exacto: **success**;
- `npm ci`: **success**;
- TypeScript/lint + guardas Fase 1–3: **success**;
- build producción: **success**;
- Playwright Fase 1: **4/4**;
- Playwright Fase 2: **8/8**;
- Playwright Fase 3: **8/8**;
- artefacto post-merge: `10181978740`.

Al finalizar el gate funcional, `Desarrollo` seguía exactamente en `2dc89a8887570a6484ccc5862be3443f42eeece4`, por lo que ningún push concurrente había pisado la implementación certificada. Este documento se actualiza después como cambio exclusivamente documental y no altera el runtime certificado.

## Observaciones no bloqueantes

El repositorio mantiene deuda transversal preexistente que no pertenece a esta fase: vulnerabilidades reportadas por `npm ci`, warnings Angular en módulos ajenos y el warning de presupuesto de `varistorehn.component.scss` (17.10 kB frente a 16 kB). El nuevo componente independiente de Fase 3 no amplió ese componente monolítico ni silenció artificialmente esos warnings.

## Fuera de alcance respetado

- `/varistorehn/producto/:slug` y navegación de detalle: **Fase 4; no activada**.
- Store/ruta global de carrito: **Fase 5; no implementada**.
- Checkout y creación de pedido real: **Fase 6; no implementados**.
- Promociones/destacados comerciales no respaldados por una fuente de verdad: no se inventaron.

## Cierre

**Fase 3 queda oficialmente completada, reauditoria y hardened después de la certificación pre-merge y post-merge completa.**
