# N2.7.H — Certification checkpoint (VAEP v3.21)

Parent: `N2.7 — Notas de crédito de proveedor`
Current child: `N2.7.H — Documentación y certificación`
Branch authority: `Desarrollo` only.

## Upstream closure evidence

`N2.7.G` is eligible to close from causal HEAD `b84479183c0cf8330f09b32dd5539a336484a686`.

- Development run `32573152091`: SUCCESS.
- Acceptance run `32573152121`: SUCCESS.
- Fase8 run `32573152063`: SUCCESS.
- M13 run `32573152079`: SUCCESS.
- Known blocking P0/P1 after the causal Moq fix: 0.

The causal fix was `fix(N2.7.G): corregir verify Moq con argumento opcional [VAEP]`.

## N2.7.H certification work still required

This checkpoint does **not** mark N2.7.H or N2.7 parent LISTO by itself. Before closure, VAEP must verify and reconcile, where applicable:

1. Canonical functional/technical documentation reflects the implemented NotaCreditoProveedor domain, persistence, Application/API, frontend, RBAC/audit and QA behavior without inventing contracts.
2. Rollback/runbook evidence is aligned with the migrations and does not claim unproven recovery guarantees.
3. OpenAPI/API documentation, ADR/ERD references and operational notes are updated only where the repository actually uses those artifacts.
4. `TASKS.md`, `CHANGELOG_AI.md` and collaboration/control records are reconciled if required by the repository's current documentation convention.
5. Final regression evidence remains causal to the final H HEAD: Development + Acceptance + Fase8 + M13 terminal SUCCESS, with P0/P1 = 0.
6. No historical Jules artifact may reopen already certified N2.7.B–G absent a new causal regression.

## Governance

- PARENT_CLOSE_FIRST = TRUE.
- CURRENT_PARENT_SWARM = MANDATORY.
- ATTEMPT1 + at most one targeted R2 per logical Jules task; R3+ PROHIBITED.
- A failed R2 transfers the logical task to ChatGPT/VAEP QA takeover.
- A Jules result is not PASS without two distinct later activities containing literal `SELF_REVIEW_PASS_1=PASS` and `SELF_REVIEW_PASS_2=PASS`.
- Jules artifacts are evidence/patch inputs only; ChatGPT/VAEP owns reconciliation and publication.
- No main/Producción/new branches/merge/force-push/secrets/deploy.
