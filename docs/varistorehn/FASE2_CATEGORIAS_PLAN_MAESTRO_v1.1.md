# VariStoreHn — Fase 2: categorías públicas

## Estado

**REABIERTA tras reauditoría.** La implementación de `/varistorehn/categorias` fue integrada y validada, pero la auditoría posterior detectó una omisión arquitectónica: Fase 0 ya había definido `/varistorehn/categoria/:slug` como URL pública canónica y `VaristorehnService` ya dispone de `obtenerCategoriaPorSlug()`, mientras la UI de Fase 2 todavía enviaba las tarjetas al home mediante `?categoria=<slug>#catalogo`.

La Fase 2 no volverá a marcarse como completada hasta activar y validar la ruta canónica de categoría sin adelantar el catálogo independiente de Fase 3.

## Alcance ya entregado

- `/varistorehn/categorias` pública y sin guards administrativos;
- `VaristorehnCategoriasComponent` reutilizando el header público;
- consumo de `GET /tienda/categorias` y mapeo a `CategoriaTienda`;
- estados `loading | empty | error | success`;
- `cantidadProductos = null` preservado como desconocido;
- sin fallback silencioso de API real a fixtures;
- categorías del home dejaron de inferirse desde texto de productos;
- continuidad de búsqueda y resumen del carrito;
- regresiones estáticas y Playwright de Fases 1 y 2.

## Gap detectado en reauditoría

La discrepancia es verificable dentro del propio código: `VARISTOREHN_PATHS.categoria(slug)` y `VaristorehnService.obtenerCategoriaPorSlug(slug)` existen desde la base técnica, pero `app.routes.ts` solo había activado `/varistorehn/categorias`, y las tarjetas enlazaban a `?categoria=<slug>#catalogo`.

- [ ] activar `/varistorehn/categoria/:slug` en `app.routes.ts`;
- [ ] crear una página pública de categoría por slug que consuma `GET /tienda/categorias/{slug}`;
- [ ] usar el slug canónico devuelto por backend y corregir URL si el prefijo cambia;
- [ ] cambiar las tarjetas de `/varistorehn/categorias` para navegar a `VARISTOREHN_PATHS.categoria(slug)`;
- [ ] representar categoría inexistente/inactiva como estado no encontrado, sin fallback demo;
- [ ] mantener búsqueda/header/carrito y tema del sistema en la página de categoría;
- [ ] ofrecer continuidad al catálogo actual sin activar todavía `/varistorehn/productos`;
- [ ] añadir pruebas estáticas y Playwright de la nueva ruta;
- [ ] revalidar Fase 1 y la página de listado de categorías;
- [ ] lint/TypeScript + build producción + Playwright verdes;
- [ ] integrar y revalidar sobre `Desarrollo`.

## Límite de fase

Fase 2 seguirá sin activar:

- `/varistorehn/productos` como catálogo independiente: Fase 3;
- detalle de producto por slug: Fase 4;
- store global/persistente del carrito: Fase 5;
- checkout/pedido real: Fase 6.

## Evidencia histórica válida

La implementación inicial de listado pasó:

- rama candidata run `34530528076` — success;
- gate de PR run `34530901540` — success;
- post-merge run `34531637269` — success;
- Playwright Fase 1: 4/4;
- Playwright Fase 2 inicial: 5/5.

Esa evidencia prueba el listado de categorías y sus estados, pero no cubría la ruta canónica omitida. Por eso la fase queda reabierta.

## Gate de cierre reaudidado

La fase solo volverá a `COMPLETADA` cuando todos los puntos del gap estén implementados y exista evidencia verde sobre el HEAD final de la rama y nuevamente sobre el commit integrado en `Desarrollo`.