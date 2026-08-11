# ERP-N0.4 — Consolidación RBAC relacional

Fecha de cierre técnico: 2026-08-11  
Plan rector: `PLAN_MAESTRO_ERP_V5`  
Rama: `Desarrollo`  
Estado: **✅ CERRADO / CERTIFICADO AUTOMÁTICAMENTE**

---

## 1. Objetivo

Eliminar la doble autoridad persistida del subsistema de autorización y dejar como única fuente de verdad de RBAC la cadena relacional:

```text
Usuario.RolId -> Rol -> RolPermiso.PermisoId -> Permiso
```

ERP-N0.4 completa el backfill de roles y permisos, retira las columnas legacy de autorización, elimina el bypass implícito de administrador como mecanismo de autorización efectiva y conserva los accesos administrativos mediante grants explícitos.

Los enums `ModuloSistema` y `AccionPermiso` permanecen como identificadores técnicos tipados de código y catálogo. No constituyen una segunda autoridad persistida.

---

## 2. Autoridad resultante

La autorización efectiva queda definida exclusivamente por una relación persistida `RolPermiso` válida entre un `RolId` y un `PermisoId`.

Reglas finales:

- un usuario debe resolver un `RolId` relacional válido;
- un rol obtiene acceso únicamente cuando existe un grant explícito `RolPermiso`;
- `EsAdministrador` no concede acceso por bypass;
- los roles administradores activos reciben grants explícitos a los permisos activos del catálogo;
- una denegación se representa mediante ausencia del grant, no mediante una fila `Permitido=false`;
- `Permiso.Modulo` y `Permiso.Accion` mantienen los códigos técnicos necesarios para atributos y resolución tipada;
- `Importar`, `Cerrar` y `Reabrir` forman parte explícita del catálogo RBAC empresarial.

---

## 3. Migración física de base de datos

Migración certificada:

```text
20260811174745_N0_4_ConsolidarRbacRelacional
```

Archivo:

```text
backend/src/Infrastructure/Migrations/20260811174745_N0_4_ConsolidarRbacRelacional.cs
```

### 3.1 Backfill seguro

Antes de retirar columnas legacy, la migración:

1. asegura los roles de sistema requeridos;
2. preserva y normaliza roles dinámicos todavía representados únicamente por `Usuarios.Rol`;
3. completa `Usuarios.RolId` desde el rol legacy cuando corresponde;
4. transforma `RolPermisos.Rol` histórico a `RolId`;
5. transforma `RolPermisos.Modulo + Accion` a `PermisoId`;
6. elimina filas legacy `Permitido=false`, porque la ausencia del grant es la semántica final de denegación;
7. ejecuta guardas fail-closed y aborta antes del DDL destructivo si existe un usuario o grant que no pueda representarse en el modelo relacional.

### 3.2 Retiro físico legacy

Después del backfill y las guardas, se retiran físicamente:

```text
Usuarios.Rol
RolPermisos.Rol
RolPermisos.Modulo
RolPermisos.Accion
RolPermisos.Permitido
```

Además:

- `Usuarios.RolId` queda obligatorio;
- `RolPermisos.RolId` queda obligatorio;
- `RolPermisos.PermisoId` queda obligatorio;
- se preservan las FKs relacionales;
- se conserva la unicidad de `(RolId, PermisoId)`;
- se materializan grants administrativos explícitos.

### 3.3 Preservación del administrador

Durante la revisión de la migración generada inicialmente se detectó que una versión automática intentaba eliminar el usuario administrador histórico `Id=1`.

Ese comportamiento fue rechazado y corregido antes de la certificación. La versión final no elimina al administrador y el CI comprueba explícitamente que el usuario `Id=1` sobrevive y queda asociado al rol `ADMINISTRADOR`.

---

## 4. Preflight y postdeploy

### Preflight

Archivo:

```text
backend/scripts/preflight-erp-n0-4-rbac.sql
```

Valida de forma no destructiva:

- usuarios sin rol representable;
- `RolId` huérfanos;
- grants permitidos sin rol mapeable;
- grants con `PermisoId` inválido;
- combinaciones `Modulo/Accion` legacy sin permiso relacional equivalente.

Resultado certificado:

```text
BloqueosN04 = 0
```

### Postdeploy

Archivo:

```text
backend/scripts/postdeploy-erp-n0-4-rbac.sql
```

Certifica:

- ausencia física de las columnas RBAC legacy;
- usuarios con roles válidos;
- grants con rol y permiso válidos;
- duplicados `(RolId, PermisoId) = 0`;
- grants administrativos completos;
- índice único relacional presente;
- FKs `RolId -> Roles` y `PermisoId -> Permisos` presentes;
- historial EF contiene la migración N0.4.

Resultado certificado:

```text
BloqueosN04 = 0
```

---

## 5. Pruebas automatizadas N0.4

Archivo dedicado:

```text
backend/tests/InventoryApp.Tests/N04RbacRelacionalTests.cs
```

Cobertura relevante:

- sin scope de usuario => denegación fail-closed;
- administrador sin grant explícito => denegado, sin bypass;
- grant explícito => autorización concedida;
- `RolPermiso` no expone campos persistentes legacy;
- `Usuario.Rol` queda únicamente como compatibilidad no persistida y `RolId` es autoridad;
- catálogo contiene `Importar`, `Cerrar` y `Reabrir`;
- cargas masivas usan permiso `Importar`;
- compras, ventas y finanzas incluyen `Cerrar/Reabrir` donde corresponde.

También se migraron las pruebas históricas de `SeedPermisoService` para que validen el modelo relacional final y no reconstruyan entidades con campos legacy eliminados.

---

## 6. CI dedicado de certificación

Workflow:

```text
.github/workflows/erp-n0-4-ci.yml
ERP-N0.4 - Certificación RBAC relacional
```

Certificación funcional canónica:

```text
Commit: 0edc0f68dcd639b8c3494d734edbc6737d4e8134
Run:    31522638499
Job:    93883233396
Estado: SUCCESS
```

Entorno de prueba:

```text
GitHub Actions
Ubuntu 24.04
.NET SDK 8.0.x
MySQL 8.4.11 efímero
Base: inventoryapp_n04_ci
```

### Resultado backend

```text
Build succeeded.
Warnings: 0
Errors:   0

Tests passed: 280
Tests failed: 0
Tests skipped: 0
```

### Resultado de migración

El workflow crea la base efímera desde cero hasta N0.3, siembra un caso RBAC legacy representativo, ejecuta el preflight, aplica N0.4 y ejecuta el postcheck.

Evidencia del run:

```text
Applying migration '20260811174745_N0_4_ConsolidarRbacRelacional'.
Done.
```

Además se comprobaron de forma automática:

- supervivencia del administrador histórico `Id=1`;
- transformación de un grant legacy permitido en grant relacional;
- eliminación de la fila legacy denegada;
- `BloqueosN04 = 0` antes y después según el gate correspondiente;
- consistencia del snapshot EF.

Resultado EF:

```text
No changes have been made to the model since the last migration.
```

---

## 7. Estrategia de reversión

N0.4 es una consolidación estructural sensible a seguridad.

La migración contiene una reconstrucción controlada del formato legacy únicamente cuando los grants existentes pueden representarse sin pérdida en el antiguo esquema `Administrador/Vendedor`. Si existen grants de roles dinámicos no representables, el downgrade se bloquea explícitamente antes de degradar la seguridad o perder información.

Para una reversión operacional real de un entorno persistente debe utilizarse el respaldo y el procedimiento de rollback aprobados para ese entorno. Esta certificación no ejecutó restauraciones ni migraciones contra Producción.

---

## 8. Seguridad y aislamiento

La certificación se ejecutó exclusivamente sobre infraestructura efímera de GitHub Actions.

No se realizó ninguna de las siguientes acciones:

- migrar Aiven Producción;
- migrar Aiven Desarrollo;
- modificar `main`;
- fusionar el PR #2;
- habilitar auto-merge;
- modificar credenciales, variables, dominios, servicios o activos productivos;
- desplegar a Producción.

`main` y `varistorehn_producción` permanecen congelados.

---

## 9. Archivos principales del cierre

```text
backend/src/Infrastructure/Migrations/20260811174745_N0_4_ConsolidarRbacRelacional.cs
backend/src/Infrastructure/Migrations/20260811174745_N0_4_ConsolidarRbacRelacional.Designer.cs
backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs
backend/scripts/preflight-erp-n0-4-rbac.sql
backend/scripts/postdeploy-erp-n0-4-rbac.sql
backend/tests/InventoryApp.Tests/N04RbacRelacionalTests.cs
backend/tests/InventoryApp.Tests/SeedPermisoServiceTests.cs
.github/workflows/erp-n0-4-ci.yml
docs/ERP_N0_4_RBAC_RELACIONAL.md
```

El workflow temporal utilizado únicamente para generar inicialmente la migración fue eliminado del repositorio después de obtener los artefactos EF definitivos.

---

## 10. Dictamen

Con base en la evidencia automatizada reproducible del run `31522638499`, N0.4 cumple el objetivo definido por la auditoría ERP-N0:

```text
BACKFILL RolId/PermisoId:                ✅
LECTURAS/AUTORIZACIÓN RELACIONAL:         ✅
BYPASS ADMIN COMO AUTORIDAD:              RETIRADO ✅
GRANTS ADMIN EXPLÍCITOS:                  ✅
LEGACY RBAC PERSISTIDO RETIRADO:          ✅
PREFLIGHT:                                0 BLOQUEOS ✅
POSTDEPLOY:                               0 BLOQUEOS ✅
BUILD RELEASE:                            0 WARNINGS / 0 ERRORS ✅
TESTS BACKEND:                            280 / 280 ✅
MYSQL 8.4:                                ✅
SNAPSHOT EF:                              CONSISTENTE ✅
PRODUCCIÓN TOCADA:                        NO ✅
MAIN MODIFICADA:                          NO ✅
```

**ERP-N0.4 = ✅ CERRADO / CERTIFICADO AUTOMÁTICAMENTE.**

La siguiente fase secuencial del plan ERP-N0 es **N0.5 — Catálogo administrable de métodos de pago**, respetando el orden del plan rector.
