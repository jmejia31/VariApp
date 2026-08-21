# VAEP Jules v3.20 — Retry Cap R2 + QA Takeover + Sprint 40

Fecha de activación efectiva de esta orden: 2026-08-20 23:02 America/Tegucigalpa.

Este documento es una extensión normativa obligatoria de `docs/VAEP_AUTHORITY.md`, `docs/VAEP_JULES.md`, `AGENTS.md` y del Plan Maestro ERP V5. En cualquier contradicción sobre rework/attempts/throughput, v3.20 prevalece sobre v3.19 y anteriores. No modifica la autoridad global `VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION` de ChatGPT/VAEP.

## 1. Límite absoluto de intentos Jules

```text
JULES_MAX_ATTEMPTS_PER_TASK=2
JULES_REWORK_MAX=1
JULES_FINAL_REWORK_LABEL=R2
JULES_R3_PLUS=PROHIBIDO
```

Una tarea/hija lógica puede recibir de Jules solamente:

1. `ATTEMPT=1`: ejecución inicial.
2. `ATTEMPT=2` / `R2`: única corrección dirigida si el primer resultado no pasa REVIEW-FIRST.

No existe un tercer intento Jules. `R3`, `R4`, `R5` o superior para la misma tarea es un incidente de orquestación v3.20.

## 2. Retry exhausted => QA takeover

Si el segundo intento conserva cualquier REQUIRED/BLOCKER/P0/P1, scope leak, contrato incorrecto, evidencia insuficiente o defecto que impida `LISTO`, la tarea pasa a:

```text
JULES_RETRY_EXHAUSTED
OWNER=CHATGPT_VAEP_VIBE
ACTION=QA_TAKEOVER_CORRECT_TEST_CERTIFY
```

ChatGPT/VAEP/Vibe preserva trabajo válido, corrige causalmente, prueba, integra y certifica. La tarea NO vuelve a Jules.

Cambiar de Jules no reinicia el contador. Work-stealing hereda `ATTEMPT_COUNT`; está prohibido usar A/B/C/D alternativamente para eludir el máximo de dos intentos.

## 3. Doble self-review obligatorio

Antes de `COMPLETED`, cada Jules ejecuta **dos pasadas de auto-revisión independientes y obligatorias**:

1. `SELF_REVIEW_PASS_1`: revisar alcance y `git diff --name-only`, diff completo, contratos/arquitectura, seguridad/RBAC/auditoría/datos y coherencia funcional; corregir cualquier defecto encontrado dentro del mismo intento.
2. `SELF_REVIEW_PASS_2`: volver a revisar desde cero el resultado ya corregido, ejecutar/confirmar pruebas proporcionales, `git diff --check`, archivos temporales/lockfiles/scope leaks, observaciones, limitaciones, riesgos, recomendaciones y pruebas no ejecutadas.

Una sola pasada no satisface el DoD Jules. Ambas deben quedar declaradas en la evidencia final (`SELF_REVIEW_PASS_1=PASS` y `SELF_REVIEW_PASS_2=PASS`, o hallazgos explícitos si no pasan). Esto **no crea intentos adicionales**: las dos pasadas ocurren dentro de ATTEMPT=1 o ATTEMPT=2.

## 4. Bootstrap no es rework de contenido

Un manifest que nunca obtiene sesión ni primera actividad técnica útil se clasifica fallo de infraestructura/`BOOTSTRAP_STALLED`. El contador de contenido empieza al existir sesión con primera actividad técnica útil o resultado terminal de la tarea. Recovery de infraestructura no autoriza más de dos intentos de contenido ni puede crear un loop de redispatch.

## 5. Zero-idle después de R2

Al agotarse R2, el Jules queda liberado de esa tarea y recibe inmediatamente el siguiente scope seguro/preasignado. `MAX_VOLUNTARY_IDLE=0` continúa vigente. QA externo absorbe el retrabajo agotado; los Jules producen trabajo nuevo.

## 6. Manifest y worker

Todo dispatch nuevo debe usar `taskAttempt: 1` o `taskAttempt: 2` cuando sea una ejecución/rework de contenido. El worker v3.20:

- rechaza `taskAttempt > 2`;
- rechaza un dispatch ID explícito `R3+`;
- considera `R2` el segundo y último intento;
- informa a Jules del cap y de que no debe pedir/crear una tercera ronda;
- exige las dos pasadas de self-review de esta norma antes de `COMPLETED`;
- conserva `ChangeSet/gitPatch`, `baseCommitId`, scope y evidencias obligatorias.

## 7. Sprint 40

```text
SPRINT_START_AT=2026-08-20T23:02:00-06:00
SPRINT_DEADLINE_AT=2026-08-21T06:00:00-06:00
SPRINT_TIMEZONE=America/Tegucigalpa
SPRINT_PARENT_TARGET=40
SPRINT_QUEUE_DEPTH_PER_JULES=40
```

Cuenta únicamente una fila operativa `MICROTAREA` de COLA que pase realmente a `LISTO` desde el inicio efectivo del sprint. No cuentan `MICROTAREA_HIJA`, support packets, preflights repetidos, manifests, sesiones ni `COMPLETED` Jules sin QA/DoD.

La cola de 40 por Jules es una **cola condicional/prearmada**, no 40 sesiones simultáneas. Cada Jules conserva máximo un ownership autoritativo activo; la cola referencia los próximos 40 padres reales ordenados por prioridad y se consume únicamente cuando dependencias, scope y ownership lo permiten. A/B/C/D pueden tener un lane exclusivo dentro de cada parent, pero nunca dos writers sobre el mismo scope. La automatización repone la cola hasta profundidad 40 cuando existan candidatos reales.

Cada checkpoint agregado :00/:15/:30/:45 debe registrar: salud real A/B/C/D, tarea y attempt de cada Jules, ambas self-reviews al terminal, terminales, review queue, QA takeovers, padres `LISTO` del sprint, faltantes hasta 40, profundidad de cola por Jules, velocidad necesaria y blockers.

La meta de 40 es un KPI operativo obligatorio, pero nunca permite false `LISTO`, saltar dependencias, omitir CI/DoD, ignorar P0/P1, integrar stale patches ni tocar `main`, Producción, secretos o deploy.

A las 06:00 se emite auditoría exacta con IDs, evidencia, commits/CI, attempts consumidos, QA takeovers, bloqueos y delta contra 40. Nunca se inventa completitud.

## 8. Patrón de ejecución

Por cada padre seguro, A/B/C/D pueden recibir scopes exclusivos y no solapados. Al terminal, REVIEW-FIRST sigue siendo obligatorio. Si una hija falla una vez, solo puede volver como R2. Si falla R2, QA externo la termina y el Jules avanza. El objetivo es reducir `PARENT_LEAD_TIME`, maximizar `FIRST_PASS_APPROVAL` y sostener una cola útil, no acumular revisiones de la misma tarea.
