# TASKS — VariApp

Registro operativo resumido de cierres ERP certificados. La autoridad de ejecución detallada permanece en COLA/CONFIG y el Plan Maestro.

## Gobierno VAEP

- [x] Autoridad operativa unificada en `docs/VAEP_AUTHORITY.md` como **MAESTRO único**.
- [x] Las reglas se editan en la misma ruta; no se crean protocolos/documentos/scripts operativos numerados.
- [x] Jules A/B/C/D y las cinco automatizaciones consumen el mismo MAESTRO.
- [x] Históricos en Git/CHANGELOG/BITACORA son evidencia únicamente y no gobiernan ejecución.
- [x] Retry, QA takeover, parent-close, ACTIVE_REAL, throughput y gates se definen exclusivamente en el MAESTRO.

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
- [x] N3.3.H documentación/certificación publicada mediante paquete atómico VAEP bajo el MAESTRO operativo.
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

## ERP-N3.5 — Venta/factura

- [x] N3.5.A-G completados y certificados en COLA mediante preflight y N/A grounded donde el desacople ya estaba implementado.
- [x] `Venta` conserva la autoridad operativa/financiera; `Factura` permanece ligada a `VentaId`; `PedidoVenta` conserva lifecycle independiente sin duplicar stock, Kardex, facturación ni finanzas.
- [x] Baseline funcional reutilizado: `a167434880eab07c3b08ca651ae9309da964c23b`, M13 `#32809392404` SUCCESS; delta funcional de N3.5.B-G=0 y P0/P1 atribuibles conocidos=0.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N3_5_VENTA_FACTURA.md`.
- [x] N3.5.H cierre documental publicado byte-perfect en `4296e72b8b5a87ef4e779e3ec6f8af083e396374`; `CHANGELOG_AI.md` quedó exactamente `+17/-0` y blob `9e0f74a66e0064543aa1a92d9ffc02c15a6d3862`.

**ERP-N3.5 queda formalmente cerrado. El siguiente foco dependency-valid es `N3.6.A — Devoluciones de cliente / Auditoría y preflight`.**

## ERP-N3.6 — Devoluciones de cliente

- [x] N3.6.A auditoría/preflight reconciliado después del cierre real de N3.5.H.
- [x] N3.6.B dominio/contratos completado y certificado.
- [x] N3.6.C persistencia/migración/datos completada y certificada.
- [x] N3.6.D Application/API completado y certificado.
- [x] N3.6.E frontend/UX completado y certificado.
- [x] N3.6.F RBAC/auditoría/seguridad/observabilidad completado y certificado.
- [x] N3.6.G QA/regresión/CI completado; baseline funcional `6c5a3164ab11a1dcdcdfa9418c61bb0165251239`, con Development `#32913855654`, Acceptance `#32913854936`, Fase 8 `#32913854958` y M13 `#32913854923` en SUCCESS; P0/P1 funcionales conocidos=0.
- [x] N3.6.H certificación canónica publicada en `4fe25e8cf656f82e3883f0585fa29358769aa48c` y runbook en `d906393fc26b0073ac782721ea08cb0fa35827b5`.
- [ ] Cierre formal H pendiente únicamente de reconciliar `CHANGELOG_AI.md` de forma aditiva/history-preserving y obtener gate causal del rollup final; no false LISTO.

**N3.6 permanece abierto exclusivamente por el rollup final de CHANGELOG. Siguiente parent dependency-valid tras H=LISTO: `N3.7.A — Nota de crédito / Auditoría y preflight`.**

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

## ERP-N3.7 — Nota de crédito de cliente — ROLLUP SUPERSEDING 2026-08-26

Este bloque es aditivo y supersede únicamente el estado operativo stale de N3.6/N3.7 registrado arriba; no elimina ni reescribe la historia previa.

- [x] N3.6.H quedó cerrado realmente antes de iniciar N3.7; el `CHANGELOG_AI.md` canónico preserva ese cierre en blob `d53c56416ac7ac01beef761adab5172cf5297487`.
- [x] N3.7.A auditoría/preflight — `LISTO_REAL`, Issue #752, P0=0/P1=0.
- [x] N3.7.B dominio/contratos — `LISTO_REAL` en `46a250fcc0cfd1562306538375e772a94c39bea5`; Development #32972568129, Acceptance #32972568251, Fase 8 #32972568127 y M13 #32972568118 en SUCCESS.
- [x] N3.7.C persistencia/migración/datos — `LISTO_REAL` en `9810cf2e7fd0289a9374a8477a4131f3f73fef38`; Acceptance #32983744613, M13 #32983745546 y Recovery MySQL #32983743533 SUCCESS; migración/snapshot/tests certificados.
- [x] N3.7.D Application/API — `LISTO_REAL` en `8bcacae8a45fe3c0072bf519610bcc1ec1203a4f`; Development #32988607673, Acceptance #32988607652, Fase 8 #32988607675 y M13 #32988607632 SUCCESS.
- [x] N3.7.E Frontend/UX — `LISTO_REAL` en `f9ef582749a79c8900741d1a40ff393039c7b287`; M10 #32998936899 SUCCESS; Issue #770 cerrado.
- [x] N3.7.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL` en `943aa0e607af3221ed8987a0edac37a539561696`; M10 #33001097160 SUCCESS; Issue #776 cerrado.
- [x] N3.7.G QA/regresión/CI — `LISTO_REAL` por rollup de regresión; Issue #781 cerrado y P0/P1 atribuibles=0.
- [ ] N3.7.H documentación/certificación — cierre canónico en curso: este TASKS rollup + entrada aditiva en `CHANGELOG_AI.md`; solo después del hard verify documental y P0=0/P1=0 pasa a `LISTO`.

**Promoción de N3.8 permanece bloqueada hasta N3.7.H=LISTO.**
## ERP-N3.8 — Nota de débito de cliente — CIERRE CONDICIONAL/N/A

- [x] N3.8.A preflight certificado en `034ec3305422016d6c571d0ffcf1332e3bbbe6b6`.
- [x] N3.8.B dominio/contratos cerrado N/A con evidencia en `affb58f2b9e7d8ab25c051fed5b9f4ee5f317584`.
- [x] N3.8.C-G cerrados N/A con evidencia en `3a89725e4a76c4d85c0c4adc04f0affa4a61e79a`.
- [x] N3.8.H certificación canónica: `docs/CERTIFICACION_N3_8_NOTA_DEBITO_CLIENTE.md`.

**ERP-N3.8 queda formalmente cerrado como N/A condicionado al requisito legal/operativo. No se implementó `NotaDebitoCliente`. Siguiente MICROTAREA dependency-valid: `N3.9.A — Cuentas por cobrar / Auditoría y preflight`.**

## ERP-N3.9 — Cuentas por cobrar — ROLLUP 2026-08-26

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N3.9.A auditoría/preflight — `LISTO_REAL`.
- [x] N3.9.B dominio/contratos — `LISTO_REAL`; `Factura` + `FacturaPago` permanecen autoridad y CxC es read-model, no segundo ledger.
- [x] N3.9.C persistencia/migración/datos — `LISTO_REAL / N_A_CERTIFIED`; no se creó tabla, migración ni backfill CxC.
- [x] N3.9.D Application/API — `LISTO_REAL` sobre `59e0c41362fe3a63765d9218a9817272ce6a7602`; GET-only `/cuentas-por-cobrar`, `[Authorize]` + `Facturacion/Ver`; Development #33019598078, Acceptance #33019598096, Fase8 #33019598185 y M13 #33019598074 SUCCESS.
- [x] N3.9.E Frontend/UX — `LISTO_REAL` sobre `9b0db22c26bce42f42f97ba1e0c6124c54d86af9`; Issue #843 cerrado.
- [x] N3.9.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL / QA_TAKEOVER_CERTIFIED` sobre `0d621920f8ebd0a7bb3f1b3af30ffbadd0f91f9c`; Issue #851 cerrado, P0/P1=0.
- [x] N3.9.G QA/regresión/CI — `LISTO_REAL / QA_REGRESSION_CERTIFIED`; Issue #858 cerrado, P0/P1=0.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N3_9_CUENTAS_POR_COBRAR.md`.
- [ ] N3.9.H documentación/certificación permanece `EN_PROGRESO` hasta reconciliar `CHANGELOG_AI.md` de forma aditiva/history-preserving, verificar checks aplicables y P0/P1=0.

**Promoción de N3.10 permanece bloqueada hasta N3.9.H=LISTO.**

## ERP-N3.10 — Crédito de cliente — ROLLUP SUPERSEDING 2026-08-27

Este bloque es aditivo y supersede únicamente el estado operativo stale de N3.9/N3.10 registrado arriba; no elimina ni reescribe historia previa.

- [x] N3.9.H quedó cerrado formalmente antes de N3.10; `CHANGELOG_AI.md` contiene el cierre canónico history-preserving.
- [x] N3.10.A auditoría/preflight — `LISTO_REAL`.
- [x] N3.10.B dominio/contratos — `LISTO_REAL`; la capacidad de crédito permanece integrada a Cliente y no introduce un motor autónomo de scoring ni una segunda autoridad comercial.
- [x] N3.10.C persistencia/migración/datos — `LISTO_REAL` en `619a0ba2a53ad70fb332c9f61198eb3b022ddcc1`; Development #33068581067, Acceptance #33068581028, Fase 8 #33068581188 y M13 #33068581299 SUCCESS.
- [x] N3.10.D Application/API — `LISTO_REAL` en `3c5a2c30a3d8427d0d0764ef1d4bc4e895d4d585`; Development #33073610169, Acceptance #33073610154, Fase 8 #33073610151 y M13 #33073610159 SUCCESS.
- [x] N3.10.E Frontend/UX — `LISTO_REAL` en `615d1a4878854bf22770b945256db39fea44e08f`; M10 #33083576709 SUCCESS.
- [x] N3.10.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL` en `98b7777555cd6f7ee881edb76321cd1226ca69eb`; Development #33086814120, Acceptance #33086814176, Fase 8 #33086814189, M13 #33086814163 y M10 #33086818401 SUCCESS.
- [x] N3.10.G QA/regresión/CI — `LISTO_REAL`, reutilizando la misma autoridad exact-head `98b7777555cd6f7ee881edb76321cd1226ca69eb` sin fabricar evidencia duplicada.
- [x] N3.10.H documentación/certificación — certificación canónica `docs/CERTIFICACION_N3_10_CREDITO_CLIENTE.md`; rollup aditivo TASKS+CHANGELOG hard-verificado; P0/P1 atribuibles conocidos=0.

**ERP-N3.10 queda formalmente cerrado. Siguiente parent dependency-valid: `N3.11.A`; su promoción solo ocurre después de este cierre real.**

## ERP-N3.11 — Punto de Venta (POS) — ROLLUP SUPERSEDING 2026-08-27

Este bloque es aditivo y supersede únicamente el estado operativo stale de N3.10/N3.11 registrado arriba; no elimina ni reescribe historia previa.

- [x] N3.10.H quedó cerrado formalmente antes de iniciar N3.11; `CHANGELOG_AI.md` contiene el cierre canónico.
- [x] N3.11.A auditoría/preflight — `LISTO_REAL`.
- [x] N3.11.B dominio/contratos — `LISTO_REAL`; `Venta` se reutiliza como autoridad central transaccional y financiera. Capacidades no soportadas (cashier/session/terminal, split-tender/change, suspension/reprint, offline, POS-specific idempotency, POS-specific RBAC) permanecen `DECISION_PENDING`.
- [x] N3.11.C persistencia/migración/datos — `LISTO_REAL`; esquema reutilizado de Ventas, sin tablas ni migraciones específicas de POS.
- [x] N3.11.D Application/API — `LISTO_REAL`.
- [x] N3.11.E Frontend/UX — `LISTO_REAL`.
- [x] N3.11.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL`; se aplican permisos generales de Venta (Ventas/Crear).
- [x] N3.11.G QA/regresión/CI — `LISTO_REAL`; P0/P1 atribuibles conocidos=0.
- [ ] N3.11.H documentación/certificación — cierre canónico en curso: este TASKS rollup aditivo hard-verificado; `LISTO` es `TARGET_AFTER_PUBLICATION`.

**ERP-N3.11 queda formalmente cerrado (TARGET_AFTER_PUBLICATION).**

## ERP-N4.3 — Conciliación bancaria — ROLLUP 2026-09-03

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.3.A-G — DoD técnico satisfecho sobre baseline funcional certificado `ad0cf70fc6ced126de1878b61fe4ae02c8d41a01`.
- [x] N4.3.F — RBAC/auditoría/seguridad/observabilidad reconciliado por QA takeover, sin R3.
- [x] N4.3.H — certificación canónica publicada en `docs/CERTIFICACION_N4_3_CONCILIACION_BANCARIA.md` mediante `3d7b8c776b813c359e373780f1a1039c1baed8b1`.
- [x] Matriz exact-head de `3d7b8c776b813c359e373780f1a1039c1baed8b1`: gates aplicables terminales `SUCCESS`; `VariApp CI=SKIPPED` excluido como PASS.
- [x] P0 abiertos=0; P1 abiertos=0.
- [x] `TASKS.md` reconciliado de forma aditiva/history-preserving por este rollup.
- [ ] `CHANGELOG_AI.md` pendiente de reconciliación aditiva/history-preserving y nueva certificación exact-head; no false `LISTO_REAL`.

**CURRENT_PARENT=N4.3.H. CLOSABLE_NOW=NO hasta reconciliar `CHANGELOG_AI.md` y obtener gates exact-head terminales sobre el HEAD resultante. La promoción del siguiente parent permanece bloqueada.**

## ERP-N4.4 — Cuentas por cobrar — ROLLUP 2026-09-03

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.4.A — `LISTO_REAL / PREFLIGHT_CERTIFIED`; autoridad CxC preservada en `Factura + FacturaPago` ligada a `Venta`.
- [x] N4.4.B — `LISTO_REAL / DOMAIN_NA_CERTIFIED`; no se creó dominio ni contrato CxC duplicado.
- [x] N4.4.C — `LISTO_REAL / DB_MIG_NA_CERTIFIED`; sin schema delta, migración ni backfill CxC.
- [x] N4.4.D — `LISTO_REAL / COVERED_EXISTING_CONTRACT`; Application/API cubiertos por contratos existentes.
- [x] N4.4.E — `LISTO_REAL / FRONTEND_CXC` sobre `61c8445ff948912a1a3e7a2792106849064e51c7`.
- [x] N4.4.F — `LISTO_REAL / SEC_AUDIT_CERTIFIED` sobre `2487badc4759db4ca87d60f823c6fffd9899f0d2`.
- [x] N4.4.G — `LISTO_REAL / TEST_CI_CERTIFIED` sobre `a85396b8e5d6ed579f2815cb7a193f45ed3d54e0`; run `33772443239` SUCCESS; P0/P1=0.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N4_4_CUENTAS_POR_COBRAR.md`.
- [x] Runbook canónico publicado: `docs/RUNBOOK_N4_4_CUENTAS_POR_COBRAR.md`.
- [x] `TASKS.md` reconciliado aditivamente por este rollup.
- [ ] `CHANGELOG_AI.md` y checks exact-head documentales pendientes antes de `N4.4.H=LISTO_REAL`; no false LISTO.

**CURRENT_PARENT=N4.4.H. CLOSABLE_NOW=NO hasta reconciliar `CHANGELOG_AI.md`, obtener gates exact-head terminales y revalidar P0=0/P1=0. `N4.5.A` permanece promotion-held aunque se permite prework seguro.**

## ERP-N4.5 — Cuentas por pagar — ROLLUP 2026-09-03

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.5.A-G — `LISTO_REAL`: alcance CxP reconciliado mediante reutilización de la autoridad ERP-N2.8, sin duplicar dominio, subledger, persistencia, API, UI, RBAC, auditoría ni pruebas.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N4_5_CUENTAS_POR_PAGAR.md`.
- [x] Baseline funcional certificado: `541ec12b72912c769c6f54b8821771e509818375`; el HEAD posterior contiene únicamente documentación y manifests de coordinación VAEP/Jules.
- [x] P0 abiertos=0; P1 abiertos=0 para el alcance A-G según la evidencia canónica revisada.
- [ ] N4.5.H documentación/certificación permanece `EN_PROGRESO` hasta reconciliar este rollup con `CHANGELOG_AI.md`, obtener gates exact-head terminales y revalidar P0=0/P1=0.

**CURRENT_PARENT=N4.5.H. CLOSABLE_NOW=NO hasta completar la reconciliación documental y los gates exact-head. `N4.6.A` permanece `PREARMED/PROMOTION_HELD`; no se promueve antes del cierre real de N4.5.H.**

## ERP-N4.6 — Plan de cuentas jerárquico — ROLLUP DE CIERRE 2026-09-04

Este rollup es append-only y supersede únicamente el estado operativo stale anterior; conserva íntegramente la historia previa.

- [x] N4.6.A — `LISTO_REAL`; preflight y dependencia de N4.6.B confirmados.
- [x] N4.6.B — `LISTO_REAL`; dominio `CuentaContable` y `TipoCuentaContable` en `2658d5b0139e85957463cb227f11ea65f42bef13`.
- [x] N4.6.C — `LISTO_REAL`; persistencia EF/migración/snapshot certificados en `12542f37132dfd4488e27e197ef548af19dee337`.
- [x] N4.6.D — `LISTO_REAL`; repositorio, servicio, DTOs, API jerárquica y validación de ciclos materializados en `a4392c44` y `9d649bbb`.
- [x] N4.6.E — `LISTO_REAL`; UI `/plan-cuentas`, árbol, formularios, permisos y estados de interfaz en `9d649bbb`.
- [x] N4.6.F — `LISTO_REAL`; autorización, RBAC, auditoría y seguridad verificadas por contratos y gates del exact-head.
- [x] N4.6.G — `LISTO_REAL`; Development `#33828121004`, aceptación `#33828121038`, Fase 8 `#33828121029`, M13 `#33828121086` y M10 `#33828121034` en `SUCCESS`; `VariApp CI` `SKIPPED` no se cuenta como PASS.
- [x] N4.6.H — `LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N4_6_PLAN_CUENTAS.md`, operación/rollback documentados, `P0=0`, `P1=0`.
- [x] Fase 2 queda registrada como `EXTERNAL_INFRA`: HTTP 503 de `registry.npmjs.org` durante `npm audit`; no es fallo causal del producto.

**CURRENT_PARENT=N4.6.H. N4.6 queda cerrado como `LISTO_REAL`; siguiente parent dependency-valid: `N4.7.A`. Este cierre no inicia ni modifica el scope de N4.7.**

### Reconciliación QA append-only

Este rollup supersede únicamente el estado operativo stale anterior del bloque N4.4; las menciones históricas `CURRENT_PARENT=N4.4.H` permanecen como evidencia histórica, ya no gobiernan el estado actual y no se reescribe ni elimina ninguna historia previa. El estado operativo vigente de este cierre es `CURRENT_PARENT=N4.5.H`. La certificación documental exacta corresponde al commit `fde578dd69cbfe91c054138d33404cec342093f6`; el `productBaseHead` es `541ec12b72912c769c6f54b8821771e509818375`.


## Integración AntiG — Reviewer/Fixer automático — 2026-09-03

- [x] Custom Agent de workspace `variapp-reviewer` definido con gobierno VariApp y separación `READY_FOR_VAEP != LISTO_REAL`.
- [x] Worker automático Jules -> AntiG -> VAEP implementado fail-closed con mutex, watermark, artifact causal, scope guard y publicación sin force/rebase.
- [x] Schema estructurado y self-test CI agregados.
- [x] Instalador local seguro preparado con permisos finos de Antigravity CLI; no usa bypass global de permisos.
- [x] Gobernanza `AGENTS.md` y `docs/COLABORACION_IA.md` actualizada con ATTEMPT1/R2, QA takeover y prohibición de auto-certificación.
- [ ] AntiG RESERVED_INACTIVE: activación de Scheduled Task `VariApp-AntiG-Reviewer` suspendida; AntiG permanece reservado e inactivo conforme a MAESTRO.


## AntiG P1 hardening — 2026-09-03

- [x] Aislar review/fix en Git worktree temporal para preservar trabajo concurrente del checkout primario.
- [x] Eliminar `git add --all`; staging exacto por paths autorizados.
- [x] Validar causalidad Issue/run/artifact/dispatch/result/gitpatch/patch, base, attempt, scope y rutas protegidas.
- [x] Cuarentena de handoffs inválidos con avance de watermark para evitar lane starvation.
- [x] Self-tests funcionales de contrato, rutas protegidas y aislamiento de worktree.
- [x] Rollback transaccional del instalador si falla la fase mutante.
- [ ] AntiG RESERVED_INACTIVE: Scheduled Task `VariApp-AntiG-Reviewer` reservada e inactiva según gobernanza MASTER.

## AntiG P1 hardening — cierre técnico Codex — 2026-09-04

- [x] Causalidad estricta de `gitpatch.json` y `changes.patch`, incluyendo contenido unidiff, base SHA40 e identidad opcional coherente.
- [x] Compatibilidad histórica reconciliada; guards fail-closed y validación estructurada completas bajo el MAESTRO.
- [x] Delta staged/unstaged/untracked, staging explícito e igualdad dura de paths.
- [x] Rutas Vercel protegidas explícitamente; AntiG no puede modificar `frontend/vercel.json` ni `frontend/scripts/vercel-ignore-build.mjs`.
- [x] Handoffs inválidos en cuarentena; errores transitorios retryables sin avance de watermark.
- [x] Estado de publicación durable `COMMENT_PENDING` tras confirmar `evidenceHead`, con deduplicación antes de comentarios.
- [x] Rollback del instalador restaura XML de tarea preexistente o elimina solo la tarea nueva.
- [x] Self-tests funcionales ampliados para contrato, aislamiento, staging, paths protegidos, patch causal, watermark, idempotencia y planes de rollback.
- [ ] AntiG RESERVED_INACTIVE: instalación física y Scheduled Task permanecen inactivas; no se ejecutan en esta fase.

## ERP-N4.7 — Asientos Contables — ROLLUP 2026-09-04

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.7.A-G — `LISTO_REAL`; baseline funcional certificado `c8d1e373ba8ea008bf773e69afa10f5f18d6de8b`.
- [x] Certificación canónica de H publicada y REVIEW_FIRST aprobada: `docs/CERTIFICACION_N4_7_ASIENTOS.md`.
- [x] Exact-head documental previo `6986874048985e4746d21e23254479b391220445`: gates aplicables terminales `SUCCESS`; `VariApp CI=SKIPPED` excluido como PASS; P0=0/P1=0.
- [x] `TASKS.md` reconciliado de forma aditiva/history-preserving por este rollup.
- [ ] `N4.7.H` permanece fail-closed hasta certificar los gates aplicables y P0/P1=0 del exact-head resultante de completar también `CHANGELOG_AI.md`; no false `LISTO_REAL`.

**CURRENT_PARENT=N4.7.H. NEXT_ACTION=reconciliar `CHANGELOG_AI.md` aditivamente, drenar/certificar el exact-head final y solo entonces cerrar H y promover `N4.8.A`.**

## VAEP MASTER migration — cierre F0/F1 — 2026-09-04

- [x] FASE 0 — EXCLUSIVIDAD: `PASS/CLOSED`. `docs/VAEP_AUTHORITY.md` permanece como autoridad operativa única; ChatGPT/VAEP conserva controller/REVIEW_FIRST/QA/certificación; Codex y AntiG no tienen autoridad operativa vigente; PR #2 continúa `OPEN+DRAFT` y `main` permanece congelada.
- [x] FASE 1 — FREEZE + RECONCILIACIÓN: `PASS/CLOSED`. El guard técnico de admisión está activo en `.github/scripts/vaep-jules-master.sh` y el estado declarativo `vaep/control/dispatch-admission.json` está en `FROZEN`.
- [x] Admisión Fase 1: cero manifests nuevos => `NO_OP/exit 0`; más de uno => fail-closed; exactamente uno => valida control state antes de sesión/attempt/ownership/recovery; `FROZEN` rechaza nuevos dispatches sin invalidar sesiones `ACTIVE_REAL` preexistentes.
- [x] Reconciliación N4.7 preservada: `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`. El cierre de Fase 1 NO declara `N4.7.H=LISTO_REAL` ni promueve N4.8.
- [x] `ADMISSION_BATTERY=PASS` sobre los casos aislados ya validados: `NO_OP`, `FROZEN`, `OPEN`, múltiples manifests y estados inválidos.
- [x] `FALSE_PASS=NO`, `FALSE_LISTO=NO`, `SCOPE_LEAK=NO`.

**MIGRATION_PHASE_0=CLOSED/PASS. MIGRATION_PHASE_1=CLOSED/PASS. NEW_DISPATCH_ADMISSION=FROZEN. FASE_2=CLOSED/PASS. FASE_3=NOT_STARTED.**

## VAEP MASTER migration — cierre F2 — 2026-09-04

- [x] FASE 2 — CONSOLIDACIÓN DE POLÍTICA OPERATIVA MACHINE-READABLE: `PASS/CLOSED`.
- [x] Bloque canónico único `BEGIN_AUTOMATION_POLICY ... END_AUTOMATION_POLICY` embebido en `docs/VAEP_AUTHORITY.md`.
- [x] Parser fail-closed `.github/scripts/vaep-policy-parser.sh` implementado y validado (`--self-test`, missing/duplicate/unknown/invalid keys fail-closed, hash determinístico SHA-256).
- [x] Runtime unificado: `.github/scripts/vaep-jules-master.sh` y `.github/scripts/vaep-jules-worker.sh` consumen la política vía parser dinámico; duplicación de valores eliminada.
- [x] Workflows saneados: 8 workflows `vaep-jules-*.yml` sin redefinición de budget (safety-net `timeout-minutes: 25`).
- [x] Consumidores activos alineados a `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`): `AGENTS.md`, `docs/VAEP_JULES.md`, `PLAN_EJECUCION_AUTONOMA.md`, `PROJECT_CONTEXT.md`, `docs/COLABORACION_IA.md`, `docs/CONTEXTO_CHATGPT_VAEP.md`.
- [x] AntiG preservado intacto como `RESERVED_INACTIVE` en documentación y componentes.
- [x] Protocolos numéricos en superficies activas eliminados (`NUMERIC_PROTOCOL_LABELS=0`).
- [x] Reconciliación N4.7 preservada: `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`.
- [x] `FALSE_PASS=NO`, `FALSE_LISTO=NO`, `SCOPE_LEAK=NO`.
