# VAEP — Autoridad de versión operativa

Estado documental vigente para VariApp al 2026-08-25, unificado por autorización expresa del propietario. v3.20 y v3.21 permanecen como historia auditable.

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
PARENT_LISTO_TARGET_ROLLING_60=3
PARENT_MAX_DWELL_MINUTES=20
PARENT_STALL_NO_PROGRESS_MINUTES=10
MAX_VOLUNTARY_IDLE=0
```

La configuración viva del tablero fue declarada por el usuario como v3.25. El Sheet registra/describe configuración, cola y bitácora; el sistema de tareas es el ejecutor. Sesiones, checkpoints y actividad solo existen cuando hay evidencia del ejecutor.

## Precedencia

1. `CONFIG.RUNNER_PROTOCOL_VERSION` gobierna el runner/control-plane global de ChatGPT/VAEP.
2. `CONFIG.JULES_PROTOCOL_VERSION` gobierna creación de sesión, seguimiento automático, recovery, review, rework y entrega de Jules A/B/C/D.
3. Este archivo y `AGENTS.md` gobiernan versión, cierre por padre, SLA de throughput, checkpoints y retry cap.
4. El manifest vigente define tarea, `primaryBaseHead`, scope, `taskAttempt` y aceptación.
5. `PLAN_EJECUCION_AUTONOMA.md` y `docs/VAEP_JULES.md` gobiernan ejecución y entrega.
6. Plan Maestro y `CONFIG/COLA/BITACORA` gobiernan roadmap/estado cuando se leen frescos.
7. HEAD, sistema de tareas, CI, código y pruebas resuelven la realidad observable.

Cualquier referencia anterior, incluidas v3.20 y v3.21, es histórica y no puede desplazar `V3.25_CURRENT`.

v3.25 no sustituye ni degrada el control-plane global v4.6. Conserva ATTEMPT1+R2, R3 prohibido, QA takeover, evidencia real, HEAD freeze, Desarrollo únicamente y protección de main/Producción.

## SLA duro de cierre de padres — ChatGPT/VAEP

El control-plane tiene como objetivo operativo obligatorio **cerrar y dejar genuinamente en `LISTO` al menos 3 tareas padre por cada ventana móvil de 60 minutos**, siempre que existan al menos tres padres elegibles/cerrables conforme a dependencias y DoD. El SLA se mide por transiciones reales de padre a `LISTO`; no cuentan hijos, manifests, sesiones, prewarm, documentación redundante, `COMPLETED` Jules ni checkpoints.

Reglas ejecutables:

- `PARENT_LISTO_TARGET_ROLLING_60=3`: cada checkpoint debe calcular y publicar `rolling60_parent_listo` y la deuda contra 3.
- `PARENT_MAX_DWELL_MINUTES=20`: un mismo `CURRENT_PARENT` no puede consumir más de 20 minutos sin una acción de cierre material o una clasificación causal de bloqueo.
- `PARENT_STALL_NO_PROGRESS_MINUTES=10`: diez minutos sin progreso técnico útil obligan a recovery/failover; no se permite seguir observando pasivamente la misma sesión.
- Si el padre supera 20 minutos sin `LISTO`, se declara `PARENT_THROUGHPUT_INCIDENT`, ChatGPT/VAEP toma `QA_TAKEOVER` del blocker y los Jules dejan de producir evidencia redundante del mismo punto. Solo pueden recibir trabajo que cierre causalmente ese blocker o `NEXT_SAFE` con scope independiente.
- Un oracle, artifact o patch ya certificado que hace posible el cierre debe ser **materializado por ChatGPT/VAEP inmediatamente**; queda prohibido abrir rondas adicionales de evidencia Jules sobre el mismo cierre si no eliminan un blocker nuevo y concreto.
- Si un bloqueo externo o una dependencia real impide materialmente alcanzar tres cierres en la ventana, no se falsifica `LISTO`: el checkpoint registra `THROUGHPUT_BLOCKED`, causa exacta, owner y acción de desbloqueo, y mantiene trabajo paralelo SAFE sobre padres independientes elegibles. El bloqueo no autoriza inactividad.
- `PARENT_CLOSE_FIRST` significa que no se promueve un sucesor dependiente antes del cierre del padre; no significa que todo el equipo quede atrapado generando prewarm o evidencia redundante mientras el closer no ejecuta el write final.
- `MAX_VOLUNTARY_IDLE=0`: una lane terminal o liberada recibe `NEXT_SAFE` inmediatamente; una lane no permanece esperando review humano rutinario.

Este SLA **nunca** autoriza false `LISTO`, reducción de DoD, omisión de seguridad/CI o saltos de dependencia. La corrección es reducir tiempo muerto, loops de evidencia y retrasos del controller, no degradar calidad.

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
- Una sesión Jules que alcance el presupuesto de lane sin terminal útil debe terminar como `JULES_LANE_BUDGET_EXCEEDED`/incidente y liberar la lane; no puede monopolizar la hora.

## Cierre por padre y checkpoints

- `PARENT_CLOSE_FIRST`: cerrar el padre vigente antes de promover el siguiente dependiente; preparación segura N+1 no equivale a promoción.
- Checkpoints declarados: `:00`, `:15`, `:30`, `:45` y respaldo `:55`.
- Cada checkpoint reconcilia padre/tarea, actividad real, owner/scope, attempts, review, CI causal, bloqueo, `RESUME_POINT`, `rolling60_parent_listo`, `parent_dwell_minutes` y deuda de throughput.
- Si `rolling60_parent_listo < 3` y existen padres cerrables, el checkpoint debe priorizar cierre/review/integración/certificación sobre nuevo prewarm.
- Nunca cuentan como `LISTO` un dispatch, sesión, checkpoint o `COMPLETED` sin DoD/evidencia.
- `HEAD_FREEZE` protege CI causal activo; durante freeze continúa análisis/review seguro sin mover HEAD.
