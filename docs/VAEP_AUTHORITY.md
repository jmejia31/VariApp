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
DEFECT_RECOVERY_FIRST=TRUE
FIRST_DETECTOR_OWNS_RECOVERY=TRUE
NO_REJECT_QUEUE=TRUE
RECOVERY_MUST_RESOLVE_SAME_RUN=TRUE
RECOVERY_UNBLOCK_DEPENDENTS_SAME_RUN=TRUE
PARENT_CLOSE_FIRST=TRUE
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
