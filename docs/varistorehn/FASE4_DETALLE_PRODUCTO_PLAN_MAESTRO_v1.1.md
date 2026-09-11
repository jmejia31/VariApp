# VariStoreHn — Fase 4: Detalle de producto

Autoridad: **Plan Maestro de Mejoras VariStoreHn v1.1**, Fase 4.

Estado: **EN DESARROLLO — no cerrar sin gate post-merge**.

## Objetivo

Dar al comprador suficiente información y confianza para decidir la compra desde una página pública independiente y compartible. La experiencia principal de detalle vive en `/varistorehn/producto/:slug`; un diálogo se usa únicamente como lightbox para ampliar fotografías.

## Implementación

### Ruta y fuente de datos

- Ruta pública lazy: `/varistorehn/producto/:slug`, sin `authGuard` ni `permisoGuard`.
- `VaristorehnService.obtenerProductoPorSlug()` consume `GET /tienda/productos/{slug}`.
- El backend público `TiendaController` está marcado `[AllowAnonymous]` y responde 404 para producto inexistente o inactivo.
- Si el backend resuelve un slug histórico a un slug canónico actual, el frontend reemplaza la URL sin duplicar historial.
- Demo y base de datos permanecen separadas. Un error real nunca se sustituye silenciosamente con fixtures.

### Información comercial

- Nombre, categoría, SKU, marca/modelo, precio, posible precio promocional y disponibilidad.
- `precioVenta(producto, modelo)` centraliza el precio efectivo para que detalle y carrito no diverjan.
- El carrito sigue persistiendo únicamente referencias `{ productoId, modeloClave, unidades }`; precio y stock se rehidratan desde la fuente pública.
- Características muestran solamente campos que realmente existen en el contrato público. No se inventan especificaciones.
- Productos relacionados se derivan del catálogo público y de la misma categoría; no son hardcodeados.

### Galería

- Imagen principal con `object-fit: contain`.
- Miniaturas cuando existen múltiples fotografías.
- Controles anterior/siguiente e indicador `N / total`.
- Swipe horizontal mediante Pointer Events en móvil y `touch-action: pan-y` para no romper el scroll vertical.
- `pointercancel` cancela el gesto sin avanzar accidentalmente.
- Lightbox nativo `<dialog>` solo para fotografías reales; el contenido comercial permanece fuera del diálogo.
- Cerrar con botón, Escape o backdrop conserva el índice y devuelve el foco a la imagen principal cuando corresponde.

### Compra

- Selector de variante/modelo cuando corresponde.
- Cantidad acotada al stock publicado de la variante; producto agotado usa cantidad `0` y no puede agregarse.
- La acción Agregar valida además el stock ya existente en el carrito.
- El contador/subtotal del header se rehidrata desde la misma referencia persistida usada por Fases 1–3.
- WhatsApp es alternativa únicamente cuando la configuración real contiene un número válido. En demo solo se muestra la vista previa y nunca se abre un envío real.
- En móvil existe CTA fijo inferior con safe area y espacio reservado para no tapar contenido.

### Integración con Fase 3

- `Ver producto` en cada tarjeta con slug navega a `/varistorehn/producto/:slug`.
- La tarjeta muestra precio normal/promocional cuando el contrato público trae una oferta válida.
- La guardia de Fase 3 se actualiza: una vez activa Fase 4, exige la navegación por slug en vez de prohibirla.

## Definition of Done

- [x] Página pública independiente por slug.
- [x] Fuente pública por slug y producto inactivo/inexistente controlado.
- [x] Breadcrumbs Inicio > Categoría > Producto.
- [x] Galería multiimagen con miniaturas.
- [x] Swipe móvil + indicador + controles accesibles.
- [x] Lightbox solo fotográfico y sin perder índice/contexto.
- [x] Imágenes sin deformación.
- [x] Nombre, SKU, categoría, precio, oferta y disponibilidad.
- [x] Selector de variante cuando aplica.
- [x] Cantidad acotada al stock.
- [x] Agregar al carrito usa la misma persistencia y actualiza contador/subtotal.
- [x] WhatsApp alternativo condicionado a configuración válida.
- [x] Descripción y características sin inventar datos.
- [x] Relacionados de la misma categoría con datos públicos.
- [x] Catálogo enlaza Ver producto por slug.
- [x] CTA móvil fijo respeta safe areas.
- [x] Sin dependencias administrativas ni colores hardcodeados.
- [ ] `npm ci` + TypeScript/lint/guardas Fases 1–4.
- [ ] Build de producción.
- [ ] Playwright Fases 1, 2, 3 y 4.
- [ ] Comparación final contra `Desarrollo` sin pisar trabajo concurrente.
- [ ] Gate post-merge sobre el SHA exacto/descendiente certificado.

## Pruebas específicas de Fase 4

La suite `frontend/e2e/varistorehn-fase4.spec.ts` cubre:

1. URL directa demo, detalle independiente, cantidad y stock.
2. Catálogo → Ver producto → Detalle → Atrás.
3. Fuente real con 3 imágenes, SKU, promoción, swipe y relacionados.
4. Lightbox, navegación, Escape/cierre, persistencia de índice y retorno de foco.
5. Precio promocional coherente con subtotal del carrito y persistencia segura.
6. 404 controlado.
7. Error real sin fallback demo.
8. Responsive 1000/760/390/320 px, CTA fijo, swipe, lightbox y cero overflow.
9. WhatsApp demo estructurado sin envío.

## Fuera de alcance

- Página completa `/varistorehn/carrito`: Fase 5.
- Checkout/pedido: Fase 6.
- Home comercial: Fase 7.
- Reglas temporales avanzadas de promociones/inventario: Fase 9.

## Evidencia

Se completará únicamente después de los gates pre-merge y post-merge. Mientras esta sección no tenga ambos resultados verdes, la Fase 4 permanece abierta.
