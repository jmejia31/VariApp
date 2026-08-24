# VAEP — Autoridad de versión operativa

> **ALERTA DE CONFLICTO OPERATIVO — 2026-08-24:** `AGENTS.md` y este archivo declaran `V3.20_CURRENT`, mientras `docs/VAEP_V321_PARENT_CLOSURE.md` declara `V3.21_CURRENT` y una precedencia incompatible. El control-plane global v4.6 no está en disputa, pero el subprotocolo Jules sí. Hasta que Javier o un changeset de gobierno autoritativo alinee esas fuentes, quedan bloqueados nuevos dispatches, redispatches, cambios de ownership y automatizaciones mutantes dependientes de la versión Jules. Se permiten reconciliación, lectura, diagnóstico y verificación sin escritura externa. No inferir la versión vigente desde commits de cierre ni desde el nombre de un documento.

Estado vigente para VariApp al 2026-08-20.

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
GLOBAL_CONTROL_PLANE=CONFIG.RUNNER_PROTOCOL_VERSION=VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION
JULES_PROTOCOL=CONFIG.JULES_PROTOCOL_VERSION=V3.20_CURRENT
JULES_MAX_ATTEMPTS_PER_TASK=2
JULES_REWORK_MAX=1
JULES_R3_PLUS=PROHIBIDO
SPRINT_PARENT_TARGET=40
SPRINT_DEADLINE=2026-08-21T06:00:00-06:00
```

## Precedencia

1. `CONFIG.RUNNER_PROTOCOL_VERSION` gobierna el runner/control-plane global de ChatGPT/VAEP.
2. `CONFIG.JULES_PROTOCOL_VERSION` gobierna creación de sesión, seguimiento automático, recovery, review, rework y entrega de Jules A/B/C/D.
3. `docs/VAEP_V320_RETRY_CAP.md` gobierna de forma dura el límite de intentos Jules, QA takeover y Sprint 40.
4. El manifest de despacho vigente define tarea, `primaryBaseHead`, `FILE_SCOPE_HINT`, `taskAttempt` y criterios de aceptación.
5. `AGENTS.md` y `docs/VAEP_JULES.md` gobiernan ingeniería, seguridad y entrega en todo lo no sustituido por v3.20.
6. El Plan Maestro ERP V5 y `CONFIG/COLA/BITACORA` gobiernan roadmap y estado operativo.
7. HEAD/código/pruebas actuales resuelven la realidad técnica.

Cualquier referencia operativa a VAEP/Jules `v3.7`, `v3.13`, `v3.14`, `v3.16`, `v3.17`, `v3.18` o `v3.19` es histórica y no puede desplazar `JULES_PROTOCOL=V3.20_CURRENT` en las materias que v3.20 sustituye.

`v3.20` no sustituye ni degrada el protocolo global v4.6: es el subprotocolo vigente de integración multi-Jules. ChatGPT/VAEP mantiene control-plane, reconciliación, publicación y certificación bajo la versión global indicada en CONFIG.

## Regla de seguimiento Jules v3.20

- A/B/C/D continúan autónomamente dentro de su tarea y scope asignado.
- Cada tarea/hija lógica tiene máximo **DOS intentos Jules de contenido**: ejecución inicial (`ATTEMPT=1`) + una única corrección (`ATTEMPT=2`, `R2`).
- `R3+` para Jules está prohibido. Si R2 no pasa REVIEW-FIRST, la tarea entra `JULES_RETRY_EXHAUSTED` y el ownership de corrección pasa a `CHATGPT_VAEP_VIBE`.
- Work-stealing no reinicia el contador de attempts; cambiar de Jules no permite eludir el cap.
- Un dispatch sin sesión/actividad útil es incidente de bootstrap y puede recuperarse técnicamente, pero no habilita más de dos intentos de contenido.
- Tras agotar R2, el Jules toma inmediatamente la siguiente tarea segura; `MAX_VOLUNTARY_IDLE=0` permanece vigente.
- Las dudas rutinarias se resuelven con este archivo, CONFIG, manifest, `docs/VAEP_V320_RETRY_CAP.md`, `AGENTS.md`, `docs/VAEP_JULES.md`, código y pruebas.
- Solo una decisión genuina de negocio/autorización humana puede dejar una sesión esperando.
- `COMPLETED` exige auto-review, observaciones/limitaciones/riesgos/recomendaciones, pruebas no ejecutadas y `ChangeSet/gitPatch` revisable con `baseCommitId`.
- No branch, PR, push, merge, deploy, main, Producción ni secretos.
- Un resultado Jules siempre entra en REVIEW-FIRST de ChatGPT/VAEP; nunca publica funcionalmente por sí solo.
- Un dispatch atómico conserva un manifest por worker y scopes exclusivos/no solapados.

## Sprint 40 — 2026-08-20 22:45 → 2026-08-21 06:00 Honduras

- Meta operativa: **40 nuevos padres `MICROTAREA` en `LISTO`** desde `SPRINT_START_AT`.
- No cuentan hijos internos, support packets, preflights repetidos, manifests, sesiones ni `COMPLETED` sin QA/DoD.
- Check agregado cada 15 minutos mediante :00/:15/:30/:45: salud A/B/C/D, assignments, attempts, review queue, QA takeover, padres cerrados, faltantes, velocidad y blockers.
- La meta nunca autoriza false `LISTO`, saltar dependencias, omitir CI/DoD, aceptar P0/P1, tocar main/Producción/secrets ni ocultar fallos.
- A las 06:00 se emite auditoría exacta del resultado real contra 40.
