# VAEP HANDOFF CURRENT

Authority: `docs/VAEP_AUTHORITY.md` is the only operational master.

This file is only a navigation aid for the live SOLQARYN state.

## Current execution model

- Repository: `solqaryn/Solqaryn`.
- Branch: `dev`.
- Runtime: `TASKS_ONLY`.
- Ten scheduled automations are the complete autonomous executor/controller set.
- Primaries are builder/closers.
- Supervisors are verifier/recovery/secondary-builders.
- Direct execution is the runtime path.

## Resolve live state before acting

1. Read `docs/VAEP_AUTHORITY.md`.
2. Read fresh `dev` HEAD and pin repository reads to it.
3. Read only the current plan-master surfaces required for the task.
4. Reconcile the active task-scope lease.
5. Validate current dependencies, tests, security and data invariants.
6. Revalidate HEAD immediately before publication.

No plan, phase, row, gate, sequence or protocol outside the current MAESTRO is allowed to constrain or reprioritize work.

## Direct execution

`fresh state -> lease -> material work -> tests -> REVIEW_FIRST -> causal gates -> LISTO -> promotion`

A checkpoint is a trigger, not a reason to abandon material work in progress.

## Evidence

`ACTIVE_REAL` requires identifiable execution + fresh exclusive lease + recent useful material activity.

`LISTO` requires the MAESTRO evidence, REVIEW_FIRST, applicable tests/gates, P0/P1=0 and exact-head/equivalence evidence.

## Safety

Work ordinarily on `dev`. Any change to `main`, PROD, productive data, secrets, domains, certificates or productive infrastructure requires explicit current owner authorization.
