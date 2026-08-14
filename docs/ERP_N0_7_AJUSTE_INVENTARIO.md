# ERP-N0.7 — AjusteInventario formal

## Dictamen

**Estado documental:** cierre preparado; la promoción operativa de `N0.7.H` a `LISTO` queda condicionada únicamente al cierre exitoso del último job runtime de M13 sobre el SHA funcional certificado.

**SHA funcional de referencia:** `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`.

El preflight `docs/ERP_N0_7_AJUSTE_INVENTARIO_PREFLIGHT.md` permanece como antecedente histórico de diseño. Este documento es la fuente canónica final del comportamiento implementado y de su estrategia de recuperación.

## 1. Contrato canónico

ERP-N0.7 introduce `AjusteInventario` como agregado formal para corregir existencias sin depender de escrituras directas ad hoc.

Ciclo de vida:

```text
Borrador -> Confirmado -> Anulado
```

Reglas principales:

- crear o editar un borrador no modifica existencias;
- confirmar es la operación que materializa el cambio de stock;
- anular un ajuste confirmado crea la reversión operacional; no borra ni reescribe la historia;
- un ajuste confirmado conserva snapshots suficientes para explicar y revertir su impacto;
- una segunda confirmación o segunda anulación falla cerrada;
- los productos con variantes deben identificar una variante concreta;
- no se admite cantidad objetivo negativa ni una confirmación sin diferencia real.

## 2. Persistencia e integridad histórica

Persistencia N0.7 incorporada mediante las migraciones:

- `backend/src/Infrastructure/Migrations/20260814013600_N0_7_AjusteInventarioPersistencia.cs`;
- `backend/src/Infrastructure/Migrations/20260814014000_N0_7_AjusteInventarioVarianteIndex.cs`.

El detalle conserva, al confirmar, snapshots de:

- cantidad anterior;
- cantidad nueva;
- diferencia;
- costo unitario;
- impacto de costo;
- nombre;
- SKU;
- marca;
- modelo;
- color;
- talla.

Los movimientos generados por un ajuste usan origen tipado `OrigenMovimientoInventario.DesdeAjusteInventario(ajuste.Id)`, por lo que `AjusteInventarioId` es la referencia relacional de autoridad para este tipo de movimiento.

## 3. Autoridad única de stock

La única autoridad de negocio que crea/confirma/anula el ajuste y materializa stock es:

`backend/src/Application/Services/AjusteInventarioService.cs`

El servicio concentra:

- bloqueo pesimista del inventario;
- validaciones fail-closed;
- actualización de producto/variante;
- snapshots;
- movimiento tipado;
- auditoría crítica;
- reversión.

N0.7.H detectó antes del cierre que `InventarioAjusteService` todavía constituía una segunda autoridad histórica. La certificación se detuvo y esa ruta se corrigió antes de declarar el punto terminado.

## 4. Compatibilidad legacy sin segunda autoridad

Los endpoints legacy de ajuste directo permanecen temporalmente por compatibilidad API, pero ya no escriben inventario por una ruta independiente.

`backend/src/Application/Services/InventarioAjusteService.cs` es ahora un adaptador puro que depende únicamente de `IAjusteInventarioService` y delega en:

```text
AjustarStockCompatibilidadAsync(productoId, varianteId, request)
```

La compatibilidad legacy mantiene su contrato `AjusteStockResultadoDto`, pero internamente ejecuta de forma atómica:

```text
Crear AjusteInventario -> bloquear inventario -> comprobar stock esperado -> Confirmar AjusteInventario
```

Todo lo anterior ocurre dentro de una sola unidad transaccional del servicio formal.

### Protección de concurrencia legacy

`CantidadActualEsperada` no se degradó a observación. Se compara contra el stock real después de adquirir el lock y antes de cualquier movimiento o mutación.

Si el cliente leyó un stock obsoleto, la operación falla cerrada con conflicto de negocio y no crea movimiento ni cambia existencias.

Esto preserva la semántica de concurrencia del endpoint histórico sin reintroducir una segunda autoridad de stock.

## 5. API y RBAC

La API formal usa permisos granulares del módulo `Inventario`:

- lectura: `Inventario:Ver`;
- creación: `Inventario:Crear`;
- edición: `Inventario:Editar`;
- confirmación: `Inventario:Confirmar`;
- anulación: `Inventario:Anular`.

Los dos endpoints legacy de ajuste directo permanecen autenticados y exigen `Inventario:Confirmar`; se eliminó el bypass histórico basado en `Productos:Editar`.

## 6. Auditoría, seguridad y observabilidad

Las dos operaciones que materializan o revierten stock (`Confirmar` y `Anular`) usan `RegistrarEstrictoAsync` dentro de la misma transacción de base de datos.

Consecuencia: una falla de persistencia de auditoría crítica impide consolidar el cambio de stock.

Crear/editar borradores conservan auditoría tolerante porque no materializan inventario.

La correlación HTTP se implementa mediante `CorrelationIdMiddleware`:

- acepta `X-Correlation-ID` válido de hasta 128 caracteres o genera uno;
- lo asigna a `HttpContext.TraceIdentifier`;
- lo devuelve en la respuesta;
- abre un scope estructurado con `CorrelationId`, método HTTP y ruta.

La aplicación mantiene además las guardas globales de excepción, cabeceras de seguridad, HTTPS/HSTS según entorno, CORS configurado, JWT, rate limiting de login y endpoints de health/readiness ya certificados transversalmente.

## 7. Frontend y UX

La superficie N0.7 permite operar el ciclo formal de ajuste desde frontend: consulta, creación/edición de borrador, confirmación y anulación, respetando el contrato del backend y las validaciones de permisos.

La aceptación integral de `Desarrollo` cubre la aplicación completa con Playwright, por lo que el cierre no se apoya únicamente en pruebas estáticas del servicio.

## 8. QA y regresión

Cobertura específica relevante:

- `backend/tests/InventoryApp.Tests/AjusteInventarioServiceTests.cs`;
- `backend/tests/InventoryApp.Tests/InventarioAjusteServiceTests.cs`;
- `backend/tests/InventoryApp.Tests/N07AjusteInventarioSeguridadRegressionTests.cs`.

La regresión protege, entre otros puntos:

- snapshots y movimiento tipado al confirmar;
- reversión sobre el stock actual sin reescribir historia;
- segunda confirmación/anulación fail-closed;
- compatibilidad legacy ejecutada en una sola transacción formal;
- stale-write legacy rechazado antes de mutar;
- adaptador legacy sin repositorios/UoW/concurrency/auditoría propios;
- endpoints legacy autenticados y protegidos por `Inventario:Confirmar`;
- auditoría estricta para confirmar/anular.

## 9. Evidencia CI del SHA funcional

SHA funcional: `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`.

### CI principal

`Desarrollo - Compilación y pruebas` — run `31808933744` — **SUCCESS completo**:

- Backend Release y pruebas: SUCCESS;
- unitarias no integración: SUCCESS;
- migraciones actuales: SUCCESS;
- integración MySQL 8.4: SUCCESS;
- verificación de variante/cargas/snapshot: SUCCESS;
- SQL forward: SUCCESS;
- frontend lint/build producción: SUCCESS;
- Docker/aislamiento: SUCCESS;
- higiene: SUCCESS.

### Aceptación funcional integral

Run `31808933692` — **SUCCESS completo**:

- backend + base temporal + migraciones: SUCCESS;
- tipado/calidad frontend: SUCCESS;
- Playwright integral: SUCCESS;
- SMTP/PDF: SUCCESS;
- evidencia: SUCCESS.

### M13

Run `31808933833`:

- frontend TypeScript/lint/build: SUCCESS;
- Docker/aislamiento/backup vigente: SUCCESS;
- backend/MySQL/migraciones/snapshot/upgrade histórico: SUCCESS;
- secretos/higiene/dependencias: SUCCESS;
- seguridad HTTP/autorización: SUCCESS;
- **runtime/Playwright: EN PROGRESO al momento de publicar esta versión documental**.

Por ello este documento no promueve por sí solo `N0.7.H` a `LISTO`; el tablero debe hacerlo únicamente cuando el último job M13 confirme `SUCCESS`.

## 10. Corrección descubierta durante N0.7.H

La revisión final evitó certificar una arquitectura incorrecta. La cadena principal de corrección fue:

- `554c9f24902e12388c00e8ca093aa29b533c2ac1` — primer adaptador hacia flujo formal;
- `3416e47e811a2f7c7387bbdaf9964e745a0f6021` — regresión inicial de autoridad única;
- `28a0fe5a945c2071fe160bd208ca9cfc4a07013d` — compatibilidad atómica dentro de la autoridad formal;
- `d0bd3b18f092d189efea5ee69b229bce669387f5` — contrato formal de compatibilidad;
- `f26b7513cfb34ce9a9be54202b2363c1f19e712c` — legacy convertido en adaptador puro;
- `6e17376837e13fb70960da7b523785f54c23b04b` — regresión estructural;
- `7079263f86461bae136b509151da491d2b8bfcbe` — pruebas de atomicidad y stale-write;
- `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850` — pruebas históricas del adaptador alineadas con la arquitectura final.

El run anterior sobre `7079263f...` detectó correctamente un test antiguo que todavía construía `InventarioAjusteService` con las dependencias eliminadas. Se corrigió forward-only en `cd5c1f05...`; el fallo no se ocultó ni se atribuyó a infraestructura.

## 11. Trazabilidad A-H

- **N0.7.A** — auditoría y preflight: alcance, riesgos, transición y no-doble-autoridad.
- **N0.7.B** — dominio/contratos formales.
- **N0.7.C** — persistencia e integridad.
- **N0.7.D** — backend/API/reglas de negocio.
- **N0.7.E** — frontend/UX.
- **N0.7.F** — RBAC, auditoría, seguridad y observabilidad.
- **N0.7.G** — QA, regresión y CI.
- **N0.7.H** — documentación/certificación y eliminación final de la segunda autoridad legacy descubierta en revisión.

## 12. Rollback y recuperación

### Reversión funcional de un ajuste confirmado

No borrar el `AjusteInventario`, no borrar sus movimientos y no restaurar manualmente snapshots como si fueran estado actual.

La operación soportada es **Anular**. La anulación:

1. bloquea inventario;
2. usa la diferencia histórica del ajuste;
3. calcula la reversión sobre el stock actual;
4. evita stock negativo;
5. emite `MovimientoInventario` de tipo reversión con origen tipado al mismo ajuste;
6. conserva el historial y audita estrictamente.

### Rollback de despliegue/código

Si una versión de aplicación debe revertirse antes de Producción, volver exclusivamente a un SHA previamente certificado y compatible con el esquema existente. No usar force-push sobre `Desarrollo`.

### Rollback de esquema

No ejecutar `Down` destructivo sobre una base con ajustes confirmados como mecanismo normal de recuperación. Antes de cualquier rollback de esquema se requiere:

- respaldo/restauración certificables;
- confirmar que no existen datos/referencias N0.7 que serían destruidos;
- validar compatibilidad del código destino;
- preferir corrección forward cuando existan datos históricos.

Producción continúa fuera de alcance y no debe recibir migraciones, restauraciones ni rollback sin autorización formal expresa.

## 13. Riesgo residual y continuidad

Los endpoints legacy todavía existen como superficie HTTP de compatibilidad, pero ya no son autoridad independiente. Su deprecación/eliminación física puede realizarse durante el saneamiento histórico de ERP-N0.8 sin riesgo de mantener dos motores de stock.

Una vez `N0.7.H` quede certificado, el siguiente foco FINISH_FIRST es `N0.8.A` conforme a dependencias VAEP.

## 14. Límites del cierre

Este cierre no autoriza:

- merge a `main`;
- auto-merge del PR #2;
- despliegue a Producción;
- cambios de secretos/dominios/servicios productivos;
- force-push;
- creación de ramas adicionales.
