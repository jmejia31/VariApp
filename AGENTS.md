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

Con acceso remoto, ChatGPT/VAEP confirma repositorio, `Desarrollo`, HEAD actual y fuentes canónicas. Toda sesión de VariApp usa únicamente contexto verificado de VariApp.

## 1. Autoridad y precedencia

Para control plane global:

```text
CONFIG.RUNNER_PROTOCOL_VERSION=VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION
```

Para Jules A/B/C/D:

```text
CONFIG.JULES_PROTOCOL_VERSION=V3.25_CURRENT
PARENT_CLOSE_FIRST=TRUE
CHECKPOINTS=:00,:15,:30,:45,:55
JULES_MAX_ATTEMPTS_PER_TASK=2
JULES_REWORK_MAX=1
JULES_R3_PLUS=PROHIBIDO
```

Orden obligatorio para Jules:

1. `docs/VAEP_AUTHORITY.md`;
2. manifest actual;
3. `AGENTS.md`;
4. `PLAN_EJECUCION_AUTONOMA.md` y `docs/VAEP_JULES.md`;
5. Plan Maestro/CONFIG/COLA/BITACORA trasladados por VAEP;
6. HEAD/código/pruebas actuales.

Las reglas v3.20/v3.21 se conservan como historia y no gobiernan nuevos dispatches. El Sheet registra/configura la operación declarada; el sistema de tareas es quien ejecuta. Esta autoridad documental no demuestra ni modifica por sí sola workers, sesiones, checkpoints o automatizaciones reales: su actividad requiere evidencia del ejecutor.

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

## 8. Retry Cap Jules v3.25

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

## 10. Cierre por padre y checkpoints v3.25

```text
PARENT_CLOSE_FIRST=TRUE
CHECKPOINTS=:00,:15,:30,:45,:55
```

Toda capacidad segura converge en el padre dependency-valid vigente con scopes no solapados. Ningún padre pasa a `LISTO` por actividad, dispatch o `COMPLETED`: requiere DoD, evidencia causal, CI/pruebas aplicables y cero P0/P1.

Los checkpoints automáticos declarados son `:00`, `:15`, `:30`, `:45` y respaldo `:55`. Cada uno reconcilia tarea/padre, ownership, actividad real, attempts, review queue, CI causal, handoff, bloqueo y punto de reanudación. Un horario declarado no prueba que el ejecutor haya corrido.

El cierre por padre no autoriza saltar dependencias, bajar pruebas, aceptar P0/P1, false PASS/LISTO, integrar patches stale ni tocar main/Producción/secrets.

## 11. Watchdog y actividad real

Dispatch != ACTIVE. Jules ACTIVE exige sesión correlacionada + actividad técnica útil.

- >5 min sin primera actividad útil: `BOOTSTRAP_STALLED`.
- >=10 min sin progreso: recovery/failover controlado, sin duplicar ownership.
- `PAUSED` sin trabajo útil: no ACTIVE.
- terminal: review y handoff inmediatos.

El ejecutor resuelve waits rutinarios dentro de su capacidad real y deja handoff al agotarla. La documentación no puede afirmar que una automatización o checkpoint corrió sin evidencia del sistema de tareas.

## 12. Handoff técnico GitHub

Al terminal, el ejecutor v3.25 debe emitir evidencia equivalente a:

```text
protocol=v3.25
taskAttempt=1|2
controllerHandoff=REVIEW_IMMEDIATELY_AND_ASSIGN_NEXT_SAFE
parentCloseFirst=true
```

La Issue o evidencia equivalente del ejecutor es la señal técnica. Los scripts/workflows existentes no fueron modificados por esta gobernanza y deben verificarse antes de atribuirles compatibilidad v3.25. Los checkpoints `:00/:15/:30/:45/:55` son red de recuperación y no permiso para dejar una entrega terminal esperando.

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


## 15. AntiG/Antigravity — reviewer/fixer automático

AntiG/Antigravity opera como `AUTOMATED_REVIEWER_FIXER` desde el checkout local autorizado. Su ruta primaria no es competir con los Jules como quinto writer, sino consumir handoffs terminales Jules y cerrar el hueco entre `COMPLETED` y la revisión VAEP.

Flujo obligatorio:

```text
JULES_COMPLETED
-> ANTIG_REVIEW
-> ANTIG_FIXING (solo defectos menores/medios dentro del mismo scope)
-> READY_FOR_VAEP
-> VAEP/controller review
-> LISTO_REAL solo por autoridad de cierre separada
```

Reglas duras:

1. AntiG nunca convierte por sí solo una tarea a `LISTO_REAL`, nunca auto-promueve COLA/BITACORA y nunca sustituye al Closure Governor.
2. El reviewer automático parte únicamente de un handoff Jules terminal con artifact/patch trazable, task/dispatch/attempt y scope verificables.
3. ATTEMPT=1 con defecto estructural puede terminar `RETURN_TO_JULES` para el único R2 permitido. ATTEMPT=2 con defecto estructural termina `BLOCKED_QA_TAKEOVER`; R3+ sigue prohibido.
4. AntiG puede corregir defectos menores/medios, completar pruebas proporcionales y aplicar el patch Jules únicamente dentro del mismo write-scope. Scope leak, dependencia inválida, patch stale material, P0/P1 o cambio arquitectónico mayor fallan cerrados.
5. El agente headless no hace `git add/commit/push/merge/rebase/reset/checkout/switch`. La publicación, cuando proceda, la realiza el wrapper local después de revalidar HEAD, working tree, scope y gates mínimos.
6. Si `origin/Desarrollo` cambia durante la revisión, la publicación automática se detiene. Nunca se usa force-push ni rebase automático.
7. Producción, `main`, secretos, credenciales, Vercel, dominios, certificados y bases/datos productivos permanecen prohibidos.
8. Cada integración AntiG exitosa genera evidencia VAEP separada; `READY_FOR_VAEP` significa candidato revisado, no certificación final.
9. El worker automático oficial vive en `scripts/antig/`; el Custom Agent de workspace vive en `.agents/agents/variapp-reviewer/agent.md`.
10. La activación local se realiza únicamente mediante `scripts/antig/install-antig-automation.ps1`, que usa permisos finos y nunca bypass global de permisos.
