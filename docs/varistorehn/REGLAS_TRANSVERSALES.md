# VariStoreHN - reglas transversales de implementacion

Estas reglas son obligatorias para cualquier cambio dentro de VariStoreHN en Solqaryn.

| Regla | Criterio verificable |
| --- | --- |
| Datos | Nombre, precio, stock, promociones e imagenes parten del contrato publico de `/tienda`; frontend normaliza con `mapearProducto` y calcula el precio visible con `precioVenta`. Checkout revalida precio y stock en servidor. |
| Diseno | La tienda usa exclusivamente tokens del tema/identidad de VariStoreHN. Ningún SCSS del storefront puede fijar colores hex/RGB/HSL propios; referencias externas solo orientan flujo o funcionalidad y no se copian identidad, paleta ni marca. |
| Estado | Toda consulta publica representa loading, error, empty y success. Recursos individuales pueden usar not-found como estado adicional. |
| Carrito | `VaristorehnCarritoService` es la unica autoridad de carrito en frontend. Persistencia local solo guarda identificadores y unidades; nunca precio, stock o imagen como autoridad. |
| URLs | Producto y categoria usan slugs publicos. Busqueda, filtros, orden y paginacion del catalogo se conservan en query params. |
| Mobile-first | Todo el CSS de VariStoreHN parte de un baseline móvil real: no se permiten breakpoints `max-width` para encoger escritorio y la expansión responsive se hace con `min-width`; la Definition of Done ejecuta primero escenarios móviles/táctiles y después tablet/escritorio/zoom. Los controles táctiles mantienen al menos 44 px y no se admite overflow horizontal. |
| Seguridad | La tienda publica consume DTOs/endpoints publicos. No reutiliza rutas, guards, JWT ni secretos administrativos. Checkout externo solo se habilita con endpoint relativo y origen HTTPS permitido. |
| Calidad | Ninguna fase se considera cerrada si lint, build o su regresion acumulativa estan rojos. Las fases posteriores vuelven a ejecutar las anteriores. |

El guard `scripts/validate-varistorehn-transversal.mjs` bloquea regresiones estructurales de estas reglas dentro del pipeline de Solqaryn.
