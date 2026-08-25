# TASKS — VariApp

Registro operativo resumido de cierres ERP certificados. La autoridad de ejecución detallada permanece en COLA/CONFIG y el Plan Maestro.

## Gobierno VAEP

- [x] Autoridad documental unificada en Jules v3.25 con cierre por padre y checkpoints `:00/:15/:30/:45/:55`.
- v3.20/v3.21 quedan como historia; se conservan ATTEMPT1+R2, R3 prohibido, QA takeover y gates de evidencia.
- [x] Worker compartido y cuatro workflows Jules alineados estáticamente con v3.25, conservando la ruta `vaep-jules-worker-v320.sh` por compatibilidad.
- [x] Prueba controlada disponible: `bash .github/scripts/vaep-jules-worker-v320.sh --static-self-test`; valida guardrails sin red, secretos, sesión ni attempt.
- Pendiente de prueba de integración real: requiere un manifest de dispatch y crea sesión/consume attempt; no ejecutar sin una tarea autorizada.

## ERP-N2.2 — Orden de compra

- [x] N2.2.A-H completados y certificados por evidencia autoritativa VAEP.

## ERP-N2.7 — Nota de crédito de proveedor

- [x] N2.7.A-H completados — paquete documental canónico `c466ec3099c2a498c2353af82b99ce0be9d46e29`. Baseline funcional certificado `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`; Development, Acceptance, Fase 8 y M13 en SUCCESS. Sin defectos bloqueantes P0/P1 conocidos.

**ERP-N2.7 queda formalmente cerrado.**

## ERP-N2.8 — Cuentas por pagar

- [x] N2.8.A-G completados y certificados en COLA.
- [x] N2.8.H paquete documental canónico materializado: ERP/ADR/contrato HTTP/runbook/rollback/certificación.
- [x] N2.8.H certificado sobre `81de833f5104d98f0aad02cf32c714640b6cea2b`: Development `#32605604928`, Acceptance `#32605604908`, Fase8 `#32605604911` y M13 `#32605604886` en SUCCESS; P0/P1 bloqueantes conocidos=0.

**ERP-N2.8 queda formalmente cerrado.**

## ERP-N2.9 — Evaluación de proveedores

- [x] N2.9.A-G completados y certificados en COLA.
- [x] Persistencia N2.9.C: migración/snapshot/preflight/postcheck/DownGuard certificados sobre `69419edf2ccb62b7d5849d242ca723f6d64b9ee5`.
- [x] Application/API N2.9.D certificado sobre `ca03082ff6bdbb587a58ee65052dd3b70df47957`.
- [x] Frontend/UX N2.9.E certificado sobre `1d7c10a9ee0132032716144ad726c3261522868f`.
- [x] QA/regresión/CI N2.9.G certificado sobre `19db085b630814b814f8c877010cc83f665b27a3`.
- [x] N2.9.H cierre documental canónico y `CHANGELOG_AI.md` reconciliados; HEAD `8b6e7e7df4b01b8f7226d7a9631506d0540f4fa5` certificado con Development `#32635282523`, Acceptance `#32635282571`, Fase8 `#32635282564` y M13 `#32635282546` en SUCCESS; P0/P1 bloqueantes conocidos=0.

**ERP-N2.9 queda formalmente cerrado. Parent40=22/40; GAP=18. El siguiente gate obligatorio es `GATE-N2`; ERP-N3 no se promueve antes de `GATE-N2=LISTO`.**

Documentación: `docs/ERP_N2_9_EVALUACION_PROVEEDORES.md`, `docs/RUNBOOK_N2_9_EVALUACION_PROVEEDORES.md`, `docs/CERTIFICACION_N2_9_EVALUACION_PROVEEDORES.md` y matriz QA `docs/qa/N2_9_H_CLOSURE_MATRIX_A1.md`.

## ERP-N3.1 — Cotizaciones

- [x] N3.1.A-G completados y certificados en COLA; baseline funcional inmediato `d4d296e229d266a1442de3bc4e07b03bfab35a9f`.
- [x] N3.1.H certificado con Development `#32687639976`, Acceptance `#32687639981`, Fase 8 `#32687640010`, M13 `#32687640016` y Recovery MySQL `#32687640017` en SUCCESS; P0/P1 bloqueantes conocidos=0.
- [x] Certificación canónica: `docs/CERTIFICACION_N3_1_COTIZACIONES.md`.

**ERP-N3.1 queda formalmente cerrado. Parent40=30/40; GAP=10. Siguiente MICROTAREA dependency-valid: `N3.2.A — Pedidos de venta / Auditoría y preflight`.**

## ERP-N3.2 — Pedidos de venta

- [x] N3.2.A-H completados y certificados en COLA.
- [x] PedidoVenta preserva lifecycle, idempotencia durable y persistencia propia sin adelantar la reserva automática de inventario.
- [x] Certificación canónica: `docs/CERTIFICACION_N3_2_PEDIDOS.md`.

**ERP-N3.2 queda formalmente cerrado. El siguiente foco dependency-valid es `N3.3.A — Reserva automática de inventario / Auditoría y preflight`.**

## ERP-N3.3 — Reserva automática de inventario

- [x] N3.3.A-G completados y certificados en COLA.
- [x] La confirmación de PedidoVenta reutiliza `ReservaInventario` y la autoridad física `ExistenciaVariante`; no crea una segunda autoridad cuantitativa ni inventa selección automática de almacén/ubicación.
- [x] N3.3.H documentación/certificación publicada mediante paquete atómico VAEP v3.25.
- [x] Certificación: `docs/CERTIFICACION_N3_3_RESERVA_AUTOMATICA.md`.
- [x] Runbook: `docs/RUNBOOK_N3_3_RESERVA_AUTOMATICA.md`.
- [x] ADR de autoridad reutilizado: `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`.

**ERP-N3.3 queda formalmente cerrado. Siguiente MICROTAREA dependency-valid: `N3.4.A — Remisiones/entregas / Auditoría y preflight`.**

## ERP-N3.4 — Preparación y despacho

- [x] N3.4.A-G completados y certificados en COLA.
- [x] Persistencia/migración N3.4.C certificada sobre `cb476879203ffb3da40fb7a670c74935c794081d` con M13 `#32803906340` SUCCESS.
- [x] Application/API N3.4.D certificada sobre `1fab396541d8ecf33e605703789809ebc1a997ef` con M13 `#32807131468` SUCCESS.
- [x] Frontend/UX N3.4.E certificado sobre `a167434880eab07c3b08ca651ae9309da964c23b` con M13 `#32809392404` SUCCESS.
- [x] N3.4.F RBAC/auditoría/seguridad/observabilidad y N3.4.G QA/regresión/CI cerrados sobre el mismo HEAD funcional, con P0/P1 atribuibles conocidos=0.
- [x] Certificación canónica: `docs/CERTIFICACION_N3_4_PREPARACION_DESPACHO.md`.
- [x] Runbook: `docs/RUNBOOK_N3_4_PREPARACION_DESPACHO.md`.

**ERP-N3.4 queda formalmente cerrado. Siguiente MICROTAREA dependency-valid: `N3.5.A — Venta/factura — Auditoría y preflight`.**

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit
