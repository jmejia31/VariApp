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
PARENT_MAX_DWELL_MINUTES=20
PARENT_STALL_NO_PROGRESS_MINUTES=10
CLOSURE_REVIEW_MAX_LATENCY_MINUTES=2
CLOSURE_DEBT_TRIGGER_LT=3
CLOSURE_CHAIN_SAME_RUN=TRUE
MAX_VOLUNTARY_IDLE=0
JULES_QUEUE_DEPTH_TARGET=2
JULES_CURRENT_RUN_REQUIRED=TRUE
JULES_NEXT_SAFE_PREARMED_REQUIRED=TRUE
JULES_NEXT_RUN_RESERVED_REQUIRED=TRUE
LANE_REFILL_DEADLINE_SECONDS=90
SCHEDULED_RUN_LANE_REFILL_BEFORE_REVIEW=TRUE
JULES_TERMINAL_HANDOFF_SAME_RUN=TRUE
NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL=TRUE
PREARM_BEFORE_CAUSAL_CI=TRUE
VAEP_CHECKPOINTS=:00,:15,:30,:45,:55
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
```

## 1. Fuente única

1. ChatGPT/VAEP, Jules A/B/C/D y las cinco automatizaciones deben leer **este mismo archivo** antes de decidir reglas operativas.
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
- Jules A/B/C/D: implementers cloud; máximo un write-scope autoritativo por Jules; entregan patch/artifact y no publican funcionalmente.
- Vibe: QA/corrector externo solo cuando VAEP lo delega.
- AntiG/Antigravity: componente de infraestructura reservado para futura reincorporación autorizada. No pertenece al equipo operativo actual.
- Codex: fuera del flujo salvo orden explícita futura del usuario.

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
- `PARENT_CLOSE_SLA_ROLLING_60M=3`: Objetivo de 3 parent microtareas en `LISTO_REAL` por ventana móvil de 60 minutos cuando sea técnicamente alcanzable.
- `PARENT_MAX_DWELL_MINUTES=20`: Límite máximo de permanencia en un mismo parent sin progreso material.
- `PARENT_STALL_NO_PROGRESS_MINUTES`: umbral de no-progreso definido exclusivamente en el bloque canónico; al alcanzarse obliga a failover controlado.
- `MAX_VOLUNTARY_IDLE`: tolerancia de ociosidad voluntaria definida exclusivamente en el bloque canónico; cuando es cero, una lane libre recibe trabajo seguro inmediatamente.
- Trayectoria obligatoria de recuperación cuando sea técnicamente alcanzable: `:15 >=1`, `:30 >=2`, `:45 >=3`; `:55` corrige deuda/huecos.
- `CLOSURE_REVIEW_MAX_LATENCY_MINUTES=2`: un terminal `READY_FOR_VAEP` que pueda decidir el cierre no puede permanecer esperando revisión administrativa más allá de este objetivo; REVIEW_FIRST/QA_TAKEOVER se drena inmediatamente.
- `CLOSURE_DEBT_TRIGGER_LT=3`: si `ROLLING60<3`, entra CLOSURE_DEBT_FASTPATH. VAEP debe intentar cerrar CURRENT_PARENT con evidencia ya existente ANTES de crear trabajo support/evidence-only adicional para ese mismo padre.
- En CLOSURE_DEBT_FASTPATH, si el padre ya cumple DoD + gates aplicables terminales + P0=0/P1=0, se declara `LISTO_REAL` inmediatamente; no se espera otro checkpoint, otro Jules ni documentación redundante.
- Si falta exactamente un gap material, solo se trabaja ese gap. Está prohibido inflar REVIEW_BACKLOG con evidencia redundante mientras exista un camino de cierre más corto.
- `CLOSURE_CHAIN_SAME_RUN=TRUE`: tras cerrar un padre, promover el siguiente dependency-valid y evaluar/cerrar inmediatamente todo padre ya pretrabajado/certificable en la MISMA corrida, repitiendo hasta `ROLLING60>=3` o hasta encontrar un blocker técnico causal exacto.
- Mantener Jules productivos en paralelo: lanes ya reservadas continúan; una lane libre recibe NEXT_SAFE material, pero ningún refill evidence-only puede retrasar un cierre ya certificable.
- `JULES_CURRENT_RUN_REQUIRED=TRUE` + `JULES_NEXT_SAFE_PREARMED_REQUIRED=TRUE`: cada lane debe conservar trabajo actual y siguiente trabajo seguro prearmado cuando exista backlog material elegible.
- `JULES_QUEUE_DEPTH_TARGET=2`: objetivo mínimo por lane = CURRENT + NEXT_SAFE.
- `JULES_NEXT_RUN_RESERVED_REQUIRED=TRUE`: NEXT_SAFE no cuenta como continuidad real hasta que exista un workflow Jules correlacionado en estado `pending|queued|in_progress` reservado para esa lane, salvo `HEAD_FREEZE_CAUSAL` real. Un archivo/row/manifiesto sin run no satisface zero-idle.
- `LANE_REFILL_DEADLINE_SECONDS=90`: cada checkpoint debe resolver primero lanes libres o sin NEXT_RUN_RESERVED; no puede gastar más de este presupuesto en reconciliación/review antes de reservar trabajo real cuando existe SAFE_WORK.
- `SCHEDULED_RUN_LANE_REFILL_BEFORE_REVIEW=TRUE`: la primera acción material de :00/:15/:30/:45/:55, después del preflight mínimo, es reservar CURRENT/NEXT run de A/B/C/D. REVIEW/CI/certificación se drenan detrás.
- Terminal CURRENT debe liberar ownership y permitir que el NEXT_RUN_RESERVED arranque por la propia concurrencia del workflow, sin esperar otro checkpoint.
- `PREARM_BEFORE_CAUSAL_CI=TRUE`: el NEXT_SAFE que requiera manifest/commit debe prepararse antes de iniciar la ventana de CI causal del FUNCTIONAL_HEAD siempre que sea técnicamente posible.
- `NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL=TRUE`: una vez exista HEAD_FREEZE_CAUSAL, está prohibido mover Desarrollo con manifests/control-plane que puedan cancelar/superseder gates del FUNCTIONAL_HEAD. Durante ese freeze, Jules continúan sobre runs ya reservados, work seguro no-head-moving y la cola declarativa; el siguiente manifest se publica inmediatamente al liberar el freeze.
- Nunca false LISTO ni busywork.

## 7. Transporte Jules

```text
A: vaep/jules/dispatch/*.json
B: vaep/jules-b/dispatch/*.json
C: vaep/jules-c/dispatch/*.json
D: vaep/jules-d/dispatch/*.json
```

Dispatch válido: un commit, exactamente un manifest nuevo, worker correcto, `expectedBranch=Desarrollo`, `primaryBaseHead` SHA40 del padre exacto, `taskAttempt` 1 o 2, scope y prompt no vacíos.

Fallo pre-session de path/base/schema/transporte no consume intento de contenido.

### Admisión de nuevos dispatches

El estado machine-readable de admisión vive en `vaep/control/dispatch-admission.json` y está subordinado a este MAESTRO. Solo se permiten dos estados:

- `FROZEN`: un commit con exactamente un manifest nuevo se rechaza antes de crear sesión, consumir attempt, reservar ownership o iniciar recovery. Sesiones `ACTIVE_REAL` ya existentes no se invalidan.
- `OPEN`: el manifest continúa por el transporte Jules normal.

Reglas fail-closed:

- cero manifests nuevos => `NO_OP/exit 0`, sin consultar ni consumir admisión;
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

## 9. REVIEW_FIRST y handoff

Al terminal Jules:

1. marcar inmediatamente `VALIDANDO/READY_FOR_VAEP` y congelar el ownership del scope entregado;
2. liberar inmediatamente la lane Jules;
3. si existe NEXT_SAFE material, dependency-valid, no solapado y prearmado, despacharlo **antes de esperar REVIEW/CI/rollup**; `MAX_VOLUNTARY_IDLE=0` prevalece sobre espera administrativa;
4. VAEP/ChatGPT inicia REVIEW_FIRST del artifact/base/diff/scope/tests/self-review/riesgos/no ejecutados en paralelo detrás de producción;
5. PASS => integrar solo delta aprobado sobre HEAD vigente + CI causal;
6. REQUIRED en ATTEMPT1 => R2 único del scope entregado solo si sigue siendo material y no compite con el NEXT_SAFE;
7. REQUIRED en ATTEMPT2 => QA_TAKEOVER; el Jules permanece en otro scope seguro, nunca esperando el takeover.

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
- Antes de CADA manifest Jules, releer HEAD y usar ese SHA exacto como `primaryBaseHead`; después de publicar un manifest, releer HEAD antes de construir el siguiente. Nunca reutilizar el mismo base para varias lanes cuando cada manifest mueve Desarrollo.
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

## 12. Las cinco automatizaciones

```text
:00 PRIMARY
:15 RECOVERY/CLOSURE
:30 REVIEW/CERT/CLOSE/HANDOFF
:45 WATCHDOG/CLOSURE
:55 BACKUP/CORRECTOR
```

Todas consumen **este MAESTRO**. Ninguna mantiene reglas por etiqueta numérica.

Orden mínimo obligatorio de cada checkpoint:
1. preflight mínimo: HEAD/FUNCTIONAL_HEAD/CURRENT_PARENT + `ROLLING60/DEFICIT` + estado A/B/C/D;
2. dentro de `LANE_REFILL_DEADLINE_SECONDS`, ejecutar **LANE_REFILL_HARD_FIRST**: toda lane sin CURRENT válido o sin NEXT_RUN_RESERVED y con SAFE_WORK debe recibir/reservar un workflow Jules real. No se inicia review largo, CI global ni auditoría antes de esto;
3. si `ROLLING60<3`, ejecutar **CLOSURE_DEBT_FASTPATH**: drenar terminales/REVIEW_FIRST/QA_TAKEOVER del camino crítico y cerrar CURRENT_PARENT inmediatamente si ya es certificable; no crear soporte/evidencia redundante;
4. mantener **LANE_REFILL_CONTINUITY** A/B/C/D usando runs/sesiones y NEXT_SAFE físicas; verificar transporte/run de cada dispatch sin retrasar un cierre certificable;
5. después de cada `LISTO_REAL`, promover y evaluar el siguiente parent en la misma corrida; encadenar cierres hasta recuperar `ROLLING60>=3` o documentar blocker causal exacto;
6. persistir BITACORA con `LANE_REFILL_RESULT A/B/C/D`, `ROLLING60`, `DEFICIT`, `CLOSURE_DEBT_MODE`, `REVIEW_BACKLOG` y `NEXT_CLOSE_TARGET`.

Una corrida con `ROLLING60<3` y un parent certificable no puede terminar sin cerrar ese parent. Una corrida con lane libre + NEXT_SAFE segura tampoco puede terminar en status-only, review-only o CI-wait-only.

## 13. Cambio del MAESTRO

Cuando Javier cambie una regla:

1. editar `docs/VAEP_AUTHORITY.md`;
2. actualizar únicamente referencias técnicas necesarias para seguir apuntando al MAESTRO;
3. sincronizar CONFIG/EJECUCION_MANUAL si cambia el estado declarativo;
4. no crear otro protocolo, documento o script numerado;
5. Git/CHANGELOG registran historia sin convertirse en autoridad.

**Regla absoluta: una sola fuente operativa, un solo MAESTRO, sin selección por etiquetas numéricas.**
