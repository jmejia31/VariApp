# PLAN DE EJECUCIÓN AUTÓNOMA — VAEP v2.1 FINISH_FIRST

> VariApp Autonomous Execution Protocol. Fuente rectora: **Plan Maestro ERP V5 — VariApp**. Fuente operativa: Google Sheets. Autoridad técnica y evidencia: GitHub `jmejia31/VariApp`, rama `Desarrollo`.

## 1. Identidad y fuentes obligatorias

- `PROJECT_ID`: `VARIAPP`
- Repositorio: `jmejia31/VariApp`
- Rama única: `Desarrollo`
- PR oficial: `#2 Desarrollo -> main`, siempre abierto y Draft hasta autorización expresa de Javier Mejía.
- Plan rector en Drive: https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit
- Tablero VAEP v2: https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit
- GitHub prevalece para código, commits, CI, arquitectura y evidencia verificable.

El `.docx` original fue convertido a Google Docs para que el runner consulte permanentemente la fuente rectora sin depender de una conversación concreta.

## 2. Cobertura integral del Plan Maestro ERP V5

VAEP cubre ERP-N0→N9 y los tracks T0–T12. Las funcionalidades futuras no-core —RRHH, CRM, MRP, activos fijos, proyectos, servicio técnico, logística avanzada y ecommerce futuro— permanecen `NO_AUTORIZADO` y no pueden autoejecutarse sin autorización explícita de Javier.

El tablero contiene `DASHBOARD`, `COLA`, `PLAN_MAESTRO`, `CONFIG`, `BITACORA` y `LEYENDA`.

## 3. Granularidad obligatoria

Ningún agente debe resolver un punto ERP grande en un único changeset. Salvo descomposición específica, cada punto se divide en:

1. `PRE`: auditoría/preflight, alcance, riesgos, dependencias, rollback y criterios.
2. `DOMAIN`: dominio, invariantes y contratos.
3. `DB_MIG`: persistencia, constraints, índices, migración/backfill/reconciliación/rollback.
4. `BACKEND_API`: aplicación, servicios, repositorios, DTOs y API.
5. `FRONTEND_UX`: UI/UX, formularios, tablas, responsive, accesibilidad y permisos UI.
6. `SEC_AUDIT`: RBAC, auditoría, seguridad y observabilidad.
7. `TEST_CI`: unit/integration/contract/E2E/security/migration/performance tests y CI aplicable.
8. `DOC_CERT`: documentación, evidencia, checkpoint, regresión y cierre.

Si una microtarea sigue siendo demasiado grande, **debe subdividirse antes de editar**. Una microtarea debe representar un solo concern coherente y verificable.

## 4. Máquina de estados

```text
PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO
                     \-> BLOQUEADO
```

`CANCELADO` requiere instrucción explícita de Javier o evidencia inequívoca de que el punto dejó de aplicar. Está prohibido `PENDIENTE -> LISTO` sin ejecución/reconciliación y evidencia verificable.

## 5. Mutex global y exclusión de corridas

`CONFIG.RUNNER_MUTEX*` implementa un mutex global obligatorio para `VariApp VAEP v2 Runner`.

Antes de cualquier lectura funcional, adquisición de tarea, modificación de Sheet, commit o validación:

1. leer `RUNNER_MUTEX_STATE/TOKEN/HEARTBEAT/TASK/CI/TTL`;
2. comprobar `COLA` y GitHub CI;
3. si otra corrida está activa, abortar la nueva invocación antes de tocar trabajo;
4. si el mutex está libre/recuperable, adquirirlo con token único y releerlo;
5. renovar heartbeat como máximo cada 5 minutos y antes/después de escrituras críticas;
6. antes de publicar, confirmar que el token sigue siendo propio;
7. liberar solo el propio mutex y solo cuando no quede CI relacionado `QUEUED/IN_PROGRESS`.

Una invocación manual o programada superpuesta se autoaborta. Nunca pueden ejecutarse dos corridas efectivas simultáneamente.

## 6. Política de selección FINISH_FIRST

Esta es la política obligatoria de selección desde 2026-08-12 y sustituye el comportamiento histórico de saltar entre puntos hermanos independientes.

### 6.1 Reconciliar antes de abrir trabajo

En cada corrida:

1. confirmar `PROJECT_ID=VARIAPP`, repo, rama y HEAD;
2. leer `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md`, última entrada relevante de `CHANGELOG_AI.md` y este protocolo;
3. leer `CONFIG`, `COLA` y `BITACORA`;
4. reconciliar contra GitHub **todas** las filas propias `EN_PROGRESO/VALIDANDO` antes de seleccionar nuevas `PENDIENTE`.

Un lock `AGENTE=ChatGPT` perteneciente al Runner no se considera automáticamente “lock ajeno”. Si está stale debe reconciliarse/recuperarse conforme al lease y CI. Locks de Javier, Codex, AntiG/Antigravity u otro agente sí son concurrencia externa.

### 6.2 Punto padre foco

El runner debe determinar el **PUNTO PADRE FOCO más antiguo ya iniciado y no cerrado dentro de la fase actual**.

Ejemplo: si `N0.5` tiene hijos/subhijos abiertos, `N0.5` es el foco. El foco incluye toda su cadena requerida hasta cierre formal, no solo la microtarea actualmente visible.

Mientras exista un punto foco abierto:

- está prohibido abrir/adquirir un hermano de la misma fase aunque sea técnicamente independiente;
- la siguiente tarea debe pertenecer al mismo árbol foco y cumplir dependencias;
- las subdivisiones adaptativas permanecen dentro de ese árbol;
- después de cada `LISTO`, se revalida HEAD/dependencias/mutex y se continúa inmediatamente con la siguiente elegible del mismo foco.

### 6.3 Sin tope artificial de microtareas

No existe un máximo fijo de 3 microtareas por corrida. El runner continúa dentro del mismo foco mientras gates, seguridad, tiempo y capacidad sigan verdes.

Puede detenerse únicamente por:

- CI relacionado aún activo y necesidad de esperar;
- bloqueo técnico real;
- dependencia pendiente no resoluble;
- gate fallido;
- conflicto/concurrencia real;
- autorización humana necesaria;
- riesgo para `main` o Producción;
- evidencia insuficiente;
- límite real de tiempo/capacidad;
- cierre completo del foco o ausencia de tareas elegibles dentro del foco.

### 6.4 Bloqueos no permiten abandonar el foco

Un `BLOQUEADO` dentro del punto foco **no autoriza saltar a un hermano independiente** para mantener throughput.

El runner debe:

1. registrar causa/evidencia;
2. no reintentar en bucle durante la misma corrida;
3. conservar el mismo punto foco;
4. detenerse y esperar nueva evidencia/condición;
5. cambiar de foco solo cuando el actual quede `LISTO`/`CANCELADO` o Javier autorice expresamente el cambio.

Esto evita árboles parcialmente abiertos y deuda operacional invisible.

### 6.5 Reconciliación de padres

Los estados padre deben reflejar a sus hijos:

- hijo `EN_PROGRESO/VALIDANDO` → padre operativo `EN_PROGRESO`;
- todos los hijos requeridos `LISTO` → padre puede cerrarse `LISTO` con evidencia;
- bloqueo dependiente que impide cierre → documentar/propagar el bloqueo.

No debe quedar un padre `EN_PROGRESO` abandonado mientras el runner abre otro árbol.

### 6.6 Recuperación actual

Mientras `CONFIG.RUNNER_CURRENT_RECOVERY_TARGET=N0.5`, el Runner debe recuperar y cerrar `N0.5` antes de abrir nuevo trabajo de `N0.6`. El trabajo ya válido realizado en `N0.6` se preserva y no se revierte; simplemente no se abren nuevas tareas allí hasta cerrar `N0.5`.

Al cerrar el recovery target, `RUNNER_CURRENT_RECOVERY_TARGET` debe actualizarse al siguiente punto padre más antiguo ya iniciado y no cerrado, o quedar vacío si no existe.

## 7. Ejecución de una microtarea

Dentro del punto foco:

1. seleccionar solo tarea con dependencias directas/transitivas `LISTO` y ningún ancestro bloqueante;
2. marcar `EN_PROGRESO` y agente/inicio antes de editar;
3. limitar cambios a archivos objetivo y dependencias directas;
4. no releer archivos ya documentados salvo que hayan cambiado;
5. pasar a `VALIDANDO` antes de validaciones finales;
6. ejecutar validaciones reales proporcionales;
7. publicar exclusivamente en `Desarrollo` por fast-forward y sin force-push;
8. actualizar `CHANGELOG_AI.md` y `TASKS.md` cuando corresponda;
9. registrar commit/validaciones en `COLA` y transición en `BITACORA`;
10. marcar `LISTO` solo con evidencia suficiente.

Nunca inventar pruebas, CI, despliegues o estados externos.

## 8. Gates de fase y orden estricto

VAEP usa `GATE-N0` ... `GATE-N9`.

```text
GATE-N0 -> ERP-N1 -> GATE-N1 -> ERP-N2 -> GATE-N2 -> ERP-N3 ->
GATE-N3 -> ERP-N4 -> GATE-N4 -> ERP-N5 -> GATE-N5 -> ERP-N6 ->
GATE-N6 -> ERP-N7 -> GATE-N7 -> ERP-N8 -> GATE-N8 -> ERP-N9 -> GATE-N9
```

Los gates aplican Definition of Done global: backend/frontend, migraciones, tests, E2E relevantes, seguridad, permisos, auditoría, backfill/reconciliación, rollback, documentación, evidencia y cero P0/P1 abiertos. Una fase no se cierra solo porque compile.

## 9. Estado especializado ERP-N0.5 — MetodoPago

La cadena N0.5 se gobierna como un único punto foco hasta su cierre formal. El orden operativo vigente es:

```text
N0.5.07B1 -> N0.5.07B2 -> N0.5.07C -> cerrar N0.5.07 ->
N0.5.08 -> N0.5.09/N0.5.10/N0.5.11 -> N0.5.12 ->
N0.5.13 -> N0.5.14 -> N0.5.15
```

`N0.5.07A` ya está certificado. `N0.5.13` debe reconciliar primero el workflow histórico existente; está prohibido duplicar workflows por confiar ciegamente en estados desactualizados.

## 10. Concurrencia y publicación

- Antes de publicar, confirmar HEAD remoto y mutex propio.
- Preservar commits de Codex, AntiG y otros agentes.
- Nunca force-push.
- No crear ramas nuevas.
- Si existe conflicto real no resoluble de forma dirigida: registrar evidencia y detener el foco; no abrir un hermano para esquivar el conflicto.

## 11. Evidencia obligatoria

Cada changeset intencional debe actualizar `CHANGELOG_AI.md`. `TASKS.md` cambia cuando cambia estado/bloqueo/pendiente. Contexto/índice/arquitectura solo si cambia la realidad que documentan.

Para `LISTO`, según aplique deben existir commit SHA, validaciones reales, `COLA` actualizada y transición en `BITACORA`.

## 12. Seguridad y Producción

VAEP no autoriza tocar `main`, fusionar PR #2, habilitar auto-merge, crear ramas nuevas, modificar Producción, secretos, variables, credenciales, bases, dominios, servicios, activos o ejecutar migraciones productivas.

ERP-N9.4 y cualquier operación productiva permanecen bloqueadas hasta autorización expresa de Javier.

## 13. Informe de cierre

Cada corrida que adquirió mutex produce un único informe consolidado al final, con:

- proyecto/repo/rama y HEAD inicial/final;
- microtareas procesadas y evidencia;
- validaciones/CI reales;
- bloqueos/riesgos;
- punto foco actual y progreso restante;
- siguiente elegible dentro del foco;
- motivo exacto de detención;
- estado de seguridad de `main`, Producción y PR #2.

Una invocación rechazada por mutex muestra únicamente la alerta breve de concurrencia.

## 14. Opción manual

Mientras exista mutex `RUNNING`, lease activo o CI relacionado `QUEUED/IN_PROGRESS`, el Runner no debe invitar al usuario a ejecutar manualmente. Si la UI nativa muestra el botón de todos modos, el mutex impide que esa pulsación produzca una segunda ejecución efectiva.

## 15. Qué debe hacer Javier

Para trabajo ya incluido en ERP V5: **nada**. No necesita escribir “continúa”. El Runner debe retomar el punto foco y seguir hasta un bloqueo real o cierre.

Para trabajo nuevo fuera del Plan Maestro, se requiere incorporación explícita a la cola/plan conforme a gobierno vigente.
