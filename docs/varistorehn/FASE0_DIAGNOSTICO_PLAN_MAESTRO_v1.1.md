# VariStoreHn — Fase 0: Diagnóstico y base técnica

Documento de trabajo iniciado desde el Plan Maestro de Mejoras v1.1.

## Objetivo
Asegurar que la tienda pública, los datos y las rutas estén preparados antes de rediseñar pantallas.

## Estado de la fase
**En desarrollo.** El escaparate actual ya consume un endpoint público/anónimo y tiene reglas puras reutilizables de catálogo/carrito, pero todavía concentra home, catálogo, detalle, carrito y checkout en un único componente. El detalle principal usa `dialog`, por lo que la arquitectura actual no debe arrastrarse a Fase 4.

## Definition of Done
- [ ] Existe un modelo único de producto y categoría usado por todas las páginas públicas.
- [x] Las rutas públicas objetivo están definidas en este documento.
- [x] Está identificado qué código actual se conserva, qué se refactoriza y qué se elimina.
- [x] La fuente pública actual no requiere componentes administrativos ni endpoints administrativos para leer el catálogo.

## Inventario actual

### Frontend — conservar
- `frontend/src/app/features/varistorehn/varistorehn.catalog.ts`: conservar las reglas puras de normalización, stock, filtros, totales y restauración segura del carrito. Separar posteriormente los contratos de transporte de los modelos públicos canónicos.
- `frontend/src/app/features/varistorehn/varistorehn.service.ts`: conservar como frontera HTTP de la tienda pública. Extenderla con categorías y producto por slug cuando el backend lo soporte.
- `frontend/src/app/features/varistorehn/varistorehn.visual.ts`: conservar solo ilustraciones/iconos realmente reutilizables por el layout público.
- `frontend/src/app/features/varistorehn/varistorehn.config.ts`: conservar configuración de integración y flags no visuales; los controles de preview nunca deben formar parte de producción.

### Frontend — refactorizar
- `frontend/src/app/features/varistorehn/varistorehn.component.ts`: actualmente concentra carga de catálogo, filtros, detalle, carrito, persistencia, WhatsApp y tarjeta. Dividir progresivamente en layout público + páginas + estado/servicios compartidos.
- `frontend/src/app/features/varistorehn/varistorehn.component.html`: separar home, catálogo, detalle y carrito. El `dialog` de producto no puede seguir siendo la experiencia principal del detalle.
- `frontend/src/app/features/varistorehn/varistorehn.component.scss` y `varistorehn.responsive.scss`: conservar tokens/reglas útiles, pero mover estilos con su componente durante las fases 1–6.
- `frontend/src/app/app.routes.ts`: mantener rutas administrativas existentes, pero encapsular el árbol público bajo `/varistorehn` y usar rutas hijas con slugs.

### Backend — conservar
- `backend/src/API/Controllers/TiendaController.cs`: ya es `[AllowAnonymous]`, usa `/tienda` y proyecta solo datos seguros para escaparate.
- `backend/src/Application/DTOs/ProductoCatalogoPublicoDto.cs`: conservar como DTO público de transporte, ampliándolo únicamente con campos comerciales necesarios.

### Eliminar/reemplazar durante el roadmap
- Detalle comercial principal basado en `dialog`/modal: reemplazar por `/varistorehn/producto/:slug` en Fase 4.
- Categorías derivadas únicamente desde `categoriaNombre` del catálogo: reemplazar por contrato de categoría pública con `id`, `name`, `slug` y metadatos mínimos.
- Lógica central de carrito dentro del componente de página: migrarla a un único store/servicio compartido antes de cerrar Fase 5.
- Datos demo mezclados con el flujo real: mantenerlos solo como fixture/preview no productivo, aislados del contrato real.

## Estado de contratos de datos

### Contrato público existente de producto
Hoy expone: `id`, nombre, descripción, categoría por nombre, marca/modelo, precio, cantidad disponible, agotado, imagen principal, imágenes y modelos/variantes. Es una buena base segura, pero no cubre todavía todo el modelo objetivo del Plan Maestro.

### Brechas respecto al modelo objetivo
Pendientes de decidir/implementar antes de depender de ellos en páginas públicas:
- `slug` estable y único de producto.
- `categoryId` y `categorySlug` en la proyección pública.
- contrato público de categoría; hoy no existe dentro de `TiendaController`.
- `sku`/código público si el negocio decide exponerlo.
- `salePrice` y vigencia/regla de promoción; no deben inferirse en distintas pantallas.
- `isFeatured` para destacados dinámicos.
- fecha de creación pública o equivalente si se necesita ordenar por recientes.

**Regla:** no inventar estos campos en el frontend. Deben provenir de una única fuente de verdad del backend o derivarse mediante una regla central explícita y estable.

## Rutas públicas objetivo
- `/varistorehn`
- `/varistorehn/productos`
- `/varistorehn/categorias`
- `/varistorehn/categoria/:slug`
- `/varistorehn/producto/:slug`
- `/varistorehn/ofertas`
- `/varistorehn/carrito`
- `/varistorehn/checkout`
- `/varistorehn/pedido/:id`

### Estado actual de rutas
Actualmente `app.routes.ts` expone `/` y `/varistorehn` al mismo componente monolítico. Las rutas administrativas permanecen fuera del prefijo público y protegidas por guards. No existen aún las rutas públicas hijas del Plan Maestro.

## Fuente de datos confirmada
El frontend usa `GET {apiUrl}/tienda/productos`. `TiendaController` fuerza productos activos, elimina scope de usuario y devuelve una proyección pública. Esto permite mantener separada la lectura del escaparate respecto de `/productos` y otros endpoints administrativos.

## Estados transversales
Toda consulta/página pública debe contemplar explícitamente:
- `loading`
- `empty`
- `error`
- `available`
- `outOfStock`
- `promotion`

El componente actual ya modela carga/error/disponibilidad en parte; estos estados deben trasladarse a los nuevos componentes sin duplicar reglas.

## Backlog técnico para cerrar Fase 0
1. Definir los modelos canónicos públicos de `Producto`, `Categoria`, `Precio/Promocion` y `Disponibilidad`, separados de los DTO HTTP.
2. Definir estrategia de slug estable y unicidad en backend para producto y categoría; evitar slugs efímeros generados solo desde el nombre en el navegador.
3. Diseñar el árbol `varistorehn.routes.ts` y el layout público que usarán Fases 1–6, sin implementar todavía estética fina.
4. Añadir/ajustar endpoints públicos mínimos: categorías y producto por slug; decidir si catálogo soportará filtros server-side o seguirá cargando el conjunto completo durante el MVP.
5. Extraer la persistencia/estado de carrito del componente monolítico hacia una única frontera reutilizable antes de construir la página `/carrito`.
6. Mantener pruebas de seguridad del endpoint público y añadir pruebas para slug, producto inexistente/inactivo y categoría vacía cuando existan los contratos.
7. Verificar que producción no renderiza controles de preview ni datos de ejemplo.

## Orden inmediato de implementación
- **0A Contratos y slugs:** cerrar modelo y fuente de verdad.
- **0B Rutas/layout público:** preparar separación de páginas.
- **0C Estado compartido:** preparar catálogo/carrito reutilizable.
- **0D Pruebas/DoD:** validar seguridad, rutas y estados.

Después de 0D se inicia Fase 1 (navegación pública y header). No se adelanta Fase 4 con un modal nuevo ni se construyen banners/animaciones como prioridad.
