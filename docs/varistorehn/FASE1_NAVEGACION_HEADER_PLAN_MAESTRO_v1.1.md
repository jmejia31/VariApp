# VariStoreHn — Fase 1: navegación pública y Header

## Estado vigente

**COMPLETADA / REAUDITADA / HARDENED.**

Este documento conserva la trazabilidad de la Fase 1, pero describe aquí el estado vigente después de completar y reauditar Fase 5. Las limitaciones temporales que existían al cierre original de Fase 1 ya no deben interpretarse como arquitectura actual.

## Alcance vigente

- `VaristorehnHeaderComponent` es el header público reutilizable y permanece desacoplado del CRUD administrativo.
- Identidad, logo, eslogan, moneda y WhatsApp provienen de `EmpresaIdentidadService` y de la configuración pública del sistema/base de datos.
- Las rutas públicas se construyen con `VARISTOREHN_PATHS`.
- Inicio, Productos y Categorías navegan hoy a sus rutas canónicas ya activadas por las fases posteriores.
- El buscador mantiene semántica `role="search"`, label accesible, búsqueda por teclado y continuidad con el catálogo público.
- El menú móvil usa `<dialog>` únicamente como navegación modal, con `aria-controls`, `aria-expanded`, Escape, foco inicial y restauración de foco.
- WhatsApp solo aparece cuando existe un número válido y el modo de compra lo permite.
- El header recibe unidades y subtotal del **store global de Fase 5**; no mantiene estado de carrito propio.
- La acción de carrito navega a la ruta canónica `/varistorehn/carrito`; el drawer de carrito histórico del home fue retirado completamente.
- No existen dependencias de `authGuard`, `permisoGuard`, `ProductosListComponent` ni `CategoriasListComponent` en la experiencia pública.
- Los estilos usan tokens del tema del sistema y los objetivos táctiles principales conservan al menos 44 px.

## Evolución controlada después de Fase 1

Al cierre original de Fase 1 todavía no existían las páginas independientes de Productos/Categorías ni el store global del carrito. Esas restricciones eran **límites cronológicos de aquel cierre**, no requisitos permanentes. Fases 2, 3 y 5 sustituyeron esos puentes de forma canónica y sus guardas actuales impiden reintroducir la arquitectura temporal.

La reauditoría final de Fases 0–5 eliminó además la última superficie duplicada del carrito en el home. El header sigue siendo la única cabecera pública compartida y cualquier acceso al carrito termina en `/varistorehn/carrito`.

## Definition of Done vigente

- [x] Header público reutilizable.
- [x] Identidad central y sin duplicación administrativa.
- [x] Navegación pública canónica.
- [x] Búsqueda accesible y funcional.
- [x] Contador/subtotal reales del carrito global.
- [x] WhatsApp secundario condicionado a configuración válida.
- [x] Navegación móvil con diálogo, ARIA, Escape y foco.
- [x] Responsive sin overflow en los viewports cubiertos.
- [x] Touch targets principales >= 44 px.
- [x] Tema gobernado por tokens globales.
- [x] Sin guards/componentes administrativos.
- [x] Guardia estática y Playwright permanentes.

## Evidencia histórica

La reauditoría inicial de Fase 1 detectó y corrigió estilos residuales del header anterior y una franja 761–1120 px donde WhatsApp desaparecía antes de activarse el menú móvil. El run de hardening `34523138867` terminó **success** con 4/4 escenarios Playwright.

Las regresiones de fases posteriores vuelven a ejecutar Fase 1 de forma acumulada, por lo que su comportamiento se valida también cuando evolucionan catálogo, detalle o carrito.

## Deuda de presupuesto resuelta

El warning histórico de presupuesto de `varistorehn.component.scss` (17.10 kB frente a 16 kB) quedó eliminado en la reauditoría final de Fases 0–5 al retirar el drawer de carrito y CSS muerto heredado. El build de producción del candidato terminó correctamente sin ese warning específico.

## Fuera de alcance actual

La Fase 6 — checkout, datos del cliente, entrega, creación/confirmación del pedido y limpieza posterior del carrito — **no forma parte de este cierre y no está activada**.
