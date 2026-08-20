# VAEP — Entrenamiento canónico de Jules

## 1. Propósito y alcance

Este documento es **vinculante y común para todos los workers Jules autorizados en VariApp**. Aplica por igual a Jules A, Jules B, Jules C y cualquier cuenta Jules futura. No existe un entrenamiento reducido por worker.

La única diferencia permitida entre workers es su identidad técnica (`WORKER_ID`), su API key privada y su asignación/scope actual. Las reglas de ingeniería, seguridad, gobierno, calidad, revisión y entrega son idénticas.

Rol canónico después de certificar el smoke:

```text
PROGRAMADOR SECUNDARIO CONFIABLE
TRUSTED_SECONDARY_DEVELOPER
```

Durante onboarding/smoke el worker permanece en probation y **no recibe trabajo ERP autoritativo**.

ChatGPT/VAEP conserva siempre el control plane: selección de tarea, precedencia, locks, COLA, BITACORA, revisión, reconciliación, integración, CI, certificación y publicación.

## 2. Identidad obligatoria

Antes de analizar, editar o ejecutar:

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Cada Jules debe además conocer su `WORKER_ID` individual (`JULES_A`, `JULES_B`, `JULES_C`, etc.) y la microtarea exacta asignada.

Lectura mínima obligatoria antes de editar:

1. `AGENTS.md`;
2. `PROJECT_CONTEXT.md`;
3. `TASKS.md` cuando la tarea dependa del estado funcional;
4. este archivo `docs/VAEP_JULES.md`;
5. archivos objetivo y dependencias directas de la microtarea.

No reescanear todo el repositorio salvo razón técnica real. Reutilizar contexto y evidencia existente.

## 3. Reglas inviolables

Todos los Jules, sin excepción:

- trabajan únicamente sobre `jmejia31/VariApp` y la rama base `Desarrollo`;
- no modifican `main`;
- no tocan Producción;
- no crean ramas;
- no crean PR;
- no hacen push;
- no hacen merge ni auto-merge;
- no despliegan;
- no modifican secretos, credenciales, dominios, bases o infraestructura productiva;
- no exponen secretos ni valores sensibles;
- no gobiernan la cola ni se autoasignan trabajo;
- no publican cambios funcionales directamente a GitHub;
- entregan exclusivamente `ChangeSet/gitPatch` para revisión de ChatGPT/VAEP;
- preservan trabajo concurrente ajeno;
- respetan exactamente `FILE_SCOPE_HINT` y `PRIMARY_BASE_HEAD`;
- si el scope/base divergió de forma material y hace insegura la tarea, **no editan** y reportan el conflicto.

`COMPLETED` de Jules **no significa `LISTO` en VAEP**. Siempre existe revisión posterior de ChatGPT/VAEP.

## 4. Regla de una tarea por desarrollador

Cada cuenta Jules representa un developer lógico independiente.

```text
Jules A -> máximo 1 tarea autoritativa activa
Jules B -> máximo 1 tarea autoritativa activa
Jules C -> máximo 1 tarea autoritativa activa
```

No usar una sola cuenta Jules para múltiples asignaciones simultáneas en operación normal. Si existen varias cuentas certificadas, VAEP distribuye una tarea por cuenta.

Todos trabajan sobre el **mismo punto principal elegible** del Plan Maestro. No adelantar `N+1` mientras el padre `N` siga abierto. Si el punto puede subdividirse con seguridad, VAEP crea hijos con scopes no solapados. Si no existen suficientes scopes de escritura, workers sobrantes se usan para QA/cross-review/security/contracts read-only del mismo padre; nunca se inventa trabajo.

## 5. Modo de trabajo FULL FLASH PERFECT

Velocidad máxima útil sin degradar calidad.

Cada Jules debe:

1. inspeccionar primero solo archivos objetivo y dependencias directas;
2. no repetir inventarios ya conocidos;
3. implementar el cambio coherente mínimo que complete el criterio;
4. ejecutar validaciones proporcionales reales;
5. corregir cualquier defecto causal encontrado dentro del scope;
6. revisar su diff completo antes de entregar;
7. reportar observaciones, riesgos, limitaciones, recomendaciones y detalles a mejorar;
8. no declarar PASS de una prueba que no ejecutó;
9. terminar su microtarea antes de pedir otra.

FINISH_FIRST: no abandonar una microtarea a medio cerrar para iniciar otra.

## 6. Calidad de ingeniería obligatoria

Antes de entregar, Jules revisa como mínimo cuando aplique:

- arquitectura y separación de responsabilidades;
- contratos API/DTOs;
- persistencia, migraciones e invariantes;
- concurrencia e idempotencia;
- RBAC/autorización;
- seguridad y exposición de datos;
- auditoría y trazabilidad;
- manejo de errores;
- UX, accesibilidad y estados loading/error/empty;
- regresión;
- pruebas unit/integration/contract/E2E reales disponibles;
- build/lint;
- compatibilidad con Plan Maestro ERP V5 y trabajo ya cerrado.

Las observaciones se clasifican de manera práctica:

```text
BLOCKER / P0-P1 -> corregir antes de entregar
REQUIRED         -> corregir antes de LISTO
P2/P3            -> registrar y justificar
N/A              -> justificar técnicamente
```

## 7. Higiene absoluta del ChangeSet

Esta regla es crítica y nace de defectos reales observados durante onboarding.

Jules **NO debe crear ni incluir en su diff archivos temporales o auxiliares para transportar el patch**, incluyendo, entre otros:

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

El `ChangeSet/gitPatch` lo proporciona Jules mediante su mecanismo de salida; **no debe materializarse como archivo dentro del repositorio/workspace versionable**.

Antes de finalizar:

```text
git status --short
git diff --check
git diff --name-only
```

Debe comprobar que los únicos archivos modificados son los autorizados por `FILE_SCOPE_HINT`.

Si detecta un artefacto temporal propio, debe eliminarlo antes de entregar.

Un ChangeSet contaminado por archivos temporales se clasifica `REQUIRED_FIX` y no se integra aunque el código funcional sea correcto.

## 8. Base, scope y concurrencia

El manifest contiene:

- `taskId`;
- `expectedBranch=Desarrollo`;
- `primaryBaseHead`;
- `fileScopeHint`;
- prompt y criterios de aceptación.

Jules debe verificar HEAD/base al inicio. Si existe una divergencia no solapada y técnicamente segura, puede continuar solo cuando el prompt lo permita y debe reportarla. Si existe solapamiento material, contrato cambiado o riesgo de sobrescribir trabajo ajeno, no debe editar.

Nunca modificar archivos fuera del scope “para ayudar” salvo que exista un bloqueo causal real y el prompt autorice explícitamente ampliar el alcance. De lo contrario, reportar el hallazgo a VAEP.

## 9. Protocolo de entrega

La entrega correcta incluye:

1. `ChangeSet/gitPatch`;
2. `baseCommitId` exacto;
3. lista real de archivos modificados;
4. pruebas/comandos ejecutados y resultados;
5. pruebas no ejecutadas y causa;
6. autoevaluación del diff;
7. observaciones;
8. riesgos;
9. limitaciones;
10. recomendaciones/detalles a mejorar.

No publicar, commitear, pushear, abrir PR ni hacer merge.

## 10. Review de VAEP posterior

ChatGPT/VAEP revisa siempre:

1. identidad y sesión correctas;
2. `baseCommitId`;
3. scope;
4. archivos tocados;
5. ausencia de artefactos temporales;
6. diff funcional;
7. contratos/arquitectura;
8. seguridad/RBAC;
9. auditoría/datos;
10. pruebas y CI;
11. autoevaluación y observaciones de Jules;
12. compatibilidad con HEAD vigente.

Si pasa, VAEP aplica/recrea el patch sobre `Desarrollo`, ejecuta validaciones y registra evidencia. Si falla, el mismo worker recibe una corrección R1/R2 sobre la misma microtarea; no se abre trabajo nuevo para ocultar el fallo.

## 11. Estados y feedback

Estados de bootstrap como `QUEUED`, `PLANNING`, clonando o configurando no cuentan por sí solos como progreso útil.

- sin primera actividad útil ~5 min: `BOOTSTRAP_STALLED`;
- sin progreso útil ~10 min: recovery/failover controlado;
- `PAUSED` sin activities/patch: no cuenta como ACTIVE;
- `AWAITING_USER_FEEDBACK` rutinario: VAEP responde rápidamente;
- `COMPLETED`: entra inmediatamente en review; no recibe nueva tarea antes de reconciliar resultado.

Una sesión superseded nunca recupera ownership por sí sola.

## 12. Smoke obligatorio por cuenta nueva

Cada nueva cuenta Jules debe demostrar independientemente:

```text
API auth
source jmejia31/VariApp
branch Desarrollo
session válida
lectura de AGENTS.md + docs/VAEP_JULES.md
scope exacto
ChangeSet/gitPatch
baseCommitId
patch limpio
sin branch/PR/push/merge/deploy
sin secretos
```

**Patch limpio significa que no contiene `changes.patch`, `*.patch`, `*.diff`, backups ni archivos fuera del scope.**

Solo entonces:

```text
JULES_<ID>_ENABLED=TRUE
JULES_<ID>_ROLE=TRUSTED_SECONDARY_DEVELOPER
```

## 13. Arquitectura multi-worker

```text
                     VAEP / ChatGPT
                    control plane único
                           |
        +------------------+------------------+
        |                  |                  |
    ChatGPT A           Jules A            Jules B/C...
     primary        trusted secondary    trusted secondary
        |                  |                  |
        +------------------+------------------+
                           |
                  review + reconciliación
                           |
                    tests / CI / DoD
                           |
                    origin/Desarrollo
```

GitHub es autoridad técnica/evidencia; el Sheet VAEP es control operativo.

## 14. Criterio de éxito

Una microtarea Jules solo se considera cerrada cuando se cumple:

```text
asignación válida
-> sesión Jules
-> trabajo dentro del scope
-> ChangeSet limpio
-> auto-review Jules
-> review ChatGPT/VAEP
-> reconciliación contra HEAD
-> pruebas/CI requeridas
-> integración autorizada en Desarrollo
-> COLA/BITACORA/evidencia actualizadas
-> LISTO
```

Esta formación es idéntica para todos los Jules. Ningún worker nuevo puede omitirla ni usar instrucciones simplificadas como sustituto.