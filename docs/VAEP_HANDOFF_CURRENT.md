# VAEP handoff actual — VariApp

> Handoff operativo compartido para ChatGPT/Chat, Codex, Antigravity, Jules J1–J6 y cualquier colaborador. La autoridad normativa sigue siendo `docs/VAEP_AUTHORITY.md`; el estado fresco siempre se confirma contra GitHub `Desarrollo` y el Plan Maestro antes de actuar. No usar este archivo como sustituto de evidencia causal.

## Corte de control sincronizado

- Repo: `jmejia31/VariApp`; rama operativa: `Desarrollo`.
- Baseline de control antes de este handoff: `82484b0628f0fa30a7fc9882a580d6c004b37e38` (`chore(vaep): recover J4 N5.2.A transport`). Después de leer este archivo, volver a consultar el HEAD vivo.
- `lastClosedParent=N5.1.H`; recibo: `vaep/evidence/fragments/N5.1.H_LISTO_REAL_20260909T1236Z.json`.
- `CURRENT_PARENT=N5.2.A` — Reportes de inventario / auditoría y preflight.
- Admission: `OPEN`, con razón `N5.2.A_CURRENT__SIX_NON_OVERLAPPING_WRITE_SCOPES_READY__NO_FALSE_ACTIVE`.
- `N5.2.A` sigue `PENDIENTE`: no declarar `LISTO_REAL`, no promover `N5.2.B` y no inferir PASS por dispatch, commit o workflow aislado. Requiere seis entregas materiales reconciliadas, REVIEW_FIRST, gates causales/DoD y P0/P1=0.
- `main`, Producción, secrets y PR #2 quedan fuera de alcance; PR #2 no se mergea desde VAEP.

## Delta material y deuda corregida

- J3 fue rechazado en REVIEW_FIRST por defectos factuales/de prueba y pasó a QA_TAKEOVER en vez de rebind genérico. Delta integrado: `6c5ca463cabdf79335123fb05e2c109173cc6668` (`docs(N5.2.A): QA takeover J3 reconciliation preflight`) → `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_RECONCILIACION_OPERATIVA.md`. La corrección usa terminología real (`DiferenciaSnapshot`) y conserva gaps/limitaciones explícitos.
- Transporte J1–J6 endurecido: `7b33ca82b446afe5e6c190fc66f807421bd06ded` acepta rutas canónicas J5/J6 y `3e46b387ef2d680a53bbfd1a48e79a462f1a27a8` repara semántica de timeout terminal. `.github/scripts/vaep-jules-master.sh` acepta `vaep/(jules|jules-b|jules-c|jules-d|j5|j6)/dispatch/*.json`.
- Recoveries pre-sesión materializados sin consumir intento Jules: J1 `82385dae67ad244ab5b5868f5c110aa821ff984c`; J2 `8103ee241a9d696f22bd44a5fe6194cd14424716`; J5 `8b86f05c76b7e4c8b9e1b60ae68a35f8c38f2df2`; J6 `c5f885fb732ccc6d140188c67fa82e509dc35036`; J4 `82484b0628f0fa30a7fc9882a580d6c004b37e38` con `vaep/jules-d/dispatch/N5-2-A-5-ARCH-J4-RECOVERY1.json`.
- Una recovery manifest/dispatch NO prueba `ACTIVE_REAL`. Antes de afirmar ACTIVE, terminal, reviewable o consumido, comprobar workflow/session evidence vivo.

## Seis scopes materiales de N5.2.A

| Lane | Task | Entrega material esperada | Estado cierto al baseline |
|---|---|---|---|
| J1 | `N5.2.A.2.VALUATION_KARDEX_PREFLIGHT` | `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_VALORIZACION_KARDEX.md` | recovery despachado; archivo aún no materializado |
| J2 | `N5.2.A.3.STOCK_ANALYTICS_PREFLIGHT` | `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_STOCK_ANALYTICS.md` | recovery despachado; archivo aún no materializado |
| J3 | `N5.2.A.4.RECONCILIATION_OPERATIONS_PREFLIGHT` | `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_RECONCILIACION_OPERATIVA.md` | QA_TAKEOVER integrado; pendiente REVIEW_FIRST/cierre causal del parent |
| J4 | `N5.2.A.5.ARCH_API_UI_TOPOLOGY_PREFLIGHT` | `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_ARCH_API_UI.md` | recovery despachado; archivo aún no materializado al baseline |
| J5 | `N5.2.A.1.REPORTES_INVENTARIO_PREFLIGHT_QA` | `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_QA.md` | recovery despachado; archivo aún no materializado |
| J6 | `N5.2.A.6.RBAC_AUDIT_SCOPE_PREFLIGHT` | `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_RBAC_AUDIT.md` | recovery despachado; archivo aún no materializado |

No rellenar capacidad ociosa con busywork ni duplicar scopes. ATTEMPT1+R2 máximo; R3 prohibido. Dos waves sin delta material => circuit breaker/QA_TAKEOVER, no rebind genérico.

## Qué debe mirar cada colaborador

### ChatGPT / Chat / VAEP

1. Leer `docs/VAEP_AUTHORITY.md` → este handoff → HEAD vivo → `vaep/control/jules-autorefill-catalog.json` → `vaep/control/dispatch-admission.json` → Plan Maestro.
2. Resolver blockers causales/materiales; no limitarse a reportar estado ni usar telemetría stale.
3. Drenar REVIEW_FIRST de cada entrega terminal y comprobar DoD/gates/P0-P1 antes de cualquier `LISTO_REAL`.
4. Tras cierre real de N5.2.A, materializar en la misma transición la promoción dependency-safe a N5.2.B y refill material, nunca antes.

### Codex

- Partir de HEAD exacto, no de memoria ni de un snapshot viejo.
- Auditar diffs, contratos, tests y causalidad de gates contra los seis scopes y el DoD de N5.2.A.
- Buscar contradicciones factual/domain/API, regresiones y P0/P1; proponer/corregir sólo deuda causal dentro de `Desarrollo`.
- No tocar `main`, Producción, secrets ni mergear PR #2. Codex no declara por sí solo `LISTO_REAL`.

### Antigravity / AntiG

- Auditoría independiente de arquitectura, automation debt, stale state, transport/session evidence y no-duplicación.
- Puede identificar causal blockers y señalar inconsistencias para takeover/controller; no inventar actividad, no duplicar un lane Jules sano y no declarar `LISTO_REAL`.
- Priorizar hechos reproducibles: HEAD exacto, archivos existentes, manifests, run/session evidence y Plan Maestro.

### Jules J1–J6

- Ejecutar únicamente la identidad/scope del manifest vigente de su lane; dos self-reviews.
- No escribir el archivo de otro lane ni redefinir semántica que ese lane posee.
- No main/Producción/secrets; no busywork; no R3.
- Una sesión o commit sólo se considera aceptable después de REVIEW_FIRST del controller.

## Orden exacto de inspección/cierre de N5.2.A

1. Confirmar HEAD vivo y admission.
2. Confirmar evidencia real de session/terminal para J1, J2, J4, J5 y J6; no usar el dispatch como sustituto.
3. Verificar/materializar los cinco documentos faltantes y preservar el J3 QA_TAKEOVER integrado.
4. REVIEW_FIRST por entrega: exactitud factual, no-duplicación, tests/evidencia, dos self-reviews y scope.
5. Reconciliar los seis documentos en la síntesis J5 sin borrar gaps reales.
6. Ejecutar/verificar gates causales aplicables al HEAD exacto; `skipped`, `action_required`, nombre coincidente o run de otro SHA no equivalen a PASS.
7. Confirmar P0=0 y P1=0 y emitir recibo sólo si el DoD real está satisfecho.
8. Sólo entonces cerrar N5.2.A y promover N5.2.B dependency-safe; sincronizar catálogo, admission, Plan Maestro, telemetría y este handoff.

## Compartidos que deben permanecer sincronizados

- Git: `docs/VAEP_AUTHORITY.md`, este `docs/VAEP_HANDOFF_CURRENT.md`, `vaep/control/jules-autorefill-catalog.json`, `vaep/control/dispatch-admission.json`, manifests/evidence y HEAD de `Desarrollo`.
- Plan Maestro: `DASHBOARD`, `CONFIG`, `COLA`, `TAREAS_PROGRAMADAS`, `CONTROL_TOWER`, `WORKERS`, `AUTOMATIZACIONES` y `BITACORA` cuando aplique.
- Las cinco tareas externas canónicas de ChatGPT deben seguir únicas y habilitadas en `:00/:12/:24/:36/:48`; comprobar el sistema de Tareas antes de recrearlas.

Si Git y el Plan discrepan, no elegir el dato más conveniente: reconciliar contra autoridad, evidencia causal y HEAD vivo, dejar rastro explícito y corregir el shared state antes de continuar.
