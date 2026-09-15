# VariStoreHn — Reauditoría de Fase 4 durante Fase 5

Fecha: 2026-09-11  
Plan Maestro: v1.1

## Resultado

El detalle independiente `/varistorehn/producto/:slug` continúa cumpliendo su Definition of Done: URL compartible, producto por slug, estados controlados, galería multiimagen, swipe, lightbox solo fotográfico, precio/promoción, SKU, disponibilidad, cantidad, CTA, WhatsApp, descripción, características, relacionados, breadcrumbs y responsive.

## Hallazgos y correcciones

1. **Stock restante vs. stock total en el selector.** La lógica de agregado ya evitaba exceder inventario si había unidades del mismo modelo en el carrito, pero el HTML seguía anunciando `max=stock total`. Se corrige para que input y botón `+` usen `stockRestante()`.
2. **Persistencia duplicada dentro del detalle.** Fase 5 sustituye la lectura/escritura local por `VaristorehnCarritoService`, manteniendo el detalle alineado con la regla transversal de un único carrito.
3. **Modal legado del home.** El home conservaba un quick-detail heredado y `abrirDetalle()` podía usarlo. Las acciones del home ahora navegan a `VARISTOREHN_PATHS.producto(producto.slug)`; la guarda de Fase 4 prohíbe volver a llamar `showModal()` desde ese flujo. El `dialog` residual puede retirarse en el refactor visual del home, pero ya no es accesible como experiencia principal de producto.
4. **Guarda Fase 4 actualizada.** Ya no exige la antigua persistencia local; exige carrito central, stock restante y navegación canónica desde home/catálogo.

## Estado

La Fase 4 solo se recertificará después de que la regresión acumulada Fases 1–5 quede verde en el candidato y nuevamente post-merge en `Desarrollo`.
