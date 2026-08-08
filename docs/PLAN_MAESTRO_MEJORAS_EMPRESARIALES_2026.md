# Plan Maestro de Mejoras Empresariales — VariApp

Versión: 3.0 — Normalización de catálogos + variantes multidimensionales
Fecha: 2026-08-08
Rama exclusiva: `Desarrollo`
PR oficial: `#2 Desarrollo -> main` (debe permanecer abierto y en borrador)
Producción: congelada

## 1. Principios obligatorios

1. No modificar `main`, no crear ramas nuevas, no fusionar PR #2 y no habilitar auto-merge.
2. No ejecutar migraciones, seeds, restauraciones, despliegues ni cambios de configuración sobre Producción.
3. Toda fase debe comenzar auditando lo existente y debe reutilizar lo correcto sin reconstruirlo innecesariamente.
4. Toda regla crítica de integridad se reforzará en backend y, cuando corresponda, también en MySQL.
5. Ninguna evolución puede reescribir históricos confirmados.
6. Las operaciones de stock, compra, venta, factura, anulación y ajuste se resolverán contra la variante exacta.
7. Los estados técnicos de máquinas de estado no se convertirán en catálogos editables sin necesidad funcional explícita.
8. Los gastos financieros no se modelarán como productos; los insumos administrativos continúan separados de mercadería vendible.
9. Cada fase se cierra con build, pruebas pertinentes, migraciones descartables cuando apliquen, regresión, CI real, documentación y evidencia.
10. No se declarará `completo`, `funciona`, `certificado` o `100 %` sin evidencia técnica real.

## 2. Aclaración funcional definitiva

La arquitectura comercial objetivo queda definida así:

### Producto

Representa la familia o concepto comercial principal. Contiene datos generales no inventariables por combinación, por ejemplo:

- Nombre.
- Categoría.
- Tipo de inventario.
- Descripción.
- Imágenes generales.
- Estado.
- Auditoría.

### Variante

Representa la unidad física exacta de inventario.

Una variante comercial puede combinar:

- Marca.
- Modelo.
- Color.
- Talla/Tamaño.
- SKU.
- Código de barras.
- Cantidad.
- Costo.
- Precio.
- Umbral de stock bajo.
- Imágenes propias.
- Estado.

La cantidad se almacena en la variante exacta y nunca en una dimensión aislada.

Ejemplos:

- Cobertor SPACE + Samsung + S24 Ultra + Negro + Sin talla = 12 unidades.
- Cobertor SPACE + Samsung + S24 Ultra + Azul + Sin talla = 7 unidades.
- Cobertor SPACE + Samsung + S23 Ultra + Negro + Sin talla = 4 unidades.
- Camiseta + Nike + Modelo A + Negro + M = 6 unidades.
- Camiseta + Nike + Modelo A + Negro + L = 9 unidades.

## 3. Normalización obligatoria de catálogos

El diseño final NO utilizará una única tabla genérica `CatalogosProducto` para Marca, Modelo, Color y Talla.

Cada concepto tendrá entidad, tabla, mantenimiento, API y permisos propios.

### 3.1 Tabla `Marcas`

Campos base previstos:

- `Id`.
- `Nombre`.
- `Descripcion`.
- `Orden`.
- `Activo`.
- `Eliminado`.
- auditoría de creación/actualización/eliminación lógica.

Reglas:

- nombre normalizado único entre registros vigentes;
- soft delete;
- no eliminar físicamente registros referenciados históricamente;
- una Marca puede tener múltiples Modelos.

### 3.2 Tabla `Modelos`

Campos base previstos:

- `Id`.
- `MarcaId` FK obligatoria.
- `Nombre`.
- `Descripcion`.
- `Orden`.
- `Activo`.
- `Eliminado`.
- auditoría.

Reglas:

- todo Modelo pertenece a una Marca;
- nombre único dentro de su Marca;
- no activar un Modelo cuya Marca esté inactiva;
- no cambiar de Marca un Modelo usado históricamente sin una regla de migración segura;
- una Marca puede tener muchos Modelos.

Se reforzará la coherencia Marca/Modelo también en el modelo de Variante mediante una FK/constraint compatible que impida asociar una combinación Modelo + Marca incoherente.

### 3.3 Tabla `Colores`

Campos base previstos:

- `Id`.
- `Nombre`.
- `CodigoVisual`.
- `Descripcion`.
- `Orden`.
- `Activo`.
- `Eliminado`.
- auditoría.

Reglas:

- nombre normalizado único;
- código visual validado cuando exista;
- soft delete;
- histórico preservado.

### 3.4 Tabla `Tallas`

Campos base previstos:

- `Id`.
- `Nombre`.
- `Descripcion`.
- `Orden`.
- `Activo`.
- `Eliminado`.
- auditoría.

La tabla podrá representar talla/tamaño comercial según el producto, sin obligar a usarla cuando no corresponda.

Reglas:

- nombre normalizado único;
- soft delete;
- histórico preservado.

### 3.5 Mantenimientos independientes

Existirán mantenimientos independientes y navegables para:

- Marcas.
- Modelos.
- Colores.
- Tallas.

Cada uno tendrá como mínimo:

- listado;
- búsqueda;
- paginación/orden;
- crear;
- editar;
- activar/desactivar;
- eliminación lógica controlada;
- auditoría;
- permisos independientes;
- estados loading/error/empty;
- responsive y accesibilidad.

Se permite reutilizar componentes/servicios base internamente para evitar duplicación, pero la semántica, entidad, tabla y endpoint público de cada mantenimiento serán independientes.

## 4. Migración desde `CatalogosProducto`

La tabla actual `CatalogosProducto` se considera infraestructura heredada/transitoria después de esta aclaración.

La migración será no destructiva y por etapas:

1. Preflight de datos actuales.
2. Crear `Marcas`, `Modelos`, `Colores`, `Tallas`.
3. Copiar datos preservando IDs cuando sea técnicamente seguro.
4. Convertir `CatalogoPadreId` de los Modelos en `Modelos.MarcaId`.
5. Verificar que cada Modelo tenga Marca válida.
6. Actualizar FKs de Producto/Variante hacia las nuevas tablas.
7. Actualizar servicios, repositorios, DTOs y frontend.
8. Mantener compatibilidad temporal mientras existan referencias antiguas.
9. Eliminar dependencia de `CatalogosProducto` del runtime.
10. Retirar la tabla genérica únicamente cuando CI, migración descartable, snapshot EF y regresión demuestren que ya no tiene referencias funcionales.

No se eliminará la tabla genérica prematuramente solo por limpieza estética.

## 5. Identidad definitiva de una variante

La identidad comercial de una variante será:

`Producto + Marca + Modelo + Color + Talla`

Las dimensiones podrán ser nullable cuando el producto no las utilice, excepto que Modelo siempre exige Marca.

Reglas de integridad:

- `ProductoId` obligatorio.
- `ModeloId` implica `MarcaId`.
- el Modelo debe pertenecer a la Marca indicada.
- no puede existir dos veces la misma combinación comercial vigente.
- SKU único globalmente.
- código de barras único globalmente cuando exista.
- soft delete no bloquea reutilización controlada de una combinación eliminada.
- una variante con stock no se elimina físicamente.
- la variante técnica continúa separada y conserva su unicidad por Producto.

La unicidad multidimensional se reforzará en MySQL con columnas generadas/normalizadas o mecanismo equivalente que trate `NULL` de forma determinista y excluya variantes técnicas/eliminadas del índice comercial único.

## 6. Fuente de verdad

Después de M2:

- `ProductoVariante.Cantidad` = fuente real de stock.
- `ProductoVariante.Costo` = costo operativo de esa combinación.
- `ProductoVariante.Precio` = precio operativo de esa combinación.
- `ProductoVariante.UmbralStockBajo` = umbral de esa combinación.
- `Producto.Cantidad` = resumen derivado/consolidado.
- `Producto.Costo` = resumen/valoración derivada.
- `Producto.Precio` = resumen informativo; la venta usa el precio de la variante.

Los campos globales heredados de Marca/Modelo/Color/Talla en Producto se mantendrán temporalmente solo por compatibilidad y se retirarán de la fuente de verdad.

## 7. Etiqueta canónica de variante

Toda VariApp utilizará una única representación de variante:

`Marca · Modelo · Color · Talla · SKU`

Solo se muestran atributos existentes.

Se reutilizará en:

- Productos.
- Compras.
- Ventas.
- Inventario.
- Facturación.
- PDF.
- impresión.
- correo/WhatsApp cuando aplique.
- escáner.
- autocomplete.
- cargas masivas.
- reportes.
- auditoría.

---

# FASE M0 — Auditoría y mapa de impacto

Estado: COMPLETADA.

La presente versión 3.0 actúa como aclaración funcional posterior a M0 y reemplaza cualquier conclusión de M0 que asumiera que `CatalogoProducto` debía conservarse como arquitectura final para Marca/Modelo/Color/Talla.

# FASE M1 — Normalización de catálogos maestros

Objetivo: crear la base normalizada que M2 necesita antes de mover todas las dimensiones a Variante.

## M1.A — Diseño y preflight

- inventariar todos los usos de `CatalogoProducto`;
- mapear FKs actuales;
- detectar IDs inválidos;
- validar Modelo -> Marca;
- detectar nombres duplicados por tipo;
- mapear permisos, endpoints, Angular y pruebas existentes;
- preparar estrategia de compatibilidad y rollback.

## M1.B — Entidades y tablas independientes

Crear:

- `Marca` / `Marcas`.
- `Modelo` / `Modelos`.
- `Color` / `Colores`.
- `Talla` / `Tallas`.

Agregar configuraciones EF, índices, FKs y query filters de soft delete.

## M1.C — Migración/backfill

- copiar datos desde `CatalogosProducto`;
- preservar IDs cuando sea seguro;
- convertir `CatalogoPadreId` a `Modelo.MarcaId`;
- validar conteos y checksums lógicos;
- ejecutar sobre MySQL descartable;
- validar EF snapshot y SQL forward.

## M1.D — Backend independiente

Crear/adaptar por dominio:

- repositories;
- services;
- DTOs;
- validators;
- controllers/endpoints;
- permisos;
- auditoría.

Rutas objetivo conceptuales:

- `/api/marcas`.
- `/api/modelos`.
- `/api/colores`.
- `/api/tallas`.

## M1.E — Frontend de mantenimientos

Crear/adaptar mantenimientos independientes para los cuatro catálogos.

Modelo incluirá selección obligatoria de Marca y filtro por Marca.

## M1.F — Compatibilidad y retirada del catálogo genérico

- migrar consumidores actuales;
- bloquear nuevos usos de `CatalogoProducto` en código nuevo;
- retirar dependencia runtime;
- eliminar tabla/entidad genérica solamente cuando no queden referencias funcionales y las pruebas sean verdes.

## M1.G — Certificación

- backend Release;
- unitarias;
- integración MySQL;
- migración desde historial realista;
- frontend lint/build;
- E2E de Marcas/Modelos/Colores/Tallas;
- permisos;
- auditoría;
- CI real.

# FASE M2 — Motor de variantes multidimensionales

Objetivo: convertir `ProductoVariante` en la unidad exacta de inventario para Marca + Modelo + Color + Talla.

## M2.A — Dominio de Variante

Agregar a `ProductoVariante`:

- `MarcaId`.
- `ModeloId`.
- `ColorId`.
- `TallaId`.
- navegaciones a las cuatro tablas normalizadas.

Mantener:

- SKU.
- código de barras.
- cantidad.
- costo.
- precio.
- umbral.
- estado.
- soft delete.
- variante técnica.

## M2.B — Integridad MySQL

- FK Variante -> Marca.
- FK Variante -> Modelo.
- FK Variante -> Color.
- FK Variante -> Talla.
- garantía Modelo pertenece a Marca mediante diseño relacional compatible.
- índice único multidimensional comercial.
- índice único SKU.
- índice único código de barras cuando aplique.
- unicidad de variante técnica por Producto.

## M2.C — Migración/backfill de variantes

Para variantes existentes:

- conservar `ProductoVariante.Id`;
- conservar Color actual;
- heredar Marca/Modelo/Talla legacy desde Producto cuando corresponda;
- preservar SKU/barcode/stock/costo/precio;
- detectar colisiones antes de aplicar;
- abortar fail-closed si dos filas terminarían representando la misma combinación.

## M2.D — DTO/API/Servicios

Actualizar todos los contratos de Producto/Variante para devolver Marca, Modelo, Color y Talla de la variante.

Corregir además la inconsistencia de SKU:

- SKU manual permitido;
- SKU omitido -> backend genera uno único;
- frontend no garantiza unicidad.

## M2.E — Nuevo constructor de variantes en Productos

La sección actual `Colores y existencias` se reemplaza por `Variantes y existencias`.

Cada variante permitirá:

- Marca.
- Modelo dependiente de Marca.
- Color.
- Talla/Tamaño.
- Cantidad.
- SKU.
- código de barras.
- costo.
- precio.
- umbral.

La información general del Producto dejará de imponer una única Marca/Modelo/Talla/Color como autoridad.

Mejoras UX obligatorias:

- `Agregar variante`.
- copiar valores de la fila anterior.
- validación inmediata de duplicados.
- etiqueta canónica en vivo.
- resumen de stock total.
- resumen por dimensión.
- errores por fila.
- responsive.
- teclado/accesibilidad.

### Generador de combinaciones

Para productos con muchas combinaciones se añadirá una herramienta que permita seleccionar múltiples:

- Modelos.
- Colores.
- Tallas.

Y generar las combinaciones válidas para revisión antes de guardar, evitando capturar manualmente decenas de filas.

No se generarán combinaciones automáticamente sin confirmación del usuario.

## M2.F — Administrador de variantes

Permitirá:

- alta;
- edición de atributos;
- activar/desactivar;
- ajuste de stock separado;
- soft delete con stock cero;
- filtros por Marca/Modelo/Color/Talla/SKU/barcode/estado;
- historial/auditoría;
- stock bajo/agotar.

## M2.G — Imágenes por variante

Extender imágenes para soportar:

- galería general de Producto;
- galería específica de Variante;
- imagen principal por ámbito;
- orden;
- preview;
- reemplazo;
- eliminación segura;
- fallback automático a imagen general del Producto.

## M2.H — Compras

Compras seleccionará `ProductoVarianteId` exacto.

Mostrar:

`Producto · Marca · Modelo · Color · Talla · SKU`

Al confirmar:

- aumentar solo esa variante;
- actualizar costo de esa variante según política;
- registrar movimiento exacto;
- snapshot Marca/Modelo/Color/Talla/SKU;
- anulación revierte la misma variante;
- locks/concurrencia sobre la variante correcta.

## M2.I — Ventas

Ventas seleccionará variante exacta.

- stock de una variante no puede utilizarse para cubrir otra;
- precio/costo salen de la variante;
- descuento/impuesto/envío permanecen compatibles;
- anulación devuelve stock a la variante original;
- concurrencia evita sobreventa.

## M2.J — Facturación y documentos

Ampliar snapshots históricos con:

- Marca.
- Modelo.
- Color.
- Talla.
- SKU/barcode cuando corresponda.

Actualizar:

- FacturaDetalle.
- vista.
- PDF.
- impresión oficina/POS.
- correo.
- WhatsApp/compartición cuando incluya detalle.

Renombrar o desactivar un catálogo nunca modifica una factura confirmada.

## M2.K — Inventario y movimientos

MovimientoInventario conservará snapshots de:

- Marca.
- Modelo.
- Color.
- Talla.
- SKU.

Reportará stock anterior/nuevo por variante exacta.

## M2.L — Productos/listados/detalle

Actualizar:

- detalle de Producto;
- listado;
- tarjetas de variantes;
- filtros;
- badges;
- precios mínimos/máximos;
- imágenes;
- resumen de stock.

## M2.M — Escáner/autocomplete/búsqueda

Los resultados deberán ser inequívocos e incluir dimensiones completas.

Búsqueda por:

- Producto.
- Marca.
- Modelo.
- Color.
- Talla.
- SKU.
- código de barras.

## M2.N — Cargas masivas

Actualizar `VariantesInventario` para soportar:

- Producto.
- Marca.
- Modelo.
- Color.
- Talla.
- SKU.
- barcode.
- cantidad.
- costo.
- precio.
- umbral.

Validar:

- Modelo pertenece a Marca;
- catálogos activos;
- combinación duplicada;
- SKU/barcode duplicados;
- valores numéricos;
- errores antes de confirmar.

## M2.O — Dashboard/reportes/exportaciones

Agregar capacidades de análisis por:

- Marca.
- Modelo.
- Color.
- Talla.
- combinación exacta.

Evitar doble conteo entre Producto y Variante.

## M2.P — Permisos/auditoría

Toda operación sensible de variante y mantenimientos deberá ser auditable y respetar permisos.

## M2.Q — Regresión y certificación M2

Validar como mínimo:

- producto simple/variante técnica;
- producto con un Modelo y varios Colores;
- producto con varios Modelos;
- Color + Talla;
- varios Modelos + Colores + Tallas;
- compra;
- anulación compra;
- venta;
- anulación venta;
- stock concurrente;
- facturación/PDF;
- scanner/autocomplete;
- carga masiva;
- imágenes por variante;
- históricos;
- permisos;
- MySQL migration/backfill;
- frontend responsive/accesibilidad;
- CI real.

# FASE M3 — Certificación ISV/ISC

La infraestructura fiscal persistente ya existe. M3 se concentrará en regresión, persistencia tras reinicio, snapshots históricos, seeds idempotentes y corrección solo si aparece una falla real.

# FASE M4 — Filtros y navegación persistente

Implementar patrón reutilizable query params + sessionStorage para búsqueda, filtros, orden, página y pageSize en Productos, Compras, Ventas, Clientes, Inventario y Finanzas, con `Limpiar filtros`.

Los nuevos filtros de variante Marca/Modelo/Color/Talla deberán integrarse.

# FASE M5 — Clientes y segmentación

Extender TipoCliente existente con filtros, KPIs, reportes, exportaciones y segmentación sin recrear `TipoCliente`.

# FASE M6 — Mercadería / Insumos / Gastos

Completar UX, reportes y valoración manteniendo:

- mercadería vendible;
- insumo administrativo inventariable no vendible;
- gasto financiero sin stock.

# FASE M7 — Costos de envío profesionales

Extender `CostoEnvio` con geografía/modalidad cuando aplique y resolver integridad concurrente del predeterminado único a nivel BD, preservando snapshots históricos.

# FASE M8 — Búsqueda y rendimiento

Ampliar búsqueda transversal incluyendo las cuatro dimensiones normalizadas. Medir p50/p95 antes de crear índices adicionales.

# FASE M9 — Cargas masivas profesionales

Elevar la infraestructura existente con progreso, versionado de plantillas, lotes, cancelación segura cuando sea viable y cobertura completa del modelo de variante M2.

# FASE M10 — UI empresarial

Normalizar tokens, componentes, estados, responsive, foco, teclado y WCAG. El constructor multidimensional de variantes será parte crítica de esta revisión.

# FASE M11 — Backup y restauración

Diseñar backup/restore verificable exclusivamente en Desarrollo/infraestructura descartable durante este ciclo.

# FASE M12 — Automatización transversal

Reducir captura repetitiva mediante defaults, sugerencias, generador de combinaciones y acciones masivas seguras, manteniendo determinismo y auditoría.

# FASE M13 — Auditoría integral y certificación final

Revisar arquitectura, seguridad, integridad, concurrencia, migraciones, rendimiento, UX, accesibilidad, documentación, dependencias, secretos/logs, backups y regresión completa.

## Orden oficial actualizado

`M0 -> M1 -> M2 -> M3 -> M4 -> M5 -> M6 -> M7 -> M8 -> M9 -> M10 -> M11 -> M12 -> M13`

Dependencia obligatoria:

`M1 normalización de Marcas/Modelos/Colores/Tallas` debe cerrar antes de implementar la migración definitiva de `ProductoVariante` en M2.

## Criterio de diseño final

Al finalizar M2, el sistema deberá cumplir simultáneamente:

- Marca tiene mantenimiento propio y tabla `Marcas`.
- Modelo tiene mantenimiento propio y tabla `Modelos`.
- Color tiene mantenimiento propio y tabla `Colores`.
- Talla tiene mantenimiento propio y tabla `Tallas`.
- Modelo depende relacionalmente de Marca.
- Marca, Modelo, Color y Talla son atributos operativos de `ProductoVariante`.
- el stock pertenece a la combinación exacta.
- Compras, Ventas, Inventario y Facturación operan contra `ProductoVarianteId`.
- documentos históricos conservan snapshots completos.
- la antigua tabla genérica `CatalogosProducto` deja de ser fuente de verdad y se retira cuando ya no tenga dependencias runtime.
