# VariStoreHn — Fase 4: Detalle de producto

Autoridad: **Plan Maestro de Mejoras VariStoreHn v1.1**, Fase 4.

Estado: **COMPLETADA / REAUDITADA / HARDENED / INTEGRADA EN `Desarrollo`**.

Tracking: #3345. Implementación: PR #3346.

## Revisión previa de Fase 3

Antes de certificar esta fase se revalidó Fase 3 (#3342). Su commit funcional certificado `2dc89a8887570a6484ccc5862be3443f42eeece4` seguía en la ascendencia de `Desarrollo`. La comparación hasta el HEAD previo a Fase 4 mostró únicamente documentación y trabajo ajeno de Sucursales/VAEP; ningún archivo runtime, ruta, prueba o guarda de VariStoreHN de Fase 3 había sido sobrescrito. Por tanto Fase 4 parte de una Fase 3 íntegra.

## Objetivo acreditado

Dar al comprador suficiente información y confianza para decidir la compra desde una página pública independiente y compartible. La experiencia principal de detalle vive en `/varistorehn/producto/:slug`; un diálogo se usa únicamente como lightbox para ampliar fotografías.

## Implementación certificada

### Ruta y fuente de datos

- Ruta pública lazy `/varistorehn/producto/:slug`, sin `authGuard` ni `permisoGuard`.
- `VaristorehnService.obtenerProductoPorSlug()` consume `GET /tienda/productos/{slug}`.
- Producto inexistente o inactivo se representa como estado controlado, sin fabricar un fixture demo.
- Si el backend resuelve un slug histórico a un slug canónico actual, el frontend reemplaza la URL sin duplicar historial.
- Demo y base de datos permanecen separadas. Un error real nunca se sustituye silenciosamente con fixtures.

### Información comercial y compra

- Nombre, categoría, SKU, marca/modelo, precio, posible precio promocional y disponibilidad.
- `precioVenta(producto, modelo)` centraliza el precio efectivo para que detalle y carrito no diverjan.
- Selector de variante/modelo cuando corresponde.
- Cantidad acotada al stock publicado de la variante; producto agotado usa cantidad `0` y no puede agregarse.
- La acción Agregar valida además el stock ya existente en el carrito.
- El carrito persiste únicamente referencias `{ productoId, modeloClave, unidades }`; no persiste precios ni datos de pago.
- El contador/subtotal del header se rehidrata desde la misma referencia persistida usada por Fases 1–3.
- WhatsApp es alternativa únicamente cuando la configuración real contiene un número válido. En demo solo se muestra una vista previa y nunca se abre un envío real.
- Características muestran solamente campos que existen en el contrato público; no se inventan especificaciones.
- Productos relacionados se derivan del catálogo público y de la misma categoría; no son hardcodeados.

### Galería y responsive

- Imagen principal con proporción preservada y `object-fit: contain`.
- Miniaturas cuando existen múltiples fotografías.
- Controles anterior/siguiente e indicador `N / total`.
- Swipe horizontal mediante Pointer Events en móvil y `touch-action: pan-y` para conservar el scroll vertical.
- `pointercancel` cancela el gesto sin avanzar accidentalmente.
- Lightbox nativo `<dialog>` solo para fotografías reales; el contenido comercial permanece fuera del diálogo.
- Cerrar con botón, Escape o backdrop conserva el índice y devuelve el foco al detalle cuando corresponde.
- CTA móvil fijo inferior con safe area y espacio reservado para no tapar contenido.
- Responsive validado hasta 320 px sin overflow en los escenarios cubiertos.

### Integración con Fase 3

- `Ver producto` en cada tarjeta con slug navega a `/varistorehn/producto/:slug`.
- El botón Atrás del navegador devuelve al catálogo en el flujo probado.
- La tarjeta muestra precio normal/promocional cuando el contrato público trae una oferta válida.
- La guarda de Fase 3 fue actualizada para exigir la navegación por slug una vez activa Fase 4.
- Tema visual exclusivamente mediante los tokens globales ya existentes del sistema/BD; no se introdujo una paleta paralela.

## Definition of Done

- [x] URL pública compartible por slug y recarga directa.
- [x] Producto inexistente/inactivo con estado controlado.
- [x] Breadcrumbs Inicio > Categoría > Producto.
- [x] Galería multiimagen con principal y miniaturas.
- [x] Swipe móvil, indicador y controles accesibles.
- [x] Lightbox solo fotográfico y sin perder índice/contexto.
- [x] Imágenes sin deformación.
- [x] Nombre, SKU, categoría, precio, oferta y disponibilidad.
- [x] Selector de variante cuando aplica.
- [x] Cantidad nunca superior al stock.
- [x] Agregar al carrito usa la misma persistencia y actualiza contador/subtotal.
- [x] Persistencia segura sin precios manipulables.
- [x] WhatsApp alternativo condicionado a configuración válida.
- [x] Descripción y características sin inventar datos.
- [x] Relacionados de la misma categoría con datos públicos.
- [x] Catálogo enlaza `Ver producto` por slug.
- [x] Botón Atrás y slug canónico conservan el flujo.
- [x] CTA móvil fijo respeta safe areas.
- [x] Sin dependencias administrativas ni colores hardcodeados.
- [x] `npm ci` + TypeScript/lint/guardas Fases 1–4.
- [x] Build de producción.
- [x] Playwright Fases 1, 2, 3 y 4.
- [x] Comparación contra `Desarrollo` sin pisar trabajo concurrente.
- [x] Gate post-merge sobre el SHA funcional exacto en `Desarrollo`.

## Pruebas específicas de Fase 4

La suite `frontend/e2e/varistorehn-fase4.spec.ts` certifica 11 escenarios:

1. URL demo directa, detalle independiente y cantidad acotada a stock.
2. Catálogo → `Ver producto` → detalle → Atrás.
3. Fuente real con galería, SKU, promoción y relacionados sin inventar datos.
4. Lightbox solo fotográfico, índice conservado y retorno de foco.
5. Precio promocional coherente con el subtotal y persistencia segura.
6. Stock cero: agotado, cantidad cero y compras bloqueadas.
7. Slug histórico reemplazado por el canónico sin duplicar historial.
8. 404 e inactivo controlados sin convertirlos en demo.
9. Error real visible sin fallback silencioso.
10. Móvil: swipe, CTA fijo, lightbox y ausencia de overflow desde 320 px.
11. WhatsApp demo estructurado sin envío externo.

## Evidencia pre-merge

Head de implementación: `f10afd777f5cdd0ef495f0adb907a99372fdd033`.

Workflow `VariStoreHn Fase 4 - regresion detalle`, run `34558418519`: **success**.

- TypeScript/lint + guardas Fases 1–4: **success**.
- Build de producción: **success**.
- Playwright Fase 1: **4/4**.
- Playwright Fase 2: **8/8**.
- Playwright Fase 3: **8/8**.
- Playwright Fase 4: **11/11**.
- Artefacto: `10183490299`.

Antes del merge se comparó la rama con el `Desarrollo` más reciente. Los commits concurrentes correspondían a Sucursales/VAEP y no solapaban ninguno de los archivos modificados por Fase 4.

## Integración

PR #3346 fusionado mediante squash.

Commit funcional en `Desarrollo`:
`906c73178ada203509582b9035399b275d0318ce`.

Este commit conserva como padre el HEAD previo de `Desarrollo` (`8f36b22827c3ae74548e891cc6471cc93dd7a5cd`), por lo que no se descartó trabajo concurrente.

## Gate post-merge exacto

Workflow `VariStoreHn Fase 4 - regresion detalle`, run `34560172361`: **success** sobre el SHA funcional exacto `906c73178ada203509582b9035399b275d0318ce`.

- `npm ci`: **success**.
- TypeScript/lint + guardas Fases 1–4: **success**.
- Build de producción: **success**.
- Playwright Fase 1: **4/4**.
- Playwright Fase 2: **8/8**.
- Playwright Fase 3: **8/8**.
- Playwright Fase 4: **11/11**.
- Artefacto post-merge: `10184113154`.

El build de producción generó el chunk lazy `varistorehn-producto-component`, confirmando la carga independiente del detalle público.

## Observaciones no bloqueantes

La ejecución sigue reportando deuda transversal preexistente del repositorio: dependencias con avisos de `npm audit`, warnings Angular en módulos ajenos y el warning histórico de presupuesto de `varistorehn.component.scss` (17.10 kB frente a 16 kB). Ninguno hizo fallar el build ni corresponde al nuevo componente de detalle.

## Fuera de alcance respetado

- Página completa `/varistorehn/carrito`: Fase 5.
- Checkout/pedido: Fase 6.
- Home comercial: Fase 7.
- Reglas temporales avanzadas de promociones/inventario: Fase 9.

## Cierre

**Fase 4 queda COMPLETADA / REAUDITADA / HARDENED e integrada en `Desarrollo`.** La certificación incluye revisión previa de Fase 3, gate pre-merge, control de concurrencia, merge y regresión post-merge sobre el commit funcional exacto.
