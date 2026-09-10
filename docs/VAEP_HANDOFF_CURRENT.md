# VAEP HANDOFF CURRENT

Authority: `docs/VAEP_AUTHORITY.md` is the only operational master.
This file is a live-source navigation guide, not a cached runtime snapshot.

## Resolve current state before acting

1. Read fresh `Desarrollo` HEAD and pin all repository reads to that SHA.
2. Read `vaep/control/jules-autorefill-catalog.json`: `currentParent`,
   `lastClosedParent`, `closureReceipts`, and the source-backed roadmap.
3. Validate the referenced closure receipt and its causal run/head evidence.
4. Read `vaep/control/dispatch-admission.json` and current manifests, result
   evidence and ownership. An old admission reason is not a parent selector.
5. Read the original [Plan Maestro Sheet](https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit).
   Reconcile CONFIG/COLA/WORKERS under the master's state synchronization contract.
   CONTROL_TOWER and DASHBOARD are derived views, never independent writers.
6. Revalidate HEAD before publishing. If it changed, rebuild from fresh inputs.

## Evidence and telemetry

`CURRENT_HEAD` means the source SHA of the last synchronized observation, not a
promise that Git has stopped advancing. Read `LAST_SYNC` and `SYNC_STATUS`.
Execution time, synchronization time and supervision time are separate facts.
An enabled task or successful planner is not evidence of useful worker activity.
ACTIVE_REAL needs a correlated session and fresh useful technical activity.
LISTO_REAL needs the master's complete closure evidence.

No parent, run, lane ownership, next action or rolling count is embedded here:
those values expire independently and must be read from their live sources.
Previous snapshots remain in Git history only. Late superseded results remain
evidence-only; do not redispatch, reopen or integrate them from this handoff.

Work only on Desarrollo. Preserve main, Production, secrets and PR #2 OPEN+DRAFT.
