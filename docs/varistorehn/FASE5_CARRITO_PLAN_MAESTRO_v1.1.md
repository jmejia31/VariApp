# VariStoreHn — Fase 5: Carrito

Plan Maestro: v1.1 — Septiembre 2026.

## Estado vigente

**COMPLETADA / REAUDITADA / HARDENED.**

La Fase 5 establece un carrito real, persistente y consistente para todas las páginas públicas de VariStoreHn. La reauditoría final previa a Fase 6 se sigue en #3372 / PR #3373 y tiene una regla explícita: **no activar ni implementar Fase 6**.

## Arquitectura vigente

- Ruta canónica pública `/varistorehn/carrito`.
- `VaristorehnCarritoService` es la única fuente de verdad del carrito.
- Store compartido por home, categorías, categoría, catálogo, detalle y página de carrito.
- El home **no contiene drawer de carrito**, ni estado de carrito paralelo, ni acciones de cierre de compra.
- Todos los accesos al carrito navegan a `/varistorehn/carrito`.
- El enlace legado `/varistorehn?carrito=1` solo existe como migración compatible: redirige con `replaceUrl` a la ruta canónica y no abre una segunda superficie.
- No existe `/varistorehn/checkout` ni `/varistorehn/pedido/:id` en `app.routes.ts`.
- No se ejecuta `crearCheckoutTarjeta`, creación de pedido ni redirección de pago desde Fases 0–5.

## Estado y persistencia

- Agregar desde catálogo y detalle usa el store central.
- Incrementar/disminuir unidades sin llegar a cero por decremento.
- Edición directa con clamp `1..stock`.
- Eliminación explícita y vaciado completo.
- Total de línea, subtotal y total del carrito se derivan del mismo estado.
- En Fase 5 `total === subtotal`: entrega/impuestos pertenecen al cierre posterior y no se inventan cargos.
- Persistencia separada por empresa y fuente (`bd`/`demo`).
- `localStorage` conserva únicamente `{ productoId, modeloClave, unidades }`.
- Precio, oferta, stock, disponibilidad, imagen y datos comerciales se reconstruyen desde el catálogo público vigente mediante `restaurarCarrito`.
- Variante desaparecida/inactiva se retira al revalidar.
- Cantidad superior al inventario actual se reduce al stock vigente.
- La falla del catálogo real no sustituye silenciosamente datos con fixtures ni sobrescribe el carrito persistido como si se hubiera validado.
- El detalle calcula `stockRestante()` descontando lo ya agregado.
- Header usa el mismo store para contador/subtotal.

## UX vigente

- Página independiente y responsive.
- Estado `loading`, `error`, `success` y vacío derivado del store.
- Carrito vacío ofrece seguir comprando y no enlaza a un checkout inexistente.
- La UI pública no muestra lenguaje interno como “Fase 6”.
- El resumen advierte que el carrito no reserva inventario y que cargos posteriores, si aplican, se confirmarán antes de finalizar.
- Touch targets principales de al menos 44 px.
- Tema visual exclusivamente mediante tokens globales configurados por el sistema/BD.

## Deuda encontrada en la reauditoría final — corregida

La segunda auditoría encontró deuda real que no debía quedar oculta aunque los tests anteriores estuvieran verdes:

1. **Drawer duplicado en el home.** Seguía coexistiendo con `/varistorehn/carrito`; fue retirado completamente.
2. **Lógica heredada que adelantaba Fase 6.** El home conservaba “Pedir por WhatsApp”, “Continuar con tarjeta”, creación de checkout/idempotencia y validación/redirección de pago para el carrito completo. Esa lógica fue eliminada; no se completó ni trasladó.
3. **CSS muerto.** Se retiraron estilos del drawer y selectores responsive del quick-detail ya eliminado en Fase 4.
4. **Estado muerto.** Se retiró `puedeContinuarCheckout` de la página del carrito porque no existe checkout activo en Fase 5.
5. **Copy interno.** Se eliminó de la UI pública cualquier referencia al número de fase del roadmap.
6. **Documentación histórica.** Fases 1–4 se sincronizaron para distinguir sus límites cronológicos originales del estado canónico actual.
7. **Regresión insuficiente contra reaparición del drawer.** Las guardas y Playwright ahora prohíben explícitamente una segunda superficie de carrito y acciones de pago/pedido en el home.

## Definition of Done vigente

- [x] Ruta pública independiente del carrito.
- [x] Una sola lógica central de carrito.
- [x] Agregar desde catálogo y detalle.
- [x] Incrementar, disminuir, editar, eliminar y vaciar.
- [x] Cantidad limitada al stock vigente.
- [x] Totales recalculados desde el estado único.
- [x] Header sincronizado.
- [x] Persistencia tras recarga.
- [x] Persistencia mínima, sin precio/stock como autoridad.
- [x] Rehidratación contra catálogo público actual.
- [x] Vacío con CTA útil y sin checkout.
- [x] Sin guards/componentes administrativos.
- [x] Responsive, touch targets y tema global.
- [x] Home sin carrito duplicado.
- [x] Fases 0–5 sin ejecución de checkout/pedido.
- [x] Guardas acumuladas Fases 1–5.
- [x] Playwright acumulado Fases 1–5.

## Evidencia histórica

Implementación inicial Fase 5: PR #3369. Commit funcional certificado `6c33aca4bbb2620a19f50f936786ffbadbc3d7e7`. Candidato `34598925462` — **success**. Post-merge exacto `34599707045` — **success**. En ese cierre: 37/37 pruebas de navegador (4 + 8 + 8 + 11 + 6).

Hardening adicional de Fase 4 posterior a Fase 5: PR #3370, commit `94fefb309c2ae498c25d25ece83f034f80ccb168`; Fase 4 run `34601213518` — **success** y Fase 5 run `34601213561` — **success**.

La suite específica de Fase 5 se amplía en la reauditoría final de 6 a **7 escenarios**, añadiendo la prohibición runtime del drawer/pago heredado y la migración de `?carrito=1` a la ruta canónica. Con las suites existentes, el total esperado del gate acumulado pasa de 37 a **38 escenarios**.

## Presupuesto del SCSS

La limpieza del drawer/CSS muerto redujo `varistorehn.component.scss` por debajo de su situación histórica y el build del candidato de reauditoría final terminó **success sin el warning de presupuesto específico de VariStoreHn**. No se modificó el umbral para conseguir ese resultado.

## Deuda transversal del repositorio

`npm ci` del repositorio continúa informando vulnerabilidades/deprecaciones compartidas y el build muestra warnings de otros módulos. No son introducidos por Fases 0–5 ni se corrigen desde esta auditoría para evitar interferir con trabajo ajeno. Se mantienen separados de la certificación específica de VariStoreHn y no se silencian.

## Fuera de alcance respetado

- Formulario de checkout y datos del cliente: Fase 6.
- Entrega y resumen final de checkout: Fase 6.
- Creación/confirmación de pedido real: Fase 6.
- Redirección de pago del carrito: Fase 6 según la integración real que se defina.
- Limpieza del carrito después de pedido confirmado: Fase 6.

**Fase 6 no ha sido iniciada ni activada.**
