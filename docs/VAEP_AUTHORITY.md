# VAEP — MAESTRO OPERATIVO ÚNICO

Este archivo es la **única autoridad operativa** de VAEP para VariApp. Toda automatización, controller, ChatGPT/VAEP, Chat B, Jules y herramienta auxiliar debe releerlo antes de decidir o ejecutar trabajo.

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
EXECUTION_MODEL=TASKS_FIRST_JULES_ON_DEMAND
DIRECT_EXECUTION_DEFAULT=TRUE
JULES_REQUIRED_FOR_PROGRESS=FALSE
JULES_OFFLOAD_ONLY_IF_CRITICAL_PATH_GAIN=TRUE
JULES_MINIMUM_UTILIZATION_TARGET=0
OFFLOAD_REQUIRES_NONOVERLAP=TRUE
OFFLOAD_NEVER_BLOCKS_CURRENT_PARENT=TRUE
PRIMARY_DIRECT_BUILD_CLOSE=TRUE
SUPERVISOR_VERIFY_RECOVER_SECONDARY_BUILD=TRUE
TASK_SCOPE_LEASE_REQUIRED=TRUE
TASK_SCOPE_LEASE_SINGLE_WRITER=TRUE
TASK_SCOPE_LEASE_TTL_MINUTES=10
STALE_LEASE_TAKEOVER_AFTER_MINUTES=10
DIRECT_NEXT_SAFE_PREARM_REQUIRED=TRUE
LOGICAL_TASK_SESSION_CONTINUES_UNTIL_LISTO_REAL=TRUE
RUN_BOUNDARY_DOES_NOT_COMPLETE_PARENT=TRUE
USER_FACING_NONTERMINAL_COMPLETION_PROHIBITED=TRUE
NONTERMINAL_INTERNAL_HANDOFF_REQUIRED=TRUE
NEXT_AUTOMATION_RESUMES_CURRENT_PARENT=TRUE
STATE_FLOW=PENDIENTE>EN_PROGRESO>VALIDANDO>LISTO
NEXT_PARENT_PREARM_MODE=READ_ONLY
NEXT_PARENT_ACTIVE_BEFORE_PREREQUISITE_LISTO_REAL=FALSE
NEXT_PARENT_PROMOTE_AFTER_LISTO_REAL_SAME_RUN=TRUE
USE_EXECUTION_BUDGET_WHILE_PRODUCTIVE=TRUE
HUMAN_ACTION_REQUIRED_ONLY_FOR_EXTERNAL_IRRESOLVABLE_BLOCKER=TRUE
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
NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL=TRUE
PREARM_BEFORE_CAUSAL_CI=TRUE
VAEP_CHECKPOINTS=:00,:12,:24,:36,:48
VAEP_SUPERVISOR_CHECKPOINTS=:05,:17,:29,:41,:53
VAEP_ALL_ACTIVE_SLOTS=:00,:05,:12,:17,:24,:29,:36,:41,:48,:53
JULES_ACTIVE_WORKERS=J1,J2,J3,J4,J5,J6
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
JULES_LANE_BUDGET_SECONDS=1080
DEFECT_RECOVERY_FIRST=TRUE
FIRST_DETECTOR_OWNS_RECOVERY=TRUE
NO_REJECT_QUEUE=TRUE
CROSS_LANE_R2_ALLOWED=TRUE
SECOND_ATTEMPT_CONTROLLER_TAKEOVER_REQUIRED=TRUE
RECOVERY_MUST_RESOLVE_SAME_RUN=TRUE
RECOVERY_UNBLOCK_DEPENDENTS_SAME_RUN=TRUE
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
```

## 1. Fuente única, precedencia y limpieza de historia

1. `docs/VAEP_AUTHORITY.md` es el único MAESTRO ejecutable de reglas.
2. GitHub manda para código y evidencia técnica; Drive/Sheet manda para estado operativo fresco; este MAESTRO manda para reglas.
3. `CONFIG`, `COLA`, `PLAN_MAESTRO`, `WORKERS`, `BITACORA`, código, CI, tests, receipts y sesiones son fuentes de estado/evidencia, nunca autoridades paralelas.
4. `CHANGELOG_AI.md`, Issues, artifacts, manifests cerrados, prompts antiguos y commits históricos son evidencia inmutable. No pueden reactivar una regla antigua.
5. Se prohíben copias operativas `*-vX*`, protocolos numerados paralelos o fuentes superseding.
6. Los artefactos operativos obsoletos se eliminan o neutralizan del runtime; **el historial Git no se reescribe**.
7. Ante contradicción, gana este MAESTRO y debe corregirse la fuente operativa stale en la misma corrida.

## 2. Modelo de ejecución vigente: TASKS-FIRST

VAEP opera **TASKS-FIRST + JULES-ON-DEMAND**.

### Camino primario

El camino normal y preferido es:

`AUTOMATION -> LEASE -> EJECUCIÓN DIRECTA -> TESTS -> REVIEW_FIRST -> INTEGRACIÓN -> GATES -> LISTO_REAL -> PROMOCIÓN`

Las diez tareas programadas son ejecutores/controllers autónomos. No son meros schedulers, observadores ni despachadores Jules.

Reglas absolutas:

- La ejecución directa es el default.
- Ningún CURRENT_PARENT puede esperar a Jules si la tarea activa puede resolver el gap con sus herramientas/autorización.
- Ningún dispatch Jules es requisito para progreso, ACTIVE_REAL, cierre o promoción.
- Cero Jules activos puede ser estado sano si no existe offload materialmente ventajoso.
- Está prohibido fabricar trabajo, subdivisiones nominales, evidencia redundante o backlog artificial para mantener Jules ocupados.
- Un slot no termina en `REPORT_ONLY`, `HANDOFF_ONLY`, `PENDING_REVIEW`, `WAIT_FOR_JULES` o equivalente si existe acción material segura que el ejecutor actual puede realizar.

### Contrato de continuidad hasta LISTO_REAL

`LISTO_REAL_CONTINUITY_HARD` es obligatorio para las diez Tasks:

1. Cada activación se adjunta al `CURRENT_PARENT` y trabaja materialmente hacia `LISTO_REAL`; una corrida de status no satisface la obligación si existe acción segura ejecutable.
2. El fin físico de una invocación **no** completa la tarea lógica. Mientras el parent no sea `LISTO_REAL`, la sesión lógica sigue abierta entre checkpoints.
3. Si runtime, gate nonterminal o dependencia externa impide certificar en esa invocación, se persiste `HANDOFF_CONTINUITY` interno con exact HEAD, owner/lease, evidencia, RCA, progreso útil y `NEXT_ACTION`; la siguiente Task reanuda exactamente el mismo `CURRENT_PARENT`.
4. Está prohibida una notificación user-facing de “terminado”, “cerrado”, “éxito” o status terminal normal si el `CURRENT_PARENT` no es `LISTO_REAL`. Sólo un bloqueo externo causal realmente irresoluble que requiera intervención humana puede notificarse como `HUMAN_ACTION_REQUIRED`, nunca como cierre.
5. La máquina de estados es estricta: `PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO`, y `LISTO_REAL` vive en evidencia/receipt, no en `COLA.ESTADO`.
6. `EN_PROGRESO` exige lease válido y actividad material real. `VALIDANDO` exige delta material completo y `REVIEW_FIRST`/gates causales en curso. `LISTO` exige receipt `LISTO_REAL`, DoD y P0/P1=0.
7. `NEXT_PARENT` permanece `PENDIENTE` y como máximo `PREARM_READ_ONLY` mientras el prerequisito no sea `LISTO_REAL`; queda prohibido marcarlo `EN_PROGRESO` o `VALIDANDO` anticipadamente.
8. Al cerrar el parent actual se materializa receipt/evidencia, se sincroniza `LAST_CLOSED_PARENT/CURRENT_PARENT` y se deja el siguiente dependency-valid, unblocked y prearmed en la misma cadena lógica. Sólo un lease nuevo + trabajo material real puede activarlo.
9. Lease fresco ajeno + progreso material <=10m implica no competir: el siguiente slot actúa como verifier/read-only del mismo parent y puede clasificar gates/preparar prearm seguro. Sin owner o con stale/no-progress >=10m, debe ejecutar takeover con read-before-write + write + readback y continuar recuperación causal.
10. Cada Task utiliza su presupuesto de ejecución mientras exista trabajo material seguro. No existe corte voluntario por conveniencia; tampoco se permite filler, busywork o actividad para aparentar utilización.
11. `FIRST_DETECTOR_OWNS_RECOVERY`: defecto o review debt accionable se corrige y revalida same-run cuando sea seguro; no se crea una cola de rechazo para trabajo que el detector puede resolver.
12. El SLA `3 LISTO_REAL/60m` acelera prioridad mediante `CLOSURE_DEBT_FASTPATH`, pero jamás autoriza false PASS, false ACTIVE_REAL, cierre falso o salto de dependencias.

### Primarias

`:00/:12/:24/:36/:48` son **builder/closer primarios**. Cada una debe tomar el gap material más corto hacia `LISTO_REAL`, adquirir ownership exclusivo y ejecutarlo directamente salvo que ya exista un owner material válido.

### Supervisoras

`:05/:17/:29/:41/:53` son **verifier/recovery/secondary-builder**. Verifican la corrida primaria precedente y:

- si existe lease fresco + progreso material, no duplican escritura; ejecutan QA, REVIEW_FIRST, CI/gates, prearm o un scope independiente seguro;
- si falta owner, el lease expiró o no hay progreso material >=10 min, toman ownership y continúan ejecución directa;
- si la primaria omitió una acción obligatoria, la supervisora la absorbe same-run.

El minuto programado es un disparador, no una precondición. Una ejecución tardía hace catch-up de la obligación material vencida sin duplicar ownership.

## 3. Ownership y lease de scope

Todo write-scope material directo requiere lease lógico antes de escribir.

Campos canónicos de lease en estado operativo:

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
2. Antes de adquirir lease: releer `Desarrollo` HEAD y estado operativo fresco.
3. El lease debe identificar parent + faceta/scope material; un nombre de slot no basta.
4. Mientras `LEASE_STATUS=ACTIVE` y `LEASE_HEARTBEAT_AT` tenga <=10 minutos con progreso material verificable, otra automatización no escribe ese scope.
5. Un lease sin progreso material verificable durante >=10 minutos es `STALE` y puede ser tomado por el siguiente ejecutor, registrando owner anterior, causa y nuevo token.
6. Google Sheets no ofrece CAS de celda: toda adquisición/takeover requiere **read-before-write + write + immediate readback**. Si el readback muestra carrera, no escribir producto; reconciliar ownership primero.
7. El lease no se almacena mediante commits Git para evitar HEAD churn.
8. Finalizado el scope, marcar `RELEASED`, `COMPLETED` o `SUPERSEDED` con evidencia exacta. No dejar leases fantasmas.
9. Cambiar de ejecutor no reinicia identidad ni intentos de una tarea Jules ya existente.

## 4. Jules J1–J6: capacidad auxiliar, nunca cuello de botella

J1–J6 se conservan como aceleradores cloud opcionales:

- J1: CODE_CORE.
- J2: CODE_BACKEND_DATA.
- J3: CODE_FRONTEND.
- J4: CODE_INFRA_INTEGRATIONS.
- J5: QA_SECURITY_REGRESSION con fallback CODE.
- J6: INTEGRATION_RECOVERY con fallback CODE/QA.

Un offload Jules sólo está permitido cuando **todas** estas condiciones son verdaderas:

1. existe scope material real derivado del roadmap;
2. el scope es independiente/no solapado con el write-scope directo vigente;
3. reduce de forma razonable el camino crítico o prepara trabajo material seguro posterior;
4. no obliga a esperar a Jules para cerrar el CURRENT_PARENT;
5. existe base HEAD exacta y dependencia válida;
6. no duplica una identidad funcional ya completada o en ownership válido;
7. la tarea programada conserva capacidad para continuar ejecución directa en paralelo.

Si no se cumplen, `NO_JULES_OFFLOAD` es el resultado correcto y **no es déficit**.

No existen objetivos obligatorios de utilización, tareas/día, queue depth ni backlog mínimo por Jules. `scripts/vaep/jules_integration_metrics.py` es telemetría diagnóstica, no KPI de producción ni gate de cierre.

### Contrato de dispatch Jules

- Sólo se crea manifest cuando una tarea/controller decide explícitamente `JULES_OFFLOAD_APPROVED` para un scope concreto.
- Antes de cada manifest releer HEAD y usar ese SHA exacto como `primaryBaseHead`.
- Jules entrega patch/artifact; no publica funcionalmente, no certifica `LISTO_REAL`, no toca `main`, Producción, secretos ni deploys.
- Resultado Jules siempre entra a `REVIEW_FIRST`.
- Resultado tardío de sesión `SUPERSEDED` es `EVIDENCE_ONLY` y no se integra automáticamente.
- Un dispatch, workflow, sesión o `COMPLETED` no cuenta como cierre por sí solo.

## 5. ACTIVE_REAL, evidencia y estados

Estados COLA válidos:

```text
PENDIENTE|EN_PROGRESO|VALIDANDO|LISTO|BLOQUEADO|CANCELADO
```

`ACTIVE_REAL` es actor-aware:

- **Automation directa**: ejecución real identificable + lease exclusivo fresco + actividad técnica útil/material reciente sobre el scope.
- **Jules**: manifest -> workflow -> `sessionId` correlacionado + actividad técnica útil reciente.

No son `ACTIVE_REAL`: tarea habilitada, trigger, planner, dispatch, workflow sin sesión, lease sin progreso, prearm read-only, comentario, Issue ni declaración.

`LISTO_REAL` sólo lo declara VAEP/controller tras:

- REVIEW_FIRST;
- DoD material completo;
- tests/gates/CI aplicables y causales terminales;
- P0=0 y P1=0;
- exact-head o equivalencia de control-plane demostrada;
- receipt/evidencia verificable.

Nunca fingir sesión, actividad, PASS, CI, evidencia o LISTO.

### Transiciones de estado obligatorias

- `PENDIENTE -> EN_PROGRESO`: sólo después de ownership/lease válido y primera actividad material verificable.
- `EN_PROGRESO -> VALIDANDO`: sólo cuando el delta material requerido está implementado y la unidad entra realmente a REVIEW_FIRST/tests/gates causales.
- `VALIDANDO -> LISTO`: sólo cuando existe receipt/evidencia `LISTO_REAL`, con DoD completo y P0/P1=0.
- `LISTO_REAL` nunca se escribe como valor de `COLA.ESTADO`.
- El siguiente parent no hereda `EN_PROGRESO/VALIDANDO` por anticipación; antes del cierre del prerequisito sólo admite `PENDIENTE + PREARM_READ_ONLY`.

## 6. Parent-close, throughput y continuidad

Objetivo contractual:

- `PARENT_CLOSE_SLA_ROLLING_60M=3`.
- `PARENT_CLOSE_SLA_ROLLING_24H=72`.
- `PARENT_MAX_DWELL_MINUTES=20`.

La producción se mide por padres certificados `LISTO_REAL`, no por triggers, commits de control-plane, tareas Jules, manifests, sesiones ni workflows verdes.

### CLOSURE_DEBT_FASTPATH

Si `ROLLING60<3`:

1. cerrar primero cualquier parent ya certificable;
2. drenar inmediatamente REVIEW_FIRST/QA/gate causal que impida cierre;
3. si falta un único gap material, ejecutar **sólo ese gap** directamente;
4. prohibido crear evidencia redundante o offload que alargue el camino;
5. al cerrar, promover el siguiente dependency-valid y evaluarlo en la misma corrida;
6. con `CLOSURE_CHAIN_SAME_RUN=TRUE`, encadenar cierres mientras sea seguro hasta recuperar el SLA o encontrar blocker externo causal exacto.

`DIRECT_NEXT_SAFE_PREARM_REQUIRED=TRUE` significa prearmar lectura/plan/scope del próximo trabajo seguro sin escribirlo prematuramente. Antes de `LISTO_REAL` del prerequisito, el siguiente parent permanece `PENDIENTE` y el prearm es read-only. No requiere Jules ni manifest.

`CLOSURE_CHAIN_SAME_RUN=TRUE` no autoriza dos writers solapados ni estados activos anticipados: significa cerrar el parent vigente, dejar receipt y sincronización coherente, promover el siguiente dependency-valid y sólo entonces adquirir un nuevo lease para trabajo material del siguiente.

## 7. Recovery sin espera administrativa

Regla: `DEFECT_RECOVERY_FIRST + FIRST_DETECTOR_OWNS_RECOVERY + NO_REJECT_QUEUE`.

Para defectos internos accionables:

- la tarea que detecta el defecto lo resuelve same-run si puede;
- un defecto reparable no se estaciona como `REJECTED`, `BLOQUEADO`, `HANDOFF_ONLY` o similar;
- si el defecto proviene de Jules ATTEMPT1: preferir direct fix del controller; si realmente conviene rework material Jules, se permite exactamente un R2, conservando `taskId` y `taskAttempt=2`;
- R2 puede ir al mismo Jules u otro compatible;
- ATTEMPT2 defectuoso obliga takeover directo del controller/tarea detectora;
- R3 está prohibido;
- después del recovery, desbloquear dependencias y continuar el camino de cierre en la misma corrida.

Un timeout Jules revoca ownership Jules, marca su resultado tardío como superseded/evidence-only y **nunca impide que la ejecución directa continúe**.

Sólo un blocker externo causal realmente irresoluble con las herramientas/autorización actuales puede persistir como `BLOQUEADO`.

## 8. Git, concurrencia, CI y seguridad

- Sólo rama `Desarrollo`.
- `main` congelada.
- PR #2 `Desarrollo -> main` debe permanecer OPEN + DRAFT, sin merge ni auto-merge.
- No crear ramas nuevas para VAEP salvo autorización futura explícita.
- Prohibidos force-push, reset destructivo, amend de historia compartida y revert que destruya trabajo concurrente.
- No Producción, secretos, credenciales, dominios, certificados, datos productivos, deploys ni infraestructura productiva.
- Revalidar HEAD antes de publicar; preservar cambios concurrentes.

### CI causal

`HEAD_FREEZE_CAUSAL` existe sólo cuando un gate crítico aplicable al functional/exact head está `queued`/`in_progress`. Un workflow legacy/no relacionado, deploy/Vercel no aplicable o CI de puro control-plane no congela la fábrica.

Durante un hold causal:

- no mover el functional head de la unidad afectada con manifest/control-plane si invalida la evidencia;
- sí ejecutar QA, review, prearm y scopes compatibles;
- al terminalizar el gate, recalcular desde cero y continuar inmediatamente.

Fallo causal interno se corrige; ruido externo/no causal se registra sin crear blocker falso.

## 9. Las diez tareas activas

```text
PRIMARIAS / BUILDER-CLOSER
:00  VAEP :00 Primary
:12  VAEP :12 Recovery
:24  VAEP :24 Review
:36  VAEP :36 Watchdog
:48  VAEP :48 Debt

SUPERVISORAS / VERIFIER-RECOVERY-SECONDARY-BUILDER
:05  Tarea Supervisión :00
:17  Tarea Supervisión :12
:29  Tarea Supervisión :24
:41  Tarea Supervisión :36
:53  Tarea Supervisión :48
```

Orden mínimo de **cualquiera** de las diez:

1. releer MAESTRO, HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, `ROLLING60/DEFICIT`, Sheet fresco, leases y deuda terminal;
2. reconciliar lease: respetar owner fresco o adquirir/tomar scope stale;
3. si el `CURRENT_PARENT` ya es certificable, cerrarlo de inmediato; si no, continuar el mismo parent, no seleccionar otro por conveniencia;
4. si falta trabajo material, ejecutar directamente el gap más corto al cierre;
5. probar y hacer REVIEW_FIRST; corregir same-run lo corregible;
6. resolver recovery accionable antes de terminar;
7. integrar sólo delta aprobado sobre HEAD vigente y verificar gates causales;
8. certificar `LISTO_REAL` sólo con evidencia completa;
9. al cierre, sincronizar receipt/estado y promover/dejar dependency-valid, unblocked y prearmed el siguiente parent; antes de ese cierre el siguiente sigue `PENDIENTE/PREARM_READ_ONLY`;
10. si queda presupuesto y no existe writer fresco, puede adquirirse un nuevo lease y comenzar materialmente el siguiente parent;
11. sólo después evaluar si un offload Jules independiente reduce camino crítico;
12. si la invocación física termina antes de `LISTO_REAL`, persistir `HANDOFF_CONTINUITY` interno con exact HEAD/lease/evidencia/RCA/NEXT_ACTION para que el siguiente slot reanude el mismo parent;
13. sincronizar CONFIG/COLA/WORKERS/BITACORA y liberar/actualizar lease con readback;
14. registrar PROOF_OF_RUN material por `AUTOMATION_ID`.

Una lane Jules libre ya **no obliga dispatch**. Una automatización directa con trabajo material seguro sí debe ejecutarlo o documentar blocker externo exacto.

Ninguna de las diez Tasks puede declarar su obligación lógica terminada porque finalice su invocación física. El terminal normal de la sesión lógica es `LISTO_REAL`; la única excepción de mensajería es `HUMAN_ACTION_REQUIRED` por blocker externo causal realmente irresoluble.

## 10. Telemetría y Sheet

Fuentes de estado:

- `CONFIG`, `COLA`, `WORKERS`: estado operativo.
- `TAREAS_PROGRAMADAS`: identidad/cadencia/última ejecución observada de primarias.
- `TAREAS_DE_SUPERVISION`: identidad/cadencia/última supervisión material.
- `BITACORA`: evidencia cronológica, no autoridad.
- `CONTROL_TOWER` y `DASHBOARD`: vistas derivadas; **no sobrescribir fórmulas con snapshots**.

Relojes separados:

- `LAST_SCHEDULER_TRIGGER`: sólo scheduler/runtime verificado.
- `LAST_MATERIAL_ACTION`: última acción material comprobada.
- `LAST_TELEMETRY_SYNC`: última reconciliación externa confirmada.
- `LAST_SUPERVISION_AT`: supervisión material, no mera consulta de runtime.

`state_sync.py` es verificador/generador; no prueba por sí solo que Sheets fue escrito. Toda escritura externa requiere readback. Si falla, registrar `SYNC_PENDING/FAILED`; nunca declarar `SYNCED` sin confirmación.

Los datos Jules en `WORKERS` son auxiliares. `J1..J6` pueden figurar `AVAILABLE_ON_DEMAND` sin que ello sea déficit. ACTIVE_REAL Jules exige la correlación definida en §5.

## 11. Dedupe e identidad

La identidad útil es `CURRENT_PARENT + semantic/material facet`, no número correlativo, filename, dispatchId, automation slot ni sessionId.

- Renombrar/recontar no crea trabajo nuevo.
- Una faceta ya completada e integrada no se redistribuye como producción nueva.
- Un transport superseded antes de contenido útil puede recuperarse sin consumir content attempt.
- Ningún planner puede reciclar facetas agotadas para llenar capacidad.
- Si no existe trabajo paralelo seguro, emitir `NO_SAFE_PARALLEL_WORK`; no fabricar filler.

## 12. Componentes auxiliares

- ChatGPT/VAEP y Chat B: full-access controller/QA dentro de `Desarrollo`; pueden desarrollar, corregir, probar, integrar y certificar bajo este MAESTRO.
- ALEX: planner/control-plane auxiliar; propone scopes y paralelismo, no obliga utilización Jules, no certifica LISTO_REAL.
- Vibe: QA/corrector externo sólo por delegación.
- AntiG/Antigravity: `RESERVED_INACTIVE`; sin scheduler/handoff/certificación hasta autorización explícita futura.
- Codex: fuera del flujo operativo salvo orden explícita.

## 13. Cambio de regla y compatibilidad histórica

Toda modificación futura de regla debe:

1. editar este mismo archivo;
2. actualizar únicamente espejos técnicos/estado que dependan de la regla;
3. neutralizar prompts/configuración operativa incompatible;
4. conservar evidencia histórica inmutable sin permitir que se ejecute;
5. no crear otra fuente de autoridad.

**Regla final:** las diez tareas programadas mantienen continuidad lógica del `CURRENT_PARENT` hasta `LISTO_REAL`, producen/corrigen/revisan/cierran directamente según su rol, y sólo tras un cierre real dejan el siguiente parent dependency-valid/unblocked/prearmed. Jules acelera únicamente cuando conviene. Ningún componente auxiliar puede convertirse otra vez en requisito implícito para que VAEP avance.
