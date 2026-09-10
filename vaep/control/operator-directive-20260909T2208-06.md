# VAEP OPERATOR DIRECTIVE — throughput recovery

Timestamp: 2026-09-09 22:08 -06
Authority: docs/VAEP_AUTHORITY.md
Branch: Desarrollo

## Objective
Restore sustained certified closure throughput toward ROLLING60 >= 3 without filler, false ACTIVE/PASS/LISTO, duplicate write ownership, R3, or unsafe branch actions.

## Mandatory recovery policy
1. Never declare or act on TRANSPORT_STALLED when a correlated Jules session exists and has fresh useful activity within the configured stall window.
2. A controller takeover is allowed only when the current owner is terminal-invalid, explicitly superseded, or the correlated session lacks useful progress for >=10 minutes after session creation / last useful activity and ownership is atomically transferred.
3. After a Jules terminal COMPLETED + PATCH_PRESENT + terminal contract valid, run REVIEW_FIRST immediately; if accepted, run proportional exact-head gates and certify the facet in the same control cycle.
4. If the current parent has no additional dependency-valid material scopes, materialize roadmap-derived future scopes for N+1/N+2 as dependency-gated READY candidates so workers can be prearmed, but never execute a scope before its dependencies are satisfied.
5. When a parent becomes certifiable, perform closure receipt -> LISTO_REAL -> promote next parent -> release prearmed dependency-valid scopes -> dispatch/refill in the same run.
6. Keep one-writer-per-taskId and one-manifest-one-run. Late results from superseded ownership are evidence-only.
7. The throughput target is a control objective, not permission to fabricate closures. When the roadmap has insufficient independent material work to mathematically support 3 parent closures/hour, record the exact causal capacity constraint and maximize legitimate closure speed instead of manufacturing work.

## Current recovery context
- N5.3.E is LISTO_REAL.
- N5.3.F is CURRENT_PARENT.
- F1 is REVIEW_FIRST accepted after controller recovery.
- F2/J2 has a real Jules session with useful activity and a completed terminal contract; a later controller takeover also modified the same test scope, so REVIEW_FIRST must reconcile ownership before certification.
- N5.3.G/H should be prearmed dependency-gated where roadmap-derived material exists.
