# VAEP HANDOFF CURRENT

Authority: `docs/VAEP_AUTHORITY.md` is the only operational master.
This file is a live-source navigation guide, never a cached runtime authority.

## Execution model

Current architecture is `TASKS_ONLY`.

- Ten scheduled automations are the complete autonomous executor/controller set.
- Primaries are builder/closers.
- Supervisors are verifier/recovery/secondary-builders.
- Direct execution is the only runtime path.
- No external worker pool, lane, manifest, dispatcher or offload mechanism belongs to current VAEP runtime.

## Resolve current state before acting

1. Read fresh `Desarrollo` HEAD and pin repository reads to it.
2. Read `docs/VAEP_AUTHORITY.md`; historical prompts/rules have no authority.
3. Read `vaep/control/dispatch-admission.json`; global admission remains OPEN_ONLY.
4. Read the Plan Maestro Sheet `19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY`: CONFIG/COLA plus task tables. CONTROL_TOWER and DASHBOARD are derived views, never independent writers.
5. Reconcile the active task-scope lease. Respect a physically live owner with material progress; a terminated/stale owner does not retain ownership.
6. Revalidate HEAD immediately before any publication; if it changed, rebuild the delta from fresh inputs and preserve concurrent work.

## Direct execution

For every active slot:

`fresh state -> lease -> shortest material gap -> tests -> REVIEW_FIRST -> causal gates -> LISTO_REAL -> promotion -> direct NEXT_SAFE prearm`

A checkpoint is a trigger, never a reason to abandon material work in progress. If the previous physical invocation ended before closure, the next eligible automation resumes the same parent from existing evidence rather than restarting diagnosis.

## Evidence and telemetry

`ACTIVE_REAL` requires identifiable direct execution + fresh exclusive lease + recent useful/material activity.

An enabled automation, planner, workflow or lease without progress is not ACTIVE_REAL.

`LISTO_REAL` requires full MAESTRO evidence, REVIEW_FIRST, DoD, applicable causal tests/gates, P0/P1=0 and exact-head/equivalence evidence.

Execution, material-action, synchronization and supervision clocks are separate. Never convert a sync timestamp into proof of work.

## Historical compatibility

Previous J1–J6/Jules manifests, receipts, sessions, recoveries and commits remain historical evidence only. They do not reactivate any worker, credential, workflow, queue, lane or dispatch path. Late historical results are evidence-only and cannot enter runtime automatically.

Work only on `Desarrollo`. Preserve `main`, Production, secrets and PR #2 OPEN+DRAFT.
