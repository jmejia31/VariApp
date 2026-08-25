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
- Los lanes Jules A/B/C/D ahora identifican v3.25; se preservaron v4.6, ATTEMPT1+R2, R3 prohibido, QA takeover, doble revisión, artefactos/Issues, `Desarrollo` y prohibición de push/merge/deploy Jules.
- Se retiró del ejecutor el sprint vencido y se añadió cierre por padre con checkpoints `:00/:15/:30/:45/:55`.
- Se añadió `--static-self-test` para validar guardrails sin red, secretos, sesión ni attempt. La prueba de integración real no se ejecutó porque un dispatch crea sesión y consume attempt.

## 2026-08-24 — Codex — autoridad VAEP/Jules v3.25

- Se unificó la gobernanza documental en `V3.25_CURRENT`, cierre por padre y checkpoints `:00/:15/:30/:45/:55`, preservando control-plane global v4.6.
- v3.20/v3.21 quedaron marcados como historia; continúan ATTEMPT1+R2, R3 prohibido, QA takeover, HEAD freeze, evidencia causal y protección de `Desarrollo`/main/Producción.
- Se aclaró que el Sheet registra/describe automatizaciones y el sistema de tareas ejecuta; no se modificó ni afirmó ejecución de una automatización real.
- Cambio exclusivamente documental; sin código, workflows, infraestructura, secretos ni Sheet.

## 2026-08-24 — Codex — reconciliación documental ChatGPT/VAEP

- Se amplió `docs/CONTEXTO_CHATGPT_VAEP.md` con roles, ciclo automático, mutex/actividad/CI/handoff, fuentes de verdad, consulta selectiva, estado local observable y mejoras priorizadas.
- Se documentó fail-closed el conflicto Jules v3.20/v3.21 en `docs/VAEP_AUTHORITY.md`, `PLAN_EJECUCION_AUTONOMA.md`, `PROJECT_CONTEXT.md` y `TASKS.md` sin reescribir el historial.
- No se consultó Sheet/Drive ni se afirmó estado externo fresco; no se modificaron código, workflows o infraestructura.

## 2026-08-24 — Codex — guía operativa por dominio

- Se amplió `PROJECT_INDEX.md` con mapa por capas, matriz por dominio, flujos transversales y límites de inspección para cambios locales.
- Se corrigió el mapa de datos para reflejar las dos ubicaciones históricas reales de migraciones.
- Se registró el cambio en `ARCHITECTURE_CHANGELOG.md`; no se modificó código ni configuración.

## 2026-08-24 — Codex — contexto ChatGPT/VAEP

- Se incorporó `docs/CONTEXTO_CHATGPT_VAEP.md` como referencia histórica/operativa de VariApp.
- Se enlazó desde `PROJECT_INDEX.md` y se registró en `ARCHITECTURE_CHANGELOG.md`.
- Se documentaron VAEP, validación causal, cadena compras-recepciones-reservas-facturación, no duplicación y consulta selectiva sin presentarlos como estado no verificado.
- Cambio exclusivamente documental; no se ejecutó ni modificó código de producción.

## 2026-08-24 — Codex — mapa técnico persistente

- Se consolidó `PROJECT_INDEX.md` como mapa rápido con índice de decisión, puntos de entrada y comandos verificados.
- Se creó `ARCHITECTURE_CHANGELOG.md` y se enlazó la convención de mantenimiento desde el contexto y la arquitectura canónicos.
- Se alineó la declaración `PROJECT_ID: VARIAPP` con el guard obligatorio de inicio de sesión.
- Cambio exclusivamente documental; no se ejecutó ni modificó código de producción.

## 2026-08-23 — ERP-N3.1 Cotizaciones — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante PARENT-CLOSURE-FIRST y QA takeover documental.

**Objetivo/alcance:** cerrar N3.1.A-H con Cotización como documento comercial previo al Pedido de Venta, snapshots de cliente/producto y lifecycle `Borrador → Enviada → Aceptada/Rechazada → Convertida`, sin adelantar el dominio de Pedidos N3.2.

**Validación final:** baseline funcional `d4d296e229d266a1442de3bc4e07b03bfab35a9f`; HEAD de control `eea11fb0e3ba1f1afc3010362f87caecf89f6c22` con Development `#32687639976`, Acceptance `#32687639981`, Fase 8 `#32687640010`, M13 `#32687640016` y Recovery MySQL `#32687640017` en SUCCESS. El único delta entre ambos era un manifest evidence-only de cierre Jules A, sin cambio funcional. P0/P1 bloqueantes conocidos=0.

**Cierre documental/control:** certificación canónica `docs/CERTIFICACION_N3_1_COTIZACIONES.md`; `TASKS.md` reconciliado. El dispatch Jules A de cierre no produjo sesión ni actividad útil dentro del umbral y quedó `BOOTSTRAP_STALLED_NO_SESSION / ACTIVE=NO`, sin consumir ATTEMPT1 funcional; ChatGPT/VAEP cerró H directamente. Parent40 avanza `29→30/40`, GAP `11→10`, y el selector fail-closed promueve inmediatamente `N3.2.A — Pedidos de venta / Auditoría y preflight`.

## 2026-08-23 — ERP-N2.9 Evaluación de proveedores — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante QA takeover y cierre canónico parent-first.

**Objetivo/alcance:** N2.9.A-H completadas; la evaluación factual de proveedores cubre tiempos/cumplimiento de entrega, diferencias, devoluciones, costos y calidad sin inventar fórmulas de scoring, pesos, umbrales ni rankings.

**Validación final:** paquete canónico `af3439ea00a7ff09333926e79f5668e0f2c8e1e9`; baseline de control `13f59ee7c6272bb3a8d02e293c20f7b645bb7017` con Development #32634001803 SUCCESS, Acceptance #32634001793 SUCCESS, Fase8 #32634001797 SUCCESS, M13 #32634001794 SUCCESS y Recovery MySQL #32634001786 SUCCESS. P0/P1 bloqueantes conocidos=0.

**Control:** Parent40 avanza 21→22/40 y GAP 19→18 únicamente tras review/CI causal de este cierre de changelog; `GATE-N2` es el siguiente padre dependency-valid y ERP-N3 no puede promoverse antes de `GATE-N2=LISTO`. Jules A agotó ATTEMPT2/2 en el cierre documental y quedó liberado; R3+ permanece prohibido.

## 2026-08-22 — ERP-N2.8 Cuentas por pagar — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante cierre canónico parent-first; artifacts Jules se usaron únicamente como evidencia revisada cuando correspondió y no sustituyen el DoD causal.

**Objetivo/alcance:** cerrar formalmente ERP-N2.8 Cuentas por pagar con N2.8.A–H completadas: preflight, dominio/contratos, persistencia y migración, Application/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación/certificación. El alcance cubre obligación financiera por factura de proveedor, contado/crédito, vencimientos, pagos parciales, anticipos, retenciones y saldo, sin adelantar evaluación de proveedores de N2.9.

**Validación final:** HEAD documental `360ff3303af3587810c21e32ceeeb88fcc9e51d3`; Development #32607259773 SUCCESS; Acceptance #32607259650 SUCCESS; Fase8 #32607259716 SUCCESS; M13 #32607259703 SUCCESS; Recovery MySQL #32607259695 SUCCESS. `TASKS.md` ya declara ERP-N2.8 cerrado y la bitácora queda ahora reconciliada. P0/P1 bloqueantes conocidos=0.

**Control:** `N2.8.A–H` quedan formalmente cerrados. Parent40 avanza 13→14/40, GAP 27→26. La siguiente MICROTAREA dependency-valid es `N2.9.A — Evaluación de proveedores — Auditoría y preflight`; reutilizar su evidencia histórica existente y no repetir preflight redundante. `main`, Producción, PR #2 merge/auto-merge, ramas nuevas, force-push, secretos y despliegues permanecen intactos.

## 2026-08-22 — ERP-N2.7 NotaCreditoProveedor — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP mediante QA takeover v3.21, reutilizando únicamente el contenido documental validado del artifact Jules D #348; el resultado Jules no se integró por incumplir el gate de self-review independiente.

**Objetivo/alcance:** cierre formal canónico de ERP-N2.7 Nota de crédito de proveedor, con N2.7.A-H completadas, sin adelantar trabajo de N2.8.

**Validación:** baseline funcional `42f83b365392f45de39bd0e0ca4fa0638dd0eb10` y paquete documental `c466ec3099c2a498c2353af82b99ce0be9d46e29`; Development #32574284665, Acceptance #32574284640, Fase 8 #32574284638 y M13 #32574284639 SUCCESS. El HEAD de control-plane `e72f709bdade0dbec6198fa483aaa213a5e6c66d` también terminó Development #32576077991, Acceptance #32576077933, Fase8 #32576077965, M13 #32576077925 y recovery MySQL #32576077970 en SUCCESS. P0/P1 bloqueantes conocidos=0.

## 2026-08-19 — ERP-N2.2 OrdenCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive, con exclusión total del scope reservado de Jules.

**Objetivo/alcance:** cerrar formalmente ERP-N2.2 después de completar preflight, dominio/contratos, persistencia/migración, aplicación/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación. `OrdenCompra` queda como documento empresarial independiente que representa el compromiso comercial con el proveedor; no representa recepción física, stock, Kardex, costeo, factura de proveedor ni obligación financiera.

**Resultado funcional:** lifecycle `Borrador → PendienteAprobacion → Aprobada` con cancelación controlada, moneda ISO, proveedor/snapshots, detalles, descuentos/impuestos, fecha esperada, observaciones e idempotencia durable `Idempotency-Key + SHA-256`. La API `/ordenes-compra` exige autenticación y grants relacionales `Compras:Ver/Crear/Editar/Confirmar/Aprobar/Anular`. Frontend cubre listado, creación/edición, detalle, aprobación/cancelación, errores fail-closed, paginación y performance. La migración canónica `20260818204700_N2_2_OrdenCompraPersistencia` crea tablas dedicadas con guards y rollback bloqueado cuando existen documentos.

**Trazabilidad:** A `73ef31c49f08c8bff9732978ffc86dbe74e0a116`; B `88047cde42929c1b2dcd8faf77da1c6543a2f2a9` + fix `f17983ef49bb8f5032e6fb328564f36c02f103b9`; C `adff03723b4336b570328179e468e8470e611b95`; D hasta `a5340f991b0f93438ac184afeac41cc9ed82a756`; E.1 `26a7eada...`, E.2 `9ede060d...`, E.3 `f9000061...`; F hasta `1eb26cf60a3d4e1e37f9c89b60929f432de3c1ac`; G.1 `23fa5ac6...`; G.2/G.3 baseline `b4d477e2de25077c459d02b479968c93c93bc910`. Paquete H: `e59b7bb59cf51b99ae14665cee18c1fe70220bbb`, `6d53ae43f4a9fa54b41f1981704cb03c427d2a74`, `74ebbe969b22b9d8e0130ea733ae0c9fa9f18891`, `821431340afceb70b93f5431a719b8adc2ab6717` y candidato documental `736683476714300d6bf29406967e17c312abac7d`; `TASKS.md` reconciliado en `da05e6625ec6caf98f4e7e4a6dc4912d284dd805`.

**Validación:** baseline funcional `b4d477e2...`: Development `32218997006`, Acceptance `32218996971`, Fase 8 `32218996994`, M10 `32218996973` y M13 `32218996978` SUCCESS. Persistencia N2.2.C: M12 `32184108722` SUCCESS en MySQL 8.4. Sobre el candidato documental `73668347...`, Development `32227719896` terminó SUCCESS completo —backend/unitarias, frontend, higiene, Docker, aplicación de migraciones, integración MySQL y SQL forward— y recovery MySQL `32227719707` SUCCESS; el diff H es exclusivamente documental/colaborativo y no modifica aplicación ni workflows.

**Documentación:** `docs/ERP_N2_2_ORDEN_COMPRA.md`, `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`, `docs/ADR_N2_2_ORDEN_COMPRA_AUTORIDAD_DOCUMENTAL.md`, `docs/OPENAPI_N2_2_ORDEN_COMPRA.md` y `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`, más el preflight histórico `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md`.

**Control:** `N2.2.A–H` quedan formalmente cerrados. El siguiente foco FINISH_FIRST elegible es `N2.3.A — Recepción de mercancía — Auditoría y preflight`, donde recién debe materializarse el incremento de stock por recepción real. El scope Jules no fue editado ni integrado. `main`, Producción, merge/auto-merge del PR #2, ramas nuevas, force-push, secretos e infraestructura productiva permanecen intactos.

## 2026-08-18 — ERP-N2.1 SolicitudCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive, preservando cambios concurrentes publicados en `Desarrollo`.

**Objetivo/alcance:** cerrar formalmente ERP-N2.1 después de completar preflight, dominio/contratos, persistencia/migraciones, aplicación/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación. `SolicitudCompra` queda como documento empresarial independiente con lifecycle `Borrador → Solicitada → Aprobada/Rechazada` y sin efectos de stock, Kardex, costeo o finanzas.

**Decisiones y seguridad:** una solicitud aprobada continúa siendo documental y no crea implícitamente una `Compra`; la materialización posterior pertenece a `N2.2` y siguientes. Update/Enviar/Aprobar/Rechazar se serializan con transacción y lock pesimista. La autorización usa grants relacionales sin bypass efectivo por `EsAdministrador`. Crear/Editar/Enviar/Aprobar/Rechazar registran auditoría estricta dentro de la unidad transaccional, sin copiar notas u observaciones sensibles. Correlation ID, health/readiness y configuración segura reutilizan la infraestructura transversal existente.

**Trazabilidad:** D `01770a23cbf9a50e7d21a0a7913f32e31ce6070a`; E.1 `f52f9f746427d18675073ba769c2a78c2f13d900`; E.2 `112ef6b8660fb12c80d6981eac81b55f6c32bdec`; E.3 hasta `07275df6af316aff83f250c6cf9d9b1b1ad335d3`; F.1 `d3f039efafe0bf7ccfd487ba4ca7c66e07625fc3`; F.2 `adea50ac65bacceff42cd23c110afea77817ca44`; F.3 `12b26459004dc01a17b5b2af4602dbb906470bae`; G baseline `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`; paquete documental H `d8760bff2e9322e6f09612f64a89c2de888aa9d8`.

**Validación:** CI funcional `32172981351` SUCCESS, incluido frontend, backend y MySQL con 994/994 pruebas backend. Sobre el commit documental `d8760bff...`, Development `32177459360`, Fase 8 `32177459423`, M10 `32177459382`, M12 `32177459385`, backup operativo `32177459445`, backup/restauración `32177459455` y recuperación MySQL `32177459334` terminaron SUCCESS; los workflows históricos ERP-N0 que fallen por incompatibilidades de su propio alcance no se usan como gate causal de N2.1.

**Documentación/riesgos residuales:** fuentes canónicas `docs/ADR_N2_1_SOLICITUD_COMPRA_INDEPENDIENTE.md`, `docs/ERP_N2_1_SOLICITUD_COMPRA.md` y `docs/RUNBOOK_N2_1_SOLICITUD_COMPRA.md`. Riesgo residual deliberado: la conversión `SolicitudCompra → OrdenCompra/Compra`, impuestos/moneda/condiciones y recepción pertenecen a microtareas posteriores; no se adelantan en N2.1. No quedan bypasses temporales conocidos atribuibles a N2.1.

**Control:** `N2.1.A–H` quedan formalmente cerrados tras reconciliar `TASKS.md`, esta bitácora y VAEP. El siguiente foco FINISH_FIRST elegible es `N2.2.A — Orden de compra — Auditoría y preflight`. `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push y ramas nuevas permanecen intactos.

## 2026-08-17 — ERP-N1.9 Series, lotes y vencimientos — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N1.9 después de completar auditoría/preflight, dominio y contratos, persistencia/migración, aplicación/API, frontend/UX, RBAC/auditoría/seguridad, QA/regresión y documentación. La capacidad queda deliberadamente opt-in por `ProductoVariante`: Lote, Número de Serie y Fecha de Vencimiento no se imponen a todos los productos y `ExistenciaVariante` continúa siendo la única autoridad cuantitativa del stock físico.

**Resultado funcional:** `LoteInventario` y `SerieInventario` funcionan como subledger de identidad trazable, no como una segunda autoridad de cantidad. La persistencia es aditiva y preserva históricos con flags desactivados por defecto, sin inventar backfill de lotes/series/vencimientos. La API y UI permiten configurar la política por variante, capturar/listar lotes y series y controlar vencimientos. La seguridad usa RBAC relacional de `MovimientosInventario`, auditoría estricta transaccional e idempotente, correlation saneado y contratos HTTP protegidos. Durante QA se detectó y corrigió causalmente la falta de límite de longitud de `NumeroSerie`; el dominio ahora rechaza valores de más de 120 caracteres antes de mutar estado.

**Documentación:** paquete canónico compuesto por `docs/ERP_N1_9_SERIES_LOTES_VENCIMIENTOS.md`, `docs/ADR_N1_9_AUTORIDAD_TRAZABILIDAD.md`, `docs/ERD_N1_9_TRAZABILIDAD.md`, `docs/RUNBOOK_N1_9_TRAZABILIDAD.md`, `docs/OPENAPI_N1_9_TRAZABILIDAD.md`, `docs/RUNBOOK_N1_9_MIGRACION.md` y `docs/CERTIFICACION_N1_9_TRAZABILIDAD.md`. El baseline funcional de QA es `4b5a5c9a8b495fcef62464bf50010ac69117fe48`; el baseline documental certificable es `7bc4b7935cc92e15d24f90a79f3915ab14e2d243`.

**Validación final de `7bc4b793...`:** Development `32089179243` SUCCESS; Acceptance `32089179228` SUCCESS; Fase 8 `32089179144` SUCCESS; M10 `32089179156` SUCCESS; M13 `32089179175` SUCCESS completo, incluido Backend/MySQL/migraciones/upgrade histórico, Frontend, Docker/backup, Secretos/Higiene, Runtime/Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS.

**Control:** `TASKS.md` queda reconciliado con VAEP, incluyendo el desfase histórico de `N1.7.H` que ya estaba `LISTO` en el tablero. `N1.8.A–H` quedan cerrados, y el siguiente foco es N1.9.A.
