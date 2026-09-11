# VariStoreHn — Fase 5: Carrito

Estado: **CANDIDATA COMPLETA — PENDIENTE CERTIFICACIÓN POST-MERGE**  
Plan Maestro: v1.1 — Septiembre 2026

## Objetivo

Implementar un carrito real, persistente y consistente para todas las páginas públicas de VariStoreHn. La Fase 5 elimina las implementaciones paralelas de estado/localStorage que existían en home, categorías, catálogo y detalle, y establece `VaristorehnCarritoService` como única fuente de verdad en frontend.

## Alcance implementado

- Ruta canónica pública `/varistorehn/carrito`.
- Página de carrito independiente y responsive.
- Store singleton `VaristorehnCarritoService` compartido por:
  - `/varistorehn`
  - `/varistorehn/categorias`
  - `/varistorehn/categoria/:slug`
  - `/varistorehn/productos`
  - `/varistorehn/producto/:slug`
  - `/varistorehn/carrito`
- Agregar desde catálogo y detalle.
- Incrementar/disminuir unidades.
- Edición numérica directa con clamp `1..stock`.
- Eliminación explícita de línea y vaciado completo.
- Subtotal, total de línea y total del carrito derivados del mismo estado. En Fase 5 `total === subtotal` porque entrega/impuestos pertenecen a Fase 6 y no se inventan cargos.
- Persistencia por empresa y fuente (`bd`/`demo`).
- `localStorage` conserva únicamente `{ productoId, modeloClave, unidades }`.
- Al hidratar, precio, oferta, stock, disponibilidad, imagen y datos comerciales se reconstruyen con el catálogo público actual mediante `restaurarCarrito`.
- Variantes desaparecidas/inactivas se eliminan del carrito al revalidar.
- Cantidades mayores al inventario actual se reducen al stock vigente.
- El detalle descuenta las unidades ya existentes al calcular `stockRestante`; selector y CTA reflejan ese valor real.
- Header usa el mismo store para contador/subtotal.
- Carrito vacío presenta CTA para seguir comprando y no ofrece navegación a un checkout todavía inexistente.
- Estados loading/error/success y empty derivado del store.
- La falla al consultar el catálogo no sustituye silenciosamente datos reales por fixtures ni borra el `localStorage` original.
- Tema visual basado exclusivamente en tokens globales configurados por el sistema/BD.

## Reauditoría de Fase 4 durante esta fase

Antes de iniciar Fase 5 se revisó el detalle contra el Plan Maestro. El runtime certificado permanecía intacto en `Desarrollo`: ruta por slug, galería multiimagen, swipe, lightbox fotográfico, cantidad, stock, CTA, WhatsApp, descripción/características, relacionados, breadcrumbs, estados y responsive.

Se detectaron y corrigieron dos hardenings:

1. Si ya existían unidades de la misma variante en el carrito, el HTML del selector mostraba como `max` el stock total aunque la lógica de agregado impedía sobrepasar el remanente. Ahora input, botón `+` y CTA usan `stockRestante()`.
2. El home heredado todavía podía abrir un quick-detail en `dialog`. Las acciones de producto del home ahora navegan a `VARISTOREHN_PATHS.producto(producto.slug)` y la guarda de Fase 4 prohíbe restaurar `showModal()` como experiencia principal. El detalle canónico sigue siendo la página independiente.

La guarda de Fase 4 también se actualizó para exigir el carrito central en vez de la antigua persistencia local del detalle.

## Definition of Done de Fase 5

- [x] El carrito tiene una ruta pública independiente y compartible.
- [x] Existe una sola lógica central de carrito.
- [x] Agregar desde catálogo usa el store central.
- [x] Agregar desde detalle usa el store central.
- [x] Se puede incrementar y disminuir sin llegar a cero.
- [x] Se puede eliminar una línea explícitamente.
- [x] Se puede vaciar el carrito.
- [x] Cantidades nunca superan el stock vigente.
- [x] Subtotal, total de línea y total del carrito se recalculan al editar.
- [x] Contador y subtotal del header provienen del mismo estado.
- [x] Persistencia sobrevive recargas.
- [x] Persistencia guarda referencias mínimas, no precios ni stock como autoridad.
- [x] Rehidratación recalcula precio/stock usando la fuente pública actual.
- [x] Estado vacío tiene acción para continuar comprando.
- [x] Un carrito vacío no ofrece continuar a checkout.
- [x] Sin dependencias/guards administrativos.
- [x] Responsive y touch targets de al menos 44 px.
- [x] Tema gobernado por tokens globales.
- [x] `npm ci` + lint/TypeScript + guardas Fases 1–5.
- [x] Build de producción.
- [x] Playwright Fases 1–5.
- [ ] Validación post-merge sobre `Desarrollo`.

## Evidencia candidata

Workflow acumulado de Fase 5: `34598925462` — **SUCCESS**.

- Fase 1: 4/4 Playwright.
- Fase 2: 8/8 Playwright.
- Fase 3: 8/8 Playwright.
- Fase 4: 11/11 Playwright.
- Fase 5: 6/6 Playwright.
- Total acumulado: **37/37 pruebas de navegador**.
- Lint/TypeScript y guardas Fases 1–5: verde.
- Build producción: verde.
- Artefacto: `10262914007`.

## Fuera de alcance respetado

- Formulario checkout, datos del cliente, entrega y confirmación: Fase 6.
- Crear número/pedido real: Fase 6.
- Limpiar carrito después de pedido confirmado: Fase 6.
- Reglas temporales avanzadas de promoción/inventario: Fase 9.
- Pulido global de todas las pantallas del MVP: Fase 10.

## Gates permanentes

- `frontend/scripts/validate-varistorehn-fase5.mjs`
- `frontend/e2e/varistorehn-fase5.spec.ts`
- `.github/workflows/varistorehn-fase5-regression.yml`

La fase no se cerrará oficialmente hasta repetir el gate sobre el commit integrado en `Desarrollo`.
