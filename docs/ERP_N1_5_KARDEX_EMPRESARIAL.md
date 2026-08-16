# ERP-N1.5 — Kardex empresarial

## 1. Alcance y objetivo

ERP-N1.5 consolida el Kardex empresarial como historial auditable de movimientos de inventario. Cada movimiento debe conservar, cuando aplique, la variante, almacén, ubicación, cantidades antes/movimiento/después, costo, fecha, documento origen, usuario y un `CorrelationId` durable.

El Kardex no reemplaza la autoridad de stock vivo de `ExistenciaVariante`; registra la evidencia histórica de las operaciones que modifican inventario y permite consultarla de forma paginada y filtrable.

## 2. Contrato de movimiento

La trazabilidad empresarial preserva los siguientes ejes:

- producto y `ProductoVarianteId`;
- `AlmacenId` y `UbicacionAlmacenId` cuando la operación dispone de contexto físico real;
- cantidad anterior, cantidad del movimiento y cantidad posterior;
- costo y valor económico cuando corresponda;
- fecha y usuario responsable;
- origen tipado del documento (`Compra`, `Venta`, `Consumo`, `Ajuste` y otros orígenes soportados);
- `CorrelationId` durable para agrupar y rastrear una operación extremo a extremo.

Nunca se inventan `AlmacenId` o `UbicacionAlmacenId` para operaciones legacy que todavía no proporcionan dimensión física. En esos casos se preserva la compatibilidad nullable y se mantiene la trazabilidad disponible sin fabricar contexto.

## 3. Escritura canónica y correlación

N1.5.D consolidó `IKardexMovimientoWriter` como contrato canónico de escritura del Kardex. Las operaciones de Compra, Venta y Consumo usan correlación determinística para sus transiciones relevantes, de forma que confirmar y anular sean distinguibles y rastreables.

La estrategia de correlación evita persistir identificadores arbitrarios no saneados y permite relacionar la auditoría, la petición HTTP y los movimientos generados por una misma operación.

La persistencia de `CorrelationId` cuenta con longitud máxima controlada e índice orientado a consulta temporal. La migración correspondiente es reversible y fue validada en MySQL controlado.

## 4. API de consulta

El backend expone consulta empresarial paginada del Kardex con filtros orientados a las dimensiones reales del movimiento. La implementación mantiene:

- paginación server-side;
- orden estable por fecha;
- límites de `Page` y `PageSize`;
- filtros por contexto físico y origen;
- filtro por causa/correlación cuando corresponde;
- contratos de error compatibles con el manejo HTTP del proyecto;
- aislamiento por `UsuarioScope` y permisos relacionales.

N1.5.G añadió índices compuestos alineados con los filtros y el orden de consulta para evitar degradaciones previsibles por scans innecesarios. La migración `20260816005000_N1_5_KardexQueryIndexes` mantiene rollback explícito.

## 5. Seguridad, RBAC, auditoría y observabilidad

La lectura del Kardex es fail-closed: un usuario no administrativo debe quedar restringido por su `UsuarioScope`, y la ausencia de scope resoluble no concede acceso implícito.

La observabilidad mantiene el identificador de correlación saneado a través del pipeline. La auditoría no persiste directamente un header inseguro: utiliza el identificador de trazabilidad validado por el runtime.

N1.5.F incorporó regresiones para:

- aislamiento del Kardex por usuario;
- comportamiento fail-closed cuando el scope no puede resolverse;
- propagación segura de `CorrelationId`;
- sustitución/rechazo de identificadores inseguros;
- auditoría consistente con el identificador saneado.

## 6. Frontend y UX

N1.5.E cortó la UI del Kardex hacia la consulta paginada real del backend. La pantalla empresarial incluye:

- filtros por dimensiones físicas/origen/causa/correlación/fechas;
- selectores dependientes;
- paginación server-side;
- estados de loading, vacío y error;
- tabla responsive;
- controles de accesibilidad y permisos de UI.

La cobertura Playwright certificó el flujo sobre API y MySQL descartables, evitando mocks como criterio único de aceptación de la experiencia.

## 7. Evidencia técnica A–G

### N1.5.A — Auditoría y preflight

Preflight canónico certificado en `d42aec2c6168d49000db8378d144b2ea3ab904fb` con CI Desarrollo `#31904962826` en `SUCCESS`.

### N1.5.B — Dominio y contratos

Cierre certificado en `625ba5a3777e0b8ffb38ecba1ea3fa1956270029`; CI `#31905282056` en `SUCCESS`.

### N1.5.C — Persistencia, migración y datos

Cierre certificado en `55dbaa334ac6bbf236f6b5f376a0dcd69f2d2354`. Evidencia principal:

- CI Desarrollo `#31911214659` `SUCCESS`;
- aceptación integral `#31911214717` `SUCCESS`;
- Fase 8 `#31911214610` `SUCCESS`;
- recuperación MySQL `#31911214658` `SUCCESS`.

### N1.5.D — Aplicación, servicios y API

Cierre certificado en `6f4a3cb8f7e854c40569db08fe2f9dd05aca126f`. CI Desarrollo push `#31916568532` y Fase 8 `#31916568530` finalizaron en `SUCCESS`.

El punto consolidó writers canónicos con `CorrelationId` determinístico y consulta paginada/filtros físicos/origen/causa.

### N1.5.E — Frontend y UX

Cierre certificado en `dd510978a16675b1839998ab8156e63f469a0b78`. Frontend producción de CI `#31917459336` finalizó `SUCCESS`; M10 `#31917459334` finalizó `SUCCESS` con Playwright del Kardex.

### N1.5.F — RBAC, auditoría, seguridad y observabilidad

Cierre certificado en `75196d80602dc1c0d715b6b52069267d0cf6282c`. CI push `#31917739811` y RBAC relacional `#31917739885` finalizaron `SUCCESS`.

### N1.5.G — QA, regresión y CI

La base funcional de cierre quedó en `4871da115e72d205513ea23aa9fe95c1e4818e6b`, tree `3472011576fbb6f2114fab15d16016e2c83d5d43`.

La QA detectó que los filtros/orden del Kardex no tenían índices compuestos suficientemente alineados. Se corrigió causalmente mediante índices para:

- producto + variante + fecha;
- almacén + ubicación + fecha;
- orígenes tipados + fecha.

También se publicó la migración reversible `20260816005000_N1_5_KardexQueryIndexes`.

`Desarrollo - Compilación y pruebas #31918223873` terminó `SUCCESS` sobre exactamente ese HEAD, incluyendo backend/unitarias, frontend producción, Docker, higiene, migraciones e integración MySQL.

La aceptación integral posterior descubrió una regresión de harness en `frontend/e2e/fase5-imagenes.spec.ts`: el fixture de `/inventario/movimientos` seguía devolviendo el contrato legacy como arreglo mientras la UI N1.5 ya consume `PagedResult`. La aplicación y el DTO continuaban exponiendo `ProductoImagenPrincipalUrl`; el fallo era del mock de regresión, no de la proyección productiva. Se corrigió en `bb2cc9753063a688796979897857d3f57364257a`, alineando el fixture con `items/totalCount/page/pageSize` y congelando además variante, causa, origen tipado y correlación del Kardex. Este commit debe quedar certificado por los pipelines causales antes del cierre formal de N1.5.H.

## 8. Rollback y operación

Rollback de código:

1. revertir explícitamente commits causales en `Desarrollo`;
2. no usar force-push ni reescritura de historia;
3. verificar nuevamente CI y compatibilidad de contratos después de cada reversión.

Rollback de datos/migraciones:

1. no ejecutar rollback destructivo automáticamente en Producción;
2. validar la dependencia de consultas e índices antes de retirar una migración;
3. conservar `CorrelationId` y el historial de movimientos ya persistidos;
4. ejecutar cualquier cambio productivo sólo mediante procedimiento operativo separado y autorización explícita.

## 9. Restricciones de despliegue

Este documento certifica ERP-N1.5 exclusivamente en `Desarrollo`. No autoriza:

- merge a `main`;
- auto-merge del PR #2;
- cambios en Producción;
- nuevas ramas;
- force-push;
- modificación de secretos o infraestructura productiva.

PR #2 debe permanecer abierto y Draft.

## 10. Criterio de cierre

ERP-N1.5 puede cerrarse cuando este documento quede publicado en `Desarrollo`, el CI causal del changeset documental y la aceptación del fix `bb2cc9753063a688796979897857d3f57364257a` queden reconciliados, y el tablero VAEP registre N1.5.H como `LISTO` con commit/evidencia y `RESUME_POINT` dirigido al siguiente punto elegible.
