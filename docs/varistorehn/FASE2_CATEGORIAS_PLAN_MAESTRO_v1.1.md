# VariStoreHn — Fase 2: categorías públicas

## Estado vigente

**COMPLETADA / REAUDITADA / HARDENED.**

Este documento conserva la trazabilidad de Fase 2 y refleja el estado vigente después de completar Fases 3–5. Los puentes temporales usados durante el cierre original ya no describen la arquitectura actual.

## Alcance vigente

- `/varistorehn/categorias` es pública y no usa guards administrativos.
- `VaristorehnCategoriasComponent` reutiliza el header público.
- La fuente real usa `GET /tienda/categorias` y mapea a `CategoriaTienda`.
- Se conservan estados `loading | empty | error | success`.
- `cantidadProductos = null` se representa como desconocida; no se fabrica `0`.
- Un error real nunca cae silenciosamente a fixtures demo.
- `/varistorehn/categoria/:slug` es pública y consume `GET /tienda/categorias/{slug}`.
- Un slug histórico se corrige con `replaceUrl` cuando el backend devuelve el slug canónico.
- Categoría inexistente/inactiva usa `not-found`; no se inventa un fixture.
- Las tarjetas enlazan con `VARISTOREHN_PATHS.categoria(slug)`.
- “Ver productos de esta categoría” continúa hoy hacia el catálogo canónico `/varistorehn/productos?categoria=<slug>` de Fase 3.
- Header, búsqueda, WhatsApp, identidad, tema y **carrito global de Fase 5** mantienen continuidad.
- El acceso al carrito usa `/varistorehn/carrito`, no un drawer ni `?carrito=1` como destino funcional.

## Evolución controlada

Durante el cierre cronológico de Fase 2, `/varistorehn/productos` todavía pertenecía a Fase 3 y la continuidad usaba temporalmente el catálogo embebido del home. Fase 3 sustituyó ese puente por el catálogo independiente. Fase 5 sustituyó cualquier puente temporal de carrito por la ruta/store globales.

Esos comportamientos antiguos quedan como historia del desarrollo y **no deben restaurarse**. Las guardas acumuladas de Fases 2–5 protegen las rutas canónicas actuales.

## Definition of Done vigente

- [x] Listado público independiente.
- [x] Ruta canónica por categoría.
- [x] Fuente `CategoriaTienda` pública.
- [x] Loading/empty/error/success y not-found controlados.
- [x] Conteo desconocido preservado como desconocido.
- [x] Sin fallback silencioso de datos reales a demo.
- [x] Navegación por slug canónico.
- [x] Continuidad al catálogo independiente de Fase 3.
- [x] Continuidad con carrito global de Fase 5.
- [x] Sin guards/componentes administrativos.
- [x] Tema por tokens del sistema y touch targets adecuados.
- [x] Guardia estática y 8 escenarios Playwright permanentes.

## Evidencia histórica

PR de cierre reauditado: #3336. Commit integrado: `fc12bf9437ba1825d3007e620064fdd84557ac4d`. Regresión post-merge Fase 2: run `34536591699` — **success**.

La suite mantiene 8 escenarios sobre listado independiente, fuente real, empty, error, continuidad de navegación, slug canónico, not-found y error por slug. Las regresiones de fases posteriores vuelven a ejecutar esta suite acumulativamente.

## Fuera de alcance actual

Checkout/pedido real pertenece a Fase 6 y **no está activado** en este cierre.
