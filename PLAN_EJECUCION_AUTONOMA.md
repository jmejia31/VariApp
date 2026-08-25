# PLAN DE EJECUCIÓN AUTÓNOMA — VAEP v2.2 EXECUTION_TRUTH

> VariApp Autonomous Execution Protocol. Fuente rectora: **Plan Maestro ERP V5 — VariApp**. Fuente operativa: Google Sheets. Autoridad técnica y evidencia: GitHub `jmejia31/VariApp`, rama `Desarrollo`.

## 1. Identidad y fuentes obligatorias

- `PROJECT_ID`: `VARIAPP`
- Repositorio: `jmejia31/VariApp`
- Rama única: `Desarrollo`
- PR oficial: `#2 Desarrollo -> main`, siempre abierto y Draft hasta autorización expresa de Javier Mejía.
- Plan rector en Drive: https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit
- Tablero VAEP v2: https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit
- GitHub prevalece para código, commits, CI, arquitectura y evidencia verificable.

El `.docx` original fue convertido a Google Docs para que el runner consulte permanentemente la fuente rectora sin depender de una conversación concreta.

### 1.1 Gate de versión antes de automatizar

La autoridad unificada es Jules `V3.25_CURRENT` bajo el control-plane global v4.6. Antes de adquirir mutex o mutar estado, comprobar que la ejecución declara v3.25, `PARENT_CLOSE_FIRST` y checkpoints `:00/:15/:30/:45/:55`. El Sheet registra/describe automatizaciones; el sistema de tareas las ejecuta. La ausencia de evidencia del ejecutor obliga a reportar `IDLE/NO_EVIDENCE`, nunca a afirmar que un checkpoint o automatización corrió.

## 2. Cobertura integral del Plan Maestro ERP V5

VAEP cubre ERP-N0→N9 y los tracks T0–T12. Las funcionalidades futuras no-core —RRHH, CRM, MRP, activos fijos, proyectos, servicio técnico, logística avanzada y ecommerce futuro— permanecen `NO_AUTORIZADO` y no pueden autoejecutarse sin autorización explícita de Javier.

El tablero contiene `DASHBOARD`, `COLA`, `PLAN_MAESTRO`, `CONFIG`, `BITACORA` y `LEYENDA`.

## 3. Granularidad y calidad obligatorias

Ningún agente debe resolver un punto ERP grande en un único changeset. Salvo descomposición específica, cada punto se divide en `PRE`, `DOMAIN`, `DB_MIG`, `BACKEND_API`, `FRONTEND_UX`, `SEC_AUDIT`, `TEST_CI` y `DOC_CERT` cuando apliquen.

Si una microtarea sigue siendo demasiado grande, **debe subdividirse antes de editar**. Una microtarea representa un solo concern coherente y verificable.

`RUNNER_QUALITY_MODE=COMPLETO_SIN_RECORTES`: está prohibido omitir, rebajar, simplificar artificialmente o diferir trabajo necesario solo para avanzar más rápido. Si una tarea exige un prerrequisito técnico accesible en `Desarrollo`, el Runner debe crearlo como hijo/prerrequisito, encadenar sus dependencias y resolverlo con criterio senior.

Solo puede escalarse como bloqueo externo real aquello que ChatGPT no pueda resolver por falta de acceso/credencial externa indispensable, autorización humana/productiva obligatoria, recurso físico/externo inaccesible, decisión de negocio no inferible o una restricción explícita de seguridad/gobernanza. Todo bloqueo externo debe registrar rol/persona, acción exacta, evidencia, condición de retorno y lo intentado antes de escalar.

## 4. Máquina de estados

```text
PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO
                     \-> BLOQUEADO
```

`CANCELADO` requiere instrucción explícita de Javier o evidencia inequívoca de que el punto dejó de aplicar. Está prohibido `PENDIENTE -> LISTO` sin ejecución/reconciliación y evidencia verificable.

## 5. Mutex global y exclusión de corridas

`CONFIG.RUNNER_MUTEX*` implementa un mutex global obligatorio para `VariApp VAEP v2 Runner`.

Antes de cualquier lectura funcional, adquisición de tarea, modificación de Sheet, commit o validación:

1. leer `RUNNER_MUTEX_STATE/TOKEN/HEARTBEAT/TASK/CI/TTL`, `RUNNER_ACTIVITY_STATE` y `RUNNER_LAST_REAL_ACTION_AT`;
2. comprobar `COLA` y GitHub CI;
3. si otra corrida está realmente activa, abortar la nueva invocación antes de tocar trabajo;
4. si el mutex está libre o recuperable, adquirirlo con token único y releerlo;
5. antes de cualquier escritura crítica, confirmar que el token sigue siendo propio;
6. nunca liberar ni reemplazar un mutex ajeno.

Una invocación manual o programada superpuesta se autoaborta. Nunca pueden ejecutarse dos corridas efectivas simultáneamente.

### 5.1 Execution Truth — mutex no equivale a actividad

`RUNNER_MUTEX_STATE=RUNNING` **no es evidencia suficiente** para afirmar que ChatGPT sigue ejecutándose.

La actividad real se clasifica mediante `RUNNER_ACTIVITY_STATE` y evidencia verificable:

- `ACTIVE`: existe una invocación viva con `RUNNER_LAST_REAL_ACTION_AT` reciente y acciones reales de conector/código/estado;
- `WAITING_CI`: la invocación puede haber terminado, pero existe CI relacionado realmente `QUEUED/IN_PROGRESS`; el lease solo protege ese trabajo;
- `IDLE_PLATFORM_LIMIT`: la invocación terminó por límite real de plataforma/capacidad y dejó handoff exacto;
- `IDLE`: no existe invocación activa ni CI relacionado ejecutándose.

El heartbeat y `RUNNER_LAST_REAL_ACTION_AT` solo se renuevan al realizar una acción real: lectura/escritura de conector, inspección GitHub, reconciliación, cambio de estado, commit o validación. Está prohibido renovar heartbeat únicamente para aparentar actividad.

Si heartbeat/last real action superan TTL y GitHub no muestra CI relacionado `QUEUED/IN_PROGRESS` ni existe progreso posterior en `COLA/BITACORA`, el lease propio es stale y debe recuperarse en la siguiente invocación.

## 6. Política de selección FINISH_FIRST

Esta es la política obligatoria de selección y sustituye el comportamiento histórico de saltar entre puntos hermanos independientes.

### 6.1 Reconciliar antes de abrir trabajo

En cada corrida:

1. confirmar `PROJECT_ID=VARIAPP`, repo, rama y HEAD;
2. leer `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md`, última entrada relevante de `CHANGELOG_AI.md` y este protocolo;
3. leer `CONFIG`, `COLA` y `BITACORA`;
4. reconciliar contra GitHub **todas** las filas propias `EN_PROGRESO/VALIDANDO` antes de seleccionar nuevas `PENDIENTE`.

Un lock `AGENTE=ChatGPT` perteneciente al Runner no es automáticamente “lock ajeno”. Si está stale debe reconciliarse/recuperarse conforme al lease y CI. Locks de Javier, Codex, AntiG/Antigravity u otro agente sí son concurrencia externa.

### 6.2 Punto padre foco

El Runner determina el **PUNTO PADRE FOCO más antiguo ya iniciado y no cerrado dentro de la fase actual**.

Mientras exista un punto foco abierto:

- está prohibido abrir/adquirir un hermano de la misma fase aunque sea técnicamente independiente;
- la siguiente tarea debe pertenecer al mismo árbol foco y cumplir dependencias;
- las subdivisiones adaptativas permanecen dentro de ese árbol;
- después de cada `LISTO`, se revalida HEAD/dependencias/mutex y se continúa inmediatamente con la siguiente elegible del mismo foco.

### 6.3 Sin tope artificial de microtareas

No existe un máximo fijo de 3 microtareas por corrida. El Runner maximiza **trabajo real dentro de la invocación disponible** mientras gates, seguridad, tiempo y capacidad sigan verdes.

Puede detenerse únicamente por:

- límite real de invocación/plataforma/capacidad;
- CI relacionado todavía activo cuando la invocación ya no tenga capacidad para seguir reconciliándolo;
- bloqueo externo real;
- dependencia no resoluble;
- gate fallido;
- conflicto/concurrencia real;
- autorización humana obligatoria;
- riesgo para `main` o Producción;
- evidencia insuficiente;
- cierre completo del foco o ausencia de tareas elegibles.

No existe garantía de un proceso ChatGPT residente durante toda la hora entre ejecuciones programadas. VAEP debe optimizar el trabajo dentro de cada invocación y dejar un handoff exacto; está prohibido fingir polling/background después de que la invocación haya terminado.

### 6.4 Bloqueos y foco

Un `BLOQUEADO` dentro del punto foco no autoriza saltar por conveniencia a un hermano. Un bloqueo externo real ya escalado puede permitir trabajo verdaderamente independiente y seguro siempre que no permita cerrar falsamente el padre/gate.

### 6.5 Reconciliación de padres

- hijo `EN_PROGRESO/VALIDANDO` → padre operativo `EN_PROGRESO`;
- todos los hijos requeridos `LISTO` → padre puede cerrarse `LISTO` con evidencia;
- bloqueo dependiente que impide cierre → documentar/propagar el bloqueo.

No debe quedar un padre `EN_PROGRESO` abandonado mientras el Runner abre otro árbol.

### 6.6 Recuperación actual

Mientras `CONFIG.RUNNER_CURRENT_RECOVERY_TARGET=N0.5`, el Runner debe recuperar y cerrar `N0.5` antes de abrir nuevo trabajo de `N0.6`. El trabajo ya válido de `N0.6` se preserva y no se revierte.

## 7. Ejecución de una microtarea

Dentro del punto foco:

1. seleccionar solo tarea con dependencias directas/transitivas `LISTO` y ningún ancestro bloqueante;
2. marcar `EN_PROGRESO` y agente/inicio antes de editar;
3. limitar cambios a archivos objetivo y dependencias directas;
4. cumplir literalmente: **“No releer archivos ya documentados a menos que hayan cambiado.”**;
5. pasar a `VALIDANDO` antes de validaciones finales;
6. ejecutar validaciones reales proporcionales;
7. publicar exclusivamente en `Desarrollo` por fast-forward y sin force-push;
8. actualizar `CHANGELOG_AI.md` y `TASKS.md` cuando corresponda;
9. registrar commit/validaciones en `COLA` y transición en `BITACORA`;
10. marcar `LISTO` solo con evidencia suficiente.

Nunca inventar pruebas, CI, despliegues, actividad o estados externos.

## 8. Continuidad anti-idle realista

`RUNNER_CONTINUITY_POLICY=NO_IDLE_WHILE_CAPACITY` y `RUNNER_TIME_BUDGET_POLICY=MAXIMIZAR_INVOCACION_REAL`.

Mientras una invocación siga viva:

- no puede terminar voluntariamente solo porque publicó un commit o inició CI si todavía dispone de capacidad real;
- si CI está `QUEUED/IN_PROGRESS`, puede consultarlo nuevamente y renovar actividad solo cuando realmente realice esa consulta;
- si CI termina `SUCCESS`, debe reconciliar/cerrar y continuar inmediatamente;
- si CI termina `FAIL`, el fallo pasa a ser trabajo de la misma microtarea y debe inspeccionarse/corregirse dentro de la misma invocación cuando sea seguro;
- si no hay CI activo y existe trabajo elegible/recuperable, una respuesta final normal está prohibida salvo una causa válida de detención.

Si la plataforma termina o limita la invocación antes de poder continuar, la causa se registra como `PLATFORM_INVOCATION_LIMIT`; no debe presentarse la espera posterior como actividad de ChatGPT.

## 9. Pre-final gate obligatorio

Antes de **cualquier respuesta final** de una corrida que adquirió mutex, el Runner debe:

1. releer token, hora, HEAD, `COLA`, CI y `RUNNER_LAST_REAL_ACTION_AT`;
2. si no existe CI activo, existe trabajo elegible/recuperable y no existe una causa válida de detención: **respuesta final prohibida; continuar trabajando**;
3. si existe CI activo y la invocación tiene capacidad, seguir reconciliándolo;
4. si la invocación termina por límite real: persistir `ACTIVITY_STATE=IDLE_PLATFORM_LIMIT`, `STOP_REASON=PLATFORM_INVOCATION_LIMIT` y `RESUME_POINT` exacto; liberar mutex si no hay CI activo;
5. si termina dejando CI activo: `ACTIVITY_STATE=WAITING_CI`, persistir CI/TASK/RESUME_POINT y conservar únicamente el lease protector;
6. si termina por bloqueo externo, seguridad o ausencia real de elegibles: `ACTIVITY_STATE=IDLE`, persistir causa y punto de retorno y liberar mutex si no hay CI activo;
7. toda detención con trabajo restante debe generar una **ALERTA DE CONTINUIDAD** con causa, tarea, último CI/evidencia, punto exacto de reanudación y próxima ventana conocida.

Un heartbeat viejo nunca puede justificar afirmar al usuario que el Runner “sigue trabajando”.

## 10. GitHub Actions — generación sí, push funcional con GITHUB_TOKEN no

`RUNNER_FORBID_GITHUB_TOKEN_PUSH=OBLIGATORIO` y `RUNNER_CI_GENERATOR_MODE=ARTIFACT_ONLY_NO_PUSH`.

Está prohibido crear o usar workflows temporales con `permissions: contents: write` para commitear/pushear cambios funcionales, migraciones o snapshots a `Desarrollo` mediante `GITHUB_TOKEN`.

GitHub Actions puede:

- compilar y probar;
- ejecutar MySQL/integraciones;
- generar SQL, migraciones, snapshots u otros artefactos;
- publicar esos resultados como artifacts.

El Runner debe descargar/inspeccionar el artifact y publicar el changeset final mediante el conector GitHub normal, confirmando HEAD y fast-forward.

Si un CI aparece `completed/action_required` y no contiene jobs, no debe tratarse como fallo funcional ni espera pasiva. El Runner debe investigar inmediatamente la causa. Si se produjo después de un commit generado desde un workflow/GITHUB_TOKEN, debe retirar el mecanismo escritor temporal y publicar la siguiente sincronización mediante el conector GitHub normal para recuperar CI ordinario. Solo se escala a Javier cuando GitHub requiera una aprobación que no pueda evitarse de forma técnica segura.

## 11. Gates de fase y orden estricto

VAEP usa `GATE-N0` ... `GATE-N9`:

```text
GATE-N0 -> ERP-N1 -> GATE-N1 -> ERP-N2 -> GATE-N2 -> ERP-N3 ->
GATE-N3 -> ERP-N4 -> GATE-N4 -> ERP-N5 -> GATE-N5 -> ERP-N6 ->
GATE-N6 -> ERP-N7 -> GATE-N7 -> ERP-N8 -> GATE-N8 -> ERP-N9 -> GATE-N9
```

Los gates aplican Definition of Done global: backend/frontend, migraciones, tests, E2E relevantes, seguridad, permisos, auditoría, backfill/reconciliación, rollback, documentación, evidencia y cero P0/P1 abiertos. Una fase no se cierra solo porque compile.

## 12. Estado especializado ERP-N0.5 — MetodoPago

La cadena N0.5 se gobierna como un único punto foco hasta su cierre formal:

```text
N0.5.07B1 -> N0.5.07B2 -> N0.5.07C -> cerrar N0.5.07 ->
N0.5.08 -> N0.5.09/N0.5.10/N0.5.11 -> N0.5.12 ->
N0.5.13 -> N0.5.14 -> N0.5.15
```

`N0.5.07A` y `N0.5.07B1` ya están certificados. `N0.5.07B2` permanece `VALIDANDO` hasta certificar Banco normalizado, FK/snapshots, fail-closed y migración/snapshot EF sin drift. `N0.5.13` debe reconciliar primero el workflow histórico existente; está prohibido duplicar workflows por confiar en estados desactualizados.

## 13. Concurrencia y publicación

- Antes de publicar, confirmar HEAD remoto y mutex propio.
- Preservar commits de Codex, AntiG y otros agentes.
- Nunca force-push.
- No crear ramas nuevas.
- Si existe conflicto real no resoluble de forma dirigida: registrar evidencia y detener el foco; no abrir un hermano para esquivarlo.

## 14. Evidencia obligatoria

Cada changeset intencional debe actualizar `CHANGELOG_AI.md`. `TASKS.md` cambia cuando cambia estado/bloqueo/pendiente. Contexto/índice/arquitectura solo si cambia la realidad que documentan.

Para `LISTO`, según aplique deben existir commit SHA, validaciones reales, `COLA` actualizada y transición en `BITACORA`.

## 15. Seguridad y Producción

VAEP no autoriza tocar `main`, fusionar PR #2, habilitar auto-merge, crear ramas nuevas, modificar Producción, secretos, variables, credenciales, bases, dominios, servicios, activos o ejecutar migraciones productivas.

ERP-N9.4 y cualquier operación productiva permanecen bloqueadas hasta autorización expresa de Javier.

## 16. Informe de cierre

Cada corrida que adquirió mutex produce un único informe consolidado al final, con proyecto/repo/rama y HEAD inicial/final; microtareas procesadas y evidencia; validaciones/CI reales; bloqueos/riesgos; punto foco; siguiente elegible; motivo exacto de detención; actividad real (`ACTIVE`, `WAITING_CI`, `IDLE_PLATFORM_LIMIT` o `IDLE`); y estado de seguridad de `main`, Producción y PR #2.

Una invocación rechazada por mutex muestra únicamente la alerta breve de concurrencia.

## 17. Opción manual

Mientras exista actividad real, lease vigente o CI relacionado `QUEUED/IN_PROGRESS`, el Runner no debe invitar al usuario a ejecutar manualmente. Si la UI nativa muestra el botón de todos modos, el mutex impide que esa pulsación produzca una segunda ejecución efectiva.

## 18. Qué debe hacer Javier

Para trabajo ya incluido en ERP V5: **nada**. No necesita escribir “continúa”. El Runner debe retomar el punto foco y seguir hasta un bloqueo real, límite verificable de invocación o cierre.

Para trabajo nuevo fuera del Plan Maestro, se requiere incorporación explícita a la cola/plan conforme a gobierno vigente.
