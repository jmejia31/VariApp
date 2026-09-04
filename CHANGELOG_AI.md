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

**Validación:** baseline funcional `42f83b365392f45de39bd0e0ca4fa0638dd0eb10` y paquete documental `c466ec3099c2a498c2353af82b99ce0be9d46e29`; Development #32574284665, Acceptance #32574284640, Fase8 #32574284638 y M13 #32574284639 SUCCESS. El HEAD de control-plane `e72f709bdade0dbec6198fa483aaa213a5e6c66d` también terminó Development #32576077991, Acceptance #32576077933, Fase8 #32576077965, M13 #32576077925 y recovery MySQL #32576077970 en SUCCESS. P0/P1 bloqueantes conocidos=0.

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

**Objetivo/alcance:** cerrar formalmente ERP-N2.1 después de completar preflight, dominio/contratos, persistencia/migración, aplicación/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación. `SolicitudCompra` queda como documento empresarial independiente con lifecycle `Borrador → Solicitada → Aprobada/Rechazada` y sin efectos de stock, Kardex, costeo o finanzas.

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

**Validación final de `7bc4b793...`:** Development `32089179243` SUCCESS; Acceptance `32089179228` SUCCESS; Fase8 `32089179144` SUCCESS; M10 `32089179156` SUCCESS; M13 `32089179175` SUCCESS. El estado colaborativo posterior es exclusivamente documental `[skip ci]`: `TASKS.md` reconciliado en `67da8adc9e3dfad87140346050ee731b3dd8abc8` y certificado final actualizado en `81b5478458f8dfd5aa33e4653a3b413e1b4bbb36`.

**Control:** `N1.9.A–H` quedan cerrados; el tablero VAEP debe avanzar a `N1.10.A` únicamente si sus dependencias están `LISTO`. `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push y ramas nuevas permanecen intactos.

## 2026-08-17 — ERP-N1.8 Reservas de inventario — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N1.8 después de completar auditoría/preflight, dominio y contratos, persistencia/migración, backend/API, frontend/UX, RBAC, auditoría crítica, seguridad/observabilidad, regresión integral y documentación. El objetivo empresarial queda cumplido: diferenciar stock físico, reservado y disponible e impedir overselling sin crear una segunda autoridad cuantitativa.

**Resultado funcional:** `ExistenciaVariante` permanece como autoridad única de cantidad por clave física `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`; `ReservaInventario` y sus detalles explican el compromiso reservado y su lifecycle. Activar/consumir/liberar/expirar/cancelar opera bajo lock pesimista y transacción; la auditoría crítica es obligatoria y usa `RegistrarEstrictoAsync` dentro de `IUnitOfWork`, por lo que una mutación no puede confirmarse si su evidencia falla. Frontend y API conservan RBAC relacional, CorrelationId saneado, estados físico/reservado/disponible y protección de rutas/acciones.

**Documentación:** `docs/ERP_N1_8_RESERVAS.md`, `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`, `docs/RUNBOOK_N1_8_RESERVAS.md` y `docs/ERD_N1_8_RESERVAS.md`, publicados en `11865b97f00f662728f7fe85a7466af89a9084df`. El baseline funcional previo es `95baf2763b912e1015a3bdd25a37aca649e34c37`.

**Validación final del HEAD documental `11865b97...`:** Development `32037186026` SUCCESS 5/5; Acceptance `32037186011` SUCCESS incluido Playwright integral + SMTP/PDF; Fase8 `32037186066` SUCCESS; M10 `32037186054` SUCCESS; M13 `32037186024` SUCCESS completo, incluido Backend/MySQL/migraciones/upgrade, Frontend, Docker/backup, Secretos/Higiene, Runtime/Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS.

**Control:** `TASKS.md` queda reconciliado con VAEP, incluyendo el desfase histórico de `N1.7.H` que ya estaba `LISTO` en el tablero. `N1.8.A–H` quedan formalmente cerrados. `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push y ramas nuevas permanecen intactos. Siguiente foco FINISH_FIRST: `N1.9.A — Series, lotes y vencimientos — Auditoría y preflight`.

## 2026-08-14 — ERP-N1.3 Ubicaciones internas de almacén — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** completar `UbicacionAlmacen` como topología física jerárquica interna de cada Almacén para pasillos, estantes, racks, secciones, bins y otras ubicaciones, sin introducir todavía existencias, cantidades ni semántica WMS avanzada.

**Resultado funcional:** `UbicacionAlmacen.AlmacenId` es la única relación organizacional persistida; `SucursalId` y `EmpresaId` se derivan transitivamente. Padre opcional restringido al mismo Almacén, prevención de ciclos directos/indirectos, protección de descendientes al mover/desactivar/eliminar, código operativo único por Almacén, soft-delete y estados idempotentes. MySQL 8.4 conserva la invariante anti-self-parent mediante triggers porque un CHECK no puede referenciar el `Id AUTO_INCREMENT`. API `/ubicaciones-almacen` soporta búsqueda, Almacén, padre/raíz, tipo, estado, paginación, CRUD y operaciones de estado. Frontend incorpora listado responsive, filtros server-side, formulario jerárquico, selectores de Almacén/padre, rutas y menú protegidos por RBAC.

**RBAC/auditoría/seguridad:** módulo `UbicacionesAlmacen`, permisos `Ver/Crear/Editar/Activar/Desactivar/EliminarLogico`, auditoría de mutaciones con referencia de entidad y pruebas que congelan los 9 contratos de autorización. Se reutilizan Correlation ID, ProblemDetails, headers de seguridad y health/readiness globales. N1.3 no contiene campos de stock; `ExistenciaVariante` queda reservado para ERP-N1.4.

**Trazabilidad:** D backend `4d2cc04b363df602f6de97b7f5ea876ea35a6196`, run `31843085895`, job `94903923345` SUCCESS; E frontend `91f878ef3cbc56219b637e9b62c99bdd1109a9df`, run `31846161956`, job `94912936660` SUCCESS; F/G baseline `4a6be38683f03fc2076f18a71115480c930ba79b`.

**QA real:** run agregado `31846485117` SUCCESS: higiene `94913888918`, Backend Release/unitarias `94913888850`, frontend producción `94913888865`, Docker `94913888808` y MySQL 8.4/integración `94913888844`; el job MySQL aplicó migraciones actuales, ejecutó `Category=Integration`, verificó snapshot/variantes/cargas y generó SQL forward sin regresiones.

**Documentación/control:** preflight `docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md`; cierre canónico `docs/ERP_N1_3_UBICACIONES_ALMACEN.md`; TASKS, CHANGELOG y tablero VAEP reconciliados preservando historial. `main`, Producción, merge/auto-merge del PR #2, secretos y force-push permanecen intactos. **ERP-N1.3 queda formalmente cerrado** y el siguiente foco FINISH_FIRST es `N1.4.A — ExistenciaVariante — Preflight y diseño`.

## 2026-08-14 — ERP-N1.2 Almacenes empresariales — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** implementar y certificar `Almacen` como maestro hijo obligatorio de `Sucursal`, con tipos Tienda/Bodega/Transito/Devolucion/Cuarentena, persistencia MySQL, API, RBAC relacional, auditoría, observabilidad, frontend responsive/accesible y QA dedicado, sin adelantar ubicaciones N1.3, existencias por almacén N1.4 ni multiempresa N6.

**Resultado funcional:** `Almacen.SucursalId` queda como única jerarquía organizacional de N1.2; una introducción concurrente de `EmpresaId` duplicada fue detectada y corregida forward-only en `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8`. Persistencia final con FK Restrict a `Sucursales`, código activo único, índices/checks y rollback fail-closed. API `/almacenes` soporta CRUD, filtros/paginación, catálogo de tipos, activos y operaciones de estado. Crear/mover/reactivar falla cerrado si la Sucursal no existe o está inactiva. RBAC `Almacenes=29`, auditoría `Entidad=Almacen` y métrica P50/P95 sin término/PII quedan integrados. Frontend ofrece lista server-side, selector Sucursal/tipo, rutas y menú protegidos, tabla/cards responsive y formulario sin stock ni EmpresaId.

**Trazabilidad:** B final `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8`; C `bebafe3abb2ddc66448c805b107f8d1f8ee3f3e9`; D `5a97bf3844069a565e1aecf39e4b8001c10f386b`; E `3a1b8004f2120c4be6459bb46fd120eff8704fe9`; F `30c7e9ff1dedf69eb860916b92b1d5bee0941084`; G base `f6f51bb6d0d5d1910e9561de30d934b30fa2d83e`, corrección harness `3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f` y corrección routing/final funcional `053152ae51de3617bf30a4e9987574c7879e3049`. Documento canónico publicado en `a507eee7e69a5bed15226855098c0c0a28e7962e`.

**QA real:** el primer certificado `31836552560` dejó 6 pruebas API verdes y detectó que el harness levantaba API en 5006 mientras Angular consumía 5005; se corrigió sin alterar la app. El segundo `31836970704` confirmó el login y detectó que `provideRoutes(ALMACENES_ROUTES)` registraba Almacenes después del wildcard `**`; se corrigió a `provideRouter([...ALMACENES_ROUTES, ...routes])`. El certificado final `31837394309`, job `94886619205`, terminó `SUCCESS`: build `-warnaserror`, 376 tests backend, API+migraciones MySQL 8.4+health, npm ci/lint/build, Angular y Playwright `8 passed / 0 failed / 0 skipped`.

**Documentación/control:** fuente canónica `docs/ERP_N1_2_ALMACENES.md`; TASKS, CHANGELOG y tablero VAEP se reconcilian en N1.2.H. `main`, Producción, PR #2 merge/auto-merge, secretos y force-push permanecen intactos. **ERP-N1.2 queda formalmente cerrado** y el siguiente foco FINISH_FIRST es `N1.3.A — Ubicaciones internas / auditoría y preflight`.

## ERP-N1.1 — Sucursales empresariales

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** implementar y certificar el primer maestro de ERP-N1, `Sucursal`, de extremo a extremo: dominio/contratos, persistencia MySQL forward-only, API, RBAC relacional, auditoría, observabilidad, frontend responsive/accesible y QA específico. `EmpresaId` queda nullable como reserva de compatibilidad futura, sin FK ni semántica tenant antes de ERP-N6; Almacenes/Ubicaciones/Existencias permanecen en N1.2/N1.3/N1.4.

**Resultado funcional:** tabla `Sucursales` con código activo único mediante columna computada, índices de EmpresaId/estado, soft-delete y rollback fail-closed; API `/sucursales` con búsqueda/filtros/paginación, CRUD, activar/desactivar idempotente y baja lógica; `ModuloSistema.Sucursales=28` con grants persistidos `Ver/Crear/Editar/Activar/Desactivar/EliminarLogico`; auditoría `Entidad=Sucursal`; métricas P50/P95 de búsqueda sin término/PII; frontend Angular con lista server-side, estados loading/error/vacío, formulario, permisos runtime, tabla desktop/cards móvil y rutas protegidas.

**Trazabilidad:** B `0a576db21e583a76418ce037ca53f8c30d3b7eb1`; C persistencia `3ca70a8b41125ba501b9d94261e43d9dcd269df9` + snapshot `65785999934d8f02ffdf947fa24f48ceb9059076`; D aplicación/API `c511039680938fb758c60cf199a0c665462c7e79` + pruebas `805818140ef78183e52a17d196f36c452d39ebc2`; E `d3009e051ffea91631673dc764e56fdf8cab70b2`; F `9ead42f594aea12c20612d7c15e21768c090f828`; G base `704d451e216ab4a48042ae8bfaca5995d77e9cdb`; fix QA `b82c8d8325866fdf4408e22424fefe692965b8d9`; certificado G `42a241162dc54c8fddf040a7321d57dd229f7e5b`.

**Defecto descubierto por E2E:** el primer certificado dedicado `31829945647` creó correctamente la Sucursal pero encontró `HTTP 500` al filtrar Auditoría por `accion=Crear`; `AuditoriaRepository` usaba `enum.ToString()` dentro de LINQ, no traducible de forma segura por EF/MySQL. Se corrigió forward-only a `Enum.TryParse<TEnum>` + comparación tipada y filtro inválido fail-closed; además `AuditoriaRepository.cs` quedó incluido en los paths del workflow N1.1 para impedir regresión silenciosa.

**Validación real final:** workflow permanente `ERP-N1.1 - Certificación Sucursales`, run `31830346962`, job `94864277702`, `SUCCESS`: restore, build Release `-warnaserror`, unit tests, API, migraciones MySQL 8.4, health/ready, npm ci, lint, build producción, Angular, Chromium/Playwright y E2E específico. El E2E valida 401 anónimo, correlation ID, alta/normalización, duplicados, auditoría, filtros/paginación, idempotencia, edición sin mutar estado, reactivación, UI móvil sin overflow y soft-delete. M10 del frontend también quedó verde en `31829186290`.

**Documentación/control:** fuente canónica `docs/ERP_N1_1_SUCURSALES.md`; `TASKS.md`, CHANGELOG y tablero VAEP reconciliados. `main`, Producción, merge/auto-merge del PR #2, secretos y force-push permanecen intactos. **ERP-N1.1 queda formalmente cerrado** y el siguiente foco autorizado es `N1.2.A — Almacenes / auditoría y preflight`.

## 2026-08-14 — ERP-N0.8 Migraciones y limpieza — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar ERP-N0.8 después de consolidar el preflight de saneamiento, materializar la relación `Compras.MetodoPagoId`, reconciliar las FKs tipadas de `MovimientoInventario` con el modelo EF, retirar el raw SQL como autoridad normal de origen, migrar Compra hacia el catálogo relacional de métodos de pago y eliminar la lista hardcodeada de pagos del formulario de Compras. El saneamiento fue deliberadamente conservador: una columna legacy solo se retira físicamente cuando históricos, reversión y consumidores permiten demostrar que el DROP es seguro.

**Resultado funcional:** `Compras.MetodoPagoId` se backfillea por `MetodosPago.Codigo` estable —nunca por equivalencia de IDs— y queda protegido por FK; Compra crea/edita/confirma mediante catálogo activo y falla cerrado ante métodos no representables. El bridge legacy es one-way y bajo lock: una fila histórica válida con FK nula converge al catálogo antes de confirmar, sin convertir el enum en autoridad. `MovimientoInventario` persiste/consulta `CompraId`/`VentaId`/`ConsumoInsumoId`/`AjusteInventarioId` mediante EF; `ReferenciaTipo/ReferenciaId` quedan solo como snapshot/correlación. El frontend de Compras consume `/metodos-pago/activos`, muestra el nombre, envía el código estable y bloquea Guardar ante loading/error/0 métodos/inactividad.

**Persistencia/rollback:** migración `20260814155400_N0_8_PersistenciaLimpiezaTransicional`, postcheck `backend/scripts/postdeploy-erp-n0-8-c-persistencia.sql` y snapshot EF reconciliado. La migración es forward-only: el rollback seguro exige respaldo/restauración compatible o corrección forward; no se autoriza un DROP improvisado de la nueva FK. `Producto.Cantidad/Costo`, `Compra.MetodoPago`, `MovimientoInventario.ReferenciaTipo/ReferenciaId` y `MovimientoFinanciero.ModuloOrigen/ReferenciaId` permanecen únicamente donde cumplen una función histórica/snapshot/bridge demostrada, no como autoridad primaria.

**Trazabilidad A–G:** A `c7d39903eb978337d501a37c4d9c32b506c450f3`; B `c20151391d696ebe1d172ae3341e579cc371c35f`; C `b7b1db8746beac2a6e3f25c68afcafd8768383c8`; D cierre dirigido `633d8fc36e2b825a6362f418c01254c8886f37fe`; E `4693502282f54e3adfeee97669e0ca7ffa10b3ae`; G/funcional final `369158761ad05671b9a1859d17796c8ca4a09bf8`. La regresión específica `frontend/e2e/n0-8-compras-metodos-pago-regresion.spec.ts` cubre método administrable dinámico y catálogo no disponible fail-closed.

**Validación final sobre `369158761ad05671b9a1859d17796c8ca4a09bf8`:** CI principal `31821172124` SUCCESS completo; M10 `31821172381` SUCCESS; Fase 8 `31821172230` SUCCESS; aceptación integral `31821172223` SUCCESS incluido Playwright/SMTP/PDF; M13 `31821172341` SUCCESS completo incluido historial MySQL, integración, SQL forward, upgrade histórico, frontend, seguridad HTTP, Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS. No quedan P0/P1 conocidos atribuibles a ERP-N0.8.

**Documentación/control:** fuente final `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA.md`; preflight `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA_PREFLIGHT.md`; `TASKS.md`, CHANGELOG y tablero VAEP se reconcilian en N0.8.H. No se tocó `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas. El siguiente foco debe seleccionarse únicamente desde el gate/dependencias VAEP.

## 2026-08-14 — ERP-N0.7 AjusteInventario formal — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N0.7 después de completar el agregado `AjusteInventario`, persistencia/snapshots, API y frontend, RBAC, auditoría crítica, correlación HTTP, regresión y certificación. Durante N0.7.H se detectó que los endpoints legacy `ajustes-stock`, aunque ya tenían el permiso correcto, todavía conservaban `InventarioAjusteService` como segunda autoridad de mutación. El cierre se detuvo y la arquitectura se corrigió antes de certificar.

**Corrección final:** `InventarioAjusteService` queda como adaptador puro hacia `IAjusteInventarioService`; el servicio formal concentra la única autoridad de stock. La compatibilidad legacy crea y confirma el `AjusteInventario` dentro de una sola transacción, conserva `CantidadActualEsperada` como precondición comprobada bajo lock y falla cerrada antes de movimiento/mutación si la lectura del cliente está obsoleta. Confirmar/Anular mantienen auditoría `RegistrarEstrictoAsync` dentro de la misma transacción y movimientos con origen tipado `AjusteInventarioId`.

**Cadena correctiva H:** `554c9f24902e12388c00e8ca093aa29b533c2ac1`, `3416e47e811a2f7c7387bbdaf9964e745a0f6021`, `28a0fe5a945c2071fe160bd208ca9cfc4a07013d`, `d0bd3b18f092d189efea5ee69b229bce669387f5`, `f26b7513cfb34ce9a9be54202b2363c1f19e712c`, `6e17376837e13fb70960da7b523785f54c23b04b`, `7079263f86461bae136b509151da491d2b8bfcbe` y SHA funcional final `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`. El run sobre `7079263f...` reveló un test histórico que aún construía el adaptador con seis dependencias eliminadas; se corrigió forward-only en `cd5c1f05...`, sin ocultar el fallo.

**Validación final sobre `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`:** CI principal `31808933744` SUCCESS completo, incluida integración MySQL 8.4; aceptación integral `31808933692` SUCCESS completo, incluido Playwright/SMTP/PDF; M13 `31808933833` COMPLETED/SUCCESS, incluido backend/MySQL/migraciones/upgrade histórico, frontend, Docker/backup, secretos/dependencias, seguridad HTTP, runtime/Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS exigiendo todos los gates verdes.

**Documentación/control:** fuente canónica `docs/ERP_N0_7_AJUSTE_INVENTARIO.md`; `TASKS.md`, CHANGELOG y tablero VAEP quedan reconciliados. N0.7.A–H quedan cerrados y el siguiente foco FINISH_FIRST elegible es `N0.8.A`. No se tocó main, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas.

## 2026-08-13 — ERP-N0.6 Referencias polimórficas críticas — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N0.6 después de migrar la autoridad de origen de movimientos de inventario desde `ReferenciaTipo/ReferenciaId` hacia relaciones tipadas `CompraId`/`VentaId`/`ConsumoInsumoId`, preservando los campos legacy sólo como snapshots/bridge de transición. En finanzas se confirmó que `CompraId`/`VentaId`/`FacturaId` siguen siendo la autoridad y `ModuloOrigen/ReferenciaId` permanecen únicamente para auditoría/correlación.

**Resultado:** dominio tipado `Compra`/`Venta`/`ConsumoInsumo`; preflight y backfill fail-closed; C2/C3 y boundary typed-first; productores Compra/Venta/ConsumoInsumo migrados; contrato DTO/API tipado; frontend y nueva superficie RBAC marcados N/A por inspección dirigida; QA/regresión N0.6 cerrada sin crear pruebas redundantes. La fuente canónica final es `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS.md`; el preflight inicial permanece como antecedente histórico.

**Validación final sobre `0e35a9f75c49b6ddfbd5ef21d426521e2b559c40`:** ERP-N0.6 `31754907625` SUCCESS; Desarrollo build/tests `31754907682` SUCCESS; recovery MySQL `31754907598` SUCCESS; M11 backup/restore `31754907601` SUCCESS; Fase 8 `31754907626` SUCCESS; aceptación integral `31754907600` SUCCESS; M13 `31754907614` SUCCESS. Las pruebas críticas demuestran que la FK tipada manda aunque el snapshot legacy discrepe, que el bridge sólo cubre escritores legacy sin FK y que un mismatch tipado/legacy falla cerrado.

**Control:** N0.6.G y N0.6.H quedan cerrados, `TASKS.md` y VAEP se reconcilian y el siguiente foco FINISH_FIRST es N0.7.A — AjusteInventario formal / auditoría y preflight. No se tocó main, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas.

## 2026-08-13 — ERP-N0.5 MetodoPago — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N0.5 después de completar frontend/selectores, RBAC/auditoría, reportes/facturas/PDF, regresión integral, workflow dedicado y recertificación M13. Se crea `docs/ERP_N0_5_METODOS_PAGO.md` como documento canónico y se reconcilia `TASKS.md` contra la evidencia real de COLA/GitHub.

**Correcciones de recertificación:** N0.5.14 detectó incompatibilidad con MySQL administrado/Aiven cuando `sql_require_primary_key=ON`. Se reemplazaron snapshots temporales `CREATE TEMPORARY TABLE ... AS SELECT` por tablas explícitas con PK y tipos históricos exactos en `20260812023600_N0_5_BackfillMetodoPagoHistorico.cs` (`20b3c3b42c8dbeff884a71493d4e1f9b33ad2394`) y, como regresión transversal descubierta por M13, en `20260812083000_N0_6_OrigenTipadoMovimientoInventario.cs` (`1bbccd9cccdcc181ab8c1e842ea0ff8343831197`). No se alteró el significado funcional ni el backfill histórico.

**Validación real final sobre `1bbccd9cccdcc181ab8c1e842ea0ff8343831197`:** ERP-N0.5 `31753406161` SUCCESS; recovery MySQL/Aiven-like `31753406119` SUCCESS; M11 backup/restore `31753406267` SUCCESS; Desarrollo build/tests `31753406190` SUCCESS; aceptación funcional integral `31753406328` SUCCESS; M13 `31753406059`, attempt 2, SUCCESS. M13 cubrió historial desde cero con MySQL estricto, integración, SQL forward, upgrade representativo, preservación histórica, frontend, runtime/Playwright, SMTP/PDF, seguridad/auditoría, Docker y vigencia de backup.

**Documentación:** `docs/ERP_N0_5_METODOS_PAGO.md` documenta contrato canónico, códigos históricos estables, migraciones, backend/API, frontend, históricos/snapshots, trazabilidad N0.5.09–N0.5.14, CI y riesgos residuales. `TASKS.md` queda reconciliado con los puntos ya certificados.

**Control:** ERP-N0.5 queda funcionalmente cerrado; N0.5.15 completa el cierre documental. No se tocó `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas.

## 2026-08-12 — N0.5.08 Backend/API/CRUD/DTOs MetodoPago — LISTO

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar el backend administrable del catálogo relacional `MetodoPago` sin reintroducir el enum legacy como autoridad. Quedaron integrados DTOs, contratos e implementación de repositorio/servicio, API CRUD, activar/desactivar, reordenamiento, validación/canonicalización de metadata, DI, RBAC relacional y auditoría de mutaciones.

**Correcciones durante validación:** el CI inicial sobre `90fa101dca265c936f9007bf26209f903e24e4e3` detectó que los atributos runtime `MetodosPago:*` todavía no podían seedearse desde `CatalogoPermisosBase`; se incorporó el módulo con el mantenimiento completo en `b94aa0d9346f6efafe73b7911f07673ef07aceee`. Después se añadieron pruebas dirigidas del servicio en `016cfa1ff5712ad1d1e14d06f179de470d6a07c1`; dos fallos estrictamente de prueba —ambigüedad entidad/enum y analyzer xUnit sobre `DateTime` no nullable— se corrigieron forward-only en `d35030bfaa10018fa1a74b6e1efeca11d5cb5bd3` y `5827e610cf9cae1b6a3d5745d10e1cee59df6c78`.

**Cobertura dirigida:** crear normaliza código/canoniza metadata y audita; código duplicado falla cerrado sin persistir; editar registra usuario/auditoría; activar-desactivar preserva eliminación lógica; eliminar aplica trazabilidad; reordenamiento rechaza IDs duplicados antes de persistir. La prueba runtime de catálogo RBAC vuelve a garantizar que todos los permisos exigidos por controladores existen en el catálogo base.

**Validación real final:** ERP-N0.5 run `31650122695` terminó `SUCCESS` completo: restore/build/pruebas backend, esquema relacional, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF. El CI general `31650122667` terminó `SUCCESS` completo en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene.

**Control:** `N0.5.08` queda `LISTO` y habilita `N0.5.09`, `N0.5.10` y `N0.5.11` según dependencias de `COLA`. No se tocó `main`, Producción, merge/auto-merge de PR #2, force-push ni ramas nuevas.

## 2026-08-12 — VAEP v2.2 EXECUTION_TRUTH + CI sin push funcional de GITHUB_TOKEN — CONFIGURADO

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive + Programación.

**Problema detectado:** la corrida programada de las 13:02 terminó después de publicar la reparación EF, pero `RUNNER_MUTEX_STATE` quedó `RUNNING` con heartbeat 13:03, lo que podía confundirse con actividad real. Además, la reparación canónica de `N0.5.07B2` fue publicada por el workflow temporal `vaep-ef-snapshot-repair.yml` con `permissions: contents: write`; el HEAD `fc2ca060bbc7eefd84ead93ea370b292e3e200f2` quedó técnicamente actualizado, pero los workflows `pull_request` asociados aparecieron `action_required` y sin jobs, por lo que no constituyen evidencia de fallo funcional ni de CI ejecutado.

**Corrección de gobierno:** `PLAN_EJECUCION_AUTONOMA.md` evoluciona a VAEP v2.2 `EXECUTION_TRUTH`: el mutex deja de ser equivalente a actividad; se incorporan `RUNNER_ACTIVITY_STATE`, `RUNNER_LAST_REAL_ACTION_AT`, `STOP_REASON`, `RESUME_POINT` y un PRE-FINAL GATE obligatorio. Una respuesta final queda prohibida cuando la invocación todavía tiene capacidad, no hay CI activo y existe trabajo recuperable. Si la plataforma termina la invocación, debe declararse `IDLE_PLATFORM_LIMIT`/`WAITING_CI` según corresponda en vez de fingir ejecución continua.

**Corrección CI:** queda prohibido usar workflows temporales con `contents: write` para commitear/pushear cambios funcionales o migraciones mediante `GITHUB_TOKEN`. Actions podrá generar artefactos, pero la publicación final debe realizarla el Runner mediante el conector GitHub normal y fast-forward. `action_required` con jobs vacíos debe investigarse inmediatamente y no dejarse esperando hasta la siguiente hora.

**Continuidad B2:** `N0.5.07B2` continúa `VALIDANDO`; el snapshot/migración EF canónicos de Banco permanecen en `fc2ca060...`. El siguiente changeset operativo retira el workflow temporal escritor mediante el conector GitHub normal para provocar una sincronización ordinaria del PR y obtener CI real. No se toca `main`, Producción, merge/auto-merge de PR #2, force-push ni ramas nuevas.

## 2026-08-12 — VAEP v2.1 FINISH_FIRST: cerrar árbol foco antes de abrir hermanos

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive + Programación.

**Objetivo/alcance:** corregir la selección que permitía dejar `N0.5` parcialmente abierto mientras el runner avanzaba `N0.6`. Se alinea `PLAN_EJECUCION_AUTONOMA.md`, `CONFIG` y el prompt de `VariApp VAEP v2 Runner` para priorizar el punto padre más antiguo ya iniciado y terminar todos sus hijos/subhijos antes de abrir un hermano.

**Cambios de gobierno:** `MAX_MICROTAREAS_POR_CORRIDA=SIN_TOPE_FIJO`; `REGLA_BLOQUEO=NO_SALTAR_ARBOL_FOCO`; política `RUNNER_SELECTION_POLICY=FINISH_FIRST`; locks propios stale deben reconciliarse/recuperarse; padres deben reflejar estado de hijos; un bloqueo real conserva el foco y detiene la corrida en vez de saltarlo. `RUNNER_CURRENT_RECOVERY_TARGET=N0.5` congela nuevas aperturas de N0.6 hasta cerrar N0.5, preservando intacto todo lo ya certificado en N0.6.

**Evidencia:** protocolo versionado en commit `9efbfbe7d7d8a701d86b1aa60940321747c61783`; tablero CONFIG/BITACORA actualizado y Runner horario actualizado en sitio. Cambio exclusivamente documental/de gobierno, con `[skip ci]`; no modifica código funcional, main, Producción, PR #2, auto-merge ni ramas.

## 2026-08-12 — N0.6.D2B/D2: productores tipados cerrados — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** reconciliar y cerrar la cadena `N0.6.D2` después de certificar los tres productores documentales de `MovimientoInventario`, sin tocar `N0.5.07B/07B1`, contratos DTO/API ni persistencia adicional. Esta entrada supersede el estado operativo antiguo de D2A que quedó registrado abajo como `VALIDANDO`: la solución finalmente certificada fue el boundary `typed-first` del repositorio, no el intento incompleto de mapping EF.

**Resultado:** `D2A` quedó certificado mediante `6eadf19a27a0c7c90b0cec54262070f896209738` y CI `31587640123`; `D2B1` Compra mediante `e62b0667f4faace2d8d6520f753547b3e2624a1d` / pruebas `c76124980914edbea57ad7ff97eaa705171a2d58` / CI `31589093189`; `D2B2` Venta mediante `bac4d61b34813168b087fd7e9caf740a518c354a` / pruebas `06dea3390e0c40bef94e80f2e0ce30f482cac1f2` / CI `31589968458`; y `D2B3` ConsumoInsumo mediante `8648cc61f29a878d213ff2ddcce4e3731a81ff43` con correcciones de prueba hasta `ed570bb842ae4fbeb57b981bd596dfafbecf6072`.

**Validación real:** el CI general `31594243722` sobre `ed570bb842ae4fbeb57b981bd596dfafbecf6072` terminó `SUCCESS` completo en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. Los intentos previos `31593684786` y `31593975660` fallaron por defectos de prueba/build (`mapping EF` asumido y API `SqlQueryInterpolated` no disponible) y fueron corregidos sin modificar el servicio funcional; la verificación final usa ADO.NET contra MySQL real.

**Control:** `N0.6.D2B3`, `N0.6.D2B` y `N0.6.D2` quedan `LISTO`; `N0.6.D3` queda habilitada. `N0.5.07B/07B1` conserva su lock concurrente. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D2B1: productor Compra migra a origen tipado — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** migrar exclusivamente el productor de movimientos de inventario de Compras al boundary `typed-first` ya certificado en D2A, sin tocar Venta, ConsumoInsumo, EF, migraciones ni contratos HTTP. D2B se subdividió adaptativamente en D2B1 Compra, D2B2 Venta y D2B3 ConsumoInsumo para mantener un concern por changeset.

**Resultado:** `CompraService.ConfirmarAsync` y `AnularAsync` escriben mediante `IMovimientoInventarioRepository.AddConOrigenTipadoAsync` con `OrigenMovimientoInventario.DesdeCompra(compra.Id)`. La anulación usa `CausaMovimientoInventario.AnulacionCompra`, por lo que el repositorio deriva el snapshot legacy `CompraAnulada` sin recuperar autoridad desde `ReferenciaTipo/ReferenciaId`.

**Evidencia funcional:** `e62b0667f4faace2d8d6520f753547b3e2624a1d`. Pruebas dirigidas actualizadas en `c76124980914edbea57ad7ff97eaa705171a2d58`, comprobando confirmación y anulación con origen Compra tipado y ausencia de uso del `AddAsync` legacy en la confirmación.

**Validación real:** CI general `31589093189` terminó `SUCCESS` completo sobre `c76124980914edbea57ad7ff97eaa705171a2d58`: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene quedaron verdes; el job MySQL completó también verificación de variante legado, cargas y snapshot sin drift.

**Control:** `N0.6.D2B1` queda `LISTO`; habilita `N0.6.D2B2`. `N0.5.07B/07B1` conserva su lock concurrente y no fue intervenido. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D2A: mapear origen tipado de MovimientoInventario en dominio/EF — VALIDANDO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** incorporar al modelo de dominio y metadatos EF las columnas `CompraId`, `VentaId` y `ConsumoInsumoId` que C2/C3 ya crearon y certificaron físicamente. Esta microtarea no crea DDL nuevo ni modifica todavía los productores.

**Resultado:** `MovimientoInventario` expone las tres FKs nullable; `MovimientoInventarioConfiguration` las mapea hacia `Compra`, `Venta` y `ConsumoInsumo` con `DeleteBehavior.Restrict`, los nombres reales de constraints N0.6 y los índices existentes. Se añadió una prueba de metadatos EF para verificar propiedades y principales relacionales.

**Control:** estado `VALIDANDO` hasta CI real. Las columnas `ReferenciaTipo/ReferenciaId` permanecen como snapshot de compatibilidad y D2B sigue pendiente. No se tocó N0.5.07B/07B1, main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D1: repositorio y consultas de MovimientoInventario usan origen tipado — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** retirar `ReferenciaTipo/ReferenciaId` como autoridad decisoria en las consultas de inventario usadas por anulación de compras, manteniendo el fallback legacy únicamente para el provider InMemory de pruebas hasta que D2 migre productores.

**Resultado:** `MovimientoInventarioRepository` consulta `CompraId` en el provider relacional para localizar el movimiento original de compra y para determinar las claves de movimientos posteriores. La prueba de integración aislada fuerza desacuerdo entre el snapshot legacy y `CompraId` y demuestra que MySQL sigue la FK tipada.

**Evidencia funcional:** `2a2e093f66899b9c02c18026ecd3f270b6a730c1`. El primer CI general `31585321041` falló exclusivamente porque el fixture generaba un `NumeroCompra` más largo que la columna; el defecto de prueba quedó corregido en `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`, sin ocultar el fallo inicial.

**Validación real:** CI general `31585718867` terminó `SUCCESS` completo sobre `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. La integración dirigida `MovimientoInventarioOrigenTipadoIntegrationTests` quedó incluida en el job MySQL que finalizó en verde.

**Control:** `N0.6.D1` queda `LISTO`; habilita `N0.6.D2`. Los locks concurrentes `N0.5.07B/07B1` no se intervinieron. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.C3: postcheck, constraints e integridad histórica — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** cerrar la persistencia/migración N0.6 con postcheck y constraints de origen tipado, preservando el bridge transitorio desde `ReferenciaTipo/ReferenciaId` y sin retirar todavía las columnas legacy.

**Resultado:** después del primer intento de C3, el CI general detectó ocho integraciones incompatibles con una restricción demasiado amplia. La corrección final `01c1116e6db4e839b56176333251e3992fa09d77` acota la obligatoriedad del origen tipado a movimientos documentales mapeables (`Compra`, `Venta`, `ConsumoInsumo`) y permite que ajustes no documentales conserven temporalmente cero FKs tipadas. Se mantiene exclusividad, equivalencia con `ReferenciaId`, triggers transitorios de bridge y fail-closed frente a combinaciones inválidas.

**Evidencia funcional:** C3 evolucionó mediante `48ec0e9b9251e95522194e1580c0702a100e026c`, `e68184b2fccc9fd3e5e8c8950e261dce2d1c3e04` y corrección final `01c1116e6db4e839b56176333251e3992fa09d77`. El fallo inicial del CI general `31580565994` quedó corregido, no ocultado.

**Validación real:** CI general `31581993565` terminó `SUCCESS` en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. ERP-N0.6 `31581993553` terminó `SUCCESS`: preflight C1 fail-closed, historia representativa, aplicación C2/C3, integridad tipada, bridge legacy→FK, constraint permanente fail-closed y snapshot EF sin drift.

**Control:** `N0.6.C1/C2/C3` y el padre `N0.6.C` quedan `LISTO`. La siguiente tarea propia del punto es `N0.6.D`; no se inicia en esta corrida porque el usuario limitó la ejecución a una única tarea independiente. `N0.5.07B/07B1` mantiene lock concurrente y no fue intervenido. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.B: contrato de origen tipado de MovimientoInventario — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** introducir exclusivamente el contrato de dominio e invariante del origen tipado definido por el preflight N0.6.A, sin adelantar persistencia, configuración EF, backfill ni consumidores de N0.6.C/D.

**Resultado:** se añadieron `TipoOrigenMovimientoInventario` y el value object `OrigenMovimientoInventario`. El contrato representa `Compra`, `Venta` o `ConsumoInsumo`, expone el identificador tipado correspondiente y falla cerrado si no existe origen, existen varios orígenes o el identificador no es positivo. La operación concreta continúa separada en `TipoMovimientoInventario`/`CausaMovimientoInventario`; no se codifican anulaciones/reversiones en strings del origen.

**Pruebas dirigidas:** `OrigenMovimientoInventarioTests` cubre los tres orígenes admitidos, exclusividad del ID tipado, cero orígenes, múltiples orígenes e IDs no positivos.

**Evidencia funcional:** `5fe605cc93470a4f4b90f73185016b9e15bc622e`, publicado por fast-forward exclusivamente en `Desarrollo`.

**Validación real:** CI general run `31575657900`: `Backend Release y pruebas` terminó `SUCCESS`, incluyendo restore, build Release y pruebas backend no-integración; `Frontend producción`, `Higiene del repositorio` y `Docker y aislamiento de entornos` también terminaron `SUCCESS`. El job MySQL continuaba ejecutándose al cierre proporcional de B y no se usa como evidencia de cierre porque esta microtarea no modifica EF ni persistencia.

**Concurrencia/control:** `N0.5.07B/07B1` mantiene lock de otro runner y no fue intervenido. `N0.6.C` queda habilitada por dependencia; deberá añadir persistencia nullable, preflight/backfill/constraints/postcheck sin retirar aún las columnas legacy. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.A: preflight de referencias polimórficas críticas — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** auditoría dirigida del punto N0.6 sin cambios funcionales. Se confirmó que `MovimientoInventario` todavía usa `ReferenciaTipo + ReferenciaId` como autoridad de origen sin FK tipada y que esa pareja participa en la seguridad de anulación de compras. Los productores confirmados son compra/compra anulada, venta/venta anulada y consumo/reversión de insumos. El DTO/API de movimientos también expone el contrato legacy.

**Finanzas:** `MovimientoFinanciero` ya dispone de `CompraId`, `VentaId` y `FacturaId`; su configuración EF declara esas FKs como autoridad y conserva `ModuloOrigen/ReferenciaId` únicamente como snapshot de auditoría/correlación. N0.6 no debe deshacer esa migración ni eliminar snapshots antes de certificar históricos.

**Diseño de transición:** `N0.6.B` debe separar origen tipado de operación/reversión y preparar como mínimo `CompraId`, `VentaId` y `ConsumoInsumoId` en inventario. `N0.6.C` deberá añadir persistencia nullable de transición, preflight fail-closed sobre valores históricos, backfill determinista, constraints/postcheck y mantener columnas legacy hasta limpieza posterior segura en N0.8.

**Evidencia:** `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md` contiene alcance, archivos afectados, riesgos, rollback y matriz de validaciones. `TASKS.md` registra N0.6.A cerrado y la continuidad B→C→D–H.

**Validación real:** validación documental proporcional: inspección dirigida de entidades, EF, repositorio, productores Compra/Venta/ConsumoInsumo, DTO/servicio/API y finanzas; no se ejecutaron builds ni tests porque el changeset es exclusivamente documental/preflight y no modifica app, workflows, migraciones ni entorno. Publicación exclusivamente en `Desarrollo` con `[skip ci]` conforme a `AGENTS.md`.

**Concurrencia/control:** `N0.5.07B/07B1` estaba tomado por otro runner ChatGPT y no fue intervenido. `N0.6.A` es independiente de ese lock; el Plan Maestro declara N0.6 dependiente de N0.0. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.5.07A: elegibilidad Activo + preservación histórica — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** primer hijo de N0.5.07. Los resolvers de `VentaRepository`, `FacturaRepository` y `MovimientoFinancieroRepository` solo devuelven métodos `Activo && !Eliminado` para operaciones nuevas. El límite de persistencia financiera rechaza también una FK/navegación directa inactiva o eliminada. Las lecturas históricas no filtran la navegación, por lo que relaciones existentes continúan visibles tras desactivar el catálogo; las reversiones históricas conservan su relación original.

**Pruebas dirigidas:** `MetodoPagoElegibilidadRepositoryTests` cubre resolver activo/inactivo/eliminado en los tres consumidores, lectura histórica de un método inactivo y fail-closed de una nueva operación financiera con catálogo inactivo.

**Validación real:** CI general `31571200414` terminó `SUCCESS` en backend/pruebas, integración MySQL, Docker, frontend e higiene; ERP-N0.5 `31571200316` terminó `SUCCESS` completo. Evidencia funcional `11c958ead2a7a8cc5a3b1db4b502cbe63e8efba7`.

## 2026-08-11 — N0.5.06 C: MovimientoFinanciero migra a autoridad relacional — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** `MovimientoFinanciero.MetodoPago` legacy dejó de ser autoridad persistente/operativa sin adelantar N0.5.07. `IMovimientoFinancieroRepository` resuelve catálogo por código/nombre; todas las lecturas cargan `MetodoPagoCatalogo`; el límite de persistencia normaliza cualquier entrada legacy transitoria hacia `MetodoPagoId` y limpia el enum antes de guardar. La reversión pagada de compra copia exclusivamente FK/navegación relacional y falla cerrada si el original carece de relación. `FinanzasService` resuelve movimientos manuales contra catálogo y sus DTOs leen el nombre relacional.

**Pruebas dirigidas:** `FinanzasServiceTests` cubre persistencia/lectura relacional y fail-closed de método inexistente; `MovimientoFinancieroRepositoryTests` cubre normalización legacy→FK, carga de navegación y reversión sin propagación del enum.

**Evidencia funcional:** commit `0f14b9b9f5248a01cb6c98fa456cd306fe38ae19` publicado en `Desarrollo`. El temporal accidental `NOPE_DO_NOT_CREATE` fue eliminado de la punta efectiva mediante fast-forward, sin force-push.

**Validación real:** workflow dedicado `ERP-N0.5 - Certificación MetodoPago histórico`, run `31568099373`, terminó `success`: restauración/compilación/pruebas backend, esquema relacional, historia representativa, fail-closed, preflight, backfill histórico, postcheck/preservación 1:1 y snapshot EF quedaron verdes. El CI general run `31568099446` también terminó `success` en sus cinco jobs: Backend Release/pruebas, migraciones e integración MySQL, Docker, frontend e higiene.

**Control:** `N0.5.06C` y su padre `N0.5.06` quedan `LISTO`; con A1/A2/A3/B/C cerradas, la siguiente tarea de la cadena es `N0.5.07`, dependiente directamente de C.

## 2026-08-11 — N0.5.06 B: FacturaPago migra hacia MetodoPago relacional — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** `FacturaPago` dejó de usar el enum como autoridad operativa. `IFacturaRepository`/`FacturaRepository` resuelven métodos por código/nombre y cargan `MetodoPagoCatalogo` tanto en pagos como en la venta de origen. `FacturaService.RegistrarPagoAsync` resuelve el DTO temporal contra catálogo y persiste `MetodoPagoId`/navegación; el enum queda solo como proyección de compatibilidad derivada. Los DTOs de factura/pago leen el nombre desde el catálogo relacional.

**Evidencia funcional:** implementación hasta `d5e9a98c17848001fc64387c709a72ce0e379cd3`; fixtures relacionales ajustados en `e8ab2b733affea70ba47b3ea8a7ff450c6b7766f`; cierre resumido en `c53a99150520d25b3a91d4e8aee7d3c6003ccd97`.

**Validación real:** CI general run `31567189353` completó `success` en Backend Release/pruebas, migraciones MySQL, Docker, frontend e higiene. Workflow dedicado ERP-N0.5 run `31567189393` completó `success` en backend, esquema, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF.

**Control:** el Sheet estaba rezagado en `VALIDANDO` y fue reconciliado contra GitHub. El siguiente punto elegible de la cadena es `N0.5.06C`, retiro de autoridad legacy en `MovimientoFinanciero`.

## 2026-08-11 — N0.5.06 A3: lectura y propagación de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A3 `LISTO`. El commit funcional `c024cc7c96da45f6d2b21867950de3c4dce49fd4` eliminó el uso de `Venta.MetodoPago` como autoridad de lectura/propagación dentro de `VentaService`: `VentaDto.MetodoPago` se obtiene desde `MetodoPagoCatalogo.Nombre` y el `MovimientoFinanciero` automático creado al confirmar una venta recibe `MetodoPagoId`/`MetodoPagoCatalogo`; su enum legacy se deriva únicamente del catálogo cuando existe.

**Pruebas dirigidas:** `05687cffcf9d34b3fdd8efd9becf9d158b61f028` añadió cobertura para comprobar que el DTO usa el nombre del catálogo aunque el enum legacy difiera y que la confirmación propaga FK/navegación al movimiento financiero sin copiar el enum legacy de Venta.

**Validación real:** CI general run `31566541771`: job `Backend Release y pruebas` completó `success`, incluyendo restore, build Release y pruebas backend no-integración; `Frontend producción`, `Higiene del repositorio` y `Docker y aislamiento de entornos` también completaron `success`. El job MySQL seguía ejecutándose al cierre operativo de A3, por lo que no se atribuye un resultado aún no finalizado. El workflow dedicado ERP-N0.5 run `31566541808` fue generado para el mismo SHA y continuaba su certificación histórica.

**Control:** A3 no modifica `FacturaPago` ni el servicio financiero general. El siguiente punto de la cadena es B, que debe retirar la autoridad enum de `FacturaPago` y sus DTOs/flujos sin ampliar todavía reglas operativas de N0.5.07.

## 2026-08-11 — N0.5.06 A2: escrituras de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A2 `LISTO`. El commit funcional `32feca8840122c7eccd58246a6db7196730d8491` migró `VentaService.CreateAsync/UpdateAsync`: el texto temporal del DTO se resuelve contra el catálogo persistente mediante `IVentaRepository.GetMetodoPagoPorCodigoONombreAsync`, se establecen `MetodoPagoId` y `MetodoPagoCatalogo`, y el enum legacy queda únicamente como proyección de compatibilidad derivada. Un método inexistente o vacío produce `BusinessRuleException`; ya no existe fallback silencioso de método desconocido a `Efectivo`.

**Pruebas dirigidas:** `e00e20c614c8c66c34f726c82ef4922d48dc21d8` añadió `VentaMetodoPagoServiceTests` para creación con FK/navegación, rechazo de método inexistente y actualización hacia catálogo relacional.

**Validación real:** workflow `ERP-N0.5 - Certificación MetodoPago histórico` run `31566179324` completó finalmente `success`: restore/build/tests backend, esquema relacional, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF quedaron verdes. CI general run `31566179269` fue generado para el mismo SHA; Docker e higiene estaban `success` durante el cierre operativo.

**Control:** A2 no cambia migraciones ni contratos HTTP; `CreateVentaDto/UpdateVentaDto.MetodoPago` sigue siendo adaptador string temporal. N0.5.06 no está cerrado: A3 migra lectura de `VentaDto` y propagación automática hacia `MovimientoFinanciero`.

## 2026-08-11 — Cierre N0.5.06 A1: repositorio Venta preparado para MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A1 `LISTO`. El commit funcional `d987cb669de6dfbd00b8691a46e27f566e32138c` añadió resolución de `MetodoPago` por código/nombre en `IVentaRepository`/`VentaRepository`, carga `MetodoPagoCatalogo` en lecturas operativas y carga explícita de la navegación en `FOR UPDATE`.

**Validación real:** en el CI general run `31563809556`, el job `Backend Release y pruebas` completó `success`, incluyendo restore, build Release y pruebas backend no-integración; frontend, higiene y Docker también completaron `success`. El workflow dedicado `ERP-N0.5 - Certificación MetodoPago histórico`, run `31563809580`, completó su job `metodo-pago-historico` en `success`: backend, esquema relacional, historia representativa, fail-closed, preflight, backfill histórico, postcheck/preservación 1:1 y snapshot EF quedaron verdes.

**Continuidad:** N0.5.06 no está cerrado. El siguiente punto elegible de esta cadena es A2: migrar escrituras de `VentaService` hacia `MetodoPagoId`/catálogo. A3, FacturaPago y MovimientoFinanciero continúan después según dependencias VAEP.

## 2026-08-11 — N0.5.06 A1: preparar Venta para autoridad relacional de MetodoPago

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** iniciar la eliminación de la doble autoridad de métodos de pago con un changeset pequeño y coherente, preparando el repositorio de Venta para que las siguientes microtareas puedan resolver y leer el catálogo relacional sin depender del enum legacy.

**Granularización VAEP:** el punto original N0.5.06 cruzaba Venta, FacturaPago y MovimientoFinanciero y resultó demasiado amplio. Se subdividió en A1 repositorio/carga relacional de Venta, A2 escrituras de Venta, A3 lecturas/propagación de Venta, B FacturaPago y C MovimientoFinanciero. N0.5.07 depende del cierre de C.

**Alcance funcional de A1:**

- `IVentaRepository` expone resolución de `MetodoPago` por código/nombre;
- `VentaRepository` carga `MetodoPagoCatalogo` en consultas operativas normales;
- la lectura transaccional `FOR UPDATE` carga explícitamente la navegación `MetodoPagoCatalogo`;
- se añade resolución dirigida contra el catálogo persistente excluyendo registros eliminados;
- no se cambia todavía el DTO/API, las reglas `Activo/RequiereReferencia/...`, ni se retiran físicamente columnas legacy.

**Validación previa real:** se verificaron de forma dirigida `Venta`, `FacturaPago`, `MovimientoFinanciero`, sus configuraciones EF, `VentaService`, `FacturaService`, `IVentaRepository` y `VentaRepository`. La revisión confirmó que `VentaService` todavía usa el enum como autoridad en creación/edición y que N0.5.06 debía dividirse antes de modificarlo. El build/CI se ejecutará sobre el commit funcional publicado; no se declara éxito de CI en esta entrada antes de que GitHub lo reporte.

**Riesgo/control:** A1 es infraestructura preparatoria y no declara N0.5.06 cerrado. Las escrituras y lecturas de `VentaService` siguen pendientes en A2/A3; GitHub/CI determinarán si A1 puede marcarse `LISTO`.

## 2026-08-11 — VAEP-001: reducir ejecuciones CI redundantes en certificaciones ERP-N0

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** evitar que los workflows históricos de certificación ERP-N0.2, N0.3, N0.4 y N0.5 consuman CI ante cambios exclusivamente frontend/documentales/no relacionados, sin reducir cobertura cuando cambien backend, tests, scripts propios o el workflow correspondiente.

**Alcance:** se añadieron filtros `paths` al evento `push` de `.github/workflows/erp-n0-2-ci.yml`, `erp-n0-3-ci.yml`, `erp-n0-4-ci.yml` y `erp-n0-5-ci.yml`, alineándolos con sus filtros de `pull_request`. `workflow_dispatch` permanece intacto y el CI general `desarrollo-ci.yml` no se reduce.

**Validación real:** el commit funcional `d2466a3047e7cd2001f1cf998faa08c4ae229c1b` fue publicado por fast-forward sobre `Desarrollo`. GitHub aceptó los cuatro YAML y generó ejecuciones `push`/`pull_request` para los workflows modificados; por ejemplo ERP-N0.2 run `31562526962` y ERP-N0.5 run `31562526984` fueron creados sobre el mismo SHA. El diff confirma que el cambio funcional se limita a los filtros `paths` de `push`; N0.1 ya estaba filtrado. Los jobs de certificación seguían ejecutándose al momento del cierre documental, por lo que no se atribuye un resultado funcional de esas suites que todavía no había concluido.

**Resultado:** `VAEP-001` queda `LISTO` porque el objetivo de trigger fue implementado y aceptado por GitHub. Los futuros pushes exclusivamente frontend/documentales/no relacionados dejan de disparar estas cuatro certificaciones históricas; cambios en backend, tests, scripts propios y los propios workflows siguen cubiertos.

## 2026-08-11 — VAEP v2: Plan Maestro ERP V5 completo + cola granular

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive.

**Objetivo:** convertir el Plan Maestro ERP V5 en una ejecución autónoma integral, granular y auditable, evitando changesets gigantes y permitiendo continuidad cuando exista un bloqueo independiente.

**Alcance:** importación del Plan Maestro a Drive, tablero VAEP v2, ERP-N0→N9, gates, T0–T12, backlog futuro no-core, 778 microtareas, granularización adaptativa y bloqueo transitivo.

**Validación real:** fuente rectora y tablero verificados; sin cambios productivos.

## 2026-08-11 — VAEP v1: ejecución autónoma, Drive y dependencias

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

Estados estrictos, selección por prioridad/dependencias, lock lógico y bloqueo no global.

## 2026-08-11 — Gobierno colaborativo v2

Gate `PROJECT_ID=VARIAPP`, aislamiento entre proyectos, lectura mínima, evidencia obligatoria y hardening de publicación.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

Creación/alineación de memoria canónica y reglas de continuidad.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

Migración, seed idempotente, backfill, preflight/postcheck y workflow N0.5 certificados.

## 2026-08-11 — Catálogo público VARISTOREHN

**Responsable:** Codex. Consulta pública segura y personalización pública.

## Formato futuro

Cada entrada debe contener fecha, agente, objetivo, alcance, validaciones reales, riesgos/pendientes y commit cuando sea útil. No registrar secretos ni datos sensibles.
## 2026-08-12 - N0.5.07B2 - snapshot EF canonico de Banco - VALIDANDO

**Responsable:** ChatGPT / VAEP v2 Runner.

**Correccion:** los CI 31622173253 y 31622173357 demostraron que la logica Banco/fail-closed y las pruebas pasaban, pero dotnet ef migrations has-pending-model-changes detecto drift porque la migracion inicial de Banco no actualizo el snapshot EF canonico. Se reemplaza esa migracion manual por una migracion generada con EF Core 8.0.8, su Designer y AppDbContextModelSnapshot, sin relajar ninguna validacion ni alterar el diseno normalizado.

**Control:** B2 permanece VALIDANDO hasta que los CI reales sobre el changeset canonico terminen en verde. No se toca main, Produccion, PR #2, auto-merge ni ramas nuevas.

## 2026-08-24 — ERP-N3.3 Reserva automática de inventario — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25 mediante PARENT CLOSURE GOVERNOR y ATOMIC_PARENT_PUBLISH documental.

**Objetivo/alcance:** cerrar formalmente N3.3.A-H sin reabrir código funcional. La confirmación de `PedidoVenta` reutiliza `ReservaInventario` y la autoridad física `ExistenciaVariante`; la reserva compromete `StockReservado` sin mover `StockFisico` por el mero acto de reservar y no introduce una segunda autoridad cuantitativa ni selección automática inventada de almacén/ubicación.

**Evidencia:** baseline funcional `960ac07ed1e96d1d2e98a51fdb5dc216fbc8d0f3`; N3.3.D/E/F/G ya estaban `LISTO` en COLA, la regresión E2E `reservation-automatic-flow.spec.ts` fue aceptada por el control VAEP y P0/P1 bloqueantes conocidos atribuibles a N3.3=0. Los fallos de workflows legacy ERP-N0 observados en paralelo no se usan como gate causal sin evidencia directa.

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

## 2026-08-26 — ERP-N3.7 Nota de crédito de cliente — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.7.A-H sin reabrir código funcional ni inventar semánticas fiscales, stock, Kardex, caja o downstream no certificadas. `NotaCreditoCliente` conserva el alcance y contratos ya certificados por N3.7.A-G.

**Evidencia:** N3.7.A Issue #752 `LISTO_REAL`; N3.7.B `46a250fcc0cfd1562306538375e772a94c39bea5`; N3.7.C `9810cf2e7fd0289a9374a8477a4131f3f73fef38`; N3.7.D `8bcacae8a45fe3c0072bf519610bcc1ec1203a4f`; N3.7.E `f9ef582749a79c8900741d1a40ff393039c7b287`; N3.7.F `943aa0e607af3221ed8987a0edac37a539561696`; N3.7.G Issue #781 `LISTO_REAL`. Los gates causales y P0/P1 atribuibles de esos padres quedaron certificados en sus cierres.

**Cierre documental/control:** `TASKS.md` ya contiene el rollup N3.7 y esta publicación agrega únicamente este bloque a `CHANGELOG_AI.md`, preservando byte por byte todo el blob source `d53c56416ac7ac01beef761adab5172cf5297487` y sin eliminar ni reformular historia previa. Issue #782 es el control de cierre; PR #2 permanece Draft `Desarrollo → main`, sin merge. P0/P1 atribuibles conocidos al cierre: 0.

**Promoción:** con esta publicación N3.7.H queda formalmente `LISTO`; el selector fail-closed puede promover `N3.8.A` y mantener N3.8.B como pipeline SAFE según dependencias.

## 2026-08-26 — ERP-N3.8 Nota de débito de cliente — CIERRE CONDICIONAL/N/A

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor.

**Dictamen:** N3.8.A-H se cierra para el alcance actual como N/A con evidencia porque el roadmap condiciona la Nota de débito a una necesidad legal/operativa y no existe todavía requisito autoritativo suficiente para fijar su contrato. No se afirma que `NotaDebitoCliente` haya sido implementada.

**Evidencia:** A=`034ec3305422016d6c571d0ffcf1332e3bbbe6b6`; B=`affb58f2b9e7d8ab25c051fed5b9f4ee5f317584`; C-G=`3a89725e4a76c4d85c0c4adc04f0affa4a61e79a`; certificación=`docs/CERTIFICACION_N3_8_NOTA_DEBITO_CLIENTE.md`. Delta funcional=0 y P0/P1 atribuibles conocidos=0.

**Reapertura:** si legislación/operación exige esta capacidad, reabrir desde N3.8.B con contrato explícito antes de dominio/persistencia/API/UI. El selector puede promover `N3.9.A`.

## 2026-08-26 — ERP-N3.9 Cuentas por cobrar — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.9.A-H con base en hechos certificados. La proyección Cuentas por cobrar está implementada como una vista de solo lectura (GET /cuentas-por-cobrar) sobre la verdad operativa de Factura y FacturaPago, reutilizando el control RBAC existente (Facturacion/Ver) sin introducir libros contables mutables, esquemas propios, endpoints de escritura, lógica de mora/anticipos, ni nuevos permisos.

**Evidencia:** N3.9.A-G están formalmente `LISTO_REAL`. La certificación canónica documental reposa en `docs/CERTIFICACION_N3_9_CUENTAS_POR_COBRAR.md`. P0/P1 atribuibles conocidos al cierre: 0.

**Promoción:** con esta publicación, N3.9.H queda formalmente `LISTO`. El selector fail-closed puede promover el siguiente padre `N3.10.A`, respetando el bloqueo que impedía avanzar antes del cierre de H.

## 2026-08-27 — ERP-N3.10 Crédito del cliente — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.10.A-H con base en las autoridades certificadas, manteniendo la capacidad de crédito integrada a Cliente. Este cierre no introduce una segunda autoridad comercial, motor autónomo de scoring, ledger paralelo, nuevos permisos RBAC ni efectos automáticos adicionales sobre venta, factura, stock, Kardex, caja o contabilidad.

**Evidencia:** N3.10.C=`619a0ba2a53ad70fb332c9f61198eb3b022ddcc1`; N3.10.D=`3c5a2c30a3d8427d0d0764ef1d4bc4e895d4d585`; N3.10.E=`615d1a4878854bf22770b945256db39fea44e08f`; N3.10.F/G=`98b7777555cd6f7ee881edb76321cd1226ca69eb`; certificación canónica=`docs/CERTIFICACION_N3_10_CREDITO_CLIENTE.md`. Los gates causales aplicables de estas autoridades están certificados y P0/P1 atribuibles conocidos al cierre=0.

**Cierre documental/control:** este bloque y el rollup paralelo de `TASKS.md` son exclusivamente aditivos. Todo el contenido histórico previo de ambos archivos debe permanecer byte-prefix intacto; PR #2 continúa Draft `Desarrollo → main`, sin merge.

**Promoción:** con esta publicación `N3.10.H` queda formalmente `LISTO_REAL`; el selector fail-closed puede promover `N3.11.A` y mantener N3.11.B/C como pipeline SAFE según dependencias.

## 2026-08-27 — ERP-N3.11 POS / Venta rápida — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.11.A-H con base en las autoridades certificadas, reutilizando la autoridad existente de Venta para el alcance de venta rápida. La experiencia existente (ventas/nueva) provee la funcionalidad requerida sin introducir una segunda superficie POS independiente en dominio, persistencia, API, frontend o permisos.

**Evidencia:** N3.11.A-G están certificados para el alcance vigente (LISTO_REAL / QA_TAKEOVER_CERTIFIED). Certificación canónica = `docs/CERTIFICACION_N3_11_POS.md`. P0/P1 atribuibles conocidos al cierre: 0.

**Decisiones pendientes:** cashier/session/terminal, split-tender/change, suspension/reprint, offline, POS-specific idempotency y POS-specific RBAC quedan explícitamente como DECISION_PENDING. No se materializan en el producto hasta un requisito autoritativo futuro.

**Cierre documental/control:** este bloque es exclusivamente aditivo sobre el histórico existente. PR #2 continúa Draft `Desarrollo → main`, sin merge.

**Promoción:** con esta publicación, N3.11.H quedará formalmente LISTO (TARGET_AFTER_PUBLICATION).

## 2026-09-03 — ERP-N4.4 Cuentas por cobrar — CIERRE DOCUMENTAL

Responsable ChatGPT/VAEP v3.25 Closure Governor / Jules A; objective finalización lógica N4.4 A-G; evidence A-F LISTO_REAL and G LISTO_REAL with docs/CERTIFICACION_N4_4_CUENTAS_POR_COBRAR.md and docs/RUNBOOK_N4_4_CUENTAS_POR_COBRAR.md; closure H is NOT declared LISTO_REAL.

## 2026-09-03 — ERP-N4.5 Cuentas por pagar — ROLLUP DOCUMENTAL

Responsable Codex local autorizado; objetivo reconciliar de forma aditiva la certificación N4.5 sin duplicar autoridad financiera ni declarar prematuramente el cierre H. N4.5.A-G quedan respaldadas por `docs/CERTIFICACION_N4_5_CUENTAS_POR_PAGAR.md`, reutilizando la autoridad ERP-N2.8 y el baseline funcional `541ec12b72912c769c6f54b8821771e509818375`. El HEAD posterior contiene únicamente documentación y manifests VAEP/Jules, sin delta productivo. N4.5.H permanece `EN_PROGRESO` hasta obtener gates exact-head terminales y revalidar P0=0/P1=0; `N4.6.A` continúa `PREARMED/PROMOTION_HELD`.

Reconciliación QA append-only: este rollup supersede únicamente el estado operativo stale anterior del bloque N4.4; las menciones históricas `CURRENT_PARENT=N4.4.H` permanecen como evidencia histórica, ya no gobiernan el estado actual y no se reescribe ni elimina ninguna historia previa. El estado operativo vigente de este cierre es `CURRENT_PARENT=N4.5.H`. Certificación documental exacta: commit `fde578dd69cbfe91c054138d33404cec342093f6`; `productBaseHead=541ec12b72912c769c6f54b8821771e509818375`. N4.5.H no se declara `LISTO_REAL` en este rollup.

## 2026-09-03 14:32:52 -06:00 - Optimización controlada CI/CD y Vercel

Responsable: Codex local autorizado en `Desarrollo`; HEAD inicial `1cf6847e4e70e2fe99ef5ec59b57c33f4f7c49d3`.

Alcance: auditoría de los 43 workflows existentes, sin eliminar workflows ni evidencias. `erp-n0-2-ci.yml`, `erp-n0-3-ci.yml`, `erp-n0-4-ci.yml` y `erp-n0-5-ci.yml` quedaron clasificados como certificación histórica y sus triggers genéricos `backend/src/**`/`backend/tests/**` fueron acotados a artefactos, migraciones, pruebas, scripts y workflows propios; todos conservan `workflow_dispatch`. `erp-n0-6-preflight-ci.yml` fue revisado y no modificado porque ya usa paths específicos. `desarrollo-ci.yml` ahora escucha solo áreas técnicas backend/frontend, configuración de build, migraciones e infraestructura asociada; la documentación, VAEP, bitácoras y evidencias no disparan el CI pesado. `vaep-jules-diagnostic.yml` conserva su no-op por ausencia de manifest y ahora filtra PR por `vaep/jules/diagnostic/*.json` y su propio workflow.

Playwright: se agregó `actions/cache@v4` para `~/.cache/ms-playwright`, versionado mediante `hashFiles('frontend/package-lock.json')`, a los 12 workflows que ejecutan `npx playwright install --with-deps chromium`. Se conserva `--with-deps` para las dependencias Linux; no se afirma ahorro cuantitativo sin medición histórica comparable. No se implementó build-once entre jobs porque los jobs de base e integración tienen restores, bases MySQL y artefactos separados; hacerlo en este cambio ampliaría el riesgo.

Vercel: `frontend/vercel.json` usa un `ignoreCommand` local fail-open que solo omite build cuando la comparación Git confirma que todos los archivos son documentación, gobierno, `.github` o `vaep`; cualquier cambio frontend, backend o no clasificado fuerza build. No se modificó el dashboard ni la separación externa de ramas/proyectos (`variapp-desarrollo` para `Desarrollo`, `varistorehn` para producción); esa configuración queda pendiente de verificación/ajuste externo seguro.

Validaciones realizadas: `git diff --check` sin errores; inspección de diff/stat/status; comprobación de que los workflows históricos conservan `workflow_dispatch`; comprobación estática de que los filtros N0.2-N0.5 ya no contienen los comodines genéricos; comprobación de los 12 caches Playwright y de la conservación de sus instalaciones; sintaxis JavaScript del script Vercel y pruebas de comportamiento fail-open/path-based con SHA document-only y SHA con cambios de código. No se ejecutaron builds o suites completas porque el changeset solo modifica workflows/configuración de CI y Vercel.

Riesgos y pendientes: GitHub Actions debe confirmar en el siguiente run que los filtros coinciden con los paths reales; Vercel debe validar desde cada proyecto que el `ignoreCommand` se ejecuta con el root esperado. La optimización no elimina certificaciones históricas ni sus pruebas manuales, y no demuestra por sí sola un ahorro medido. Sin cambios a `main`, Producción, secretos, bases productivas, dominios, deploys ni PR #2.

## 2026-09-03 15:07:51 -06:00 - RCA P0 Vercel y aislamiento Desarrollo/Producción

Responsable: Codex local autorizado en `Desarrollo`; HEAD inicial de esta auditoría `2658d5b0139e85957463cb227f11ea65f42bef13`. La consulta read-only del equipo `VariApp` confirmó dos proyectos Vercel vinculados al mismo repositorio GitHub `jmejia31/VariApp`: `variapp-desarrollo` (`prj_JkRGpdSnGlMQ4Qc3eqw4bscY4Flu`) y `varistorehn` (`prj_djMCand2yYeY3AvaUWsjwHDDJDkM`).

RCA confirmado: `varistorehn` acepta pushes de `Desarrollo` mediante Git Integration y crea deployments de fuente `git` con `githubCommitRef=Desarrollo`, `target=null` y alias `varistorehn-git-desarrollo-vari-app.vercel.app`. Ocurrió para `eaacb832dfc78723ad9cb7d119d88a32c62a0047` y nuevamente para `2658d5b0139e85957463cb227f11ea65f42bef13`. Por tanto, fijar solamente `Production Branch=main` no basta: debe deshabilitarse la creación de Preview Deployments de `Desarrollo` en el proyecto `varistorehn` mediante la configuración de Git Integration/Preview Branches, conservando producción en `main`. El conector read-only no expone ni permite editar esos campos; no se realizó cambio externo.

Estado observado: para `2658d5b...`, `Vercel - variapp-desarrollo` quedó `FAILURE` con `Deployment rate limited - retry in 24 hours`; `Vercel - varistorehn` quedó `SUCCESS`. `variapp-desarrollo` sí generó su deployment para `Desarrollo` (`target=production`, alias `variapp-desarrollo-git-desarrollo-vari-app.vercel.app`), consistente con el diseño documentado. La duplicación real de builds/deployments quedó probada en ambos proyectos.

La configuración local `frontend/vercel.json` y `frontend/scripts/vercel-ignore-build.mjs` no se modificó en esta toma: JSON y JavaScript válidos; pruebas del ignore: solo documentación=`exit 0`, frontend/runtime=`exit 1`, diff no clasificable=`exit 1`. El mecanismo es fail-open y basado en paths, pero no puede impedir que un segundo proyecto Git cree el deployment antes de evaluar el ignore; además, un deployment cancelado puede seguir consumiendo cuota.

Ajuste externo pendiente, seguro y reversible: en `varistorehn`, confirmar `Production Branch=main`, desactivar Preview Deployments para la rama `Desarrollo` (o excluir explícitamente `Desarrollo` en la regla de ramas de preview), conservar root `frontend` y no cambiar dominio, secrets ni producción. En `variapp-desarrollo`, confirmar `Production Branch=Desarrollo`, root `frontend` y que sus previews/runtime apunten únicamente a Desarrollo. La aplicación debe registrar estado antes/después y no usar deploy manual. No se relanzaron workflows, no se cambió `main`, no se tocó N0.2-N0.5, y no se modificó el commit concurrente N4.6.B.

## 2026-09-03 15:33:53 -06:00 - Certificación read-only del aislamiento Vercel

Responsable: Codex local autorizado; HEAD inicial `e5d48ef2f5dfdfabe0866f957beef4b744f9ac33`, repositorio `jmejia31/VariApp`, rama `Desarrollo`. El preflight confirmó árbol limpio, `HEAD=origin/Desarrollo` y no hubo fast-forward adicional.

Ajuste externo manual reportado y verificado por el operador: `varistorehn` queda con Production Branch/tracking `main` y Preview/Avance `Disabled`; `variapp-desarrollo` queda con Production Branch/tracking `Desarrollo` y Preview/Avance `Disabled`. Codex no modificó Vercel, no hizo deployment, rollback, cambio de Git Integration, dominio ni secreto.

Evidencia read-only: el equipo `VariApp` mantiene exactamente los proyectos `variapp-desarrollo` (`prj_JkRGpdSnGlMQ4Qc3eqw4bscY4Flu`) y `varistorehn` (`prj_djMCand2yYeY3AvaUWsjwHDDJDkM`), ambos vinculados a `jmejia31/VariApp`. Desde el commit documental anterior `e5d48ef2` no aparecen deployments nuevos en ninguno. El último evento de `varistorehn` es el Preview de `Desarrollo` cancelado por `Ignored Build Step`; no existe evidencia posterior que contradiga el ajuste manual.

RCA previo: antes del ajuste, ambos proyectos reaccionaban al mismo repositorio y `varistorehn` creaba Preview Deployments desde `Desarrollo`; el ignore podía cancelar el build, pero el deployment ya consumía cuota. El estado manual actual asigna `Desarrollo` únicamente a `variapp-desarrollo` y `main` únicamente a `varistorehn`, desactivando Preview tracking automático en ambos proyectos.

Limitación de certificación: el conector read-only no expone los campos Production Branch/Preview Tracking y todavía no existe un push legítimo posterior al cambio manual. Por ello, la configuración final se registra como verificada manualmente por el operador y respaldada por ausencia de deployments posteriores, pero la prueba natural definitiva queda pendiente del próximo push legítimo de `Desarrollo`. El resultado esperado es deployment solo en `variapp-desarrollo` cuando corresponda y ningún Preview en `varistorehn`.

El estado `build-rate-limit` queda documentado como limitación previa de `variapp-desarrollo`; no se relanzó ningún workflow/deployment para intentar evadir la cuota. No se modificaron `main`, PR #2, Producción, secretos, BD, dominios, N0.2-N0.5, `frontend/vercel.json` ni `frontend/scripts/vercel-ignore-build.mjs`.

## 2026-09-03 16:10:00 - N4.6.C persistencia de plan de cuentas

Responsable: Codex local autorizado sobre `a85cb962`; la implementación posterior de C en `Desarrollo` fue reconciliada desde los commits concurrentes hasta `f3f92b4b`. La persistencia EF de `CuentaContable` quedó materializada con configuración jerárquica, FK restrictiva, restricciones, índices, migración compatible con MySQL 8.4 y snapshot efectivo.

Evidencia local de la revisión inicial: Infrastructure/API compilaron y las pruebas dirigidas de persistencia pasaron. El controller de cierre registró posteriormente N4.6.C como `LISTO_REAL` en el handoff autoritativo de GitHub, con CI exact-head terminal y P0/P1=0; Codex no sustituye esa evidencia ni duplica el changeset concurrente.

## 2026-09-03 16:35:00 - N4.6.D backend API — candidato local

Responsable: Codex local autorizado; base reconciliada `f3f92b4b`. Se integró y revisó el contrato de DTO/servicio de CuentaContable y se añadió la superficie HTTP protegida de lectura jerárquica, raíces, creación y actualización bajo `ModuloSistema.Finanzas`, reutilizando el repositorio y excepciones existentes. La lectura construye el árbol desde el conjunto completo para no truncar subcuentas profundas.

Validación local: `dotnet test --filter FullyQualifiedName~CuentaContable` PASS (5/5). El artifact Jules D no se integró: su review autoritativo lo rechazó por patch anidado, stubs y pruebas RBAC insuficientes. N4.6.D no se declara `LISTO_REAL` hasta completar security/QA/CI exact-head y revisión del controller; no se afirma evidencia no ejecutada.

## 2026-09-03 17:20:00 - N4.6.E frontend del plan de cuentas

Responsable: Codex local autorizado; se añadió la feature standalone `PlanCuentasComponent`, su modelo/servicio HTTP y la ruta protegida `/plan-cuentas`. La UI presenta el árbol completo, alta/edición, selección de padre sin descendientes cíclicos, tipo, estado, aceptación de movimientos, loading/empty/error states, responsive y navegación bajo permisos de Finanzas. La autoridad de validación permanece en el backend.

Validación real: `npm.cmd run lint` PASS; `npm.cmd run build -- --configuration development` PASS. El build de producción no se pudo completar porque Angular intentó inlining de fuentes externas y el entorno rechazó la conexión; no se cambió configuración de producción ni Vercel.

## 2026-09-04 02:31:00Z - Cierre N4.6 Plan de cuentas

Responsable: Codex local autorizado en `Desarrollo`; cierre documental append-only sobre el exact-head funcional `9d649bbbb4279e41e8cf5b7f5f9b84c26cc362bf`.

N4.6.A-H queda `LISTO_REAL` con persistencia jerárquica EF, repositorio, API protegida, UI Angular, RBAC/auditoría y certificación en `docs/CERTIFICACION_N4_6_PLAN_CUENTAS.md`. Gates reales: Development `#33828121004`, aceptación `#33828121038`, Fase 8 `#33828121029`, M13 `#33828121086` y M10 `#33828121034`, todos `SUCCESS`; `VariApp CI` `SKIPPED` no se usa como PASS. P0/P1 atribuibles al alcance: `0/0`.

La ejecución Fase 2 terminó `FAILURE` únicamente por HTTP 503 de `registry.npmjs.org` durante `npm audit`; se clasifica `EXTERNAL_INFRA`, sin evidencia de regresión causal y sin rerun artificial. No se modificaron producción, secretos, dominios, Vercel ni workflows históricos N0.2-N0.5. El siguiente parent dependency-valid es `N4.7.A`; este changeset no inicia su scope.
