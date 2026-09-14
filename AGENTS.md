# Reglas obligatorias de colaboración — VariApp

Este archivo es vinculante para Javier Mejía, ChatGPT/VAEP, Chat B, Vibe y cualquier agente autorizado.

## Gate obligatorio

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
EXECUTION_MODEL=TASKS_ONLY
```

Antes de analizar, editar, ejecutar o publicar trabajo VAEP, leer `docs/VAEP_AUTHORITY.md`. Para continuidad, leer también `docs/VAEP_HANDOFF_CURRENT.md`; el handoff nunca sustituye al MAESTRO.

## Autoridad única

- `docs/VAEP_AUTHORITY.md` es el único MAESTRO operativo.
- No crear protocolos paralelos, revisiones numeradas, copias `*-vX*` ni reglas ejecutables duplicadas.
- Git/CHANGELOG/BITACORA/Issues/artifacts/prompts históricos son evidencia, no autoridad.
- Si una fuente operativa contradice al MAESTRO, corregirla o neutralizarla.

## Modelo vigente

VAEP opera `TASKS_ONLY`.

- Las diez automatizaciones programadas son los únicos ejecutores/controllers del runtime VAEP.
- Las cinco primarias son `BUILDER_CLOSER`.
- Las cinco supervisoras son `VERIFIER_RECOVERY_SECONDARY_BUILDER`.
- Ejecución directa es el único camino operativo.
- No existen workers externos, lanes, manifests ni offload como parte del runtime vigente.
- No se fabrica filler/busywork para ocupar actores.

## Ownership

Todo write-scope directo exige lease operativo conforme al MAESTRO. Un solo writer por scope. Un owner con lease fresco y progreso material no se duplica; un lease sin invocación física viva o sin progreso material puede ser tomado conforme a las reglas de takeover.

Una supervisora que encuentra la primaria trabajando correctamente ejecuta QA/review/gates/prearm o scope seguro independiente. Si encuentra ausencia, stall o deuda accionable, toma ownership y desarrolla/corrige directamente.

## Equipo

- Javier: propietario y autorización final.
- ChatGPT/VAEP: controller, developer directo, QA, REVIEW_FIRST, integración, CI, certificación, rollup y failover.
- Chat B: controller/developer/QA par de ChatGPT/VAEP en `Desarrollo`, bajo el mismo MAESTRO.
- Vibe: QA/corrector externo sólo por delegación.
- AntiG/Antigravity: reservado e inactivo; no scheduler, handoff ni certificación.
- Codex: fuera del flujo salvo orden explícita del usuario.

## Ejecución y recovery

Regla obligatoria: `DEFECT_RECOVERY_FIRST + FIRST_DETECTOR_OWNS_RECOVERY + NO_REJECT_QUEUE`.

1. Cada tarea activa relee HEAD/parent/rolling60/leases/deuda fresca.
2. Respeta un owner directo fresco; si no existe, adquiere/toma lease y ejecuta el gap material más corto al cierre.
3. REVIEW_FIRST, tests, corrección, exact-head gates y P0/P1=0 son obligatorios para `LISTO_REAL`.
4. Defecto interno reparable se resuelve same-run; no se estaciona como `REJECTED`, `BLOCKED`, `HANDOFF_ONLY` o `WAITING` si existe acción segura.
5. Tras recovery, desbloquear dependencias y continuar cierre/promoción same-run.

## ACTIVE_REAL y LISTO_REAL

`ACTIVE_REAL` exige run identificable + lease fresco exclusivo + actividad técnica útil/material reciente.

Un trigger, workflow, planner, lease sin progreso o declaración no es `ACTIVE_REAL`.

`LISTO_REAL` sólo lo declara VAEP/controller con REVIEW_FIRST + DoD + tests/gates causales aplicables + P0=0/P1=0 + evidencia exact-head/receipt verificable.

## Git y Producción

- Trabajar sólo en `Desarrollo`.
- `main` permanece congelada.
- PR #2 debe permanecer OPEN + DRAFT; no merge ni auto-merge.
- No ramas nuevas, force-push, reset destructivo ni amend de historia compartida.
- No Producción, secretos, credenciales, dominios, certificados, datos productivos, deploys ni infraestructura productiva.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.

## Historia de workers retirados

Los antiguos workers J1–J6/Jules fueron retirados por completo del runtime de VariApp. Sus commits y evidencia histórica permanecen inmutables para auditoría, pero no tienen autoridad ni vínculo operativo vigente.
