# VAEP/Jules v3.21 — Rolling Parent40

> HISTÓRICO desde 2026-08-24: v3.25 gobierna la operación actual. Esta cola conserva evidencia del esquema v3.21 y no autoriza trabajo nuevo.

> HISTORIA: este documento sustituye operativamente `docs/VAEP_V320_SPRINT40_QUEUE.md`. La cola v3.20 se conserva para auditoría; su ventana de tiempo ya venció.

## Objetivo

```text
ROLLING_PARENT_TARGET=40
TARGET_MODE=CONTINUOUS_UNTIL_40
SPRINT40_REACTIVATED_AT=2026-08-21T22:57:00-06:00
PARENT_CLOSE_FIRST=TRUE
CURRENT_PARENT_SWARM=MANDATORY
```

El contador comienza en cero al activar v3.21. Solo incrementa cuando una fila real `TYPE=MICROTAREA` pasa a `ESTADO=LISTO` después de la activación y cumple DoD/gates/P0-P1 aplicables.

No cuentan hijos, support, preflight, manifests, sesiones, dispatches, `COMPLETED`, `EN_PROGRESO` ni `VALIDANDO`.

## Selección dinámica

No existe una lista fija que autorice saltar dependencias. En cada checkpoint VAEP relee `COLA` y obtiene:

1. el padre más antiguo ya iniciado y no cerrado;
2. si no existe, la primera `MICROTAREA` dependency-valid por `ORDEN`;
3. ese padre se convierte en `CURRENT_PARENT`;
4. toda capacidad segura A/B/C/D + ChatGPT/VAEP se rebalancea para reducir su `PARENT_LEAD_TIME`.

Al activar v3.21, el estado fresco marcaba `N2.7.C` como `CURRENT_PARENT`. Este dato es un checkpoint histórico; **siempre manda COLA fresca**.

## Swarm

Los lanes se asignan por valor crítico y exclusión de scope, no para mantener actividad artificial. Ejemplos válidos dentro de un mismo padre:

- implementer de dominio/persistencia/API/frontend;
- reviewer independiente read-only;
- pruebas/CI/rollback;
- seguridad/RBAC/auditoría;
- documentación/certificación;
- diagnóstico/corrección causal.

Dos writers no comparten archivo/scope. Un Jules conserva máximo un ownership autoritativo.

## Early finisher

Prioridad obligatoria:

1. R2 todavía permitido de su tarea, si corresponde;
2. otro scope útil y exclusivo del `CURRENT_PARENT`;
3. review/tests/docs/read-only del mismo padre;
4. solo si un gate/dependencia real agota trabajo seguro del padre, N+1 independiente `evidence-only`.

N+1 nunca se promueve antes del prerequisito: `WORK_CAN_PIPELINE__PROMOTION_CANNOT`.

## Checkpoints

Los checkpoints agregados `:00/:15/:30/:45` son red de recuperación. Deben registrar:

- `CURRENT_PARENT`;
- `PARENTS_LISTO_SINCE_REACTIVATION`;
- `GAP_TO_40`;
- estado y actividad real A/B/C/D + ChatGPT/VAEP;
- `PARENT_LEAD_TIME`;
- `TIME_TO_FIRST_USEFUL_ACTIVITY`;
- `REVIEW_WAIT`;
- `CI_WAIT`;
- retrabajo/conflictos/blockers;
- `ZERO_IDLE_VIOLATION`.

Una ejecución viva no espera al próximo checkpoint para cerrar, revisar o reasignar.

## Calidad

El objetivo 40 nunca autoriza false `PASS`, false `ACTIVE`, false `LISTO`, saltar dependencias, omitir tests/CI/DoD, aceptar P0/P1, integrar stale patches, romper `HEAD_FREEZE`, tocar `main`/Producción/secrets ni superar el retry cap Jules v3.21.
