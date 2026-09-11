# VariStoreHn — Plan Maestro v1.1 — Fase 3: catálogo

## Estado vigente

**COMPLETADA / REAUDITADA / HARDENED / INTEGRADA EN `Desarrollo`.**

Este documento refleja el estado vigente después de Fase 5. Los mecanismos transitorios que existían durante el cierre cronológico de Fase 3 —en especial el puente `?carrito=1` como destino funcional— fueron reemplazados posteriormente y no forman parte de la arquitectura actual.

## Alcance vigente

- Ruta pública `/varistorehn/productos`, sin `authGuard` ni `permisoGuard`.
- `VaristorehnProductosComponent` standalone y desacoplado del CRUD administrativo.
- Header público reutilizado.
- Fuente real mediante `VaristorehnService.obtenerCatalogo()` y frontera pública `GET /tienda/productos`.
- Lectura de todas las páginas del catálogo público antes del filtrado local.
- Categorías del filtro obtenidas desde `GET /tienda/categorias`.
- Fixtures únicamente en modo demo/preview; un error real permanece error.
- Estados `loading`, `empty`, `error` y `success`.
- Búsqueda por nombre, descripción, categoría, marca, modelo y SKU.
- Deep link `?q=...` y filtro canónico `?categoria=<slug>`.
- Filtros por disponibilidad y precio máximo.
- Orden por recomendados, precio ascendente/descendente y nombre.
- Paginación local de 12 resultados.
- Tarjetas con imagen/fallback, categoría, descripción, disponibilidad, marca/modelo, SKU y precio.
- Selector de variante pública cuando corresponde.
- “Ver producto” navega hoy a `/varistorehn/producto/:slug` de Fase 4.
- Agregar usa `VaristorehnCarritoService` de Fase 5; no existe persistencia paralela en el catálogo.
- Header obtiene contador/subtotal del mismo store global.
- El botón de carrito navega a `/varistorehn/carrito`; no usa el home como intermediario.
- Responsive sin overflow en los viewports cubiertos y tema exclusivamente mediante tokens del sistema.

## Evolución controlada

Al cerrar originalmente Fase 3, el detalle por slug y el store/ruta global del carrito aún pertenecían a Fases 4 y 5. Por eso se documentaron puentes temporales hacia el home. Fases 4 y 5 los sustituyeron por rutas canónicas y estado único.

La reauditoría final de Fases 0–5 protege expresamente que el catálogo no vuelva a introducir `localStorage` propio ni rutas temporales de carrito.

## Definition of Done vigente

- [x] Catálogo público independiente.
- [x] Fuente pública completa y sin dependencia administrativa.
- [x] Estados reales y sin fallback silencioso.
- [x] Búsqueda, filtros, ordenamiento y paginación funcionales.
- [x] Deep links por búsqueda/categoría.
- [x] Tarjetas y selección de variante con datos públicos.
- [x] Navegación por slug al detalle canónico.
- [x] Carrito global único y ruta `/varistorehn/carrito`.
- [x] Persistencia mínima sin precio/stock como autoridad.
- [x] Tema del sistema, touch targets y responsive.
- [x] Guardia estática y 8 escenarios Playwright permanentes.

## Evidencia histórica

Tracking #3342; implementación PR #3343. Commit funcional integrado `2dc89a8887570a6484ccc5862be3443f42eeece4`. Gate post-merge `34554231445` — **success**.

El cierre histórico ejecutó TypeScript/lint, build de producción y Playwright Fases 1–3: 4/4 + 8/8 + 8/8. Las regresiones de Fases 4–5 vuelven a ejecutar esta suite de forma acumulada.

## Fuera de alcance actual

Checkout, datos de cliente, entrega, creación/confirmación de pedido y limpieza posterior del carrito pertenecen a Fase 6 y **no están activados**.
