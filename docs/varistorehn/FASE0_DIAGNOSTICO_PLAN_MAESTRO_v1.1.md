# VariStoreHn — Fase 0: Diagnóstico y base técnica

Fuente de autoridad: **Plan Maestro de Mejoras de VariStoreHn v1.1**.

## Objetivo
Asegurar que la tienda pública, los datos y las rutas estén preparados antes de rediseñar pantallas. Esta fase no adelanta estética fina, banners, animaciones ni páginas de fases posteriores.

## Estado
**COMPLETADA técnicamente y validada.** La implementación funcional de Fase 0 fue verificada en GitHub Actions sobre el HEAD funcional `6b5cbc7f95d7cd33aeff9bdce86427f6d9742b11`. Backend y frontend terminaron en verde. La integración a `Desarrollo` se realiza únicamente después de comprobar nuevamente que no existe conflicto material con cambios concurrentes.

## Definition of Done del Plan Maestro
- [x] Existe un modelo único de producto y categoría para las páginas públicas.
- [x] Las rutas públicas objetivo están definidas y centralizadas.
- [x] Está identificado qué código actual se conserva, qué se refactoriza y qué se elimina.
- [x] La tienda pública no depende de componentes ni endpoints exclusivamente administrativos.

## 1. Mapa de componentes y responsabilidades

### Frontend — conservar
- `varistorehn.models.ts`: contratos canónicos del escaparate. Ninguna página pública debe crear modelos alternativos de producto/categoría.
- `varistorehn.catalog.ts`: reglas puras de normalización, disponibilidad, filtros, carrito, restauración y fixtures demo.
- `varistorehn.service.ts`: única frontera HTTP del escaparate para catálogo, producto por slug, categorías y categoría por slug.
- `varistorehn.paths.ts`: única fuente de construcción de URLs públicas canónicas.
- `varistorehn.visual.ts`: ilustraciones/iconos propios reutilizables.
- `varistorehn.config.ts`: flags técnicos de integración; no define colores ni identidad empresarial.
- `EmpresaIdentidadService`: identidad/configuración empresarial compartida. La tienda no duplica nombre, logo, WhatsApp, moneda ni tema.

### Frontend — refactorizar en fases siguientes
- `varistorehn.component.ts/.html`: hoy concentran home, catálogo, filtros, detalle, carrito y acciones de cierre. Se dividirán por páginas conforme avance el roadmap.
- `varistorehn.component.scss` y `varistorehn.responsive.scss`: conservar reglas/tokens útiles y mover estilos junto a cada componente público.
- `app.routes.ts`: conservar administración existente; las páginas públicas nuevas usarán el prefijo `/varistorehn` y `VARISTOREHN_PATHS`.

### Reemplazar/eliminar durante el roadmap
- El `dialog` como detalle comercial principal: se elimina en Fase 4 y se sustituye por `/varistorehn/producto/:slug`.
- Categorías inferidas solo desde texto: Fase 2 consumirá el contrato público de categorías.
- Estado/persistencia de carrito dentro de una página: Fase 5 lo moverá a una única frontera compartida.
- Datos demo como posible sustituto silencioso del backend: prohibido. Son únicamente preview/fixture fuera de producción.

## 2. Modelo de datos público canónico

### Producto HTTP (`ProductoCatalogoPublicoDto` / `ProductoCatalogoPublico`)
Contrato preparado desde Fase 0 para crecer sin rehacer páginas:
- `id`
- `slug`
- `nombre`
- `descripcion`
- `categoriaId`
- `categoriaNombre`
- `marcaNombre`
- `modeloNombre`
- `precio`
- `precioOferta` (nulo mientras la fuente real no implemente promociones)
- `cantidadDisponible`
- `estaAgotado`
- `sku` (solo cuando puede determinarse sin ambigüedad desde la fuente real)
- `activo`
- `esDestacado` (false mientras no exista regla persistida de destacados)
- `fechaCreacion`
- `imagenPrincipalUrl`
- `imagenes[]`
- `modelos[]`

### Producto normalizado de tienda (`ProductoTienda`)
Es el modelo que deben consumir las páginas públicas: identidad, slug, categoría, SKU, precio normal/oferta, stock, disponibilidad, estado activo, destacado, fecha, imágenes y modelos. Los componentes no deben volver a mapear DTO administrativos.

### Categoría pública
`CategoriaCatalogoPublicoDto` y `CategoriaTienda` definen:
- `id`
- `slug`
- `nombre`
- `descripcion`
- `totalProductos` / `cantidadProductos`, nullable cuando la consulta no puede acreditar un conteo real
- imagen opcional reservada para cuando exista fuente real

### Regla para campos aún no existentes en negocio
No inventar datos. `precioOferta` permanece nulo y `esDestacado=false` hasta que Fase 9/operación real defina una fuente de verdad. El contrato ya reserva esos campos para evitar romper páginas futuras. Un conteo desconocido de productos por categoría se representa con `null`, nunca con un cero fabricado.

## 3. Estrategia de slugs

Los slugs públicos se generan exclusivamente en backend mediante `PublicSlug` con formato legible `texto-normalizado-{id}`.

Ejemplos:
- `Cámara Wi-Fi`, id 21 -> `camara-wi-fi-21`
- `Audio y Vídeo`, id 4 -> `audio-y-video-4`

El **id final es la identidad durable**. El backend resuelve la entidad por ese id y devuelve siempre el slug canónico actual. Esto evita depender únicamente del nombre y permite que un enlace con un prefijo antiguo siga resolviendo el recurso si el nombre comercial cambia. No se requiere migración de base de datos ni se introduce una segunda fuente de identidad.

## 4. API pública confirmada

Controlador: `TiendaController`, `[AllowAnonymous]`, prefijo `/tienda`.

Endpoints de Fase 0:
- `GET /tienda/productos`
- `GET /tienda/productos/{slug}`
- `GET /tienda/categorias`
- `GET /tienda/categorias/{slug}`

Reglas:
- catálogo fuerza `Activo=true` y elimina scope de usuario;
- producto inexistente o inactivo responde 404;
- categoría inexistente o inactiva responde 404;
- DTO público no expone costo, auditoría ni información reservada;
- los datos provienen de `IProductoService`/`ICategoriaService`, no de controladores administrativos;
- SKU solo se expone cuando el grupo público no es ambiguo;
- si existen variantes, stock y precio público se calculan solo desde variantes activas.

## 5. Rutas públicas objetivo

Centralizadas en `varistorehn.paths.ts`:
- `/varistorehn`
- `/varistorehn/productos`
- `/varistorehn/categorias`
- `/varistorehn/categoria/:slug`
- `/varistorehn/producto/:slug`
- `/varistorehn/ofertas`
- `/varistorehn/carrito`
- `/varistorehn/checkout`
- `/varistorehn/pedido/:id`

Fase 0 define y protege el contrato de URLs. Las rutas se activarán al existir su página en la fase correspondiente; no se publican placeholders rotos solo para aparentar avance.

## 6. Separación público / administrativo

### Público
- `/` y `/varistorehn` cargan el escaparate.
- Datos de catálogo usan `/tienda/*` anónimo y DTO públicos.
- Modelos públicos viven dentro de la feature VariStoreHn.

### Administrativo
- `/productos`, `/categorias`, `/ventas`, etc. siguen fuera de `/varistorehn`.
- Conservan `authGuard`, `permisoGuard` y contratos administrativos.
- Ningún componente administrativo se importa en `VaristorehnComponent`.

**Regla permanente:** la evolución de la tienda no reutilizará pantallas CRUD de administración como experiencia de compra.

## 7. Estados transversales obligatorios

Contratos definidos:
- consulta: `loading | empty | error | success`
- disponibilidad: `available | lowStock | outOfStock`
- promoción: `none | active | expired`

Cada página futura debe representar explícitamente sus estados relevantes. Ninguna llamada puede sustituir un error de datos reales por fixtures demo de forma silenciosa.

## 8. Datos demo y producción

`VARISTOREHN_CONFIGURACION` establece:
- producción: `utilizarDatosBaseDatos=true`;
- controles de preview: `false` en producción;
- demo: disponible únicamente fuera de producción;
- checkout de tarjeta: no se finge; permanece deshabilitado hasta existir endpoint real y orígenes autorizados.

El carrito demo y el carrito de base de datos utilizan claves de almacenamiento separadas.

## 9. Identidad visual y configuración del sistema

Fase 0 no introduce paleta paralela. VariStoreHn conserva los tokens globales (`--color-*`, radios y demás variables del sistema) y la identidad empresarial central. Los colores configurados por el sistema/base de datos continúan siendo la autoridad visual. ACOSA es exclusivamente referencia de capacidades y flujo.

## 10. Deuda técnica inicial registrada

No bloquea Fase 0 cuando está explícitamente asignada a su fase correcta:
- Fase 1: extraer layout/header público y navegación móvil.
- Fase 2: página de categorías y consumo visual de `CategoriaTienda`.
- Fase 3: grid de catálogo como página independiente y ordenamiento definido por roadmap.
- Fase 4: eliminar detalle principal en modal y usar producto por slug.
- Fase 5: extraer estado único de carrito del componente actual.
- Fase 6: checkout/pedido real; WhatsApp puede cerrar el MVP.
- Fase 9: persistencia/reglas reales de promociones y destacados si negocio las requiere.

No se considera deuda de Fase 0 la implementación visual de páginas que el Plan Maestro asigna expresamente a fases posteriores.

## 11. Pruebas de aceptación de Fase 0

Backend incluye pruebas para:
- controlador público anónimo y rutas esperadas;
- slug legible con acentos y resolución por id estable;
- listado que excluye inactivos;
- contrato comercial (slug, categoría, SKU, precio, estado);
- producto por slug y slug canónico;
- producto inválido/inactivo -> 404;
- categorías públicas activas;
- categoría por slug e inactiva -> 404;
- exclusión de stock/precio perteneciente a variantes inactivas;
- conteos de categoría desconocidos sin falsos ceros.

## 12. Evidencia de validación

Workflow temporal de rama: **VariStoreHn Fase 0 - verificación temporal**, ejecución `34517337336`, sobre HEAD funcional `6b5cbc7f95d7cd33aeff9bdce86427f6d9742b11`.

Resultado:
- [x] `dotnet restore InventoryApp.sln`
- [x] `dotnet build InventoryApp.sln --configuration Release --no-restore`
- [x] `dotnet test InventoryApp.sln --configuration Release --no-build --filter "Category!=Integration"`
- [x] `npm ci`
- [x] `npm run lint`
- [x] `npm run build:prod`
- [x] Job backend: `success`
- [x] Job frontend: `success`

La verificación temporal se retira antes de integrar; el código funcional que validó permanece sin cambios. `Desarrollo - Compilación y pruebas` volverá a validar el resultado combinado al integrarse.

## 13. Gate para iniciar Fase 1

- [x] Modelo público único definido.
- [x] Fuente pública y contratos definidos.
- [x] Slugs y rutas objetivo definidos.
- [x] Separación público/admin acreditada.
- [x] Estados y deuda técnica documentados.
- [x] Backend compila y pruebas pasan.
- [x] Frontend pasa lint/TypeScript y build de producción.
- [x] PR sin conflicto material al momento de la validación.

**Fase 0 queda cerrada técnicamente.** La Fase 1 puede comenzar después de integrar esta base en `Desarrollo` y confirmar el CI de la rama destino.
