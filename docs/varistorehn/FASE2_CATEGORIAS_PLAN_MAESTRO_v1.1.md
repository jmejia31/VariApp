# VariStoreHn — Fase 2: categorías públicas

## Estado

**COMPLETADA / REAUDITADA.** La omisión arquitectónica detectada después del primer cierre fue corregida, integrada en `Desarrollo` y revalidada sobre el commit exacto resultante.

PR de cierre reaudidado: #3336.
Commit integrado: `fc12bf9437ba1825d3007e620064fdd84557ac4d`.
Regresión post-merge Fase 2: run `34536591699` — **success**.

## Alcance certificado

- `/varistorehn/categorias` pública y sin guards administrativos;
- `VaristorehnCategoriasComponent` reutilizando el header público;
- consumo de `GET /tienda/categorias` y mapeo a `CategoriaTienda`;
- estados `loading | empty | error | success` en el listado;
- `cantidadProductos = null` preservado como desconocido;
- sin fallback silencioso de API real a fixtures;
- categorías del home obtenidas desde la fuente pública dedicada, no inferidas desde texto de productos;
- `/varistorehn/categoria/:slug` pública y sin guards administrativos;
- página `VaristorehnCategoriaComponent` consumiendo `GET /tienda/categorias/{slug}`;
- URL corregida con `replaceUrl` cuando el backend devuelve un slug canónico distinto;
- estados `loading | error | not-found | success` en la página canónica;
- categoría inexistente o inactiva representada como no encontrada, sin fabricar fixtures;
- tarjetas de `/varistorehn/categorias` enlazadas mediante `VARISTOREHN_PATHS.categoria(slug)`;
- continuidad de identidad, búsqueda, WhatsApp y resumen del carrito;
- continuidad al catálogo actual mediante `?categoria=<slug>#catalogo`, sin adelantar `/varistorehn/productos` de Fase 3;
- estilos gobernados por tokens del tema del sistema y objetivos táctiles de al menos 44 px.

## Gap de reauditoría — resuelto

La discrepancia original era verificable dentro del código: `VARISTOREHN_PATHS.categoria(slug)` y `VaristorehnService.obtenerCategoriaPorSlug(slug)` ya existían, pero `app.routes.ts` no activaba la ruta y las tarjetas regresaban al home con `?categoria=<slug>#catalogo`.

- [x] activar `/varistorehn/categoria/:slug`;
- [x] crear página pública por slug;
- [x] consumir el endpoint canónico y manejar cambio de slug;
- [x] cambiar tarjetas al enlace canónico;
- [x] representar loading/error/not-found/success sin fallback silencioso;
- [x] mantener header, búsqueda, carrito e identidad/tema;
- [x] no activar todavía `/varistorehn/productos`;
- [x] ampliar guardas estáticas y Playwright;
- [x] revalidar Fase 1 + listado Fase 2;
- [x] lint/TypeScript + build producción + E2E verdes;
- [x] integrar y revalidar post-merge en `Desarrollo`.

## Evidencia ejecutable

### Rama de reauditoría

HEAD funcional: `0d406d20454311ad8fa82d629fb4d4be4b4a643f`.
Run `34535758631`: **success**.

- TypeScript/lint: success;
- guardia Fase 1: success;
- guardia Fase 2: success;
- build de producción: success;
- Playwright Fase 1: **4/4**;
- Playwright Fase 2: **8/8**.

### PR #3336

HEAD: `8b8e279dca411988b67918ad68a3bab8bb925158`.

- run Fase 2 `34536153026`: **success**;
- run independiente Fase 1 `34536152956`: **success**.

### Post-merge exacto

Commit: `fc12bf9437ba1825d3007e620064fdd84557ac4d`.
Run Fase 2 `34536591699`: **success**.

El job post-merge aprobó instalación, lint/guardas estáticas, build de producción, servidor Angular, regresión Fase 1, Playwright Fase 2 y publicación de evidencia.

Los ocho escenarios de Fase 2 cubren:

1. listado independiente, header compartido, fixtures explícitos y reflow sin overflow;
2. fuente real `CategoriaTienda` y conteo `null` preservado como desconocido;
3. estado `empty` sin fabricar categorías;
4. error del listado real sin fallback silencioso a demo;
5. continuidad búsqueda → categoría canónica → catálogo filtrado → carrito;
6. consumo por slug y corrección de URL al slug canónico devuelto por backend;
7. categoría inexistente como `not-found` sin fixture inventado;
8. error de categoría por slug manteniendo el fallo real.

Durante el hardening inicial, la guardia de calidad bloqueó correctamente una versión que contenía `outline: none`; se eliminó la supresión de foco antes de certificar el HEAD verde.

## Concurrencia

La integración se realizó mediante PR aislado. Antes del merge, `Desarrollo` solo había avanzado en documentación VAEP ajena a VariStoreHn, sin solapamiento con los archivos de esta fase. El merge se ejecutó con SHA de cabeza esperado para evitar integrar una revisión distinta de la validada.

## Límite de fase

Permanecen fuera de Fase 2:

- `/varistorehn/productos` como catálogo independiente: Fase 3;
- detalle de producto por slug: Fase 4;
- store global/persistente del carrito: Fase 5;
- checkout/pedido real: Fase 6.

## Observación transversal

La instalación de dependencias reporta vulnerabilidades preexistentes del repositorio. No se atribuyen a la implementación de Fase 2 y deben mantenerse como deuda de seguridad transversal separada.

**Conclusión:** Fase 2 recertificada con ruta canónica por categoría, estados reales, regresión estática, navegador real y validación exacta post-merge.