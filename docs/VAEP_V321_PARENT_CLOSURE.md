# VAEP/Jules v3.21 — Parent Closure First + Rolling Parent40

> HISTÓRICO desde 2026-08-24: v3.25 es la única autoridad operativa. Este archivo conserva la evolución v3.21 y no gobierna nuevos dispatches.

Activación: `2026-08-21T22:57:00-06:00` (`America/Tegucigalpa`).

Este documento fue la norma v3.21. Se conserva como historial y no degrada el control-plane global `VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION`.

## 1. Ley principal

```text
JULES_PROTOCOL_VERSION=V3.21_CURRENT
PARENT_CLOSE_FIRST=TRUE
CURRENT_PARENT_SWARM=MANDATORY
MAX_VOLUNTARY_IDLE=0
MAX_AUTHORITATIVE_TASKS_PER_JULES=1
ROLLING_PARENT_TARGET=40
TARGET_MODE=CONTINUOUS_UNTIL_40
SPRINT40_REACTIVATED_AT=2026-08-21T22:57:00-06:00
CHECKPOINTS=:00,:15,:30,:45
```

`CURRENT_PARENT` es la primera fila `TYPE=MICROTAREA` real, dependency-valid, que aún no está `LISTO`. El objetivo primario de VAEP, ChatGPT y Jules A/B/C/D es llevar ese padre a `LISTO` con el menor `PARENT_LEAD_TIME` posible **sin sacrificar calidad**.

## 2. Swarm obligatorio del padre

Toda capacidad segura disponible converge sobre `CURRENT_PARENT` con scopes exclusivos/no solapados o trabajo read-only compatible:

- dominio/contratos;
- persistencia/migración/datos;
- aplicación/API;
- frontend/UX;
- RBAC/auditoría/seguridad;
- pruebas/regresión/CI;
- performance/observabilidad;
- documentación/certificación;
- cross-review, reproducción y corrección causal.

No se crea trabajo cosmético para aparentar utilización. Dos workers nunca escriben simultáneamente el mismo archivo/scope.

## 3. ChatGPT/VAEP también produce

ChatGPT/VAEP no es un observador pasivo. Mientras exista trabajo seguro debe ejecutar el camino crítico: drenar `REVIEW_QUEUE`, revisar artifacts/diffs, resolver waits rutinarios, corregir defectos causalmente, integrar, validar, diagnosticar CI, actualizar evidencia y certificar el rollup. Un R2 Jules fallido pasa a `QA_TAKEOVER` y se termina fuera de Jules.

## 4. Zero idle real, no actividad ficticia

```text
CHATGPT_VAEP_IDLE_WHEN_SAFE_WORK_EXISTS=PROHIBITED
JULES_IDLE_WHEN_SAFE_WORK_EXISTS=PROHIBITED
```

Un Jules libre recibe inmediatamente un scope seguro del mismo padre cuando exista. `ACTIVE` solo puede declararse con sesión correlacionada + actividad técnica útil. `dispatch != ACTIVE`. Un gate/dependencia técnica real puede producir `WAITING`, pero debe registrar trigger exacto y siguiente acción prearmada.

Los checkpoints de :00/:15/:30/:45 son **fallback de recuperación**, no permiso para dormir. Una ejecución viva cierra/reasigna sin esperar al siguiente checkpoint.

## 5. Excepción N+1

`WORK_CAN_PIPELINE__PROMOTION_CANNOT` permanece vigente. Solo cuando `CURRENT_PARENT` no pueda absorber más trabajo seguro útil por dependencia/gate real se permite preparación independiente/evidence-only de N+1. Ese trabajo no se promueve ni sustituye el swarm del padre.

## 6. Retry cap heredado y reforzado

```text
ATTEMPT=1
ATTEMPT=2 / R2   # único rework Jules
R3+ = PROHIBIDO
R2_FAIL = QA_TAKEOVER
```

Cambiar de Jules no reinicia attempts. Después de R2 fallido:

```text
JULES_RETRY_EXHAUSTED
OWNER=CHATGPT_VAEP_VIBE
ACTION=QA_TAKEOVER_CORRECT_TEST_CERTIFY
```

## 7. Dos self-reviews independientes

Antes de `COMPLETED`, Jules debe emitir en **dos actividades distintas**:

```text
SELF_REVIEW_PASS_1=PASS
SELF_REVIEW_PASS_2=PASS
```

La primera revisa scope/diff/contratos/arquitectura/seguridad/RBAC/auditoría/datos/funcionalidad. La segunda vuelve a revisar desde cero pruebas, `git diff --check`, temporales/lockfiles/scope leaks, observaciones, limitaciones, riesgos, recomendaciones y pruebas no ejecutadas. Si faltan las dos emisiones independientes, REVIEW-FIRST no puede declarar PASS.

## 8. Rolling Parent40

El objetivo es **40 padres `MICROTAREA` reales en `LISTO` desde la activación v3.21**, y continúa hasta alcanzar 40.

No cuentan:

- `MICROTAREA_HIJA`;
- support/preflight;
- manifests/dispatches;
- sesiones;
- Jules `COMPLETED`;
- `EN_PROGRESO` o `VALIDANDO`.

Solo cuenta el padre con implementación + QA + DoD + gates aplicables + P0/P1=0 + evidencia real.

Cada checkpoint registra como mínimo:

```text
CURRENT_PARENT
PARENTS_LISTO_SINCE_REACTIVATION
GAP_TO_40
PARENT_LEAD_TIME
TIME_TO_FIRST_USEFUL_ACTIVITY
REVIEW_WAIT
CI_WAIT
RETRABAJO
CONFLICTS
BLOCKERS
A/B/C/D/CHATGPT utilization
ZERO_IDLE_VIOLATION
```

## 9. Cierre y rebinding inmediato

Cuando `CURRENT_PARENT` cumple DoD:

1. actualizar `COLA.ESTADO=LISTO` con evidencia;
2. registrar BITACORA;
3. seleccionar inmediatamente el siguiente padre dependency-valid;
4. reubicar los lanes seguros A/B/C/D + ChatGPT/VAEP en la misma ejecución.

No existe pausa administrativa entre padres.

## 10. HEAD_FREEZE no es descanso

Si un write a `Desarrollo` cancelaría/supersedería Development, Acceptance, Fase8, M13 u otro gate causal crítico activo, no se mueve HEAD. Durante freeze continúan review, Issues, Sheet, análisis read-only, QA compatible, diagnóstico y preparación de la siguiente acción. Al quedar terminal el gate, la acción se libera inmediatamente cuando exista una ejecución viva.

## 11. Seguridad inviolable

- solo `jmejia31/VariApp` / `Desarrollo`;
- no `main`, Producción, ramas nuevas, merge, auto-merge, force-push ni deploy;
- no secretos/credenciales/DB/servicios/dominios productivos;
- Jules nunca publica cambios funcionales: solo `ChangeSet/gitPatch` para REVIEW-FIRST;
- no false PASS, false ACTIVE ni false LISTO.

## 12. Precedencia

1. `CONFIG.RUNNER_PROTOCOL_VERSION` para control-plane global;
2. `CONFIG.JULES_PROTOCOL_VERSION=V3.21_CURRENT`;
3. este documento;
4. manifest vigente;
5. `docs/VAEP_AUTHORITY.md`;
6. `AGENTS.md` y `docs/VAEP_JULES.md`;
7. Plan Maestro / CONFIG / COLA / BITACORA;
8. HEAD/código/pruebas reales.

`docs/VAEP_V320_RETRY_CAP.md` y `docs/VAEP_V320_SPRINT40_QUEUE.md` quedan como evidencia histórica; sus reglas de retry válidas fueron incorporadas explícitamente aquí.
