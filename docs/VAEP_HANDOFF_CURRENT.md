# VAEP HANDOFF CURRENT

Authority: `docs/VAEP_AUTHORITY.md` is the only operational master. Always reread live `Desarrollo`, `vaep/control/jules-autorefill-catalog.json`, `vaep/control/dispatch-admission.json`, and the original Plan Maestro spreadsheet before acting.

## Current parent

- Last closed parent: `N5.2.C`, certified `LISTO_REAL` by `vaep/evidence/fragments/N5.2.C_LISTO_REAL_20260909T1642Z.json`.
- CURRENT_PARENT: `N5.2.D`.
- Admission: `OPEN`; existing active sessions allowed.
- Admission reason: `VERIFIED_ROADMAP_PROMOTION__N5.2.C__N5.2.D__FIVE_ALEX_MATERIAL_SCOPES__J6_NO_BUSYWORK`.
- Next roadmap node: `N5.2.E`, dependency-gated and not eligible until `N5.2.D` is genuinely `LISTO_REAL`.

## Material continuity

ALEX reconciled exactly five source-backed, non-overlapping material scopes for `N5.2.D`; no safe sixth scope exists for J6 and no busywork may be fabricated:

- J1 — `N5.2.D.1.VALUATION_BACKEND_API`
- J2 — `N5.2.D.2.KARDEX_BACKEND_API`
- J3 — `N5.2.D.3.STOCK_HEALTH_BACKEND_API`
- J4 — `N5.2.D.4.RECONCILIATION_BACKEND_API`
- J5 — `N5.2.D.5.COMMON_API_SHELL`
- J6 — no safe material scope for the current parent; remain idle unless a new source-backed non-overlapping scope becomes dependency-valid.

Runtime is volatile and must be reread, never inferred from this file. During the latest PRIMARY reconciliation: J1 was rematerialized with a fresh immutable ATTEMPT1 after an `INVALID_REDISPATCH` transport rejection that did not consume R2; J2, J3 and J5 reached their permitted R2 after causal attempt-1 failures; duplicate non-executing R2 reservations for J2 and J3 were removed. J4 ATTEMPT1 eventually became terminal after the GitHub poller had exceeded its budget; the canonical post-terminal autorefill then materialized R2 `N5-2-D-4-RECON-API-J4-R2-20260909T171752Z-4627` and its `dispatch-jules` worker entered runtime. A second non-executing J4 R2 reservation (`...171758Z-2019`) was removed, preserving exactly one R2 and the R3 prohibition. A manifest, queued job, or historical label is not `ACTIVE_REAL`; require runtime evidence.

## Close-first rule

`N5.2.D` is not closed merely because work was dispatched. Every terminal Jules delivery must enter `REVIEW_FIRST` immediately, then correction/integration where applicable, DoD, causal gates, exact-head revalidation, and parent-level P0/P1=0. Do not certify or promote while R2/review/integration debt remains. If and only if `N5.2.D` becomes `LISTO_REAL`, promote the next dependency-valid node in the same run and refill real material work without waiting for another checkpoint.

## Guardrails

Work only on `Desarrollo`. Do not touch `main`, Producción or secrets, and do not merge PR #2. Maximum ATTEMPT1 + R2; no R3. Do not create work to satisfy lane-count targets. Use PLAN_MAESTRO/COLA/roadmap as source-backed planning inputs with semantic dedupe, non-overlapping scopes and dependency gates. Google Sheets is synchronized current-state telemetry; `BITACORA` alone is append-only history. Do not recreate `EJECUCION_MANUAL` or retired identities/cadences. A16–A25 are ACTIVE only with runtime evidence.
