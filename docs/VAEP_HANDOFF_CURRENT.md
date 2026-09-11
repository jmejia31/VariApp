# VAEP HANDOFF CURRENT

Authority: `docs/VAEP_AUTHORITY.md` is the only operational master.
This file is a live-source navigation guide, never a cached runtime authority.

## Execution model

Current architecture is `TASKS_FIRST_JULES_ON_DEMAND`.

- Ten scheduled automations are the primary autonomous executors/controllers.
- Primaries are builder/closers.
- Supervisors are verifier/recovery/secondary-builders.
- Direct execution is default.
- J1–J6 are optional accelerators only after explicit `JULES_OFFLOAD_APPROVED` for independent, non-overlapping material work that improves the critical path.
- Jules utilization, queue depth and programmed backlog are not production gates.
- A CURRENT_PARENT must never wait for Jules when a scheduled task/controller can safely execute the material gap directly.

## Resolve current state before acting

1. Read fresh `Desarrollo` HEAD and pin repository reads to it.
2. Read `docs/VAEP_AUTHORITY.md` and use no historical prompt/rule as authority.
3. Read `vaep/control/jules-autorefill-catalog.json` only as a live roadmap/control snapshot; validate `currentParent`, `lastClosedParent`, receipts and dependencies against fresh evidence.
4. Read `vaep/control/dispatch-admission.json`; global admission is OPEN_ONLY. Jules admission never gates direct execution.
5. Read the Plan Maestro Sheet `19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY`: CONFIG/COLA/WORKERS plus task tables. CONTROL_TOWER and DASHBOARD are derived views, never independent writers.
6. Reconcile the active task-scope lease. Respect a fresh owner with material progress; stale/no-progress >=10 minutes permits takeover under the MAESTRO.
7. Revalidate HEAD immediately before any publication; if it changed, rebuild the delta from fresh inputs and preserve concurrent work.

## Direct execution first

For every active slot:

`fresh state -> lease -> shortest material gap -> tests -> REVIEW_FIRST -> integration -> causal gates -> LISTO_REAL -> promotion -> direct NEXT_SAFE prearm`

Only after that path is protected may the task decide to offload an independent scope to Jules. A Jules dispatch must never become a reason to wait, duplicate work or delay parent-close.

## Evidence and telemetry

`ACTIVE_REAL` is actor-aware:

- direct task: identifiable run + fresh exclusive lease + recent useful/material activity;
- Jules: correlated manifest/workflow/sessionId + recent useful technical activity.

An enabled automation, planner, manifest, workflow or lease without progress is not ACTIVE_REAL.

`LISTO_REAL` requires full MAESTRO evidence, REVIEW_FIRST, DoD, applicable causal tests/gates, P0/P1=0 and exact-head/equivalence evidence.

Execution, material-action, synchronization and supervision clocks are separate. Never convert a sync timestamp into proof of work.

## Historical compatibility

Previous Jules-first manifests, receipts, recoveries and commits remain historical evidence only. They do not re-enable mandatory Jules refill, queue depth, utilization targets or old prompt behavior. Late superseded Jules results remain evidence-only and cannot be integrated automatically.

Obsolete active copies should be disabled/neutralized, not used as alternate authorities. Git history is preserved and is never rewritten for this cutover.

Work only on `Desarrollo`. Preserve `main`, Production, secrets and PR #2 OPEN+DRAFT.