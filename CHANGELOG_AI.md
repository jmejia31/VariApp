# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-08-22 — ERP-N2.7 NotaCreditoProveedor — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP mediante QA takeover v3.21, reutilizando únicamente el contenido documental validado del artifact Jules D #348; el resultado Jules no se integró por incumplir el gate de self-review independiente.

**Objetivo/alcance:** cierre formal canónico de ERP-N2.7 Nota de crédito de proveedor, con N2.7.A-H completadas, sin adelantar trabajo de N2.8.

**Validación:** baseline funcional `42f83b365392f45de39bd0e0ca4fa0638dd0eb10` y paquete documental `c466ec3099c2a498c2353af82b99ce0be9d46e29`; Development #32574284665, Acceptance #32574284640, Fase8 #32574284638 y M13 #32574284639 SUCCESS. El HEAD de control-plane `e72f709bdade0dbec6198fa483aaa213a5e6c66d` también terminó Development #32576077991, Acceptance #32576077933, Fase8 #32576077965, M13 #32576077925 y recovery MySQL #32576077970 en SUCCESS. P0/P1 bloqueantes conocidos=0.

## 2026-08-19 — ERP-N2.2 OrdenCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive, con exclusión total del scope reservado de Jules.

**Objetivo/alcance:** cerrar formalmente ERP-N2.2 después de completar preflight, dominio/contratos, persistencia/migración, aplicación/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación. `OrdenCompra` queda como documento empresarial independiente que representa el compromiso comercial con el proveedor; no representa recepción física, stock, Kardex, costeo, factura de proveedor ni obligación financiera.

**Resultado funcional:** lifecycle `Borrador → PendienteAprobacion → Aprobada` con cancelación controlada, moneda ISO, proveedor/snapshots, detalles, descuentos/impuestos, fecha esperada, observaciones e idempotencia durable `Idempotency-Key + SHA-256`. La API `/ordenes-compra` exige autenticación y grants relacionales `Compras:Ver/Crear/Editar/Confirmar/Aprobar/Anular`. Frontend cubre listado, creación/edición, detalle, aprobación/cancelación, errores fail-closed, paginación y performance. La migración canónica `20260818204700_N2_2_OrdenCompraPersistencia` crea tablas dedicadas con guards y rollback bloqueado cuando existen documentos.

**Trazabilidad:** A `73ef31c49f08c8bff9732978ffc86dbe74e0a116`; B `88047cde42929c1b2dcd8faf77da1c6543a2f2a9` + fix `f17983ef49bb8f5032e6fb328564f36c02f103b9`; C `adff03723b4336b570328179e468e8470e611b95`; D hasta `a5340f991b0f93438ac184afeac41cc9ed82a756`; E.1 `26a7eada...`, E.2 `9ede060d...`, E.3 `f9000061...`; F hasta `1eb26cf60a3d4e1e37f9c89b60929f432de3c1ac`; G.1 `23fa5ac6...`; G.2/G.3 baseline `b4d477e2de25077c459d02b479968c93c93bc910`. Paquete H: `e59b7bb59cf51b99ae14665cee18c1fe70220bbb`, `6d53ae43f4a9fa54b41f1981704cb03c427d2a74`, `74ebbe969b22b9d8e0130ea733ae0c9fa9f18891`, `821431340afceb70b93f5431a719b8adc2ab6717` y candidato documental `736683476714300d6bf29406967e17c312abac7d`; `TASKS.md` reconciliado en `da05e6625ec6caf98f4e7e4a6dc4912d284dd805`.

**Validación:** baseline funcional `b4d477e2...`: Development `32218997006`, Acceptance `32218996971`, Fase 8 `32218996994`, M10 `32218996973` y M13 `32218996978` SUCCESS. Persistencia N2.2.C: M12 `32184108722` SUCCESS en MySQL 8.4. Sobre el candidato documental `73668347...`, Development `32227719896` terminó SUCCESS completo —backend/unitarias, frontend, higiene, Docker, aplicación de migraciones, integración MySQL y SQL forward— y recovery MySQL `32227719707` SUCCESS; el diff H es exclusivamente documental/colaborativo y no modifica aplicación ni workflows.

**Documentación:** `docs/ERP_N2_2_ORDEN_COMPRA.md`, `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`, `docs/ADR_N2_2_ORDEN_COMPRA_AUTORIDAD_DOCUMENTAL.md`, `docs/OPENAPI_N2_2_ORDEN_COMPRA.md` y `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`, más el preflight histórico `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md`.

**Control:** `N2.2.A–H` quedan formalmente cerrados. El siguiente foco FINISH_FIRST elegible es `N2.3.A — Recepción de mercancía — Auditoría y preflight`, donde recién debe materializarse el incremento de stock por recepción real. El scope Jules no fue editado ni integrado. `main`, Producción, merge/auto-merge del PR #2, ramas nuevas, force-push, secretos e infraestructura productiva permanecen intactos.
