# VAEP — MAESTRO OPERATIVO ÚNICO

Este archivo es la **única autoridad operativa** de la automatización de VariApp.

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
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_CLOSE_SLA_ROLLING_24H=72
JULES_TASKS_TARGET_ROLLING_24H_PER_WORKER=100
JULES_TASKS_TARGET_ROLLING_24H_TOTAL=600
JULES_REFILL_MAX_GAP_MINUTES=12
PARENT_MAX_DWELL_MINUTES=20
PARENT_STALL_NO_PROGRESS_MINUTES=10
CLOSURE_REVIEW_MAX_LATENCY_MINUTES=2
CLOSURE_DEBT_TRIGGER_LT=3
CLOSURE_CHAIN_SAME_RUN=TRUE
MAX_VOLUNTARY_IDLE=0
JULES_QUEUE_DEPTH_TARGET=2
JULES_PROGRAMMED_BACKLOG_TARGET_PER_WORKER=12
JULES_PROGRAMMED_BACKLOG_REFILL_FLOOR_PER_WORKER=4
JULES_PROGRAMMED_BACKLOG_TARGET_TOTAL=72
JULES_DISPATCH_ELIGIBLE_MIN_PER_WORKER=2
JULES_CURRENT_RUN_REQUIRED=TRUE
JULES_NEXT_SAFE_PREARMED_REQUIRED=TRUE
JULES_NEXT_RUN_RESERVED_REQUIRED=TRUE
LANE_REFILL_DEADLINE_SECONDS=30
SCHEDULED_RUN_LANE_REFILL_BEFORE_REVIEW=TRUE
JULES_TERMINAL_HANDOFF_SAME_RUN=TRUE
NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL=TRUE
PREARM_BEFORE_CAUSAL_CI=TRUE
VAEP_CHECKPOINTS=:00,:12,:24,:36,:48
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
```

## 1. Fuente única

1. ChatGPT/VAEP, J1/J2/J3/J4/J5/J6 y todas las automatizaciones activas deben leer **este mismo archivo** antes de decidir reglas operativas.
2. Cuando una regla cambia, **se modifica este archivo en el mismo lugar**. No se crea una copia, revisión numerada, protocolo paralelo ni documento `*-vX*`.
3. Git conserva el historial; no se crean fuentes operativas duplicadas para conservar reglas anteriores.
4. `CHANGELOG_AI.md`, `BITACORA`, Issues, artifacts, prompts anteriores y commits pueden contener etiquetas históricas; son **evidencia**, nunca autoridad ejecutable.
5. Si una fuente histórica contradice este MAESTRO, gana este MAESTRO.
6. Ningún worker puede elegir reglas por número, fecha o etiqueta histórica.

## 2. Precedencia

1. `docs/VAEP_AUTHORITY.md` — MAESTRO de reglas.
2. Manifest actual — tarea, base, scope, attempt y aceptación.
3. `AGENTS.md` — identidad, seguridad y obligación de consumir el MAESTRO.
4. `CONFIG/COLA/PLAN_MAESTRO/BITACORA/EJECUCION_MANUAL` frescos — estado y roadmap, no reglas alternativas.
5. HEAD, código, CI, tests, Issues, artifacts y sesiones actuales — realidad técnica observable.

GitHub manda para código/evidencia. Drive manda para estado operativo. El MAESTRO manda para reglas.

## 3. Equipo

- Javier: propietario y autorización final.
- ChatGPT/VAEP: controller, REVIEW_FIRST, QA, integración, corrección, CI, certificación, rollup y failover.
- Chat B (ChatGPT Business): colaborador full-access par de ChatGPT/VAEP para controller, REVIEW_FIRST, QA, corrección, integración, CI, certificación, rollup, continuidad y failover. Opera sobre `Desarrollo` bajo este MAESTRO, con las mismas funciones de control que ChatGPT/VAEP y sin crear una quinta lane Jules.
- J1/J2/J3/J4: implementers cloud CODE. J5: QA/security/regression con fallback CODE. J6: integration/recovery con fallback CODE/QA. Los seis conservan máximo un write-scope autoritativo por ejecución; entregan patch/artifact y no publican funcionalmente.
- Vibe: QA/corrector externo solo cuando VAEP lo delega.
- AntiG/Antigravity: componente de infraestructura reservado para futura reincorporación autorizada. No pertenece al equipo operativo actual.
- Codex: fuera del flujo salvo orden explícita futura del usuario.

### Workers Jules canónicos

```text
JULES_ACTIVE_WORKERS=J1,J2,J3,J4,J5,J6
JULES_ACTIVE_WORKER_COUNT=6
JULES_WORKER_REGISTRY=vaep/control/jules-workers.json
J1_ROLE=CODE_CORE
J2_ROLE=CODE_BACKEND_DATA
J3_ROLE=CODE_FRONTEND
J4_ROLE=CODE_INFRA_INTEGRATIONS
J5_ROLE=QA_SECURITY_REGRESSION__FALLBACK_CODE
J6_ROLE=INTEGRATION_RECOVERY__FALLBACK_CODE_QA
JULES_QUEUE_DEPTH_PER_WORKER=2
JULES_MAX_LIVE_RUNS_TOTAL=12
```

Los IDs operativos nuevos son exclusivamente `J1..J6`. `JULES_A..JULES_D` pueden aparecer únicamente como evidencia histórica o para terminar una sesión ya iniciada antes del cutover; no son IDs válidos para nuevos dispatches. J3 y J4 usan exclusivamente sus secretos nominales `JULES_J3_API_KEY` y `JULES_J4_API_KEY`; no existe fallback ni referencia temporal de credenciales legacy para estos workers.

### Contrato operativo Chat B

```text
CHATGPT_BUSINESS_OPERATIONAL=TRUE
CHATGPT_BUSINESS_ROLE=FULL_ACCESS_PEER_CONTROLLER_QA
CHATGPT_BUSINESS_AUTHORITY=MASTER
CHATGPT_BUSINESS_BRANCH=Desarrollo
CHATGPT_BUSINESS_REVIEW_FIRST=TRUE
CHATGPT_BUSINESS_QA_TAKEOVER=TRUE
CHATGPT_BUSINESS_CAN_INTEGRATE=TRUE
CHATGPT_BUSINESS_CAN_CERTIFY_LISTO_REAL=TRUE_ONLY_WITH_MASTER_EVIDENCE
CHATGPT_BUSINESS_IS_JULES_LANE=FALSE
```

Chat B puede ejecutar cualquier operación técnica necesaria dentro del alcance autorizado de VariApp en `Desarrollo`, incluyendo corrección causal, pruebas, CI, review, integración y cierre. No puede saltarse el MAESTRO, inventar actividad/evidencia, abrir R3+, tocar `main`/Producción/secretos ni declarar `LISTO_REAL` sin REVIEW_FIRST, DoD, gates aplicables y P0/P1=0.

### Estado canónico AntiG

```text
ANTIG_STATUS=RESERVED_INACTIVE
ANTIG_OPERATIONAL_NOW=FALSE
ANTIG_SCHEDULER=DISABLED
ANTIG_HANDOFF_PROCESSING=DISABLED
ANTIG_AUTHORITY=MASTER
ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE
ANTIG_FUTURE_REINCORPORATION=EXPLICIT_AUTHORIZATION_REQUIRED
```

Este estado es fail-closed: el árbol vigente no permite instalar scheduler AntiG ni procesar handoffs AntiG. Una reincorporación futura exige autorización explícita de Javier y un changeset posterior que cambie este mismo MAESTRO; ningún documento, script, Issue o artifact histórico puede reactivarlo por sí solo.

## 4. Seguridad y Git

- Solo `Desarrollo`.
- `main` congelada.
- PR #2 `Desarrollo -> main` OPEN + DRAFT.
- No ramas nuevas, merge, auto-merge, force-push ni reset destructivo.
- No Producción, secretos, credenciales, dominios, certificados, datos productivos, deploys ni infraestructura productiva.
- Jules no crea branch/PR/push/merge/deploy.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.

## 5. Estado y evidencia

Estados COLA válidos:

```text
PENDIENTE|EN_PROGRESO|VALIDANDO|LISTO|BLOQUEADO|CANCELADO
```

- Dispatch != ACTIVE.
- Workflow != ACTIVE.
- Issue != ACTIVE.
- ACTIVE_REAL exige sesión Jules correlacionada + actividad técnica útil reciente.
- `COMPLETED` Jules nunca equivale a `LISTO`.
- `LISTO_REAL` solo lo declara VAEP tras REVIEW_FIRST + DoD + gates/CI aplicables + P0=0/P1=0.
- Nunca fingir sesión, actividad, PASS, CI, evidencia o LISTO.

## 6. Parent-close y continuidad

La política de parent-close, dwell time y SLA está gobernada por el bloque canónico `BEGIN_AUTOMATION_POLICY`:
- `PARENT_CLOSE_FIRST=TRUE`: Cerrar CURRENT_PARENT antes de promover un sucesor dependiente.
- Los checkpoints activos provienen exclusivamente de `VAEP_CHECKPOINTS` en el bloque canónico.
- `PARENT_CLOSE_SLA_ROLLING_60M=3`: mínimo operativo de 3 padres en `LISTO_REAL` por ventana móvil de 60 minutos.
- `PARENT_CLOSE_SLA_ROLLING_24H=72`: objetivo contractual de 72 padres `LISTO_REAL` por ventana móvil de 24 horas; el contador de 24h no reemplaza el gate de 3/h, ambos deben cumplirse.
- `JULES_TASKS_TARGET_ROLLING_24H_PER_WORKER=100`: objetivo de 100 tareas Jules realmente integradas por cada worker J1/J2/J3/J4/J5/J6 en 24h. Solo cuenta un commit de integración con receipt VAEP válido, REVIEW_ACCEPTED e INTEGRATED; no cuentan busywork, dispatch, manifest, autorefill, reserva, workflow verde, sesión creada ni SESSION_COMPLETED por sí solos.
- `JULES_TASKS_TARGET_ROLLING_24H_TOTAL=600`: objetivo agregado de 600 tareas Jules validadas como INTEGRATED en 24h entre J1/J2/J3/J4/J5/J6; la fuente de verdad del KPI es `scripts/vaep/jules_integration_metrics.py --rolling-hours 24`.
- **TASK_IDENTITY_UNIQUE**: para métricas y autorefill, una tarea Jules útil se identifica por `CURRENT_PARENT + identidad funcional/semantic facet`, no por número correlativo, dispatchId, filename ni sessionId. Renumerar la misma prueba/comportamiento NO crea trabajo nuevo.
- Si una identidad funcional ya tuvo resultado `COMPLETED` + `patchPresent=true` + sesión útil, cualquier ejecución posterior equivalente se clasifica `DUPLICATE_EVIDENCE_ONLY`, no integra automáticamente y **NO CUENTA** en `TASKS_24H` ni en el objetivo de 100 por Jules.
- Un manifest/run `SUPERSEDED` antes de sesión útil no consume la identidad funcional y puede recuperarse; dedupe no debe impedir un recovery legítimo.
- El catalog floor no puede reciclar una faceta ya completada asignándole otro número/archivo. Si se agotan facetas materialmente únicas, debe emitir `UNIQUE_WORK_EXHAUSTED` y devolver prioridad a cierre/promoción o a un scope realmente nuevo del roadmap; nunca fabricar una variante nominal.
- `JULES_REFILL_MAX_GAP_MINUTES=12`: las cinco automatizaciones reconciliadoras se distribuyen uniformemente cada 12 minutos (:00/:12/:24/:36/:48). Este valor NO permite ociosidad de 12 minutos: la continuidad se garantiza con CURRENT_RUN + NEXT_RUN_RESERVED real, y al consumirse la reserva el siguiente checkpoint debe reponerla.
- `PARENT_MAX_DWELL_MINUTES=20`: Límite máximo de permanencia en un mismo parent sin progreso material.
- `PARENT_STALL_NO_PROGRESS_MINUTES`: umbral de no-progreso definido exclusivamente en el bloque canónico; al alcanzarse obliga a failover controlado.
- `MAX_VOLUNTARY_IDLE`: tolerancia de ociosidad voluntaria definida exclusivamente en el bloque canónico; cuando es cero, una lane libre recibe trabajo seguro inmediatamente.
- Trayectoria de recuperación: los cinco checkpoints activos `:00/:12/:24/:36/:48` garantizan primero `CURRENT_RUN + NEXT_RUN_RESERVED` y además mantienen un backlog programado material de `JULES_PROGRAMMED_BACKLOG_TARGET_PER_WORKER=12` por Jules, reponiéndolo antes de caer por debajo de `JULES_PROGRAMMED_BACKLOG_REFILL_FLOOR_PER_WORKER=4`. El backlog programado NO equivale a runs vivos: solo `CURRENT + NEXT` puede estar reservado/ejecutándose por lane. Con esa continuidad satisfecha, `CLOSURE_DEBT_FASTPATH` domina hasta cerrar/promover el CURRENT_PARENT. El único SLA horario canónico es `PARENT_CLOSE_SLA_ROLLING_60M=3`; ningún checkpoint histórico es operativo.
- `CLOSURE_REVIEW_MAX_LATENCY_MINUTES=2`: un terminal `READY_FOR_VAEP` que pueda decidir el cierre no puede permanecer esperando revisión administrativa más allá de este objetivo; REVIEW_FIRST/QA_TAKEOVER se drena inmediatamente.
- `CLOSURE_DEBT_TRIGGER_LT=3`: si `ROLLING60<3`, entra CLOSURE_DEBT_FASTPATH. VAEP debe intentar cerrar CURRENT_PARENT con evidencia ya existente ANTES de crear trabajo support/evidence-only adicional para ese mismo padre.
- En CLOSURE_DEBT_FASTPATH, si el padre ya cumple DoD + gates aplicables terminales + P0=0/P1=0, se declara `LISTO_REAL` inmediatamente; no se espera otro checkpoint, otro Jules ni documentación redundante.
- Si falta exactamente un gap material, solo se trabaja ese gap. Está prohibido inflar REVIEW_BACKLOG con evidencia redundante mientras exista un camino de cierre más corto.
- `CLOSURE_CHAIN_SAME_RUN=TRUE`: tras cerrar un padre, promover el siguiente dependency-valid y evaluar/cerrar inmediatamente todo padre ya pretrabajado/certificable en la MISMA corrida, repitiendo hasta `ROLLING60>=3` o hasta encontrar un blocker técnico causal exacto.
- Mantener Jules productivos en paralelo: lanes ya reservadas continúan; una lane libre recibe NEXT_SAFE material, pero ningún refill evidence-only puede retrasar un cierre ya certificable.
- `JULES_CURRENT_RUN_REQUIRED=TRUE` + `JULES_NEXT_SAFE_PREARMED_REQUIRED=TRUE`: cada lane debe conservar trabajo actual y siguiente trabajo seguro prearmado cuando exista backlog material elegible.
- `JULES_QUEUE_DEPTH_TARGET=2`: objetivo y límite físico de RUNS VIVOS por lane = exactamente `1 CURRENT_RUN + como máximo 1 NEXT_RUN_RESERVED`. Nunca crear un tercer run vivo. Esta regla NO limita el número de tareas PROGRAMADAS en catálogo.
- `NO_SUPERSEDE_PENDING=TRUE`: si ya existe un run de la misma lane en `pending|queued|in_progress` distinto del CURRENT, cualquier fuente de dispatch (timer, autorefill, manual recovery) debe NO-OP. GitHub concurrency mantiene un único pending; publicar otro puede supersederlo y está prohibido.
- `ONE_MANIFEST_ONE_RUN=TRUE`: un manifest se publica en un único commit y produce exactamente un run. Un commit externo/manual sobre `Desarrollo` usa exclusivamente `push:path`; un commit interno creado con `GITHUB_TOKEN` (que no dispara Actions recursivamente) usa exactamente un `workflow_dispatch` correlacionado por `manifest_commit=<SHA exacto>`. Está prohibido combinar ambos mecanismos para el mismo manifest.
- `CANCELLED_RUN_POLICY=FAILURE_TO_PREVENT`: una cancelación/supersession causada por VAEP, timer o control-plane es incidente operativo, no throughput. Debe corregirse la causa antes de generar más trabajo en esa lane. Sólo una cancelación externa/usuario explícita puede quedar fuera de esta clasificación.
- **CONTINUIDAD PRIMARIA EVENT-DRIVEN**: cada workflow J1/J2/J3/J4/J5/J6 ejecuta `.github/scripts/vaep-jules-autorefill.sh` al terminar su corrida (también después de timeout/fallo de lane) y reserva el siguiente NEXT_SAFE material desde `vaep/control/jules-autorefill-catalog.json`. Los checkpoints horarios son watchdog/recovery; NO son el mecanismo primario de handoff.
- El autorefill debe crear exactamente un manifest nuevo, con `primaryBaseHead` igual al padre real del commit, respetar `dispatch-admission=OPEN`, no duplicar scopes/dispatches y detenerse ante `HEAD_FREEZE_CAUSAL` real. La admisión se valida antes de construir, inmediatamente antes de publicar el ref y antes de emitir el run interno; si se cierra durante la carrera, el commit Git huérfano no se publica. Una actualización concurrente de HEAD obliga a reintentar contra el nuevo padre, nunca a publicar una base stale.
- El catálogo de autorefill es BACKLOG PROGRAMADO, separado de la cola viva. Debe mantener `JULES_PROGRAMMED_BACKLOG_TARGET_PER_WORKER=12` tareas materiales por Jules (72 agregadas), con reposición obligatoria cuando las no consumidas bajen de 4 por worker. Solo las entradas `dispatchEligible=true` pueden convertirse en runs; las futuras pueden quedar `dispatchEligible=false` hasta que sus dependencias sean válidas. La cola viva sigue limitada por `JULES_QUEUE_DEPTH_TARGET=2` (`CURRENT + NEXT`). La regeneración genérica de facetas está PROHIBIDA: el controller repone únicamente scopes genuinos/únicos del roadmap; nunca recicla una identidad completada, renumera la misma prueba ni fabrica busywork.
- `JULES_NEXT_RUN_RESERVED_REQUIRED=TRUE`: NEXT_SAFE no cuenta como continuidad real hasta que exista un workflow Jules correlacionado en estado `pending|queued|in_progress` reservado para esa lane, salvo `HEAD_FREEZE_CAUSAL` real. Un archivo/row/manifiesto sin run no satisface zero-idle.
- `LANE_REFILL_DEADLINE_SECONDS=30`: cada checkpoint debe resolver primero lanes libres o sin NEXT_RUN_RESERVED; no puede gastar más de este presupuesto en reconciliación/review antes de reservar trabajo real cuando existe SAFE_WORK.
- `SCHEDULED_RUN_LANE_REFILL_BEFORE_REVIEW=TRUE`: la primera acción material de `:00/:12/:24/:36/:48`, después del preflight mínimo, es reservar CURRENT/NEXT run de J1/J2/J3/J4/J5/J6. REVIEW/CI/certificación se drenan detrás.
- Terminal CURRENT debe liberar ownership y permitir que el NEXT_RUN_RESERVED arranque por la propia concurrencia del workflow, sin esperar otro checkpoint.
- Cuando el CURRENT terminaliza y consume la reserva existente, el mismo workflow debe ejecutar AUTOREFILL **solo después de que el runtime state confirme terminal/stall/timeout**; entonces debe dejar un nuevo run `pending|queued|in_progress` siempre que exista SAFE_WORK y no haya freeze causal. Un fallo previo a sesión/admisión/transport no autoriza post-terminal refill. Esperar al siguiente checkpoint teniendo catálogo material disponible es incumplimiento.
- `PREARM_BEFORE_CAUSAL_CI=TRUE`: el NEXT_SAFE que requiera manifest/commit debe prepararse antes de iniciar la ventana de CI causal del FUNCTIONAL_HEAD siempre que sea técnicamente posible.
- `NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL=TRUE`: una vez exista HEAD_FREEZE_CAUSAL, está prohibido mover Desarrollo con manifests/control-plane que puedan cancelar/superseder gates del FUNCTIONAL_HEAD. Durante ese freeze, Jules continúan sobre runs ya reservados, work seguro no-head-moving y la cola declarativa; el siguiente manifest se publica inmediatamente al liberar el freeze.
- Nunca false LISTO ni busywork.

- **EVIDENCE_GAP_NO_R2**: si un patch no vacio demuestra base, scope y ausencia de cambios al control-plane, pero solo faltan los marcadores `SELF_REVIEW_PASS_1`, `SELF_REVIEW_PASS_2` o `TESTS_EXECUTED`, el contrato terminal sigue invalido y no puede marcar `READY_FOR_VAEP`; el handoff pasa directamente a `EVIDENCE_GAP_REVIEW_REQUIRED` (o `QA_TAKEOVER_REQUIRED` al agotar el intento). No se consume un intento adicional de contenido ni se crea R2/R3 para reimplementar el mismo scope.

## 7. Transporte Jules

```text
J1: vaep/jules/dispatch/*.json
J2: vaep/jules-b/dispatch/*.json
J3: vaep/jules-c/dispatch/*.json
J4: vaep/jules-d/dispatch/*.json
J5: vaep/j5/dispatch/*.json
J6: vaep/j6/dispatch/*.json
```

Dispatch válido: un commit, exactamente un manifest nuevo, worker correcto, `expectedBranch=Desarrollo`, `primaryBaseHead` SHA40 del padre exacto, `taskAttempt` 1 o 2, scope y prompt no vacíos.

Fallo pre-session de path/base/schema/transporte no consume intento de contenido.

Invariantes P0/P1 de entrega y productividad:
- `IMMUTABLE_MANIFEST=TRUE`: todo intento usa un archivo NUEVO; modificar, renombrar, borrar o reutilizar un manifest histórico es `INVALID_REDISPATCH` y falla antes de reservar NEXT o crear sesión.
- `ONE_ATTEMPT_ONE_MANIFEST_ONE_DISPATCH_ID=TRUE`: ATTEMPT1 y R2 usan `dispatchId` y filename nuevos e inmutables; filename = `<dispatchId>.json`.
- Pipeline obligatorio: `TRIGGERED -> MANIFEST_ACCEPTED -> SESSION_CREATED -> SESSION_COMPLETED -> PATCH_PRESENT -> SELF_REVIEW_COMPLETE -> REVIEW_ACCEPTED -> INTEGRATED`. Solo el último estado realmente alcanzado cuenta; `COMPLETED` solo no es integración.
- `AWAITING_USER_FEEDBACK` no recibe follow-ups genéricos repetidos: capturar la pregunta real expuesta por Jules, emitir como máximo una respuesta específica ligada a `taskId + dispatchId + primaryBaseHead + taskAttempt + fileScopeHint`, y si la pregunta persiste transferir a `QA_TAKEOVER`. Si la API no expone la pregunta, registrar la ausencia explícitamente y transferir directamente a QA.
- `PARENT_STALL_NO_PROGRESS_MINUTES` se aplica a progreso observable de estado/timestamp/actividad y puede cortar antes de `JULES_LANE_BUDGET_SECONDS`; stall revoca ownership y pasa a QA_TAKEOVER.
- Antes de crear una sesión, el worker enumera sesiones remotas y bloquea cualquier sesión existente del mismo `taskId` todavía activa, incluyendo otro `dispatchId`, base o intento. El guard registra `taskId + dispatchId + primaryBaseHead + taskAttempt + session + state`; reusar un manifest cuyo dispatch ya tuvo sesión se clasifica fail-closed.
- Un `COMPLETED` entregable exige ChangeSet/gitPatch no vacío, `baseCommitId == primaryBaseHead` o una equivalencia verificable de descendiente exclusivamente control-plane, anclaje al `fileScopeHint`, ausencia de cambios al control-plane, marcador `TESTS_EXECUTED`, `SELF_REVIEW_PASS_1` y `SELF_REVIEW_PASS_2`. La equivalencia solo es válida cuando GitHub compara ambos SHA y demuestra que todos los commits intermedios pertenecen a manifests/control-plane canónico; cualquier cambio funcional, historia no verificable o deriva fuera de esa lista falla cerrado. El artifact `lifecycle.json` conserva aceptación del manifest, creación de sesión, primer IN_PROGRESS, última actividad, terminal, patch, relación de base, review e integración.
- Cuando ese contrato terminal pasa, el handoff explícito es `READY_FOR_VAEP`; su scope entregado espera REVIEW_FIRST. La lane puede reservar otro NEXT_SAFE material, independiente y no solapado conforme a las secciones 6 y 9. Revisar el artifact y mantener continuidad son obligaciones separadas.
- Toda corrida Jules debe conservar correlación `dispatchCommitSha -> manifestPath -> workflowRunId -> sessionName`; si falta un eslabón no es ACTIVE_REAL ni productividad.
- Recovery manual exige `manifest_commit` exacto y `--transport-preflight` del manifest inmutable antes de crear o reusar sesión.
- Si un stop de una sesión SUPERSEDED deja el estado remoto activo, la lane queda `QUARANTINED_REMOTE_ACTIVE_NO_DUPLICATE`; ningún recovery equivalente puede nacer hasta estado remoto terminal y RCA causal.
- Los checkpoints tienen responsabilidades distintas: `:00` despacho material/correlación; `:12` RCA-recovery/cierre; `:24` REVIEW_FIRST/certificación/integración; `:36` stall-watchdog/cierre; `:48` deuda material priorizando cierre/review/QA antes que refill. Floor/target de catálogo son observabilidad, nunca obligación de fabricar tareas.


### Admisión de nuevos dispatches

El estado machine-readable de admisión vive en `vaep/control/dispatch-admission.json` y está subordinado a este MAESTRO. Solo se permiten dos estados:

- `FROZEN`: un commit con exactamente un manifest nuevo se rechaza antes de crear sesión, consumir attempt, reservar ownership o iniciar recovery. Sesiones `ACTIVE_REAL` ya existentes no se invalidan.
- `OPEN`: el manifest continúa por el transporte Jules normal.

Reglas fail-closed:

- cero manifests nuevos en un workflow Jules => `INVALID_TRIGGER` y fallo explícito; nunca SUCCESS/NO_OP. Un workflow Jules válido exige exactamente un manifest NUEVO para su lane;
- más de un manifest nuevo => fail-closed;
- exactamente un manifest => el control state debe existir, tener contrato válido y `allowExistingActiveSessions=true`;
- control ausente, malformado, con claves desconocidas o valor distinto de `FROZEN|OPEN` => fail-closed;
- Fase 7/certificación integral autoriza el primer retorno a `OPEN` tras la migración. Después de `MIGRATION_F0_F7=CLOSED/PASS`, un hardening posterior del control-plane puede volver de `FROZEN` a `OPEN` sin reabrir fases únicamente si el HEAD de hardening tiene `VAEP engine lightweight checks=SUCCESS` y `VAEP Jules Diagnostic=SUCCESS`, no existe regresión concreta de MASTER y PR #2 permanece OPEN+DRAFT.

## 8. Retry cap

La política de reintentos está gobernada por el bloque canónico:
- `JULES_MAX_ATTEMPTS=2`: ATTEMPT=1 ejecución inicial, ATTEMPT=2 / R2 única y última corrección Jules.
- `JULES_REWORK_MAX=1`: Máximo un rework dirigido.
- R3+ está terminantemente prohibido.
- Cambiar de Jules no reinicia attempts. Work-stealing hereda attempts.
- ATTEMPT1 puede tener máximo un R2 dirigido.
- R2 fallido o bloqueado => QA_TAKEOVER por ChatGPT/VAEP; Jules pasa a otro scope material.
- No existe R3 operativo.

Al agotar R2, el runtime emite `QA_TAKEOVER_REQUIRED` con causa, taskId, dispatchId, sesión, base y artifact; el responsable es ChatGPT/VAEP. Emitir ese handoff no significa que el takeover haya sido ejecutado. Un R2 válido pasa a `READY_FOR_VAEP`, sin otro intento por falta de revisión. Fallos de evidencia se revisan primero desde el artifact disponible, sin volver a implementar por defecto. El objetivo de 100 integraciones por worker no es evidencia de cuota disponible del proveedor ni autoriza fabricar trabajo o reiniciar intentos. Se conservan dos intentos de contenido por tarea para priorizar trabajo útil.

Cada checkpoint reconcilia deuda terminal después del refill. Si no hay ejecutor conectado de REVIEW_FIRST/QA, debe persistir las tareas afectadas y declarar `REVIEW_EXECUTOR_NOT_CONFIGURED`; contar Issues o registrar ownership no certifica operación completa. La corrección e integración requieren un ejecutor real autorizado con acceso al repositorio, artifacts y pruebas. La comprobación de deuda no despacha sesiones Jules ni integra parches.

## 9. REVIEW_FIRST y handoff

Al terminal Jules:

1. marcar inmediatamente `VALIDANDO/READY_FOR_VAEP` y congelar el ownership del scope entregado;
2. liberar inmediatamente la lane Jules;
3. si existe NEXT_SAFE material, dependency-valid, no solapado y prearmado, despacharlo **antes de esperar REVIEW/CI/rollup**; `MAX_VOLUNTARY_IDLE=0` prevalece sobre espera administrativa;
4. VAEP/ChatGPT inicia REVIEW_FIRST del artifact/base/diff/scope/tests/self-review/riesgos/no ejecutados en paralelo detrás de producción;
5. PASS => integrar solo delta aprobado sobre HEAD vigente + CI causal;
6. REQUIRED en ATTEMPT1 => R2 único del scope entregado solo si sigue siendo material y no compite con el NEXT_SAFE;
7. REQUIRED en ATTEMPT2 => QA_TAKEOVER; el Jules permanece en otro scope seguro, nunca esperando el takeover.
8. PASS de REVIEW_FIRST no basta para productividad: la integración funcional debe publicarse en un commit exclusivo de un solo dispatch (`ONE_INTEGRATION_COMMIT_ONE_DISPATCH=TRUE`) y ese commit debe llevar este receipt inmutable:
   - `VAEP-Dispatch: <dispatchId>`
   - `VAEP-Task: <taskId>`
   - `VAEP-Worker: JULES_A|JULES_B|JULES_C|JULES_D`
   - `VAEP-Session: sessions/<id>`
   - `VAEP-Task-Attempt: 1|2`
   - `VAEP-Dispatch-Manifest: <ruta exacta del manifest>`
   - `VAEP-Patch-SHA256: <sha256 real del patch revisado>`
   - `VAEP-Patch-Base: <primaryBaseHead del manifest>`
   - `VAEP-Review: ACCEPTED`
   - `VAEP-Review-Evidence: <artifact/tests/review verificable>`
   - `VAEP-Scope-Decision: PASS`
   - `VAEP-Reviewed-Files: <lista ; separada exactamente igual al diff del commit>`
   - `VAEP-Tests: <evidencia real o NOT_APPLICABLE:<razón>>`
   - `VAEP-P0: 0`
   - `VAEP-P1: 0`
   - `VAEP-Integrated: TRUE`
   - `VAEP-Integration-Branch: Desarrollo`
9. `scripts/vaep/jules_integration_metrics.py` valida el receipt contra el manifest y el diff real. Receipt inválido => CI failure y **NO CUENTA**. Receipt válido => emite `VAEP_METRIC stage=REVIEW_ACCEPTED` y `VAEP_METRIC stage=INTEGRATED`.
10. KPI canónico: solamente `INTEGRATED` validado cuenta como productividad Jules. `TRIGGERED`, `MANIFEST_ACCEPTED`, `SESSION_CREATED`, `SESSION_COMPLETED`, `PATCH_PRESENT` y `SELF_REVIEW_COMPLETE` son etapas diagnósticas, nunca throughput final.

`REVIEW_FIRST` significa que todo resultado terminal entra primero a la cola de revisión de VAEP; **no** significa que el Jules deba esperar a que esa revisión termine.

Mantener `QUEUE_DEPTH_TARGET>=2` por Jules cuando exista roadmap seguro: una tarea autoritativa actual + al menos una NEXT_SAFE física prearmada en COLA con agente, scope exclusivo, dependencia real y estado canónico.

- **PREARM_BEFORE_TERMINAL obligatorio**: no esperar a que una lane quede libre para crear NEXT_SAFE. Si el worker tiene CURRENT válido y existe trabajo material seguro posterior, debe existir también un NEXT_SAFE físico ya materializado/reservable antes del terminal. Objetivo operativo por lane: `CURRENT_RUN + NEXT_SAFE_PREARMED`. Al terminalizar CURRENT, el siguiente run debe poder arrancar sin esperar otro checkpoint.
- Si no existe NEXT_SAFE material seguro, registrar explícitamente `NO_SAFE_NEXT`; está prohibido simular cola con busywork/evidence-only redundante.
- Los checkpoints deben reparar cualquier lane con `CURRENT` pero sin `NEXT_SAFE_PREARMED` cuando exista backlog elegible, incluso si CURRENT sigue `IN_PROGRESS`.

Dos auto-revisiones independientes son obligatorias antes de COMPLETED válido.

## 10. Watchdog

- >5m sin sesión + actividad útil: STALLED/BOOTSTRAP_STALLED.
- >=10m sin progreso: recovery o reassign sin duplicar ownership.
- Terminal sin review: drenar inmediatamente.
- R2 agotado: QA_TAKEOVER.
- Al exceder `JULES_LANE_BUDGET_SECONDS`, el runtime intenta una señal de detención remota únicamente mediante una operación Jules ya soportada; no inventa endpoints ni depende de detener físicamente la sesión para continuar.
- Un timeout revoca ownership local, marca la sesión `STALLED/SUPERSEDED`, libera la lane y entrega control a QA_TAKEOVER/NEXT_SAFE.
- Todo resultado tardío de una sesión `SUPERSEDED` es evidencia histórica únicamente y queda bloqueado de integración automática.
- La evidencia de timeout/supersession debe conservar `MASTER_COMMIT_SHA` y `AUTOMATION_POLICY_HASH` junto con worker/dispatch/task/attempt/session y estados antes/después.
- No dejar lane esperando review/CI si existe NEXT_SAFE material.
- Si una lane terminal queda libre y existe NEXT_SAFE, el refill debe ocurrir en la misma corrida que detecta el terminal; esperar al siguiente checkpoint es incumplimiento de continuidad.
- Antes de CADA manifest Jules, releer HEAD y usar ese SHA exacto como `primaryBaseHead`; después de publicar un manifest, releer HEAD antes de construir el siguiente. Nunca reutilizar el mismo base para varias lanes cuando cada manifest mueve Desarrollo. Si Jules devuelve un descendiente posterior, solo puede aceptarse mediante `CONTROL_PLANE_BASE_EQUIVALENCE` con evidencia GitHub de que la deriva fue exclusivamente control-plane; esa excepción no autoriza a ignorar cambios funcionales ni a reutilizar manifests.
- Un fallo de transporte pre-session por `primaryBaseHead`/ruta/trigger no consume content attempt: recuperar la MISMA tarea con un manifest nuevo sobre el parent inmediato y verificar que aparezca run correlacionado.

## 11. CI y cierre

- Proteger causalidad de Development/Acceptance/Fase8/M13/Recovery cuando apliquen.
- `HEAD_FREEZE_CAUSAL` existe únicamente cuando el HEAD funcional/integración que VAEP está certificando tiene al menos un gate crítico causal en estado `queued` o `in_progress`.
- Un workflow legacy de otro módulo, un gate global no relacionado, Vercel/deploy no aplicable, CI de otro HEAD o CI disparado únicamente por `vaep/**`/manifests/control-plane **no** constituye freeze y no puede dejar lanes Jules voluntariamente idle.
- Aplicar `CONTROL_PLANE_HEAD_EQUIVALENCE` a commits manifest/control-plane: conservar como `FUNCTIONAL_HEAD` el último HEAD funcional/integración y permitir handoffs Jules mientras no se invalide evidencia causal crítica.
- No mover HEAD con un manifest si invalidaría evidencia causal crítica activa del `FUNCTIONAL_HEAD`.
- Con `NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL=TRUE`, cualquier intento de dispatch que requiera commit durante freeze se mantiene PREARMED/WAITING_CAUSAL_GATE y NO se publica hasta que el gate crítico quede terminal. Esto evita HEAD churn y cancelaciones de CI.
- Durante un freeze causal real, ejecutar trabajo compatible y drenar REVIEW_FIRST/QA_TAKEOVER; al quedar terminal el gate, recalcular freeze desde cero y publicar inmediatamente el NEXT_SAFE pendiente si sigue siendo válido.
- Fallo causal interno se corrige; ruido externo o fallo no causal se registra pero no se convierte en blocker falso ni serializa el CURRENT_PARENT.
- Cierre requiere DoD real, gates/CI aplicables terminales y P0/P1=0.
- Estado de cierre es monotónico: una tarea/padre con evidencia canónica `LISTO`/`LISTO_REAL` no puede volver a `EN_PROGRESO`/`PENDIENTE` por una fila stale. Si COLA contradice BITACORA/GitHub/certificación fresca, reconciliar COLA; reabrir solo con evidencia nueva explícita de defecto causal que invalide el cierre.

## 12. Checkpoints de automatización activos

```text
:00 PRIMARY / DAILY THROUGHPUT
:12 RECOVERY / CLOSURE
:24 REVIEW / CERT / CLOSE
:36 WATCHDOG / CLOSURE
:48 DEBT CORRECTOR
```

Estos son los únicos cinco checkpoints programados. Cualquier referencia histórica a `:05/:10/:15/:20/:25/:30/:35/:40/:45/:50/:55` queda explícitamente superseded y no es ejecutable.

Todas consumen **este MAESTRO**. Ninguna mantiene reglas por etiqueta numérica.

Orden mínimo obligatorio de cada checkpoint:
1. preflight mínimo: HEAD/FUNCTIONAL_HEAD/CURRENT_PARENT + `ROLLING60/DEFICIT` + `ROLLING24H_PARENT` + `JULES24H_J1/J2/J3/J4/J5/J6/TOTAL` + estado J1/J2/J3/J4/J5/J6;
2. dentro de `LANE_REFILL_DEADLINE_SECONDS`, ejecutar **LANE_REFILL_HARD_FIRST**: toda lane sin CURRENT válido o sin NEXT_RUN_RESERVED y con SAFE_WORK debe recibir/reservar un workflow Jules real. No se inicia review largo, CI global ni auditoría antes de esto;
3. si `ROLLING60<3`, ejecutar **CLOSURE_DEBT_FASTPATH**: drenar terminales/REVIEW_FIRST/QA_TAKEOVER del camino crítico y cerrar CURRENT_PARENT inmediatamente si ya es certificable; no crear soporte/evidencia redundante;
4. mantener **LANE_REFILL_CONTINUITY** J1/J2/J3/J4/J5/J6 usando runs/sesiones y NEXT_SAFE físicas; verificar transporte/run de cada dispatch sin retrasar un cierre certificable;
5. después de cada `LISTO_REAL`, promover y evaluar el siguiente parent en la misma corrida; encadenar cierres hasta recuperar `ROLLING60>=3` o documentar blocker causal exacto;
6. persistir BITACORA con `LANE_REFILL_RESULT J1/J2/J3/J4/J5/J6`, `ROLLING60`, `DEFICIT`, `ROLLING24H_PARENT`, `JULES24H_J1/J2/J3/J4/J5/J6/TOTAL`, `JULES24H_DEFICIT_J1/J2/J3/J4/J5/J6`, `CLOSURE_DEBT_MODE`, `REVIEW_BACKLOG` y `NEXT_CLOSE_TARGET`.
7. Si cualquier Jules está por debajo de la trayectoria proporcional de 100/24h y existe SAFE_WORK, refill gana prioridad sobre soporte administrativo; si padres están por debajo de 72/24h o 3/60m, REVIEW/CERT/CLOSE gana prioridad sobre evidencia redundante.

Una corrida con `ROLLING60<3` y un parent certificable no puede terminar sin cerrar ese parent. Una corrida con lane libre + NEXT_SAFE segura tampoco puede terminar en status-only, review-only o CI-wait-only.

## 13. Cambio del MAESTRO

Cuando Javier cambie una regla:

1. editar `docs/VAEP_AUTHORITY.md`;
2. actualizar únicamente referencias técnicas necesarias para seguir apuntando al MAESTRO;
3. sincronizar CONFIG/EJECUCION_MANUAL si cambia el estado declarativo;
4. no crear otro protocolo, documento o script numerado;
5. Git/CHANGELOG registran historia sin convertirse en autoridad.

**Regla absoluta: una sola fuente operativa, un solo MAESTRO, sin selección por etiquetas numéricas.**

## Certificaciones cerradas — no reapertura automática

- Fase 8 y M13 son certificaciones cerradas. No pueden dispararse por `push`, `pull_request`, timers, autorefill, commits de Jules ni cambios ordinarios en Desarrollo.
- Su única vía de reejecución es `workflow_dispatch` con autorización explícita del propietario y el token textual `AUTORIZADO_REABRIR`.
- Ejecutar nuevamente una certificación cerrada sin esa autorización se considera incidente de control-plane, no avance.
