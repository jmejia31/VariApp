# VariStoreHn — Fase 2: categorías públicas

## Estado

**CANDIDATA A CIERRE tras reauditoría.** La omisión arquitectónica detectada después del primer cierre ya fue corregida en la rama `chatgpt/varistorehn-fase2-categoria-canonica`: la experiencia pública dispone ahora de una ruta canónica por categoría, el listado navega hacia ella y existe regresión estática + navegador real para impedir volver al atajo del home.

La fase se marcará como **COMPLETADA / REAUDITADA** únicamente después de integrar el PR en `Desarrollo` y repetir la regresión sobre el commit resultante.

## Alcance de Fase 2

- `/varistorehn/categorias` pública y sin guards administrativos;
- `VaristorehnCategoriasComponent` reutilizando el header público;
- consumo de `GET /tienda/categorias` y mapeo a `CategoriaTienda`;
- estados `loading | empty | error | success` en el listado;
- `cantidadProductos = null` preservado como desconocido;
- sin fallback silencioso de API real a fixtures;
- categorías del home obtenidas desde la fuente pública dedicada, no inferidas desde texto de productos;
- continuidad de búsqueda y resumen del carrito;
- `/varistorehn/categoria/:slug` pública y sin guards administrativos;
- página `VaristorehnCategoriaComponent` que consume `GET /tienda/categorias/{slug}`;
- URL corregida mediante `replaceUrl` cuando el backend devuelve un slug canónico diferente al solicitado;
- estados `loading | error | not-found | success` en la página canónica;
- errores reales no se sustituyen por datos de demostración;
- tarjetas de `/varistorehn/categorias` enlazadas mediante `VARISTOREHN_PATHS.categoria(slug)`;
- continuidad al catálogo actual mediante `?categoria=<slug>#catalogo`, sin activar prematuramente `/varistorehn/productos`;
- identidad, búsqueda, WhatsApp, resumen del carrito y tokens visuales compartidos con el resto de la tienda.

## Gap de reauditoría — resolución

La discrepancia original era verificable dentro del propio código: `VARISTOREHN_PATHS.categoria(slug)` y `VaristorehnService.obtenerCategoriaPorSlug(slug)` ya existían, pero `app.routes.ts` solo había activado `/varistorehn/categorias` y las tarjetas navegaban directamente a `?categoria=<slug>#catalogo`.

- [x] activar `/varistorehn/categoria/:slug` en `app.routes.ts`;
- [x] crear una página pública de categoría por slug que consume `GET /tienda/categorias/{slug}`;
- [x] usar el slug canónico devuelto por backend y corregir URL si el prefijo cambia;
- [x] cambiar las tarjetas de `/varistorehn/categorias` para navegar a `VARISTOREHN_PATHS.categoria(slug)`;
- [x] representar categoría inexistente/inactiva como estado no encontrado, sin fallback demo;
- [x] mantener búsqueda/header/carrito y tema del sistema en la página de categoría;
- [x] ofrecer continuidad al catálogo actual sin activar todavía `/varistorehn/productos`;
- [x] añadir pruebas estáticas y Playwright de la nueva ruta;
- [x] revalidar Fase 1 y la página de listado de categorías;
- [x] lint/TypeScript + build producción + Playwright verdes en la rama candidata;
- [ ] integrar y revalidar sobre `Desarrollo`.

## Evidencia de la rama reauditoria

HEAD funcional certificado antes de este cambio documental: `0d406d20454311ad8fa82d629fb4d4be4b4a643f`.

GitHub Actions run `34535758631`: **success**.

El gate ejecutó en ese SHA:

- `npm ci`: success;
- TypeScript + lint general: success;
- guardia estática Fase 1: success;
- guardia estática Fase 2: success, incluyendo listado, ruta canónica, estados, navegación y separación administrativa;
- `npm run build:prod`: success;
- Playwright Fase 1: **4/4 success**;
- Playwright Fase 2: **8/8 success**.

Los ocho escenarios de Fase 2 cubren:

1. listado independiente, header compartido, fixtures explícitos y reflow sin overflow;
2. fuente real `CategoriaTienda` y conteo `null` preservado como desconocido;
3. estado `empty` sin fabricar categorías;
4. error del listado real sin fallback silencioso a demo;
5. continuidad búsqueda → categoría canónica → catálogo filtrado → carrito;
6. consumo por slug y corrección de URL al slug canónico devuelto por backend;
7. categoría inexistente como `not-found` sin fixture inventado;
8. error de la categoría por slug manteniendo el fallo real.

Durante el primer intento del hardening, la guardia de calidad detectó `outline: none` en la nueva hoja de estilos y bloqueó el pipeline antes del build. La regla fue corregida eliminando esa supresión de foco y el HEAD funcional posterior quedó verde. Este fallo previo se conserva como evidencia de que la guardia realmente está protegiendo accesibilidad y no solo comprobando presencia de archivos.

## Concurrencia

La rama se creó desde `9e9336c02973f31a8de0b56b26420b0e93af882d`. Mientras se trabajó la fase, `Desarrollo` avanzó con trabajo VAEP de otros agentes. La comparación previa al PR debe volver a ejecutarse y cualquier integración se hará únicamente si esos cambios no pisan los archivos de VariStoreHn de esta fase.

## Límite de fase

Fase 2 sigue sin activar:

- `/varistorehn/productos` como catálogo independiente: Fase 3;
- detalle de producto por slug: Fase 4;
- store global/persistente del carrito: Fase 5;
- checkout/pedido real: Fase 6.

## Evidencia histórica válida

La implementación inicial del listado pasó:

- rama candidata run `34530528076` — success;
- gate de PR run `34530901540` — success;
- post-merge run `34531637269` — success;
- Playwright Fase 1: 4/4;
- Playwright Fase 2 inicial: 5/5.

Esa evidencia probaba el listado y sus estados, pero no la URL canónica omitida. La reauditoría actual añade la cobertura que faltaba.

## Gate de cierre reaudidado

La fase solo vuelve a `COMPLETADA` cuando el PR de esta reauditoría sea mergeable, sus checks sean verdes y exista una regresión verde sobre el commit integrado en `Desarrollo`. Hasta entonces este documento conserva el estado de candidata a cierre.