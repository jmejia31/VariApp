# VAEP — Jules como segundo desarrollador

## 1. Objetivo

Integrar Jules como **segundo desarrollador del VAEP junto a ChatGPT** para consumir la cola pendiente de VariApp con mayor throughput sin perder la autoridad técnica de GitHub, la rama única `Desarrollo`, los gates ni la trazabilidad.

Jules no sustituye al Runner ni crea una automatización soberana. ChatGPT/VAEP conserva el control plane: selección, locks, `COLA`, `BITACORA`, reconciliación, certificación y publicación.

## 2. Identidad invariable

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Reglas inviolables para Jules:

- leer `AGENTS.md` antes de editar;
- trabajar únicamente sobre el source de `jmejia31/VariApp` y `Desarrollo`;
- no tocar `main` ni Producción;
- no crear ramas, PR, merge, auto-merge ni deployment;
- no publicar cambios funcionales por sí mismo;
- no exponer secretos;
- preservar trabajo concurrente;
- ejecutar validaciones proporcionales reales;
- devolver cambios como `ChangeSet/gitPatch` para reconciliación del VAEP.

## 3. Modelo de ejecución

Jules ejecuta el agente en su entorno cloud. La CLI `jules` puede instalarse y usarse desde la PC para iniciar/monitorizar sesiones y traer patches, pero no convierte el runtime del agente en un proceso local.

Arquitectura:

```text
                       VAEP / ChatGPT
                    control plane único
                            |
              +-------------+-------------+
              |                           |
      carril primario                carril Jules
        ChatGPT                      secundario
              |                           |
      cambios + pruebas         ChangeSet/gitPatch artifact
              |                           |
              +-------------+-------------+
                            |
                reconciliación de HEAD
                            |
                 publicación autorizada
                     origin/Desarrollo
```

## 4. Paralelismo seguro

El carril primario conserva `FINISH_FIRST`. Jules puede recibir **una** microtarea adicional cuando todas estas condiciones sean verdaderas:

1. pertenece a la fase/gate vigente;
2. todas sus dependencias directas y transitivas están `LISTO`;
3. ningún ancestro está bloqueado;
4. no está tomada por otro agente;
5. está marcada `PARALLEL_SAFE=SI` tras inspección real;
6. `FILE_SCOPE_HINT` no solapa archivos, contratos, migraciones, tablas ni invariantes con el trabajo primario;
7. su resultado puede integrarse sin cerrar falsamente un padre/gate;
8. no implica Producción, secretos ni autorización humana obligatoria.

Si cualquiera falla, Jules no recibe la tarea. La ausencia de una tarea paralela segura nunca detiene a ChatGPT.

El límite inicial es `JULES_MAX_CONCURRENT=1`. Solo se aumenta después de evidencia real de estabilidad y cero colisiones.

## 5. Protocolo de despacho

No se usa el flujo estándar de PR de Jules. Tampoco se requiere modificar `main`.

El VAEP crea un único manifest inmutable por despacho:

```text
vaep/jules/dispatch/<TASK_ID>-<BASE8>-<UTC_TIMESTAMP>.json
```

Formato:

```json
{
  "dispatchId": "N2.3.D-abcdef12-20260818T161500Z",
  "taskId": "N2.3.D",
  "expectedBranch": "Desarrollo",
  "primaryBaseHead": "40-char-git-sha",
  "fileScopeHint": "backend/src/...; backend/tests/...",
  "prompt": "Alcance exacto + criterios de aceptación + validaciones requeridas",
  "createdAt": "2026-08-18T16:15:00Z"
}
```

Reglas del manifest:

- un solo manifest nuevo por commit;
- el commit contiene únicamente el manifest de control plane;
- no usar `[skip ci]`, porque debe disparar el workflow Jules;
- nunca mezclar código de aplicación con el dispatch;
- `primaryBaseHead` es el HEAD conocido al asignar la tarea;
- el workflow obtiene el `baseCommitId` real del patch y VAEP decide compatibilidad al reconciliar.

## 6. GitHub Action

`.github/workflows/vaep-jules-secondary.yml` escucha exclusivamente pushes a `Desarrollo` que agregan `vaep/jules/dispatch/*.json`.

El workflow:

1. valida el manifest;
2. exige el secret `JULES_API_KEY`;
3. lista los sources de Jules y exige `jmejia31/VariApp` + rama `Desarrollo`;
4. crea o reutiliza idempotentemente una sesión titulada con `dispatchId`;
5. no envía `automationMode=AUTO_CREATE_PR`;
6. deja el plan autoaprobado para ejecución unattended;
7. monitorea la sesión hasta estado terminal;
8. descarga actividades y `ChangeSet/gitPatch`;
9. conserva `session.json`, `activities.json`, `gitpatch.json`, `changes.patch`, `dispatch.json` y `result.json` como artifact de GitHub Actions;
10. crea un Issue `[VAEP-JULES] ... result` solo como señal/evidencia;
11. **no hace commit ni push funcional**.

GitHub Actions actúa aquí como generador de artifact, compatible con `RUNNER_CI_GENERATOR_MODE=ARTIFACT_ONLY_NO_PUSH`.

## 7. Reconciliación del resultado

ChatGPT/VAEP reconcilia antes de publicar:

1. identificar la fila `AGENTE=Jules` y su `dispatchId/session`;
2. recuperar artifact/Issue del worker;
3. leer `gitPatch.baseCommitId` y `unidiffPatch`;
4. revalidar HEAD actual de `Desarrollo`;
5. comprobar que los archivos tocados respetan `FILE_SCOPE_HINT` y no invaden el carril primario;
6. revisar código, seguridad y criterios de aceptación;
7. aplicar/recrear el patch sobre HEAD actual únicamente si es seguro;
8. ejecutar pruebas/CI proporcionales;
9. actualizar `COLA`, `BITACORA`, `CHANGELOG_AI.md` y `TASKS.md` cuando corresponda;
10. publicar por fast-forward normal en `Desarrollo`;
11. marcar `LISTO` solo con evidencia suficiente.

Si la base divergió pero el cambio es claramente integrable y no solapa, el VAEP puede rebasarlo/recrearlo de forma controlada. Si existe conflicto material, descarta el resultado y redispatcha sobre un HEAD nuevo; nunca force-push.

## 8. Estados en COLA

Columnas adicionales:

- `PARALLEL_SAFE`: `SI | NO | EVALUAR`;
- `FILE_SCOPE_HINT`: archivos/capas/contratos permitidos;
- `WORKER_SESSION`: `dispatchId` y/o sesión Jules;
- `WORKER_BASE_HEAD`: base conocida y luego `baseCommitId` real;
- `WORKER_RESULT`: estado, artifact/run/Issue y reconciliación.

`AGENTE=Jules` solo se usa para una tarea realmente despachada/activa. Una fila candidata no se bloquea anticipadamente solo por estar marcada `PARALLEL_SAFE=SI`.

## 9. Activación segura

`CONFIG.JULES_ENABLED` permanece `PENDING_EXTERNAL_AUTH` hasta completar:

1. conectar `jmejia31/VariApp` en Jules mediante su GitHub App;
2. confirmar que el source expone `Desarrollo`;
3. generar una Jules API key;
4. guardar esa clave exclusivamente como GitHub Actions secret `JULES_API_KEY`;
5. ejecutar smoke test;
6. comprobar que el workflow genera evidencia sin rama/PR/push funcional;
7. cambiar `JULES_ENABLED=TRUE`.

La clave nunca se copia al Sheet, código, manifest, Issue, log o prompt.

## 10. Configuración local opcional

Para control desde la PC:

```powershell
npm install -g @google/jules
jules login
jules remote list --repo
```

El script `scripts/configurar-jules-vaep.ps1` automatiza el preflight local y el registro seguro del secret mediante GitHub CLI cuando esas herramientas están disponibles.

Una vez activado, la ejecución ordinaria se hace desde VAEP; no se deben lanzar manualmente sesiones Jules sobre las mismas filas sin registrarlas en `COLA`.

## 11. Criterio de éxito

La integración queda operativa cuando se demuestre un ciclo completo:

```text
PENDIENTE elegible
  -> asignación Jules
  -> manifest Desarrollo
  -> sesión Jules
  -> patch artifact
  -> reconciliación ChatGPT
  -> validaciones reales
  -> commit Desarrollo
  -> COLA/BITACORA/CHANGELOG actualizados
  -> LISTO
```

Después de varias ejecuciones sin colisión puede evaluarse aumentar `JULES_MAX_CONCURRENT`, manteniendo un único control plane y scopes no solapados.
