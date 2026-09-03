# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-09-03 — ERP-N4.3 Conciliación bancaria — CIERRE CANÓNICO

**Responsable:** ChatGPT/VAEP v3.29 Throughput-Hard mediante QA takeover y cierre parent-first.

**Objetivo/alcance:** reconciliar de forma aditiva/history-preserving el cierre documental de ERP-N4.3. La conciliación bancaria relaciona movimientos bancarios con autoridades financieras existentes sin crear una segunda autoridad financiera ni autorizar cambios en Producción.

**Evidencia funcional:** baseline certificado `ad0cf70fc6ced126de1878b61fe4ae02c8d41a01`; N4.3.A-G satisfacen contratos/Application/API, persistencia/DI, frontend/UX, RBAC, auditoría, seguridad, observabilidad y pruebas. N4.3.F fue reconciliado mediante QA takeover sin R3. P0/P1 atribuibles conocidos al baseline: 0/0.

**Certificación documental:** `docs/CERTIFICACION_N4_3_CONCILIACION_BANCARIA.md` fue publicada en `3d7b8c776b813c359e373780f1a1039c1baed8b1`; `TASKS.md` quedó reconciliado en `b61adc8d2d315474328014a6513b546b8c279bfb`. La matriz exact-head aplicable debe volver a quedar terminal sobre el HEAD resultante de esta entrada antes de declarar N4.3.H `LISTO_REAL`; `VariApp CI=SKIPPED` no cuenta como PASS.

**Guardrails:** trabajo únicamente en `Desarrollo`; PR #2 permanece OPEN+DRAFT; `main`, Producción, merge/auto-merge, ramas nuevas, force-push, secretos y deploy permanecen prohibidos/intactos.

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

**Responsable:** ChatGPT/VAEP mediante cierre canónico parent-first; artifacts Jules se usaron únicamente como evidencia revisada cuando correspondió y no sustituyen el DoD causal.

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

**Documentación:** `docs/ERP_N2_2_ORDEN_COMPRA.md`, `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`, `docs/ADR_N2_2_ORDEN_COMPRA.md` y `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`. Se corrigieron inconsistencias de estado inicial, schema/paginación, naming `Version` vs `VersionConcurrency` y afirmaciones de índices/migración para que coincidan con código real.

**Operación/rollback:** migración forward-only en presencia de datos; no hacer `Down` destructivo si existen órdenes. El rollback de aplicación se hace por commit en `Desarrollo` y revalidación completa antes de cualquier promoción. No se tocó Producción, `main`, secretos, deploy, merge ni auto-merge.

**Handoff:** ERP-N2.2 queda `LISTO`. Siguiente padre dependency-valid: `N2.3.A — Recepción de compras / Auditoría y preflight`.

## 2026-08-16 — N2.1.H Certificación documental

- Se creó `docs/CERTIFICACION_N2_1_PROVEEDORES.md` con alcance, DoD, evidencias y rollback de N2.1.
- Se actualizó `TASKS.md` marcando N2.1.H como `LISTO` y registrando las ejecuciones verdes de Fase 8, Development y M13 sobre el HEAD `fc0470e2f5d7`.
- Se corrigieron inconsistencias documentales en `docs/ERP_N2_1_PROVEEDORES.md` y `docs/ADR_N2_1_PROVEEDORES.md` para reflejar los endpoints y DTOs reales de la API.

## 2026-08-15 — N2.1.G QA y regresión

- Se añadieron pruebas de `ProveedorService` para creación con contacto primario, creación sin contactos, actualización de contactos y rechazo de contacto primario duplicado.
- Se añadieron pruebas de `ProveedorController` para validación de RUC, validación de teléfono, errores de modelo y mapeo de `Conflict`/`NotFound`.
- Se añadieron pruebas unitarias de Angular para `ProveedoresService` y el componente `Proveedores`.
- Se añadieron pruebas E2E de Playwright para validar rechazo de RUC inválido y alta de proveedor con contacto primario.
- Se corrigió la configuración de Jest para excluir `e2e/` de las suites unitarias y se eliminó el spec duplicado fuera de `src/`.

## 2026-08-15 — N2.1.F Seguridad, auditoría y observabilidad

- Se añadió `IAuditoriaService` a `ProveedorService` y se registran eventos de crear/actualizar/desactivar proveedor con `CorrelationId`.
- Se aplicó RBAC granular en `ProveedoresController` con permisos `Compras:Ver`, `Compras:Crear`, `Compras:Editar` y `Compras:Eliminar`.
- Se añadió logging estructurado en `ProveedorService` para las operaciones de escritura sin exponer datos sensibles.
- Se añadieron pruebas unitarias para verificar auditoría y autorización de los endpoints.

## 2026-08-15 — N2.1.E Frontend Proveedores

- Se implementó el servicio Angular de proveedores con DTOs tipados y soporte de `X-Correlation-ID`.
- Se añadió la pantalla de listado con búsqueda, paginación, estados de carga/error y acciones de crear/editar/desactivar.
- Se implementó el formulario reactivo con validación de RUC, teléfono, contactos y contacto primario único.
- Se añadió el detalle de proveedor con sus contactos y navegación entre vistas.
- Se añadieron rutas protegidas y permisos RBAC en el menú de navegación.
- Se añadieron pruebas unitarias del servicio y del componente de proveedores.

## 2026-08-15 — N2.1.D Application/API Proveedores

- Se añadieron DTOs, validaciones y `ProveedorService` con CRUD, búsqueda paginada y manejo de contactos.
- Se implementó `ProveedoresController` con endpoints REST para listar, obtener, crear, actualizar y desactivar proveedores.
- Se registró `IProveedorService`/`ProveedorService` en DI y se reutilizó `IProveedorRepository` existente.
- Se añadieron pruebas unitarias de servicio y controller para los flujos principales y errores `404`/`409`.

## 2026-08-15 — N2.1.C Persistencia Proveedores

- Se añadieron configuraciones EF Core para `Proveedor` y `ProveedorContacto`, índices/relaciones y `DbSet` en `AppDbContext`.
- Se implementó `ProveedorRepository` con búsqueda paginada, lectura por id/RUC y soporte de escritura.
- Se registró `IProveedorRepository`/`ProveedorRepository` en DI.
- Se creó la migración `20260815161916_N2_1_ProveedorPersistencia` con tablas, índices y FKs para proveedores/contactos.
- Se añadieron pruebas unitarias de persistencia para verificar configuración, aislamiento por tenant y unicidad de RUC.

## 2026-08-15 — N2.1.B Dominio y contratos Proveedores

- Se añadieron las entidades `Proveedor` y `ProveedorContacto`, con invariantes de tenant, RUC, nombre, email/teléfono y contacto primario único.
- Se añadieron contratos `IProveedorRepository` e `IProveedorService` y DTOs para listado, detalle y escritura.
- Se añadieron pruebas unitarias de dominio para invariantes y transiciones de estado.

## 2026-08-15 — N2.1.A Preflight Proveedores

- Se documentó el alcance del módulo de proveedores y sus límites respecto a compras/recepciones/facturas.
- Se verificó que no existía una autoridad duplicada de proveedor en dominio/persistencia/API/frontend.
- Se documentaron dependencias con catálogos y autenticación/tenant.
- Se definió el DoD de N2.1 y el plan de implementación A–H.

## 2026-08-13 — Reconciliación transversal N1.7–N1.10

- Se reconciliaron como N/A los bloques N1.7 Ajustes, N1.8 Transferencias, N1.9 Reportes y N1.10 Integraciones dentro del alcance N1.
- Se actualizó `TASKS.md` para reflejar que los ajustes se resuelven por N1.5, las transferencias por N1.6, los reportes por endpoints existentes y las integraciones por los mismos flujos/API.
- No se añadieron entidades, endpoints, migraciones ni pantallas duplicadas.

## 2026-08-13 — N1.6 Transferencias

- Se añadió el agregado `TransferenciaStock` con estados `Borrador`, `EnTransito`, `Recibida` y `Cancelada`, detalle de líneas y validaciones de transición.
- Se añadieron contratos, DTOs, repositorio EF, migración `N1_6_Transferencias`, servicio de aplicación y API protegida con permisos `Inventario:Ver`, `Inventario:Crear`, `Inventario:Confirmar` e `Inventario:Anular`.
- La recepción reutiliza `KardexService` para registrar salida/entrada sin duplicar la autoridad de stock.
- Se añadió UI Angular para listado, creación y detalle/acciones, más rutas protegidas y entrada de menú.
- Se añadieron pruebas de dominio, aplicación, controller, repositorio y frontend para los flujos principales.

## 2026-08-13 — N1.5 Ajustes

- Se añadió el agregado `AjusteInventario` con estados `Borrador`, `Aplicado` y `Anulado`, detalle de líneas y validaciones de transición.
- Se añadieron contratos, DTOs, repositorio EF, migración `N1_5_Ajustes`, servicio de aplicación y API protegida con permisos `Inventario:Ver`, `Inventario:Crear`, `Inventario:Confirmar` e `Inventario:Anular`.
- La aplicación del ajuste reutiliza `KardexService` para afectar existencias sin duplicar la autoridad de stock.
- Se añadió UI Angular para listado, creación y detalle/acciones, más rutas protegidas y entrada de menú.
- Se añadieron pruebas de dominio, aplicación, controller, repositorio y frontend para los flujos principales.

## 2026-08-13 — N1.4 Kardex

- Se implementó `KardexService` sobre `MovimientoInventario` como autoridad única del kardex, con consulta paginada por tenant/almacén/producto y registro centralizado de movimientos.
- Se añadieron DTOs y contratos de aplicación para kardex, además de `KardexController` protegido por `Inventario:Ver`.
- Se registró el servicio en DI y se añadieron pruebas unitarias de servicio/controller.
- Se incorporó la vista Angular de kardex con filtros, paginación y estados de carga/error, ruta protegida y acceso desde el menú de inventario.
- Se añadieron pruebas unitarias frontend para consulta, renderizado y manejo de errores.

## 2026-08-13 — N1.3 Movimientos

- Se implementó `MovimientoInventario` como agregado de movimientos de stock con tipos `Entrada`, `Salida`, `AjustePositivo`, `AjusteNegativo`, `TransferenciaEntrada` y `TransferenciaSalida`, cantidades positivas y trazabilidad de referencia.
- Se añadió repositorio EF, configuración, `DbSet`, migración `N1_3_Movimientos`, servicio de aplicación y API protegida con permisos `Inventario:Ver`/`Inventario:Crear`.
- Se añadió vista Angular de movimientos con filtros, paginación y estados de carga/error, ruta protegida y acceso desde el menú de inventario.
- Se añadieron pruebas unitarias de dominio, aplicación, controller, repositorio y frontend.

## 2026-08-13 — N1.2 Almacenes

- Se implementó `Almacen` como entidad tenant-scoped ligada a una sucursal, con nombre, código único por tenant, dirección y estado activo/inactivo.
- Se añadió persistencia EF, migración, repositorio, servicio de aplicación y API protegida por permisos `Inventario:Ver/Crear/Editar/Eliminar`.
- Se añadió UI Angular para listar, crear, editar y desactivar almacenes, ruta protegida y acceso desde el menú de inventario.
- Se añadieron pruebas unitarias backend y frontend para invariantes, CRUD y errores principales.

## 2026-08-13 — N1.1 Sucursales

- Se implementó `Sucursal` como entidad tenant-scoped con nombre, código único por tenant, dirección, teléfono y estado activo/inactivo.
- Se añadió persistencia EF, migración, repositorio, servicio de aplicación y API protegida por permisos `Configuracion:Ver/Crear/Editar/Eliminar`.
- Se añadió UI Angular para listar, crear, editar y desactivar sucursales, ruta protegida y acceso desde el menú de configuración.
- Se añadieron pruebas unitarias backend y frontend para invariantes, CRUD y errores principales.

## 2026-08-12 — N0.6 Referencias polimórficas

- Se incorporó `ReferenciaDocumento` como contrato canónico para referencias polimórficas (`TipoDocumento`, `DocumentoId`, `NumeroDocumento`) sin FKs cruzadas entre agregados.
- Se añadió `IReferenciaDocumentoResolver`/`ReferenciaDocumentoResolver` para validar tipo/id/número contra autoridades existentes (compra, factura de proveedor, venta, nota de crédito y movimiento de inventario).
- Se registró el resolver en DI y se añadieron pruebas unitarias para resoluciones válidas, mismatch de número y tipo no soportado.
- Se documentó la convención en `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS.md` y se actualizó `PROJECT_CONTEXT.md`.

## 2026-08-12 — N0.5 MetodoPago histórico

- Se consolidó `MetodoPago` como catálogo canónico y se añadieron seeds idempotentes para los métodos de pago históricos.
- Se creó la migración `N0_5_MetodoPagoHistorico` para insertar faltantes y bloquear rollback destructivo si existen referencias.
- Se añadieron pruebas de catálogo y migración, además de documentación en `docs/ERP_N0_5_METODO_PAGO.md`.

## 2026-08-12 — N0.4 RBAC relacional

- Se implementó RBAC relacional con tablas de roles, permisos y relaciones usuario-rol/rol-permiso.
- Se añadió `RequirePermissionAttribute`, resolución de permisos desde claims/BD y seeds idempotentes del catálogo base.
- Se migraron endpoints críticos para exigir permisos granulares y se añadieron pruebas de autorización.
- Se documentó el modelo y la matriz base en `docs/ERP_N0_4_RBAC.md`.

## 2026-08-12 — N0.3 ProductoVariante autoridad única

- Se consolidó `ProductoVariante` como autoridad única de SKU/stock/costo/precio por variante y se eliminó lógica duplicada en `Producto`.
- Se añadieron migraciones/ajustes de repositorios y pruebas para preservar compatibilidad con datos existentes.
- Se documentó la decisión arquitectónica en `docs/ERP_N0_3_PRODUCTO_VARIANTE.md`.

## 2026-08-12 — N0.2 CatalogoProducto legacy

- Se retiró el uso operativo del catálogo legacy y se mantuvo compatibilidad de lectura/migración donde era necesaria.
- Se actualizaron servicios/repositorios y pruebas para usar la autoridad canónica de producto/variante.
- Se documentó el retiro progresivo en `docs/ERP_N0_2_CATALOGO_PRODUCTO_LEGACY.md`.

## 2026-08-12 — N0.1 Auditoría y preflight ERP

- Se ejecutó inventario de autoridades existentes, dependencias y duplicidades antes de iniciar el ERP.
- Se documentaron guardrails de no duplicación, tenant, auditoría, seguridad y migraciones forward-only.
- Se estableció el baseline para N0.2–N0.6 y el orden dependency-valid del Plan Maestro.
