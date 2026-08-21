# Reglas obligatorias de colaboración — VariApp

Este archivo es vinculante para Javier Mejía, Codex, AntiG/Antigravity, ChatGPT, Vibe, Jules A/B/C/D y cualquier agente autorizado.

## 0. Gate obligatorio de identidad

Antes de analizar, editar, ejecutar o publicar:

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Con acceso local, Javier/Codex/AntiG ejecutan:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\iniciar-sesion-ia.ps1
```

Con acceso remoto, ChatGPT/VAEP confirma repositorio, `Desarrollo`, HEAD actual y fuentes canónicas. Una sesión pertenece a un solo proyecto; contexto de otros proyectos no es confiable.

## 1. Autoridad y precedencia

Para control plane global:

```text
CONFIG.RUNNER_PROTOCOL_VERSION=VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION
```

Para Jules A/B/C/D:

```text
CONFIG.JULES_PROTOCOL_VERSION=V3.20_CURRENT
JULES_MAX_ATTEMPTS_PER_TASK=2
JULES_REWORK_MAX=1
JULES_R3_PLUS=PROHIBIDO
```

Orden obligatorio para Jules:

1. `docs/VAEP_AUTHORITY.md`;
2. `docs/VAEP_V320_RETRY_CAP.md`;
3. manifest actual;
4. `AGENTS.md`;
5. `docs/VAEP_JULES.md`;
6. Plan Maestro/CONFIG/COLA/BITACORA trasladados por VAEP;
7. HEAD/código/pruebas actuales.

Cualquier referencia operativa incompatible a v3.19 o anterior es histórica y no puede gobernar nuevos dispatches. Los cuatro workflows Jules deben ejecutar exclusivamente `.github/scripts/vaep-jules-worker-v320.sh`.

Fuentes canónicas adicionales:

- `PROJECT_CONTEXT.md` — contexto técnico.
- `TASKS.md` — pendientes resumidos.
- `CHANGELOG_AI.md` — evidencia colaborativa.
- `PLAN_EJECUCION_AUTONOMA.md` — protocolo autónomo general.
- Plan rector Drive — Plan Maestro ERP V5.
- Sheet VAEP — `CONFIG/COLA/BITACORA` operativos.

GitHub es autoridad técnica/evidencia; Drive es control operativo.

## 2. Equipo y acceso

- Javier: propietario, prioridades y autorizaciones finales.
- Codex: implementación/pruebas desde checkout local autorizado.
- AntiG/Antigravity: implementación/pruebas desde checkout local autorizado.
- ChatGPT/VAEP: control plane, QA, arquitectura, reconciliación, integración, publicación remota autorizada, CI, certificación y failover.
- Vibe: QA/corrector externo autorizado cuando VAEP delega `QA_TAKEOVER`.
- Jules A/B/C/D: implementers autónomos confiables en workspace cloud, máximo un write-scope autoritativo por cuenta; entregan `ChangeSet/gitPatch` y nunca publican directamente.

Solo Javier/Codex/AntiG se consideran con acceso local al checkout de la PC. ChatGPT/Vibe/Jules no deben afirmar acceso local por tener conectores remotos.

## 3. Git y Producción — reglas inviolables

- `Desarrollo` es la única rama de trabajo.
- `main` está congelada.
- PR #2 `Desarrollo -> main` permanece Draft.
- No crear ramas adicionales sin autorización explícita.
- No merge/auto-merge/force-push/reset destructivo.
- No tocar Producción, secretos, credenciales, dominios, certificados, bases/datos productivos, servicios, deploys o infraestructura productiva.
- No ejecutar migraciones productivas sin autorización expresa.
- Jules: no branch, PR, push, merge, deploy ni publicación funcional; solo artifact/patch.

Preservar trabajo concurrente ajeno y revalidar HEAD antes de publicar.

## 4. Rendimiento y continuidad

1. Reutilizar `PROJECT_CONTEXT.md`, bitácora, commits y evidencia previa.
2. No reescanear todo el repo salvo cambio estructural o causa técnica real.
3. Revisar primero archivos objetivo y dependencias directas.
4. No releer archivos ya documentados si no cambiaron.
5. Buscar por símbolo/ruta antes de listados recursivos.
6. Agrupar validaciones cuando sea seguro.
7. Elegir el cambio suficiente de menor superficie.
8. Evitar temporales y trabajo redundante.
9. Continuar mientras exista trabajo autorizado/seguro y capacidad real.
10. Nunca fingir actividad, PASS, CI, sesión, progreso o `LISTO`.

`MAX_VOLUNTARY_IDLE=0` para lanes Jules cuando exista trabajo seguro.

## 5. Evidencia y validación

Todo changeset intencional debe dejar evidencia trazable. Actualizar `CHANGELOG_AI.md`; `TASKS.md` si cambia estado/pendiente; gobierno solo si cambian reglas/accesos.

Validación proporcional:

- docs/gobierno: diff, sintaxis y consistencia;
- backend: build/tests dirigidos; ampliar para seguridad/datos/migraciones/cierre;
- frontend: lint/build/tests; E2E en auth/permisos/navegación/facturación/flujos críticos;
- gates ERP: DoD, migraciones, seguridad, RBAC, auditoría, QA, documentación, rollback y regresión aplicable;
- Jules: verificar `baseCommitId`, attempt, scope, diff, archivos, pruebas, auto-review, observaciones, riesgos y compatibilidad con HEAD antes de integrar.

`COMPLETED` Jules nunca equivale a `LISTO`.

## 6. Estados de COLA

Únicos valores válidos:

```text
PENDIENTE|EN_PROGRESO|VALIDANDO|LISTO|BLOQUEADO|CANCELADO
```

Metadata de workers, waits, dispatches, timestamps, SHA, attempts o CI nunca se escribe en `ESTADO`.

Una tarea se promueve a `LISTO` solo con evidencia suficiente, dependencias válidas y cero P0/P1.

## 7. Cuatro Jules y ownership

Para padres nuevos/no iniciados cuando aplique:

```text
.1 = Jules A
.2 = Jules B
.3 = Jules C
.4 = Jules D
ChatGPT/VAEP = QA/controller/release/failover externo
```

Varios Jules pueden colaborar sobre el mismo padre o tarea lógica solo con scopes de escritura mutuamente excluyentes o review/tests/read-only. Nunca dos writers sobre el mismo archivo/scope.

Un dispatch nuevo válido = un commit = exactamente un manifest nuevo del worker correspondiente.

Paths:

```text
A: vaep/jules/dispatch/*.json
B: vaep/jules-b/dispatch/*.json
C: vaep/jules-c/dispatch/*.json
D: vaep/jules-d/dispatch/*.json
```

## 8. Retry Cap Jules v3.20

Regla dura:

```text
ATTEMPT=1          ejecución inicial
ATTEMPT=2 / R2     única y última corrección Jules
R3+                PROHIBIDO
```

Si ATTEMPT=1 falla REVIEW-FIRST, se permite como máximo un R2 dirigido. Si ATTEMPT=2 conserva REQUIRED/BLOCKER/P0/P1, scope leak, evidence mismatch, contrato incorrecto o defecto que impida `LISTO`:

```text
JULES_RETRY_EXHAUSTED
OWNER=CHATGPT_VAEP_VIBE
ACTION=QA_TAKEOVER_CORRECT_TEST_CERTIFY
```

No devolver la tarea a ningún Jules. Cambiar de Jules no reinicia el contador; work-stealing hereda attempts.

Un dispatch sin sesión ni primera actividad útil es fallo de bootstrap/infraestructura y puede recuperarse de forma controlada, pero ese recovery no habilita más de dos intentos de contenido.

## 9. Handoff y cero idle

Al terminal Jules:

1. resultado -> `VALIDANDO`/REVIEW-FIRST;
2. scope anterior queda congelado para QA;
3. lane Jules se reutiliza inmediatamente si existe trabajo seguro;
4. si ATTEMPT=1 requiere fix, solo un R2 puede volver al Jules;
5. si R2 falla, QA externo corrige y el Jules pasa a otra tarea segura.

Prioridad de lane libre:

1. único R2 todavía permitido de su tarea cuando corresponda;
2. trabajo que cierre el padre actual con scope exclusivo;
3. siguiente SAFE elegible/preasignado;
4. preflight/tests/security/contracts/performance/docs útiles ante dependencia real.

No reabrir `LISTO` solo para ocupar capacidad. No duplicar preflights certificados.

## 10. Sprint 40 — regla extraordinaria vigente

```text
SPRINT_START_AT=2026-08-20T22:45:00-06:00
SPRINT_DEADLINE_AT=2026-08-21T06:00:00-06:00
SPRINT_TIMEZONE=America/Tegucigalpa
SPRINT_PARENT_TARGET=40
```

Objetivo operativo: 40 nuevos padres reales `MICROTAREA` en `LISTO` desde el inicio del sprint. No cuentan `MICROTAREA_HIJA`, support packets, preflights repetidos, manifests, sesiones ni `COMPLETED` sin QA/DoD.

Los cuatro Jules deben recibir esta meta en cada sesión v3.20. Checkpoints agregados :00/:15/:30/:45 deben revisar salud A/B/C/D, tarea/attempt, terminales, review queue, QA takeovers, padres `LISTO`, faltantes hasta 40, velocidad requerida y blockers.

La meta no autoriza saltar dependencias, bajar tests, aceptar P0/P1, false PASS/LISTO, integrar stale patches ni tocar main/Producción/secrets.

## 11. Watchdog y actividad real

Dispatch != ACTIVE. Jules ACTIVE exige sesión correlacionada + actividad técnica útil.

- >5 min sin primera actividad útil: `BOOTSTRAP_STALLED`.
- >=10 min sin progreso: recovery/failover controlado, sin duplicar ownership.
- `PAUSED` sin trabajo útil: no ACTIVE.
- terminal: review y handoff inmediatos.

El worker v3.20 resuelve waits rutinarios inline; máximo tres auto-followups por ejecución y luego `AUTO_FEEDBACK_EXHAUSTED`.

## 12. Handoff técnico GitHub

Al terminal, `.github/scripts/vaep-jules-worker-v320.sh` crea Issue de resultado con:

```text
protocol=v3.20
taskAttempt=1|2
controllerHandoff=REVIEW_IMMEDIATELY_AND_ASSIGN_NEXT_SAFE
SPRINT_PARENT_TARGET=40
```

Esa Issue es la señal técnica inmediata para el control plane. Los checkpoints ChatGPT :00/:15/:30/:45 son la red de seguridad de recolección/reasignación; no existe permiso para dejar una entrega terminal esperando por conveniencia.

## 13. Definition of Done y cierre

Una tarea solo puede quedar `LISTO` cuando:

```text
requisito funcional completado
+ arquitectura/persistencia/API/frontend correctos cuando apliquen
+ permisos/auditoría correctos
+ tests/migración/documentación correctos
+ observabilidad cuando aplique
+ regresión verde
+ cero P0/P1
+ evidencia real
```

Rapidez nunca sustituye calidad.

## 14. Commits y handoff humano

Formato recomendado:

```text
<tipo>(<área>): <descripción> [agente]
```

Cada entrega debe indicar proyecto, objetivo, validaciones reales, riesgos/pendientes y SHA. Para Jules incluir `dispatchId`, `session`, `taskAttempt`, `baseCommitId`, resultado de review y decisión PASS/R2/QA_TAKEOVER.
