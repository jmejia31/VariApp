# Contexto de proyecto ChatGPT/VAEP

> Guía operativa de VariApp actualizada el 2026-08-24. Resume el flujo; no sustituye las fuentes de verdad ni demuestra estado externo no consultado.

## Estado seguro de protocolo

- Control-plane global observado: `VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION`.
- Subprotocolo vigente: `V3.25_CURRENT`, con cierre por padre y checkpoints `:00/:15/:30/:45/:55`.
- v3.20/v3.21 son historia; conservan evidencia, no autoridad operativa.
- El Sheet registra/describe configuración y estado; el sistema de tareas ejecuta. Sin evidencia del ejecutor no se afirma actividad, checkpoint ni automatización real.

## Roles reales

- **ChatGPT/VAEP:** control plane, reconciliación de estado, selección segura, review-first, integración, diagnóstico causal de CI, certificación, handoff y failover. No debe figurar como activo si no existe una invocación realizando acciones reales.
- **Codex:** implementación, pruebas y documentación desde el checkout local autorizado; respeta gate, alcance y trabajo concurrente.
- **AntiG/Antigravity:** implementación y revisión local autorizada bajo el mismo gobierno y exclusión de scope.
- **Jules A/B/C/D:** workers de cambios revisables con un único ownership autoritativo y scopes no solapados; no publican cambios funcionales. Sus intentos, self-review y handoff dependen del protocolo Jules que debe reconciliarse.

## Ciclo de una ejecución automática

1. Confirmar proyecto, repo, `Desarrollo`, HEAD, divergencia y archivos sucios.
2. Confirmar v3.25, cierre por padre y evidencia del ejecutor; si no coinciden, detener mutaciones y emitir handoff.
3. Leer mutex, actividad real, tarea/CI relacionados y estado operativo fresco; reconciliar trabajo `EN_PROGRESO/VALIDANDO` antes de elegir otro.
4. Seleccionar una tarea dependency-valid sin duplicar implementación, commit, artifact o evidencia existentes.
5. Reservar ownership/scope exclusivo; cambiar a `EN_PROGRESO` antes de editar.
6. Implementar el menor changeset completo y pasar a `VALIDANDO`; ejecutar pruebas proporcionales.
7. Atribuir CI al SHA exacto. `QUEUED/IN_PROGRESS` implica espera verificable, no éxito; un CI de otro SHA no certifica el HEAD.
8. Aplicar review-first, corregir causalmente y promover a `LISTO` solo con DoD/evidencia suficientes.
9. Persistir estado, CI, SHA y próximo punto; liberar o conservar lease según exista actividad/CI real; emitir handoff accionable.

## Mutex, actividad, CI y reanudación

- Mutex evita dos corridas mutantes; poseerlo no prueba actividad.
- `ACTIVE` requiere invocación viva y acción técnica reciente. `WAITING_CI` requiere CI relacionado realmente activo. Si la invocación terminó, registrar `IDLE` o `IDLE_PLATFORM_LIMIT` y un `RESUME_POINT` exacto.
- Heartbeat solo se renueva por acciones reales; nunca para aparentar continuidad.
- Antes de mover HEAD, comprobar gates causales activos. Durante un freeze se permite review/diagnóstico read-only.
- Todo handoff mínimo incluye tarea, owner/scope, HEAD/SHA evaluado, CI y resultado, bloqueo, próxima acción y condición de reanudación.

## Mapa de fuentes de verdad

| Pregunta | Fuente |
| --- | --- |
| Identidad, seguridad, ramas y reglas vinculantes | `AGENTS.md` |
| Versión operativa VAEP/Jules | `docs/VAEP_AUTHORITY.md` |
| Algoritmo global, mutex, actividad y cierre | `PLAN_EJECUCION_AUTONOMA.md` |
| Arquitectura y navegación de código | `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md` |
| Pendientes resumidos locales | `TASKS.md` |
| Evidencia de changesets | `CHANGELOG_AI.md`, Git y CI del SHA exacto |
| Roadmap/estado operativo externo | Plan/`CONFIG/COLA/BITACORA`, solo después de lectura fresca |
| Realidad técnica | HEAD, código y pruebas actuales |

## Consulta selectiva de bajo contexto

1. Gate + `docs/VAEP_AUTHORITY.md`.
2. `PROJECT_CONTEXT.md`, fila relevante de `TASKS.md` y última entrada aplicable de `CHANGELOG_AI.md`.
3. Fila del dominio en `PROJECT_INDEX.md`; abrir solo punto de entrada y dependencias directas.
4. Consultar documento VAEP especializado solo si la tarea toca su regla.
5. Ampliar únicamente por contradicción, cambio transversal, seguridad/datos o fallo causal.

No releer todo `docs`, controladores, migraciones o historial. Antes de crear algo, buscar símbolo, ruta, tarea y commit para no duplicar trabajo.

## Estado local observable

- Observado antes de esta edición: `Desarrollo` limpio y sincronizado en `4a42220905aedc9c35f426994b9b8618a2478408`.
- Commits recientes visibles: documentación de navegación (`4a422209`, `4f3850da`, `f9230284`) y cambios funcionales N3.3.E inmediatamente anteriores (`960ac07e`, `a448edc6`).
- El historial dirigido contiene cierres v3.21; permanecen válidos como evidencia histórica, no como gobierno vigente.
- No se leyó Sheet/Drive en esta actualización; por tanto no se afirma `CURRENT_PARENT`, Parent40, owner, mutex ni CI externos actuales.

## Mejoras priorizadas

1. **P0 — protocolo único:** mantener v3.25 alineado entre `AGENTS.md`, autoridad, plan y contexto; marcar toda versión anterior como historia.
2. **P0 — reconciliar antes de automatizar:** hacer de v3.25 + mutex + HEAD + tarea/CI fresca una precondición dura para cualquier mutación.
3. **P1 — visibilidad de ejecución:** exponer siempre tarea, owner/scope, estado real, SHA, CI causal, última acción y `RESUME_POINT`.
4. **P1 — reanudación determinista:** cada detención deja condición de retorno y próxima acción exacta; la siguiente corrida reconcilia ese handoff antes de seleccionar trabajo.
5. **P2 — economía de contexto:** mantener resúmenes canónicos cortos y actualizar solo el módulo/cambio afectado.
