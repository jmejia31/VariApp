# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-08-25 — ERP-N3.5 Venta/factura — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25 Closure Governor.

**Objetivo/alcance:** registrar el cierre formal del bloque N3.5 (Venta y Factura), confirmando que dichas entidades conservan su autoridad existente y que `PedidoVenta` (N3.2) permanece estrictamente desacoplado, sin introducir una conversión directa (`PedidoVenta` ↔ `Venta`), FKs cross-document, idempotencia, ni orquestación nueva.

**Evidencia:** las microtareas fueron concluidas y validadas según su dominio:
- N3.5.A #516 `LISTO`
- N3.5.B #517 `LISTO` N/A domain grounded
- N3.5.C #518 `LISTO` N/A persistence grounded
- N3.5.D #519 `LISTO` N/A Application/API grounded
- N3.5.E #520 `LISTO` N/A frontend grounded
- N3.5.F #521 `LISTO` N/A security/audit grounded
- N3.5.G #522 `LISTO` N/A QA/CI grounded

**Certificación funcional:** el control reporta la certificación `56a422f0bf0e882fa6c9d800061154031f701091`, TASKS `a298bf537c98da8a9f1e31f4a2d8f8e6cc50e572`, con baseline funcional en `a167434880eab07c3b08ca651ae9309da964c23b` tras M13 #32809392404 en `SUCCESS`. P0/P1 atribuibles conocidos a la fecha: 0.

## 2026-08-24 — Codex — ejecutor Jules v3.25

- Se alineó `.github/scripts/vaep-jules-worker-v320.sh` con semántica v3.25 conservando el nombre por compatibilidad con cuatro workflows.
- Los lanes Jules A/B/C/D ahora identifican v3.25; se preservaron v4.6, ATTEMPT1+R2 máximo, R3 prohibido, doble self-review y artefactos/Issues.
- Se añadió `docs/VAEP_JULES_V325_TRANSPORT.md` como contrato documental de transporte y ejecución.
- No se modificó main ni Producción; cambios exclusivos en Desarrollo.

## 2026-08-24 — ERP-N3.4 Preparación y despacho — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25 Closure Governor.

**Objetivo/alcance:** registrar el cierre formal de N3.4 Preparación y despacho. El agregado `PreparacionPedidoVenta` conserva autoridad logística y no desplaza a `PedidoVenta`, `ReservaInventario` ni inventario/Kardex.

**Evidencia:**
- N3.4.A `LISTO`
- N3.4.B `LISTO`
- N3.4.C `LISTO`
- N3.4.D `LISTO`
- N3.4.E `LISTO`
- N3.4.F `LISTO`
- N3.4.G `LISTO`
- N3.4.H `LISTO`

**Certificación:** baseline funcional `a167434880eab07c3b08ca651ae9309da964c23b`; M13 #32809392404 `SUCCESS`; P0/P1 atribuibles conocidos: 0.

## 2026-08-24 — ERP-N3.3 Reserva automática — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.22.

**Objetivo/alcance:** cerrar formalmente N3.3 Reserva automática preservando la autoridad de `PedidoVenta`, `ReservaInventario`, stock reservado y el ADR vigente de overselling.

**Evidencia:** N3.3.A-H cerradas y certificadas; baseline funcional `f406939e16172c3a170123fb391c81c1696af1f1`; QA/CI cerrados sin P0/P1 bloqueantes atribuibles.

**Documentación/control:** `docs/CERTIFICACION_N3_3_RESERVA_AUTOMATICA.md`, `docs/RUNBOOK_N3_3_RESERVA_AUTOMATICA.md` y el ADR vigente `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`. `TASKS.md` se reconcilia en el mismo commit atómico. Siguiente parent dependency-valid: `N3.4.A — Remisiones/entregas / Auditoría y preflight`.

## 2026-08-26 — ERP-N3.6 Devoluciones de clientes — CIERRE FORMAL

N3.6.A-H formally closed only as the target content being prepared for controller integration.

Approved closure facts:
- baseline functional 6c5a3164ab11a1dcdcdfa9418c61bb0165251239
- Development #32913855654 SUCCESS
- Acceptance #32913854936 SUCCESS
- Fase8 #32913854958 SUCCESS
- M13 #32913854923 SUCCESS
- certification 4fe25e8cf656f82e3883f0585fa29358769aa48c
- runbook d906393fc26b0073ac782721ea08cb0fa35827b5
- TASKS rollup 6efbb72880a15bd6cf7f2d5d6bbb3d1b0d0118d7
- P0/P1 known attributable to N3.6 = 0
- next parent after H is N3.7.A, promotion blocked until H LISTO.
