# N5.4.C — Persistence, migration and data decision

Authority: `docs/VAEP_AUTHORITY.md`.

## Controller takeover context

The original ATTEMPT1 transport for `N5.4.C.1.PERSISTENCE_MIGRATION_DECISION` remained in `Execute VAEP/Jules MASTER` beyond the MAESTRO `PARENT_STALL_NO_PROGRESS_MINUTES=10` threshold without a controller-visible correlated `sessionId`, terminal issue, result artifact, or later observable progress. Ownership was therefore transferred to `CHATGPT_VAEP`; any late Jules result is evidence-only and is not eligible for automatic integration.

This document is the bounded direct recovery for the same semantic facet. It does not create an R2 or a new functional identity.

## Decision

**NO_SCHEMA_CHANGE / NO_NEW_MIGRATION / NO_BACKFILL for N5.4.C.**

The source-backed N5.4 profitability contract that is currently safe to implement is already representable by persisted sales data. Adding new profitability columns, historical dimensions, allocation fields, or permission state in this DB/migration stage would encode semantics that N5.4.B explicitly left unresolved.

## Existing persistence that already satisfies the accepted absolute contract

The live `Venta` model persists the sale-level monetary fields required by the accepted absolute-profitability contract: `Subtotal`, `Descuento`, `CostoEnvio`, `Total`, `CostoTotal` and `UtilidadBruta`. `VentaDetalle` persists `Cantidad`, `PrecioUnitario`, `CostoUnitarioSnapshot`, `Subtotal` and `UtilidadBruta`, plus product/variant identity and product display snapshots.

The live EF mappings keep the monetary fields at `decimal(18,2)`. The original `Fase3Fase4VentasFacturacionFinanzas` migration already created `Ventas.CostoTotal`, `Ventas.UtilidadBruta`, `VentaDetalles.CostoUnitarioSnapshot`, `VentaDetalles.Subtotal` and `VentaDetalles.UtilidadBruta`; therefore N5.4.C does not need a new migration merely to store the accepted sale-level absolute cost/gross-profit values.

`ReporteVentasService` already performs read-only server-side projections from the persisted `Venta.CostoTotal` / `Venta.UtilidadBruta` fields for the report summary and from `VentaDetalle.CostoUnitarioSnapshot` / `Subtotal` / `UtilidadBruta` for detail rows. No persistence adapter or shadow copy is required for those existing values.

## Grouping/data identity consequences

N5.4.B established these source-backed identities and they require no new N5.4.C columns:

- seller: `Venta.CreadoPorUsuarioId` / `CreadoPorNombreUsuario`;
- customer: `Venta.ClienteId` / `ClienteNombre`;
- product: `VentaDetalle.ProductoId`, with variant independently available as `ProductoVarianteId`;
- category: the current `VentaDetalle.Producto.CategoriaId` relationship.

There is no persisted historical category snapshot on `VentaDetalle`. N5.4.B explicitly accepted current master-data category association as the current source-backed category dimension and prohibited inventing a historical snapshot in the domain-contract stage. N5.4.C therefore does **not** add or backfill a historical category field.

## Explicit non-persistence gates

The following unresolved semantics must **not** be persisted or migrated in N5.4.C:

1. **Margin-percentage denominator.** N5.4.B found no source-backed sales denominator. No `MargenPorcentaje` persisted column or precomputed percentage is introduced.
2. **Sale-header discount allocation to product/category.** The sale has a header `Descuento`, while `VentaDetalle` has no approved per-line share. N5.4.C does not add `DescuentoAsignado`, `UtilidadAjustada`, or equivalent fields and does not invent a proration/backfill algorithm.
3. **Financial-disclosure permission state.** N5.4.B preserved the existing sales row-scope contract and deferred any new financial-censorship rule to the security/RBAC stage. Permission state does not belong in this persistence migration.
4. **Shipping as profitability cost.** The accepted absolute contract keeps shipping out of persisted `CostoTotal` and `UtilidadBruta`; N5.4.C does not rewrite historical values or add a second contribution-margin field by inference.

## Constraints and indexes

No new N5.4.C constraint or index is justified by the accepted persistence contract alone. The current model already has the report-relevant sale indexes on `Ventas.Fecha` and `Ventas.CreadoPorUsuarioId`, and detail indexing/relationships for product/variant and warehouse dimensions. N5.4.C does not invent a speculative index for a downstream aggregation shape before N5.4.D establishes the exact application/query contract.

This is not a claim that every future profitability query is optimally indexed; it is a gate against speculative schema work. If N5.4.D introduces a source-backed query shape that demonstrably needs an additional index, that index must be justified causally from that exact query and returned through the ordered dependency chain rather than being guessed here.

## Migration / operational runbook disposition

Because the decision is `NO_SCHEMA_CHANGE`:

- migration generation: **N/A**;
- logical backup for this change: **N/A** — no DB mutation is authorized or required;
- preflight DDL check: **N/A** beyond verifying the already-live model/migration definitions in source;
- backfill: **N/A**;
- production migration execution: **PROHIBITED / NOT PERFORMED**;
- post-migration reconciliation: **N/A** because no migration is applied;
- rollback SQL/migration: **N/A** because no schema/data change is introduced.

Historical integrity is preserved by leaving the existing cost snapshots and already-persisted sale gross-profit values unchanged.

## Proportional verification

`TESTS_EXECUTED: NOT_APPLICABLE__DOCUMENTATION_ONLY_NO_CODE_OR_SCHEMA_CHANGE`

Controller source inspection covered:

- `backend/src/Domain/Entities/Venta.cs`;
- `backend/src/Domain/Entities/VentaDetalle.cs`;
- `backend/src/Infrastructure/Persistence/Configurations/VentaConfiguration.cs`;
- `backend/src/Infrastructure/Persistence/Configurations/VentaDetalleConfiguration.cs`;
- `backend/src/Infrastructure/Migrations/20260710050958_Fase3Fase4VentasFacturacionFinanzas.cs`;
- `backend/src/Infrastructure/Services/ReporteVentasService.cs`;
- all six REVIEW_FIRST-accepted N5.4.B contract documents.

No runtime/build gate is causally required for this direct recovery because it changes no C#, EF model, migration, SQL, frontend, or production state. The applicable gate is exact-source consistency of the N/A decision against the live branch.

`SELF_REVIEW_1: PASS` — no unresolved N5.4.B semantic was converted into persistence.

`SELF_REVIEW_2: PASS` — no schema, data, Production, main, secrets, or PR #2 merge action is introduced.

## REVIEW_FIRST handoff

Disposition requested: `REVIEW_FIRST` by `CHATGPT_VAEP` on the exact integrated documentation head. Parent closure remains prohibited until the controller receipt records causal review, applicable gates, DoD, and `P0=0 / P1=0`.
