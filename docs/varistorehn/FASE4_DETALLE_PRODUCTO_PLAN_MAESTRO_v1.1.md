# VariStoreHn — Fase 4: Detalle de producto

Autoridad: **Plan Maestro de Mejoras VariStoreHn v1.1**, Fase 4.

## Estado vigente

**COMPLETADA / REAUDITADA / HARDENED / INTEGRADA EN `Desarrollo`.**

La experiencia principal de producto vive exclusivamente en `/varistorehn/producto/:slug`. El `<dialog>` de esta fase se usa únicamente como lightbox fotográfico. El quick-detail heredado del home fue retirado completamente —markup, estado y CSS— durante el hardening posterior a Fase 5.

## Alcance vigente

### Ruta y fuente de datos

- Ruta pública lazy `/varistorehn/producto/:slug`, sin guards administrativos.
- `VaristorehnService.obtenerProductoPorSlug()` consume `GET /tienda/productos/{slug}`.
- 404/inactivo/error se representan de forma controlada y nunca fabrican demo ante un fallo real.
- Slug histórico se reemplaza por el slug canónico con `replaceUrl`.
- Demo y base de datos permanecen separadas.

### Información comercial y carrito

- Nombre, categoría, SKU, marca/modelo, precio, promoción válida y disponibilidad.
- `precioVenta(producto, modelo)` centraliza el precio efectivo.
- Selector de variante cuando corresponde.
- Cantidad acotada al **stock restante**, descontando las unidades de la misma variante ya existentes en `VaristorehnCarritoService`.
- Input, botón `+` y CTA usan `stockRestante()`; no muestran un máximo superior al realmente agregable.
- Agregar usa el store global de Fase 5; el detalle no mantiene `localStorage` propio.
- Header recibe contador/subtotal del mismo store.
- WhatsApp de detalle es una alternativa/consulta para el producto actual cuando hay número válido. En demo no se realiza un envío externo.
- Descripción y características solo muestran datos públicos existentes.
- Relacionados se derivan del catálogo público y categoría, no de inventario hardcodeado.

### Galería, navegación y responsive

- Imagen principal con proporción preservada (`object-fit: contain`).
- Miniaturas, anterior/siguiente e indicador `N / total`.
- Swipe mediante Pointer Events, `touch-action: pan-y` y cancelación segura.
- Lightbox fotográfico con botón/Escape/backdrop y retorno de foco.
- Breadcrumbs Inicio > Categoría > Producto.
- Catálogo y home navegan al detalle por slug; no existe detalle modal alternativo en el home.
- Botón Atrás conserva el flujo probado.
- CTA móvil fijo respeta safe area y no tapa contenido.
- Responsive cubierto hasta 320 px sin overflow en la suite.
- Tema exclusivamente mediante tokens globales del sistema/BD.

## Definition of Done vigente

- [x] URL pública compartible por slug y recarga directa.
- [x] Estados not-found/error/inactivo controlados.
- [x] Breadcrumbs.
- [x] Galería multiimagen, swipe, indicador y lightbox fotográfico.
- [x] Imágenes sin deformación.
- [x] Datos comerciales públicos completos disponibles en el contrato.
- [x] Selector de variante.
- [x] Cantidad nunca superior al stock restante.
- [x] Agregar al carrito central y contador/subtotal sincronizados.
- [x] Persistencia segura sin precios/stock como autoridad.
- [x] WhatsApp condicionado a configuración válida.
- [x] Relacionados públicos.
- [x] Navegación canónica desde catálogo/home.
- [x] CTA móvil, safe area y responsive.
- [x] Sin dependencias administrativas ni colores hardcodeados.
- [x] Guardia estática y 11 escenarios Playwright permanentes.

## Evidencia histórica y hardening

Tracking inicial #3345; implementación PR #3346. Commit funcional `906c73178ada203509582b9035399b275d0318ce`; gate post-merge `34560172361` — **success**, con Fases 1–4 acumuladas y 11/11 escenarios específicos de detalle.

Durante Fase 5 se corrigió el `max` visual para que use el stock realmente restante. Posteriormente PR #3370, commit `94fefb309c2ae498c25d25ece83f034f80ccb168`, retiró por completo del home el quick-detail legado. La regresión Fase 4 sobre ese commit, run `34601213518`, terminó **success**.

La reauditoría final de Fases 0–5 retiró además selectores responsive muertos `.detail-layout`, `.detail-media` y `.detail-body` que ya no correspondían a ningún DOM del home, y las guardas actuales impiden reintroducirlos allí.

## Deuda de presupuesto histórica

El warning histórico de presupuesto del SCSS monolítico del home quedó eliminado en el build del candidato de reauditoría final de Fases 0–5 después de retirar el drawer de carrito y estilos muertos. No se aumentó el presupuesto para silenciarlo.

## Fuera de alcance actual

Checkout/pedido corresponde a Fase 6 y **no está activado**. La reauditoría final elimina explícitamente cualquier lógica heredada del home que intentara adelantar ese cierre de compra.
