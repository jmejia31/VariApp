# VAEP — MAESTRO OPERATIVO ÚNICO

Este archivo es la única autoridad operativa de VAEP para Solqaryn.

```text
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=Desarrollo
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
NUMERIC_PROTOCOL_LABELS=PROHIBITED
CERTIFIED_DONE_STATE=LISTO
ALTERNATE_DONE_STATES=PROHIBITED
```

```text
BEGIN_AUTOMATION_POLICY
EXECUTION_MODEL=TASKS_ONLY
DIRECT_EXECUTION_DEFAULT=TRUE
PRIMARY_DIRECT_BUILD_CLOSE=TRUE
SUPERVISOR_VERIFY_RECOVER_SECONDARY_BUILD=TRUE
TASK_SCOPE_LEASE_REQUIRED=TRUE
TASK_SCOPE_LEASE_SINGLE_WRITER=TRUE
TASK_SCOPE_LEASE_TTL_MINUTES=10
STALE_LEASE_TAKEOVER_AFTER_MINUTES=10
LEASE_FRESH_REQUIRES_LIVE_MATERIAL_PROGRESS=TRUE
ENDED_INVOCATION_RELEASES_LEASE_IMMEDIATELY=TRUE
DIRECT_NEXT_SAFE_PREARM_REQUIRED=TRUE
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_CLOSE_SLA_ROLLING_24H=72
PARENT_MAX_DWELL_MINUTES=20
PARENT_STALL_NO_PROGRESS_MINUTES=10
CLOSURE_REVIEW_MAX_LATENCY_MINUTES=2
CLOSURE_DEBT_TRIGGER_LT=3
CLOSURE_CHAIN_SAME_RUN=TRUE
MAX_VOLUNTARY_IDLE=0
GLOBAL_DISPATCH_ADMISSION=OPEN_ONLY
GLOBAL_FROZEN_PROHIBITED=TRUE
CAUSAL_HOLD_SCOPE=TASK_OR_EXECUTION_ONLY
CAUSAL_GATE_MATRIX_REQUIRED=TRUE
UNRELATED_WORKFLOW_BLOCKING_PROHIBITED=TRUE
REVIEW_BEFORE_EXPENSIVE_CI=TRUE
CLOSURE_CANDIDATE_HEAD_FREEZE=TRUE
LOGICAL_CLOSER_CONTINUITY=TRUE
SPLIT_BEFORE_WRITE_OVER_20M=TRUE
CYCLE_TIME_TELEMETRY_REQUIRED=TRUE
PREARM_BEFORE_CAUSAL_CI=TRUE
VAEP_CHECKPOINTS=:00,:12,:24,:36,:48
VAEP_SUPERVISOR_CHECKPOINTS=:05,:17,:29,:41,:53
VAEP_ALL_ACTIVE_SLOTS=:00,:05,:12,:17,:24,:29,:36,:41,:48,:53
MANUAL_RUN_SLOT_GUARD=TRUE
MANUAL_RUN_NEAREST_DUE_SLOT_ONLY=TRUE
MANUAL_RUN_PAST_WINDOW_MINUTES=3
MANUAL_RUN_FUTURE_SLOT_PREEMPT_PROHIBITED=TRUE
MANUAL_RUN_OFF_SLOT_PROHIBITED=TRUE
MANUAL_RUN_SELECTION_TIEBREAK=LATEST_DUE_SLOT
DEFECT_RECOVERY_FIRST=TRUE
FIRST_DETECTOR_OWNS_RECOVERY=TRUE
NO_REJECT_QUEUE=TRUE
RECOVERY_MUST_RESOLVE_SAME_RUN=TRUE
RECOVERY_UNBLOCK_DEPENDENTS_SAME_RUN=TRUE
PARENT_CLOSE_FIRST=TRUE
OWNER_INTERVENTION_GATE_ACTIVE=TRUE
OWNER_INTERVENTION_AUTOMATIONS_PAUSED=FALSE
OWNER_PAUSE_OVERRIDES_CANONICAL_LIVENESS=TRUE
OWNER_INTERVENTION_QUEUE_ANCHOR_ROW=640
OWNER_INTERVENTION_FIRST_ROW=641
OWNER_INTERVENTION_ENTRY_AFTER=N8.3.A
OWNER_INTERVENTION_ALLOWED_PARENTS=N8.15,N8.16,N8.17,N8.18,N8.19,N8.20,N8.21,N8.22,N8.23,N8.24
OWNER_INTERVENTION_EXIT=N8.24.H
OWNER_INTERVENTION_REPLAY_START=N8.22.E
OWNER_INTERVENTION_RESUME_FROM=N8.3.B
OWNER_INTERVENTION_BYPASS_PROHIBITED=TRUE
POST_INTERVENTION_REVALIDATION_REQUIRED=TRUE
POST_INTERVENTION_REVALIDATION_START_ROW=701
POST_INTERVENTION_REVALIDATION_END_ROW=881
POST_INTERVENTION_REVALIDATE_EXISTING_LISTO=TRUE
POST_INTERVENTION_REVALIDATION_BASIS=N8.15,N8.16,N8.17,N8.18
POST_INTERVENTION_REOPEN_ON_GAP=TRUE
POST_INTERVENTION_HISTORICAL_STATUS_PRESERVED=TRUE
POST_INTERVENTION_REVALIDATION_ADMISSION=SEQUENTIAL_REVIEW_AFTER_OWNER_BULK_RESET
POST_INTERVENTION_REVALIDATION_STATE=PENDIENTE
POST_INTERVENTION_BULK_REOPEN_PROHIBITED=FALSE
OWNER_BULK_REOPEN_701_881_EXECUTED=TRUE
PRODUCTION_RELEASE_EXCEPTION_EXECUTED=TRUE
PRODUCTION_RELEASE_PR=2
PRODUCTION_RELEASE_PR_STATE=CLOSED_MERGED
PRODUCTION_RELEASE_MAIN_BASELINE=6ad48116a93bb1d02a85b941994395ba7382dc93
PR2_REOPEN_REQUIRED=FALSE
FUTURE_MAIN_CHANGES_REQUIRE_FRESH_OWNER_AUTH=TRUE
FUTURE_PRODUCTION_CHANGES_REQUIRE_FRESH_OWNER_AUTH=TRUE
END_AUTOMATION_POLICY
```

## 1. Fuente única y precedencia

1. `docs/VAEP_AUTHORITY.md` es el único MAESTRO ejecutable de reglas.
2. GitHub manda para código y evidencia técnica; Drive/Sheet manda para estado operativo fresco; este MAESTRO manda para reglas.
3. `CONFIG`, `COLA`, `PLAN_MAESTRO`, `BITACORA`, código, CI, tests, receipts y sesiones son fuentes de estado/evidencia, nunca autoridades paralelas.
4. `CHANGELOG_AI.md`, Issues, artifacts, commits y registros históricos son evidencia inmutable. No reactivan reglas anteriores.
5. El historial Git no se reescribe.
6. Ante contradicción, gana este MAESTRO y la fuente operativa stale se corrige en la misma corrida cuando sea seguro.

## 2. Modelo de ejecución vigente: TASKS_ONLY

VAEP opera exclusivamente con las diez automatizaciones programadas de ChatGPT como ejecutores/controllers autónomos.

Camino canónico:

`AUTOMATION -> LEASE -> EJECUCIÓN DIRECTA -> TESTS -> REVIEW_FIRST -> GATES -> LISTO -> PROMOCIÓN`

Reglas absolutas:

- La ejecución directa es el único camino operativo.
- Ningún servicio de workers externo, manifest, lane, queue paralela ni dispatcher externo forma parte del runtime VAEP.
- Las cinco primarias `:00/:12/:24/:36/:48` son builder/closer primarios.
- Las cinco supervisoras `:05/:17/:29/:41/:53` son verifier/recovery/secondary-builder.
- Un slot no termina en `REPORT_ONLY`, `HANDOFF_ONLY`, `PENDING_REVIEW`, `WAITING` o equivalente si existe acción material segura que el ejecutor puede realizar.
- Un checkpoint es un disparador, no una frontera de ownership.
- Tras `LISTO`, promover el sucesor dependency-valid y continuar same-run si es seguro.
- `LISTO` es el único estado de cierre certificado. No existen estados alternativos de cierre en COLA ni en las vistas derivadas.

### 2.1 Integridad de slots para `run_now` manual

Una recuperación manual desde chat/controller es excepcional y no puede romper el orden temporal de las diez automatizaciones.

1. Antes de cualquier `run_now`, obtener la hora local fresca en `America/Tegucigalpa` y releer los diez horarios canónicos.
2. Sólo es elegible manualmente el slot canónico YA VENCIDO más cercano al momento actual, siempre que su minuto canónico haya ocurrido hace como máximo 3 minutos.
3. Si no existe un slot vencido dentro de esa ventana, NO lanzar otra automatización fuera de hora; dejar que el siguiente slot exacto programado dispare por sí mismo.
4. Está prohibido adelantar manualmente un slot futuro y está prohibido elegir una automatización más lejana sólo por su nombre, rol, ownership histórico o parent previo.
5. En empate, gana el slot vencido más reciente. Nunca se salta hacia atrás a un slot antiguo si existe uno más cercano temporalmente.
6. `run_now` no modifica RRULE, timezone, título ni prompt. El schedule canónico sigue siendo la fuente de cadencia.
7. Un `run_now` solicitado sólo demuestra que la ejecución inmediata fue pedida; nunca equivale a `ACTIVE_REAL`, progreso material, PASS o `LISTO` sin readback posterior.

## 3. Ownership y lease

Todo write-scope material requiere lease lógico antes de escribir.

Campos canónicos:

```text
LEASE_SCOPE
LEASE_OWNER_AUTOMATION_ID
LEASE_ACQUIRED_AT
LEASE_HEARTBEAT_AT
LEASE_BASE_HEAD
LEASE_STATUS
LEASE_TOKEN
LEASE_TTL_MINUTES=10
```

Contrato:

1. Un solo writer autoritativo por scope.
2. Antes de adquirir lease, releer `Desarrollo` HEAD y estado operativo fresco.
3. `LEASE_STATUS=ACTIVE` o un heartbeat reciente no bastan: la frescura exige invocación física viva y progreso material verificable reciente.
4. Si la invocación propietaria terminó, liberar el lease inmediatamente.
5. Si el estado físico no puede determinarse, >=10 minutos sin progreso material permite takeover seguro tras reread/readback.
6. Google Sheets no ofrece CAS: adquisición/takeover requiere read-before-write + write + immediate readback.
7. Finalizado el scope, marcar `RELEASED`, `COMPLETED`, `HANDOFF_RELEASED` o `SUPERSEDED` con evidencia exacta.
8. Está prohibido dejar leases fantasmas.

## 4. ACTIVE_REAL y LISTO

`ACTIVE_REAL` para una automatización directa exige ejecución identificable + lease exclusivo fresco + actividad técnica útil/material reciente.

No son `ACTIVE_REAL`: tarea habilitada, trigger, planner, lease sin progreso, prearm read-only, comentario, Issue ni declaración.

`LISTO` sólo lo declara VAEP/controller tras:

- REVIEW_FIRST;
- DoD material completo;
- tests/gates/CI aplicables y causales terminales;
- P0=0 y P1=0;
- exact-head o equivalencia de control-plane demostrada;
- receipt/evidencia verificable;
- write + immediate readback del estado final.

Nunca fingir actividad, PASS, CI, evidencia o LISTO. Un `LISTO` sin estas condiciones es falso positivo y debe volver a `PENDIENTE` para revalidación.

## 5. Throughput y continuidad

Objetivo contractual:

- `PARENT_CLOSE_SLA_ROLLING_60M=3`.
- `PARENT_CLOSE_SLA_ROLLING_24H=72`.
- `PARENT_MAX_DWELL_MINUTES=20`.

La producción se mide por parents certificados `LISTO`, no por triggers, mensajes, commits administrativos ni workflows verdes.

Si `ROLLING60<3`: cerrar primero cualquier parent certificable; drenar REVIEW_FIRST/QA/gate causal; ejecutar sólo el gap material mínimo; promover el siguiente dependency-valid y continuar same-run cuando sea seguro. Nunca filler, falsos cierres, skip de gates o doble writer.

Antes de CI costoso, agotar REVIEW_FIRST y pruebas dirigidas razonables. Cuando exista functional closure candidate head, evitar churn no causal hasta completar sus gates.

## 6. Recovery

Regla: `DEFECT_RECOVERY_FIRST + FIRST_DETECTOR_OWNS_RECOVERY + NO_REJECT_QUEUE`.

- La tarea que detecta un defecto interno accionable lo resuelve same-run si puede.
- Si el owner terminó, está stale o no produce progreso material, la siguiente automatización realiza takeover seguro y continúa el mismo parent.
- Recovery no crea colas de rechazo ni espera administrativa.
- Tras recovery, desbloquear dependencias y continuar cierre/promoción same-run.

## 7. Git, CI y seguridad

- El trabajo ordinario continúa exclusivamente en `Desarrollo`.
- La excepción productiva autorizada por el propietario para ERP-N9 ya fue ejecutada y queda cerrada como hecho histórico.
- PR #2 fue merged y cerrada durante esa liberación autorizada. Su estado `closed + merged + not-draft` es válido y **no debe reabrirse** para satisfacer reglas anteriores.
- `main@6ad48116a93bb1d02a85b941994395ba7382dc93` es el baseline productivo congelado current-standard después del release y hotfix tenant de smoke.
- A partir de ese baseline, cualquier cambio futuro en `main`, Producción, deploys productivos, datos productivos, dominios, certificados o infraestructura productiva requiere una **nueva autorización explícita del propietario**.
- La regla histórica “PR #2 OPEN + DRAFT + unmerged” queda retirada porque su propósito era impedir una liberación no autorizada antes del go-live; no puede reinterpretarse para invalidar retroactivamente una liberación que el propietario autorizó expresamente y que ya fue certificada.
- Esta reconciliación documental no autoriza ningún nuevo merge, deploy, cambio de schema/datos ni modificación productiva.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.
- CI causal debe corresponder al functional head o a una equivalencia demostrada.
- Workflows no relacionados no bloquean un cierre.

## 8. Retiro definitivo de workers externos

La antigua infraestructura J1–J6/Jules está retirada del runtime de Solqaryn. Sus commits, receipts, Issues y demás evidencia histórica permanecen únicamente como historial inmutable. Ningún artefacto histórico puede reactivar workers externos, crear manifests, asignar lanes, consumir credenciales o convertirse en requisito de progreso, cierre o certificación.

El runtime vigente y completo es exclusivamente las diez automatizaciones canónicas definidas por este MAESTRO.

## 9. Revalidación ordenada por el propietario desde COLA 701 hasta 881

El propietario ordenó reabrir los cierres existentes desde la fila 701 hasta la 881 porque la arquitectura y los contratos cambiaron y un cierre histórico no puede suponerse vigente.

Reglas:

1. El reset masivo solicitado por el propietario es una excepción explícita y ya ejecutada sobre `COLA!701:881`.
2. Todo cierre que existía en ese rango fue devuelto a `PENDIENTE`; la evidencia histórica permanece en commits, receipts y columnas de evidencia, pero no certifica el estado actual.
3. La cadena debe retomarse desde la primera tarea dependency-valid pendiente del rango. Mientras `N8.22.E` esté pendiente, ninguna tarea posterior dependiente puede promoverse.
4. `N8.23.A` y cualquier otra tarea que hubiera quedado activa pero dependa de un cierre reabierto debe volver a `PENDIENTE` antes de continuar, preservando su evidencia previa como historia.
5. La revisión es secuencial por dependencias. Está prohibido marcar de nuevo `LISTO` por el hecho de que existan receipts históricos.
6. Cada tarea debe releerse contra la arquitectura, catálogo/matrices, contratos, RBAC, backend authority, seguridad, datos, pruebas y criterios actuales resultantes de `N8.15–N8.18`.
7. Si una tarea sigue cumpliendo completamente, puede volver a `LISTO` con `REVALIDATED_CURRENT_STANDARD` y evidencia causal fresca. Si existe gap material, se corrige bajo `FIRST_DETECTOR_OWNS_RECOVERY`, se ejecutan pruebas/gates aplicables y sólo entonces vuelve a `LISTO`.
8. Si existe bloqueo externo real, se preserva sólo en su scope y se continúa únicamente trabajo independiente permitido por dependencias; nunca se falsea `PASS`.
9. La auditoría forense `N8.19` mantiene su valor histórico para detectar cierres rápidos, N/A, timestamps y receipts dudosos, pero no sustituye esta revalidación.
10. `GATE-N8` y los gates posteriores deben cerrarse de nuevo sólo después de que sus prerequisitos reabiertos hayan sido revalidados bajo el estándar actual.
11. El estado de trabajo visible en `COLA`, `DASHBOARD`, `PLAN_MAESTRO`, `CONFIG` y demás vistas debe usar exclusivamente el enum vigente: `PENDIENTE`, `EN_PROGRESO`, `VALIDANDO`, `LISTO`, `BLOQUEADO`, `CANCELADO`.
12. Las vistas derivadas deben contar `LISTO` directamente y actualizarse por fórmulas/estado fuente; está prohibido depender de snapshots manuales para el conteo de cola.

## 10. Intervención prioritaria N8.15–N8.24

Mientras `OWNER_INTERVENTION_GATE_ACTIVE=TRUE`, las diez canónicas trabajan exclusivamente la cadena permitida `N8.15.A -> ... -> N8.24.H` y, por la revalidación ordenada, retoman desde el primer punto reabierto dependency-valid dentro de esa cadena (`N8.22.E`). No se permite bypass por estados históricos.

Al cerrar nuevamente `N8.24.H` con evidencia actual, se continúa secuencialmente por las filas posteriores ya reabiertas desde `N8.3.B` y siguientes hasta completar la cola 881 bajo el mismo estándar actual.

Especificación ejecutable del paréntesis: `docs/matrices-evaluacion/00_GOBERNANZA/ESPECIFICACION_EJECUCION_N8_15_N8_24.md`.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Regla vinculante: este archivo solo puede interpretarse con contexto de SOLQARYN. Está prohibido consultar o usar skills, documentación, chats, repositorios, memorias o reglas fuera de SOLQARYN salvo autorización explícita del propietario para la fuente/alcance concreto o una entrada `ACTIVE` en la allowlist versionada. La disponibilidad técnica no equivale a permiso. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/Solqaryn`. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.


