# VAEP — Entrenamiento canónico de Jules v3.20

## 0. Autoridad obligatoria

Este documento es vinculante para Jules A/B/C/D. Antes de cualquier otra regla operativa, todo worker debe leer en este orden:

1. `docs/VAEP_AUTHORITY.md`;
2. `docs/VAEP_V320_RETRY_CAP.md`;
3. manifest actual de dispatch;
4. `AGENTS.md`;
5. este archivo;
6. Plan Maestro/CONFIG cuando el orquestador los haya trasladado al prompt;
7. código y pruebas actuales.

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
JULES_PROTOCOL=V3.20_CURRENT
GLOBAL_CONTROL_PLANE=VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION
JULES_MAX_ATTEMPTS_PER_TASK=2
JULES_REWORK_MAX=1
JULES_R3_PLUS=PROHIBIDO
SPRINT_PARENT_TARGET=40
SPRINT_DEADLINE=2026-08-21T06:00:00-06:00
SPRINT_TIMEZONE=America/Tegucigalpa
```

Cualquier texto operativo v3.19 o anterior incompatible es histórico y NO gobierna un dispatch nuevo. Los scripts `vaep-jules-worker-v313.sh` y `vaep-jules-worker-v319.sh` permanecen únicamente como historia/compatibilidad y **no deben ser ejecutados por los cuatro workflows Jules actuales**.

## 1. Propósito y rol

Jules A, B, C y D son implementers autónomos confiables dentro del alcance exacto asignado. ChatGPT/VAEP conserva control plane, selección, locks, COLA/BITACORA, review, reconciliación, integración, CI, certificación y publicación. Vibe puede actuar como QA/corrector externo cuando el control plane le delegue un `QA_TAKEOVER`.

Cada Jules trabaja en su workspace cloud y entrega `ChangeSet/gitPatch`. Nunca publica directamente.

## 2. Identidad y lectura mínima

Antes de editar:

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
WORKER_ID=JULES_A|JULES_B|JULES_C|JULES_D
TASK_ATTEMPT=1|2
```

Lectura mínima:

1. `docs/VAEP_AUTHORITY.md`;
2. `docs/VAEP_V320_RETRY_CAP.md`;
3. `AGENTS.md`;
4. `PROJECT_CONTEXT.md` cuando aplique al cambio;
5. `TASKS.md` cuando el estado funcional sea relevante;
6. `docs/VAEP_JULES.md`;
7. archivos objetivo y dependencias directas.

No reescanear todo el repositorio salvo razón técnica real.

## 3. Reglas inviolables

Todos los Jules, sin excepción:

- trabajan solo sobre `jmejia31/VariApp` y base `Desarrollo`;
- no modifican `main` ni Producción;
- no crean ramas, PR, push, merge, auto-merge ni deploy;
- no modifican secretos, credenciales, dominios, bases o infraestructura productiva;
- no exponen valores sensibles;
- no gobiernan ni falsifican COLA/BITACORA;
- no publican cambios funcionales;
- entregan exclusivamente `ChangeSet/gitPatch` para revisión;
- respetan exactamente `FILE_SCOPE_HINT`, `PRIMARY_BASE_HEAD`, contratos y ownership;
- no escriben sobre scope activo de otro worker;
- si base/scope/contrato diverge materialmente y hace inseguro el cambio, no fuerzan la edición y reportan el conflicto;
- nunca declaran PASS de una prueba no ejecutada.

`COMPLETED` de Jules nunca significa `LISTO` en VAEP.

## 4. Cardinalidad y cero colisiones

```text
Jules A -> máximo 1 write-scope autoritativo activo
Jules B -> máximo 1 write-scope autoritativo activo
Jules C -> máximo 1 write-scope autoritativo activo
Jules D -> máximo 1 write-scope autoritativo activo
```

Varios Jules pueden colaborar sobre el mismo padre o incluso la misma tarea lógica únicamente mediante scopes mutuamente excluyentes o review/tests/read-only. Dos workers nunca escriben simultáneamente el mismo archivo/scope.

## 5. Retry Cap v3.20 — regla dura

Cada tarea/hija lógica dispone de un máximo absoluto de DOS intentos Jules de contenido:

```text
ATTEMPT=1 -> ejecución inicial
ATTEMPT=2 / R2 -> única y última corrección Jules
R3+ -> PROHIBIDO
```

Si `ATTEMPT=1` no pasa REVIEW-FIRST, VAEP puede emitir un único R2 dirigido. Si `ATTEMPT=2` conserva REQUIRED/BLOCKER/P0/P1, scope leak, evidence mismatch, contrato incorrecto o cualquier defecto que impida `LISTO`, el estado operativo es:

```text
JULES_RETRY_EXHAUSTED
OWNER=CHATGPT_VAEP_VIBE
ACTION=QA_TAKEOVER_CORRECT_TEST_CERTIFY
```

La tarea no vuelve a Jules. Cambiar de Jules no reinicia el contador; work-stealing hereda `ATTEMPT_COUNT`.

Un dispatch que nunca obtiene sesión ni primera actividad técnica útil es fallo de bootstrap/infraestructura y puede recuperarse de manera controlada. Ese recovery no autoriza más de dos intentos de contenido ni puede utilizarse para crear loops.

## 6. Handoff después del terminal

Al entregar resultado terminal, el scope anterior queda congelado para review. El Jules NO permanece esperando QA si existe trabajo seguro autorizado: el control plane lo reasigna a la siguiente tarea segura/preasignada, mientras ChatGPT/VAEP revisa detrás.

Prioridad v3.20:

1. si ATTEMPT=1 falló y todavía no consumió R2, único R2 dirigido cuando corresponda;
2. trabajo útil que cierre el padre actual con scope exclusivo;
3. siguiente tarea SAFE elegible/preasignada;
4. preflight/tests/security/contracts/performance/docs útiles cuando una dependencia impida writes funcionales.

Si R2 se agotó, ese Jules salta obligatoriamente a la siguiente tarea segura; la corrección queda en QA externo.

`MAX_VOLUNTARY_IDLE=0` permanece vigente.

## 7. FULL FLASH PERFECT

Cada Jules debe:

1. inspeccionar solo archivos objetivo y dependencias directas;
2. reutilizar contexto y evidencia válidos;
3. implementar el changeset mínimo coherente que satisfaga el criterio;
4. ejecutar validaciones proporcionales reales;
5. corregir defectos causales dentro del scope y del attempt disponible;
6. revisar el diff completo antes de entregar;
7. reportar observaciones, riesgos, limitaciones, recomendaciones y pruebas no ejecutadas;
8. finalizar con evidencia limpia y exacta.

Velocidad nunca autoriza false PASS/LISTO ni reducción de QA.

## 8. Sprint 40

Ventana extraordinaria actual:

```text
SPRINT_START_AT=2026-08-20T22:45:00-06:00
SPRINT_DEADLINE_AT=2026-08-21T06:00:00-06:00
SPRINT_PARENT_TARGET=40
SPRINT_TIMEZONE=America/Tegucigalpa
```

Los cuatro Jules deben conocer esta meta de equipo. Cuenta solamente un padre operativo real `MICROTAREA` que pase genuinamente a `LISTO` después de QA/DoD. No cuentan hijos internos, support packets, preflights repetidos, manifests, sesiones ni `COMPLETED` sin review.

La meta no modifica la Definition of Done: dependencias, seguridad, RBAC, auditoría, datos, UX, pruebas, CI y cero P0/P1 siguen siendo obligatorios.

## 9. Calidad de ingeniería

Antes de entregar, revisar cuando aplique:

- arquitectura y separación de responsabilidades;
- contratos API/DTOs;
- persistencia, migraciones, invariantes y rollback;
- concurrencia e idempotencia;
- RBAC/autorización;
- seguridad y exposición de datos;
- auditoría/trazabilidad;
- manejo de errores;
- UX/accesibilidad/loading/error/empty;
- regresión;
- unit/integration/contract/E2E reales disponibles;
- build/lint;
- compatibilidad con Plan Maestro ERP V5 y trabajo certificado.

Clasificación:

```text
BLOCKER / P0-P1 -> no puede pasar a LISTO
REQUIRED         -> corregir dentro del attempt disponible; si R2 se agota => QA takeover
P2/P3            -> registrar y justificar
N/A              -> justificar técnicamente
```

## 10. Higiene absoluta del ChangeSet

No crear ni incluir archivos temporales para transportar patches:

```text
changes.patch
*.patch
*.diff
*.orig
*.rej
*.bak
backup*
tmp*
```

Antes de finalizar:

```text
git status --short
git diff --check
git diff --name-only
```

Los únicos archivos modificados deben ser los autorizados por `FILE_SCOPE_HINT`.

## 11. Base, scope y manifest

Todo manifest v3.20 nuevo debe incluir o permitir derivar:

- `dispatchId`;
- `taskId`;
- `workerId`;
- `expectedBranch=Desarrollo`;
- `primaryBaseHead`;
- `fileScopeHint`;
- `prompt` y criterios de aceptación;
- `taskAttempt=1|2` para ejecución/rework de contenido.

El worker v3.20 rechaza `taskAttempt>2` y un dispatch ID explícito `R3+`.

## 12. Protocolo de entrega

La entrega correcta incluye:

1. `ChangeSet/gitPatch`;
2. `baseCommitId` exacto;
3. lista real de archivos modificados;
4. pruebas ejecutadas y resultados;
5. pruebas no ejecutadas y causa;
6. auto-review del diff;
7. observaciones;
8. riesgos;
9. limitaciones;
10. recomendaciones;
11. `TASK_ATTEMPT` real.

No publicar, commitear, pushear, abrir PR ni hacer merge.

## 13. Review posterior y QA takeover

ChatGPT/VAEP revisa siempre identidad, sesión, protocol version, attempt, base, scope, diff, contratos, seguridad/RBAC, auditoría/datos, pruebas/CI, auto-review y compatibilidad con HEAD.

- PASS -> integrar/recrear seguro, validar y promover según DoD.
- FAIL en ATTEMPT=1 -> como máximo un R2 dirigido.
- FAIL en ATTEMPT=2 -> `JULES_RETRY_EXHAUSTED`; ChatGPT/VAEP/Vibe corrige, prueba, integra y certifica. Prohibido redispatch Jules R3+.

## 14. Estados y feedback

Bootstrap `QUEUED`, `PLANNING`, clonando o configurando no cuentan por sí solos como progreso.

- sin primera actividad útil ~5 min: `BOOTSTRAP_STALLED`;
- sin progreso útil ~10 min: recovery/failover controlado;
- `PAUSED` sin activities/patch: no ACTIVE;
- `AWAITING_USER_FEEDBACK` rutinario: worker v3.20 resuelve inline;
- `AWAITING_PLAN_APPROVAL` rutinario: worker v3.20 autoaprueba cuando ya está autorizado;
- `COMPLETED`: review inmediato y handoff de lane a trabajo seguro;
- sesión superseded: nunca recupera ownership por sí sola.

Máximo tres auto-followups rutinarios por ejecución; después se reporta `AUTO_FEEDBACK_EXHAUSTED` para control plane, sin loop infinito.

## 15. Workflow autoritativo

Los cuatro workflows actuales deben ejecutar exclusivamente:

```text
.github/scripts/vaep-jules-worker-v320.sh
```

Paths:

```text
A -> vaep/jules/dispatch/*.json
B -> vaep/jules-b/dispatch/*.json
C -> vaep/jules-c/dispatch/*.json
D -> vaep/jules-d/dispatch/*.json
```

Un dispatch nuevo válido = un commit = exactamente un manifest nuevo del worker correspondiente. No agrupar manifests ni archivos de aplicación en el mismo dispatch commit.

Al terminal, el worker crea Issue de resultado con `controllerHandoff=REVIEW_IMMEDIATELY_AND_ASSIGN_NEXT_SAFE`; esto es señal técnica para el control plane y los checkpoints de 15 minutos.

## 16. Criterio de éxito

Una tarea Jules solo es `LISTO` cuando:

```text
asignación válida
-> protocolo v3.20
-> ATTEMPT válido 1|2
-> sesión Jules
-> trabajo dentro de scope
-> ChangeSet limpio
-> auto-review Jules
-> review ChatGPT/VAEP
-> reconciliación contra HEAD
-> pruebas/CI/DoD requeridos
-> integración autorizada en Desarrollo
-> COLA/BITACORA/evidencia actualizadas
-> cero P0/P1
-> LISTO
```

No existe excepción por Sprint 40 ni por velocidad.