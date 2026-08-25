# VAEP — Autoridad de versión operativa

Estado documental vigente para VariApp al 2026-08-24, unificado por autorización expresa del propietario. v3.20 y v3.21 permanecen como historia auditable.

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
GLOBAL_CONTROL_PLANE=CONFIG.RUNNER_PROTOCOL_VERSION=VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION
JULES_PROTOCOL=CONFIG.JULES_PROTOCOL_VERSION=V3.25_CURRENT
PARENT_CLOSE_FIRST=TRUE
CHECKPOINTS=:00,:15,:30,:45,:55
JULES_MAX_ATTEMPTS_PER_TASK=2
JULES_REWORK_MAX=1
JULES_R3_PLUS=PROHIBIDO
```

La configuración viva del tablero fue declarada por el usuario como v3.25. El Sheet registra/describe configuración, cola y bitácora; el sistema de tareas es el ejecutor. Esta actualización no escribió el Sheet ni modificó o verificó una automatización real. Sesiones, checkpoints y actividad solo existen cuando hay evidencia del ejecutor.

## Precedencia

1. `CONFIG.RUNNER_PROTOCOL_VERSION` gobierna el runner/control-plane global de ChatGPT/VAEP.
2. `CONFIG.JULES_PROTOCOL_VERSION` gobierna creación de sesión, seguimiento automático, recovery, review, rework y entrega de Jules A/B/C/D.
3. Este archivo y `AGENTS.md` gobiernan versión, cierre por padre, checkpoints y retry cap.
4. El manifest vigente define tarea, `primaryBaseHead`, scope, `taskAttempt` y aceptación.
5. `PLAN_EJECUCION_AUTONOMA.md` y `docs/VAEP_JULES.md` gobiernan ejecución y entrega.
6. Plan Maestro y `CONFIG/COLA/BITACORA` gobiernan roadmap/estado cuando se leen frescos.
7. HEAD, sistema de tareas, CI, código y pruebas resuelven la realidad observable.

Cualquier referencia anterior, incluidas v3.20 y v3.21, es histórica y no puede desplazar `V3.25_CURRENT`.

v3.25 no sustituye ni degrada el control-plane global v4.6. Conserva ATTEMPT1+R2, R3 prohibido, QA takeover, evidencia real, HEAD freeze, Desarrollo únicamente y protección de main/Producción.

## Regla de seguimiento Jules v3.25

- A/B/C/D continúan autónomamente dentro de su tarea y scope asignado.
- Cada tarea/hija lógica tiene máximo **DOS intentos Jules de contenido**: ejecución inicial (`ATTEMPT=1`) + una única corrección (`ATTEMPT=2`, `R2`).
- `R3+` para Jules está prohibido. Si R2 no pasa REVIEW-FIRST, la tarea entra `JULES_RETRY_EXHAUSTED` y el ownership de corrección pasa a `CHATGPT_VAEP_VIBE`.
- Work-stealing no reinicia el contador de attempts; cambiar de Jules no permite eludir el cap.
- Un dispatch sin sesión/actividad útil es incidente de bootstrap y puede recuperarse técnicamente, pero no habilita más de dos intentos de contenido.
- Tras agotar R2, el Jules toma inmediatamente la siguiente tarea segura; `MAX_VOLUNTARY_IDLE=0` permanece vigente.
- Las dudas rutinarias se resuelven con este archivo, CONFIG fresca, manifest, `AGENTS.md`, `docs/VAEP_JULES.md`, sistema de tareas, código y pruebas.
- Solo una decisión genuina de negocio/autorización humana puede dejar una sesión esperando.
- `COMPLETED` exige auto-review, observaciones/limitaciones/riesgos/recomendaciones, pruebas no ejecutadas y `ChangeSet/gitPatch` revisable con `baseCommitId`.
- No branch, PR, push, merge, deploy, main, Producción ni secretos.
- Un resultado Jules siempre entra en REVIEW-FIRST de ChatGPT/VAEP; nunca publica funcionalmente por sí solo.
- Un dispatch atómico conserva un manifest por worker y scopes exclusivos/no solapados.

## Cierre por padre y checkpoints

- `PARENT_CLOSE_FIRST`: cerrar el padre vigente antes de promover el siguiente; preparación segura N+1 no equivale a promoción.
- Checkpoints declarados: `:00`, `:15`, `:30`, `:45` y respaldo `:55`.
- Cada checkpoint reconcilia padre/tarea, actividad real, owner/scope, attempts, review, CI causal, bloqueo y `RESUME_POINT`.
- Nunca cuentan como `LISTO` un dispatch, sesión, checkpoint o `COMPLETED` sin DoD/evidencia.
- `HEAD_FREEZE` protege CI causal activo; durante freeze continúa análisis/review seguro sin mover HEAD.
