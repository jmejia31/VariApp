# VAEP — MAESTRO OPERATIVO ÚNICO

Este archivo es la única autoridad operativa de VAEP para VariApp.

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
NUMERIC_PROTOCOL_LABELS=PROHIBITED
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
OWNER_INTERVENTION_RESUME_FROM=N8.3.B
OWNER_INTERVENTION_BYPASS_PROHIBITED=TRUE
POST_INTERVENTION_REVALIDATION_REQUIRED=TRUE
POST_INTERVENTION_REVALIDATION_START_ROW=721
POST_INTERVENTION_REVALIDATE_EXISTING_LISTO=TRUE
POST_INTERVENTION_REVALIDATION_BASIS=N8.15,N8.16,N8.17,N8.18
POST_INTERVENTION_REOPEN_ON_GAP=TRUE
POST_INTERVENTION_HISTORICAL_STATUS_PRESERVED=TRUE
POST_INTERVENTION_REVALIDATION_ADMISSION=JIT_REOPEN_ONE_AT_A_TIME
POST_INTERVENTION_REVALIDATION_STATE=PENDIENTE
POST_INTERVENTION_BULK_REOPEN_PROHIBITED=TRUE
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

`AUTOMATION -> LEASE -> EJECUCIÓN DIRECTA -> TESTS -> REVIEW_FIRST -> GATES -> LISTO_REAL -> PROMOCIÓN`

Reglas absolutas:

- La ejecución directa es el único camino operativo.
- Ningún servicio de workers externo, manifest, lane, queue paralela ni dispatcher externo forma parte del runtime VAEP.
- Las cinco primarias `:00/:12/:24/:36/:48` son builder/closer primarios.
- Las cinco supervisoras `:05/:17/:29/:41/:53` son verifier/recovery/secondary-builder.
- Un slot no termina en `REPORT_ONLY`, `HANDOFF_ONLY`, `PENDING_REVIEW`, `WAITING` o equivalente si existe acción material segura que el ejecutor puede realizar.
- Un checkpoint es un disparador, no una frontera de ownership.
- Tras `LISTO_REAL`, promover el sucesor dependency-valid y continuar same-run si es seguro.

### 2.1 Integridad de slots para `run_now` manual

Una recuperación manual desde chat/controller es excepcional y no puede romper el orden temporal de las diez automatizaciones.

1. Antes de cualquier `run_now`, obtener la hora local fresca en `America/Tegucigalpa` y releer los diez horarios canónicos.
2. Sólo es elegible manualmente el slot canónico YA VENCIDO más cercano al momento actual, siempre que su minuto canónico haya ocurrido hace como máximo 3 minutos.
3. Si no existe un slot vencido dentro de esa ventana, NO lanzar otra automatización fuera de hora; dejar que el siguiente slot exacto programado dispare por sí mismo.
4. Está prohibido adelantar manualmente un slot futuro y está prohibido elegir una automatización más lejana sólo por su nombre, rol, ownership histórico o parent previo.
5. En empate, gana el slot vencido más reciente. Nunca se salta hacia atrás a un slot antiguo si existe uno más cercano temporalmente.
6. `run_now` no modifica RRULE, timezone, título ni prompt. El schedule canónico sigue siendo la fuente de cadencia.
7. Un `run_now` solicitado sólo demuestra que la ejecución inmediata fue pedida; nunca equivale a `ACTIVE_REAL`, progreso material, PASS o `LISTO_REAL` sin readback posterior.

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

## 4. ACTIVE_REAL y LISTO_REAL

`ACTIVE_REAL` para una automatización directa exige ejecución identificable + lease exclusivo fresco + actividad técnica útil/material reciente.

No son `ACTIVE_REAL`: tarea habilitada, trigger, planner, lease sin progreso, prearm read-only, comentario, Issue ni declaración.

`LISTO_REAL` sólo lo declara VAEP/controller tras:

- REVIEW_FIRST;
- DoD material completo;
- tests/gates/CI aplicables y causales terminales;
- P0=0 y P1=0;
- exact-head o equivalencia de control-plane demostrada;
- receipt/evidencia verificable.

Nunca fingir actividad, PASS, CI, evidencia o LISTO.

## 5. Throughput y continuidad

Objetivo contractual:

- `PARENT_CLOSE_SLA_ROLLING_60M=3`.
- `PARENT_CLOSE_SLA_ROLLING_24H=72`.
- `PARENT_MAX_DWELL_MINUTES=20`.

La producción se mide por parents certificados `LISTO_REAL`, no por triggers, mensajes, commits administrativos ni workflows verdes.

Si `ROLLING60<3`: cerrar primero cualquier parent certificable; drenar REVIEW_FIRST/QA/gate causal; ejecutar sólo el gap material mínimo; promover el siguiente dependency-valid y continuar same-run cuando sea seguro. Nunca filler, falsos cierres, skip de gates o doble writer.

Antes de CI costoso, agotar REVIEW_FIRST y pruebas dirigidas razonables. Cuando exista functional closure candidate head, evitar churn no causal hasta completar sus gates.

## 6. Recovery

Regla: `DEFECT_RECOVERY_FIRST + FIRST_DETECTOR_OWNS_RECOVERY + NO_REJECT_QUEUE`.

- La tarea que detecta un defecto interno accionable lo resuelve same-run si puede.
- Si el owner terminó, está stale o no produce progreso material, la siguiente automatización realiza takeover seguro y continúa el mismo parent.
- Recovery no crea colas de rechazo ni espera administrativa.
- Tras recovery, desbloquear dependencias y continuar cierre/promoción same-run.

## 7. Git, CI y seguridad

- Trabajar sólo en `Desarrollo`.
- `main` permanece congelada.
- PR #2 permanece OPEN + DRAFT; no merge ni auto-merge.
- No Producción, secretos, credenciales, dominios, certificados, datos productivos, deploys ni infraestructura productiva.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.
- CI causal debe corresponder al functional head o a una equivalencia demostrada.
- Workflows no relacionados no bloquean un cierre.

## 8. Retiro definitivo de workers externos

La antigua infraestructura J1–J6/Jules está retirada del runtime de VariApp. Sus commits, receipts, Issues y demás evidencia histórica permanecen únicamente como historial inmutable. Ningún artefacto histórico puede reactivar workers externos, crear manifests, asignar lanes, consumir credenciales o convertirse en requisito de progreso, cierre o certificación.

El runtime vigente y completo es exclusivamente las diez automatizaciones canónicas definidas por este MAESTRO.

## 9. Intervención prioritaria del propietario N8.15–N8.24

Existe un gate deliberado temporal ordenado por el propietario para resolver arquitectura, matrices y preparación pre-go-live antes de continuar el resto del plan histórico.

Reglas durante `OWNER_INTERVENTION_GATE_ACTIVE=TRUE`:

1. El ancla física de `COLA` es la fila 640. La primera microtarea de la intervención DEBE estar en la fila 641.
2. La entrada causal inmediata es `N8.3.A`; la primera tarea de intervención es `N8.15.A`.
3. Sólo son elegibles materialmente `N8.15` a `N8.24` y sus microtareas dependency-valid.
4. `UNRELATED_WORKFLOW_BLOCKING_PROHIBITED` no puede usarse para escapar de esta intervención: esto no es un blocker, es un filtro deliberado de admisión.
5. `OWNER_INTERVENTION_BYPASS_PROHIBITED=TRUE` prevalece sobre selección de trabajo independiente fuera del conjunto permitido.
6. La pausa temporal fue levantada explícitamente por el propietario. Las diez automatizaciones canónicas quedan autorizadas a reanudarse conservando exactamente títulos, prompts, horarios, timezone y política de notificaciones.
7. Mientras la intervención esté abierta, las diez canónicas trabajan exclusivamente la cadena `N8.15.A -> ... -> N8.24.H`; ningún `LISTO` histórico posterior autoriza bypass.
8. Al cerrar `N8.24.H` con evidencia material, se realiza readback global y se reanuda el plan histórico desde la fila 721 (`N8.3.B`), salvo evidencia causal posterior que exija un sucesor más temprano. La reentrada NO confía automáticamente en estados `LISTO` históricos.
9. Desde la fila 721 en adelante, toda tarea histórica —incluidas las que ya muestran `LISTO`— debe revalidarse contra la arquitectura, catálogo/matrices, contratos, RBAC, backend authority, seguridad, datos, pruebas y criterios resultantes de `N8.15–N8.18`. El estado histórico se preserva como historia, pero no equivale por sí solo a certificación vigente.
10. Para compatibilidad con `GLOBAL_DISPATCH_ADMISSION=OPEN_ONLY`, la revalidación se hace `JIT_REOPEN_ONE_AT_A_TIME`: al llegar causalmente a una fila histórica que figure `LISTO`, el controller preserva su status/evidencia histórica en receipt/observaciones, cambia únicamente esa tarea a `PENDIENTE` con marcador `REVALIDATION_REQUIRED__CURRENT_STANDARD`, hace readback y recién entonces adquiere lease y la revalida. Está prohibido reabrir masivamente la cola.
11. Si la tarea revalidada sigue cumpliendo completamente, vuelve a `LISTO` con `REVALIDATED_CURRENT_STANDARD` y evidencia causal sin rehacer trabajo inútil. Si existe gap material, se corrige bajo `FIRST_DETECTOR_OWNS_RECOVERY`, se ejecutan pruebas/gates aplicables y sólo entonces vuelve a `LISTO`. Si existe bloqueo externo real, se preserva sólo en su scope y se continúa únicamente trabajo independiente permitido por dependencias; nunca se falsea `PASS`.
12. Las filas posteriores no se abren hasta que la fila revalidada actual cierre; así se conservan dependencias y se evita doble writer, bypass o cascadas artificiales de bloqueos.
13. La auditoría forense `N8.19` conserva su scope especial desde `N8.6.G` en adelante y sirve además como control de cierres rápidos, N/A, timestamps y receipts; no reemplaza la revalidación post-matriz de la fila 721+.
14. `GATE-N8` debe incluir la intervención y la revalidación vigente de sus prerequisitos como condición formal de cierre.

Especificación ejecutable del paréntesis: `docs/matrices-evaluacion/00_GOBERNANZA/ESPECIFICACION_EJECUCION_N8_15_N8_24.md`.
