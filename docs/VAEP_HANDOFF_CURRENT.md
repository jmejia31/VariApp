# VAEP handoff actual — VariApp

> Estado operativo actual, no archivo histórico. La única autoridad normativa es `docs/VAEP_AUTHORITY.md`. Antes de actuar se debe releer el HEAD vivo de `Desarrollo`, `vaep/control/jules-autorefill-catalog.json`, `vaep/control/dispatch-admission.json` y el Plan Maestro original. Git y `BITACORA` conservan historia; este archivo conserva sólo el handoff vigente.

## Estado vigente

- Repo: `jmejia31/VariApp`; rama operativa: `Desarrollo`.
- `lastClosedParent=N5.2.B`; receipt: `vaep/evidence/fragments/N5.2.B_LISTO_REAL_20260909T1530Z.json`.
- `CURRENT_PARENT=N5.2.C` — Reportes de inventario / Persistencia, migración y datos.
- Admission: `OPEN`; razón `VERIFIED_ROADMAP_PROMOTION__N5.2.B__N5.2.C`; sesiones existentes permitidas.
- N5.2.C tiene seis scopes materiales no solapados, uno por J1–J6. Transporte/workflow no se convierte automáticamente en `ACTIVE_REAL`; cada terminal vuelve a `REVIEW_FIRST`.
- `NEXT_PARENT=N5.2.D`, dependency-gated por N5.2.C. El catálogo vigente no tiene material N5.2.D ya prearmado; ALEX puede emitir solicitudes de generación roadmap-derived, pero no inventa scopes ni los vuelve elegibles antes de dependencias.
- `main`, Producción, secrets, deploy y merge de PR #2 quedan fuera del flujo VAEP.

## J1–J6 actuales

| Lane | Task actual |
|---|---|
| J1 | `N5.2.C.2.VALUATION_KARDEX_PERSISTENCE_PLAN` |
| J2 | `N5.2.C.1.REPORT_QUERY_PERSISTENCE_PLAN` |
| J3 | `N5.2.C.3.RECONCILIATION_PERSISTENCE_PLAN` |
| J4 | `N5.2.C.4.MIGRATION_ROLLBACK_TOPOLOGY` |
| J5 | `N5.2.C.5.PERSISTENCE_RECONCILIATION_QA_PLAN` |
| J6 | `N5.2.C.6.SECURITY_PERSISTENCE_REQUIREMENTS` |

Regla operacional: terminal → `REVIEW_FIRST` inmediato → corrección/integración/DoD/gates → refill same-run si admission y dependencias lo permiten. `COMPLETED` Jules no equivale a `LISTO_REAL`. ATTEMPT1+R2 máximo; R3 prohibido.

## ALEX

- Autorización: `AUTHORIZED_NOW` / `ACTIVE_AUTHORIZATION` en `vaep/control/alex-owner-authorization.json`.
- Runtime: `ACTIVE_REAL`; A16–A25 = `ACTIVE_REAL / ACTIVE` en `vaep/control/alex-capabilities.json`.
- Evidencia deep-refill verificada: `VAEP ALEX Material Planner` run #12, ID `34372114115`, `success`.
- El run #12 verificó en runtime `ALEX_ROADMAP_GENERATION_REQUEST` y `ALEX_MATERIAL_GENERATION_REQUEST` cuando una lane cayó bajo floor/minimum.
- Deep refill: `ACTIVE_ROADMAP_GENERATION_REQUESTS`.
- Política: `ROADMAP_DERIVED__DEPENDENCY_SAFE__SEMANTIC_DEDUPE_ONLY`.
- Target: 12 scopes programados por lane; floor 4; mínimo elegible 2. Son objetivos de continuidad, no permiso para fabricar busywork.
- ALEX no es Jules lane, no escribe código de producto, no certifica `LISTO_REAL`, no omite `REVIEW_FIRST`, no salta dependencias y no usa regeneración genérica.

## Próximo paso N5.2.D

ALEX puede producir solicitudes deterministas de candidatos para N5.2.D y posteriores desde el roadmap/Plan/COLA vigente. Esos candidatos deben ser material-only, semánticamente únicos, con write scopes exclusivos y dependency-gated. Mientras N5.2.C no sea `LISTO_REAL`, un scope futuro no puede convertirse en dispatch elegible por conveniencia.

## Plan Maestro original

El archivo original `VariApp — PLAN MAESTRO DE AUTOMATIZACIONES` debe contener sólo hojas operativas útiles: `DASHBOARD`, `COLA`, `PLAN_MAESTRO`, `CONFIG`, `BITACORA`, `LEYENDA`, `TAREAS_PROGRAMADAS`, `CONTROL_TOWER`, `WORKERS`, `AUTOMATIZACIONES`. `EJECUCION_MANUAL` fue retirada y no debe recrearse.

Las hojas operativas son current-state y se actualizan por clave/ID; los valores stale se sobrescriben o eliminan. `BITACORA` es la única hoja append-only de historia. En `TAREAS_PROGRAMADAS`, cada tarea se localiza por `AUTOMATION_ID` y sólo se actualizan sus campos dinámicos; identidad, cadencia, objetivo y guardrails se preservan.

## Cinco tareas ChatGPT canónicas

- `:00` Primary
- `:12` Recovery
- `:24` Review
- `:36` Watchdog
- `:48` Debt

Las cinco deben releer el estado vivo, mantener terminal → REVIEW_FIRST → refill same-run, activar ALEX roadmap-refill cuando haya material seguro y sincronizar el Plan Maestro por UPSERT sin reintroducir identidades/cadencias retiradas ni snapshots stale.

Si Git y Drive discrepan, reconciliar contra `docs/VAEP_AUTHORITY.md` + evidencia causal + HEAD vivo. Nunca escoger el dato más conveniente ni convertir manifest, dispatch, workflow verde o telemetría stale en PASS/ACTIVE_REAL/LISTO_REAL.
