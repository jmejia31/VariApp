# VariStoreHN — Reauditoría final Fases 0–5

Fecha: 2026-09-11  
Plan rector: **Plan Maestro de Mejoras VariStoreHn v1.1**  
Tracking: #3372  
Implementación/auditoría: PR #3373

## Regla de alcance

Esta auditoría se detiene estrictamente al terminar Fase 5. **No inicia, activa ni completa Fase 6.** En particular, no se habilitan `/varistorehn/checkout`, `/varistorehn/pedido/:id`, formulario de cliente, entrega, creación de pedido, confirmación ni limpieza post-pedido.

## Resultado por fase

| Fase | Estado reauditoría | Resultado vigente |
| --- | --- | --- |
| 0 — Fundaciones | Verde | Contrato público, rutas centralizadas, separación público/admin, endpoints GET y fallback demo explícito conservados. |
| 1 — Navegación/Header | Verde tras hardening | Header compartido, búsqueda, WhatsApp, responsive/accesibilidad y carrito canónico. |
| 2 — Categorías | Verde | Listado y categoría por slug, estados reales, conteo desconocido preservado y navegación canónica. |
| 3 — Catálogo | Verde | Catálogo independiente, búsqueda/filtros/orden/paginación, variantes, detalle por slug y carrito global. |
| 4 — Detalle | Verde tras hardening | Página por slug, galería/swipe/lightbox, stock restante, WhatsApp de detalle, relacionados y quick-detail del home eliminado. |
| 5 — Carrito | Verde pendiente de gate final de esta auditoría | Store único, persistencia mínima, rehidratación segura, edición/totales/stock, ruta única y ausencia de checkout heredado. |

## Hallazgos reales de esta auditoría

### H1 — Segunda superficie de carrito en el home

Aunque `/varistorehn/carrito` y `VaristorehnCarritoService` ya existían, el home conservaba un `<dialog class="cart-dialog">` con edición propia. Usaba el store central, por lo que varias pruebas históricas seguían verdes, pero duplicaba UX y responsabilidad.

**Corrección:** drawer retirado completamente. `abrirCarrito()` navega a `VARISTOREHN_PATHS.carrito`. El puente histórico `?carrito=1` solo redirige con `replaceUrl` a la ruta canónica.

### H2 — Lógica ejecutable que adelantaba Fase 6

El drawer heredado todavía contenía acciones de carrito completo para “Pedir por WhatsApp” y “Continuar con tarjeta”, además de creación de checkout, idempotencia, validación de URL y redirección.

**Corrección:** eliminada esa lógica de Fases 0–5. No fue trasladada ni completada. La Fase 6 queda sin iniciar.

### H3 — CSS/estado muerto

Persistían estilos completos del drawer eliminado y selectores responsive `.detail-layout`, `.detail-media` y `.detail-body` del quick-detail ya retirado en Fase 4. La página de carrito conservaba además el computed `puedeContinuarCheckout` sin checkout activo.

**Corrección:** estilos y estado muertos retirados. Las guardas ahora prohíben reintroducir esas superficies en el home.

### H4 — Copy interno del roadmap visible al comprador

El carrito público mencionaba “Fase 6”.

**Corrección:** la UI usa lenguaje de comprador y no expone numeración interna del roadmap.

### H5 — Documentación histórica interpretada como estado actual

Los cierres de Fases 1–3 todavía describían puentes que fueron correctos en su momento (`?carrito=1`, catálogo embebido, páginas todavía futuras), pero ya no representaban el estado posterior a Fase 5.

**Corrección:** documentación Fases 1–5 sincronizada para separar historia/evidencia del estado canónico vigente.

### H6 — Regresión no protegía suficientemente el retiro del drawer

La arquitectura central podía coexistir con la segunda superficie sin que la guarda de Fase 5 fallara.

**Corrección:** `validate-varistorehn-fase5.mjs` ahora exige ruta única/store único, prohíbe drawer/pago/pedido heredados y prohíbe rutas de Fase 6. Playwright Fase 5 añade un escenario runtime específico. La suite Fase 5 pasa de 6 a 7 escenarios; el total acumulado esperado pasa de 37 a 38.

## Invariantes vigentes tras la corrección

1. El carrito tiene una sola fuente de verdad: `VaristorehnCarritoService`.
2. `localStorage` guarda únicamente `{ productoId, modeloClave, unidades }`.
3. Precio/stock/oferta/disponibilidad se reconstruyen desde el catálogo público actual.
4. Home, categorías, catálogo y detalle no mantienen persistencia propia de carrito.
5. Todos los accesos al carrito terminan en `/varistorehn/carrito`.
6. No existe un drawer de carrito alternativo en el home.
7. No existe ruta pública de checkout/pedido en `app.routes.ts`.
8. Fases 0–5 no ejecutan creación de checkout/pedido del carrito completo.
9. El detalle descuenta unidades ya agregadas antes de permitir nuevas cantidades.
10. Tema visual usa tokens del sistema/BD, sin paleta paralela.
11. Los errores de fuente real no caen silenciosamente a fixtures.
12. Las páginas públicas siguen desacopladas de guards/componentes administrativos.

## Presupuesto visual

El warning histórico de presupuesto de `varistorehn.component.scss` quedó eliminado en el build del candidato de esta auditoría después de retirar el drawer y CSS muerto. No se aumentó ni silenció el presupuesto.

## Deuda transversal ajena a VariStoreHN

La instalación compartida del repositorio continúa reportando vulnerabilidades/deprecaciones de dependencias y el build reporta warnings de otros módulos. No fueron introducidos por VariStoreHN y no se modifican desde esta auditoría para no interferir con trabajo concurrente ajeno. Esta distinción evita declarar falsamente que todo el repositorio carece de deuda cuando el objetivo certificable aquí son Fases 0–5 de VariStoreHN.

## Evidencia final

Los IDs del gate candidato definitivo y del gate post-merge exacto se incorporarán al cerrar #3372 después de que las cinco regresiones acumuladas terminen verdes sobre la revisión final.
