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
- Checkpoints declarados: `:00`, `:15`, `:30`, `:45`, `:55`.
- `PARENT_CLOSE_SLA_ROLLING_60M=3`: Objetivo de 3 parent microtareas en `LISTO_REAL` por ventana móvil de 60 minutos cuando sea técnicamente alcanzable.
- `PARENT_MAX_DWELL_MINUTES=20`: Límite máximo de permanencia en un mismo parent sin progreso material.
- `PARENT_STALL_NO_PROGRESS_MINUTES=10`: Diez minutos sin progreso obliga a failover controlado.
- `MAX_VOLUNTARY_IDLE=0`: Lane libre recibe trabajo seguro inmediatamente.
- Trayectoria orientativa: `:15 >=1`, `:30 >=2`, `:45 >=3`; `:55` corrige deuda/huecos.
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

1. `VALIDANDO/READY_FOR_VAEP`;
2. revisar artifact/base/diff/scope/tests/self-review/riesgos/no ejecutados;
3. PASS => integrar solo delta aprobado sobre HEAD vigente + CI causal;
4. REQUIRED en ATTEMPT1 => R2 único;
5. REQUIRED en ATTEMPT2 => QA_TAKEOVER;
6. reasignar lane a NEXT_SAFE cuando exista.

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

## 11. CI y cierre

- Proteger causalidad de Development/Acceptance/Fase8/M13/Recovery cuando apliquen.
- No mover HEAD con un manifest si invalidaría evidencia causal crítica activa.
- Durante freeze, ejecutar trabajo compatible.
- Fallo causal interno se corrige; ruido externo no se convierte en blocker falso.
- Cierre requiere DoD real, gates/CI aplicables terminales y P0/P1=0.

## 12. Las cinco automatizaciones

```text
:00 PRIMARY
:15 RECOVERY/CLOSURE
:30 REVIEW/CERT/CLOSE/HANDOFF
:45 WATCHDOG/CLOSURE
:55 BACKUP/CORRECTOR
```

Todas consumen **este MAESTRO**. Ninguna mantiene reglas por etiqueta numérica.

## 13. Cambio del MAESTRO

Cuando Javier cambie una regla:

1. editar `docs/VAEP_AUTHORITY.md`;
2. actualizar únicamente referencias técnicas necesarias para seguir apuntando al MAESTRO;
3. sincronizar CONFIG/EJECUCION_MANUAL si cambia el estado declarativo;
4. no crear otro protocolo, documento o script numerado;
5. Git/CHANGELOG registran historia sin convertirse en autoridad.

**Regla absoluta: una sola fuente operativa, un solo MAESTRO, sin selección por etiquetas numéricas.**
