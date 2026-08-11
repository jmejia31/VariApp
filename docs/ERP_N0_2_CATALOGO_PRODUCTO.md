# ERP-N0.2 — Retiro de `CatalogoProducto` legacy

Fecha de cierre técnico: 2026-08-10  
Rama: `Desarrollo`  
Estado: **COMPLETADA / CERTIFICADA**

## 1. Objetivo

Eliminar `CatalogoProducto` como entidad, tabla y fuente runtime sin romper los contratos HTTP existentes ni los históricos válidos. Desde N0.2, Marca, Modelo, Color y Talla persisten exclusivamente en sus tablas normalizadas y se consultan siempre de forma tipada.

Este documento cierra el hallazgo F-N0-002 de `ERP_N0_LEGACY_AUDIT.md`. La auditoría N0.0 se conserva como evidencia histórica del estado previo y no se reescribe retrospectivamente.

## 2. Autoridad final

Las únicas fuentes persistentes administrables de dimensiones son:

- `Marcas`
- `Modelos`, con `MarcaId` obligatorio
- `Colores`
- `Tallas`

`ProductoVariante` continúa siendo la autoridad operacional de dimensiones, SKU/código de barras, stock, costo, precio y umbral de variante. N0.2 no revierte la autoridad establecida por N0.1.

`Producto` conserva proyecciones familiares de compatibilidad (`MarcaId`, `ModeloId`, `ColorId`, `TallaId`), pero desde N0.2 esas FKs apuntan directamente a las tablas normalizadas. Los textos `Marca` y `Modelo` siguen siendo snapshots/compatibilidad; no son un catálogo administrable paralelo.

## 3. Eliminación del stack persistente legacy

Se retiraron del modelo runtime:

- `backend/src/Domain/Entities/CatalogoProducto.cs`
- `CatalogoProductoConfiguration`
- `AppDbContext.CatalogosProducto`
- la tabla física `CatalogosProducto`
- las cuatro FKs de `Productos` hacia `CatalogosProducto`
- la escritura espejo que mantenía IDs globales entre el catálogo polimórfico y las tablas normalizadas

La migración final es:

`20260811013917_N0_2_RetirarCatalogoProductoLegacy`

Su `Up`:

1. elimina las cuatro FKs de `Productos` al catálogo legacy;
2. elimina `CatalogosProducto`;
3. conecta `Productos.ColorId -> Colores.Id`;
4. conecta `Productos.TallaId -> Tallas.Id`;
5. conecta `Productos.MarcaId -> Marcas.Id`;
6. conecta `Productos.ModeloId -> Modelos.Id`;
7. mantiene `SET NULL` como política de eliminación para las proyecciones familiares.

## 4. Migración forward-only

El `Down` generado automáticamente por EF no era seguro: recreaba una tabla `CatalogosProducto` vacía y pretendía volver a conectar FKs a ella.

Eso es inválido después de N0.2 porque Marca, Modelo, Color y Talla utilizan espacios de identidad independientes. No existe una reconstrucción determinista de un único espacio global de IDs sin reintroducir colisiones o modificar datos normalizados.

Por esa razón, N0.2 es explícitamente **forward-only**. El método `Down` falla de forma cerrada con `NotSupportedException` y exige restaurar un respaldo tomado antes de N0.2 si fuese necesario regresar al esquema anterior.

## 5. Compatibilidad HTTP y frontend

Los nombres públicos `CatalogoProductoService`, `ICatalogoProductoService`, `ICatalogoProductoRepository`, DTOs y `CatalogoProductoControllerBase` se conservan como **fachada de compatibilidad de contrato**, no como entidad persistente.

Esto evita una ruptura innecesaria de:

- rutas `marcas`, `modelos`, `colores`, `tallas`;
- componentes Angular compartidos de mantenimiento;
- contratos DTO existentes.

Internamente, el repositorio y servicio operan contra entidades normalizadas tipadas.

## 6. Corrección de colisiones de IDs

El modelo legacy usaba un único espacio global de IDs. Las tablas normalizadas utilizan identidades independientes, por lo que `Marca.Id = 10` y `Color.Id = 10` pueden coexistir correctamente.

N0.2 elimina cualquier lookup ambiguo `GetById(id)` sobre un catálogo polimórfico. Las búsquedas relevantes reciben también `TipoCatalogoProducto`, por ejemplo:

- `(Marca, 10)`
- `(Color, 10)`

La suite incluye una prueba explícita de IDs solapados para impedir regresiones de este tipo.

## 7. Producto y navegaciones normalizadas

Las navegaciones familiares de `Producto` son ahora:

- `MarcaCatalogo : Marca`
- `ModeloCatalogo : Modelo`
- `ColorCatalogo : Color`
- `TallaCatalogo : Talla`

Los aliases internos `Color` y `Talla` se conservan únicamente como propiedades no persistentes para consumidores históricos de código y son ignorados explícitamente por EF.

`ProductoRepository` realiza `Include` únicamente sobre las navegaciones persistentes normalizadas.

## 8. Carga masiva

`CargaMasivaService` dejó de consultar o escribir `CatalogosProducto`.

Cambios principales:

- validación de productos usa `Marcas`, `Modelos` y `Tallas` normalizados;
- Modelo se valida por `Modelo.MarcaId`, no por un padre polimórfico;
- importación de colores escribe únicamente `Colores`;
- importación de productos asigna IDs normalizados de Marca/Modelo/Talla;
- no existe escritura espejo legacy.

## 9. Preflight y postcheck

Preflight:

`backend/scripts/preflight-erp-n0-2-catalogos-normalizados.sql`

Valida antes del DROP:

- equivalencia legacy -> maestro normalizado;
- alineación Modelo -> Marca;
- ausencia de FKs huérfanas en `Productos`;
- ausencia de FKs huérfanas en `ProductoVariantes`.

Criterio: `Bloqueos = 0`.

Postcheck:

`backend/scripts/postdeploy-erp-n0-2-catalogos-normalizados.sql`

Valida después de N0.2:

- `CatalogosProducto` no existe;
- FKs familiares de producto resuelven contra maestros normalizados;
- FKs de variantes siguen íntegras;
- `Modelos.MarcaId` sigue íntegro.

Criterio: `ErroresN02 = 0`.

## 10. Certificación automatizada

Workflow permanente:

`.github/workflows/erp-n0-2-ci.yml`

Gate funcional exitoso de referencia:

- run: `31450333907`
- SHA funcional: `95bf467c8eba9e2ab204a4010e9678d4c4e9d51b`
- MySQL: 8.4
- build Release con `-warnaserror`: aprobado
- backend: **270/270 pruebas aprobadas**
- esquema inmediatamente anterior a N0.2: creado desde migraciones EF
- dataset representativo legacy + normalizado: sembrado
- preflight: `0`
- migración N0.2: aplicada
- postcheck: `0`
- relaciones Marca/Modelo/Color/Talla del producto representativo: conservadas
- `dotnet ef migrations has-pending-model-changes`: aprobado
- guardas estáticas de entidad/DbSet/configuración/acceso runtime legacy: aprobadas

El workflow se ejecuta también en cada push a `Desarrollo`, por lo que el SHA final de documentación debe quedar verde antes de considerar cerrado el punto.

## 11. Alcance deliberadamente conservado

N0.2 **no** elimina:

- nombres públicos de servicio/DTO/controlador usados como fachada compatible;
- snapshots históricos válidos;
- proyecciones familiares de `Producto` necesarias por compatibilidad;
- `ProductoVariante`, que sigue siendo autoridad operacional;
- ninguna estructura de N0.3 en adelante.

El hecho de que una clase pública conserve la palabra `CatalogoProducto` no significa que exista la entidad o tabla legacy. Las guardas de CI distinguen contrato nominal de persistencia real.

## 12. Criterios de salida

N0.2 se considera cerrada únicamente si se cumplen simultáneamente:

- [x] entidad persistente `CatalogoProducto` eliminada;
- [x] configuración EF legacy eliminada;
- [x] `DbSet<CatalogoProducto>` eliminado;
- [x] escritura espejo eliminada;
- [x] consumidores de carga masiva migrados;
- [x] lookups de maestro tipados;
- [x] Producto conectado a maestros normalizados;
- [x] migración EF + snapshot actualizados;
- [x] migración forward-only segura;
- [x] preflight y postcheck disponibles;
- [x] build sin warnings/errors;
- [x] suite backend completa verde;
- [x] migración probada en MySQL 8.4 con datos representativos;
- [x] snapshot EF sin cambios pendientes;
- [x] guardas estáticas legacy verdes;
- [x] producción no tocada;
- [x] `main` no modificada ni fusionada desde este trabajo.

## 13. Siguiente punto

El siguiente punto lógico es **ERP-N0.3 — endurecimiento final de `ProductoVariante` y autoridad operativa única**.

N0.3 no forma parte de este cierre y no debe darse por iniciado por el solo hecho de cerrar N0.2.
