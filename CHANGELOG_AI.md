## 2026-09-15 — Cierre visual SMTP — materialización de capturas

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se materializaron como PNG las pantallas reales de Render (servicio Live, Environment con secretos ocultos, arranque y logs del envío) y VariApp (facturación, FAC-000003, historial y evidencia persistente posterior al único envío). Se incorporaron la captura de recepción, el PDF A4 y la captura final del inventario de la carpeta en `docs/evidencias/cierre-correo-smtp/2026-09-15_1048/`. No hubo cambios de código, nuevas ventas, nuevos correos, cambios de variables, Producción ni WhatsApp. La única reapertura diagnóstica permitida mostró un fallo transitorio y quedó descrita sin fabricar un PASS.
# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT, Chat B (ChatGPT Business) y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-09-15 — Reconciliación de cola N8.5/N8.6

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se registró el receipt de cierre de los bloqueos externos sustituidos por evidencia SMTP existente y el modo WhatsApp `USER_INITIATED_HANDOFF`. La validación visual CUA queda pendiente por ausencia de sesión; no se fabricó evidencia. No se modificó `N8.7.A`, su lease ni las automatizaciones.

## 2026-09-15 — WhatsApp — fallback visible ante popup bloqueado

El flujo de factura conserva el enlace `wa.me` seguro y lo muestra como fallback cuando el navegador bloquea la apertura automática.

## 2026-09-15 — WhatsApp — erratum de contrato de auditoría

Se corrigió el doble enmascarado: factura envía el teléfono normalizado de forma transitoria y el backend aplica la máscara una sola vez antes de persistir. Se añadió manejo visible de error de auditoría y cobertura frontend/backend del contrato. No se enviaron mensajes reales ni se tocaron `N8.7.A` o las automatizaciones.

El receipt correctivo quedó fijado al HEAD funcional exacto de la corrección.

## 2026-09-15 — WhatsApp — UX veraz del popup y auditoría desacoplada

Se aisló el booleano real de `window.open`: el fallback depende solo de la apertura aceptada y el error de auditoría usa wording neutral. Se añadió el contrato ejecutable Factura→Service con casos popup aceptado, bloqueado y auditoría fallida.

Se añadió el addendum y receipt final de N8.6 con el contrato UX veraz, sin capturas fabricadas y sin interferencia con `N8.7.A`.

## 2026-09-15 — WhatsApp — handoff canónico sin API de proveedor

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se consolidó `WhatsAppShareService` y `WhatsAppSharePolicy` para normalización multi-país con fallback Honduras, enlaces `wa.me` URL-encoded, apertura oficial y destinatarios enmascarados. El historial acepta únicamente estados de handoff y rechaza afirmaciones de entrega/lectura. Facturas, catálogo público y checkout consumen la misma política. No se añadieron APIs pagadas, tokens, secretos, envíos reales ni cambios al scheduler.

Validación: 24 pruebas backend dirigidas, 4 pruebas Vitest de política frontend, `npm run lint`, `npm run build:prod` y `git diff --check` superados.

## 2026-09-15 — Responsive — paneles globales de select y autocomplete

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Los paneles de Angular Material para selects y autocompletado ahora tienen un ancho mínimo legible y un máximo ligado al viewport, con opciones de altura flexible y texto envolvente. El gate responsive abre un select real por ruta cuando existe y falla si el panel u opciones quedan fuera, estrechos o recortados.

## 2026-09-15 — Responsive — corrección de campos de detalle en Nueva venta

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

La fila de detalle móvil de `Ventas/Nueva venta` tenía una columna automática que comprimía `Cantidad` y dejaba el outline del campo junto a `Precio unitario`. La grilla ahora usa dos columnas fluidas reales para ambos controles, manteniendo producto/variante/acción y evitando colisiones. El gate global añade detección de campos de detalle demasiado estrechos y solapamientos entre outlines.

Validación: build productivo y lint del frontend superados antes del cambio; la verificación remota se repetirá sobre la ruta desplegada sin crear una venta ni enviar correo.

## 2026-09-15 — Responsive global — foundation y gate automático

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se consolidó la base responsive global del frontend: shell con drawer móvil, topbar y contenedores fluidos, controles y medios que respetan su contenedor, diálogos limitados al viewport y primitivas compartidas para grids, filtros, detalle, KPI, acciones y tablas. Se eliminó la dependencia de `overflow-x: hidden` en `body`. Se añadió `docs/FRONTEND_RESPONSIVE_STANDARD.md` y el gate `npm run test:responsive`, que compara el inventario de rutas fuente y recorre 320/360/375/390/412/430/768/1024/1440 px comprobando overflow, clipping, tablas, objetivos táctiles, accesibilidad y drawer.

Validación real: `npm run lint`, `npm run build:prod` y `git diff --check` superados. El build conserva únicamente advertencias Angular preexistentes de proyección/imports; no hubo cambios de backend, secretos, Producción, `main`, WhatsApp ni flujos transaccionales.

## 2026-09-15 — UX — eliminación de diálogos nativos en flujos empresariales

**Responsable:** ChatGPT/VAEP, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se reemplazaron todos los `window.confirm`, `confirm`, `window.prompt` y `prompt` de `frontend/src/app` por el `AppAlertService` compartido. Las acciones de compras, ventas, facturación, pagos, inventario, productos, solicitudes, órdenes, preparaciones, cargas masivas y administración usan ahora modales propios con texto semántico, motivos obligatorios cuando aplican, cancelación accesible y estado de confirmación sin ventanas nativas del navegador.

Validación real: barrido `rg` sin diálogos nativos de producción; `npm run lint` y `npm run build:prod` superados. El build backend Release superó 0 advertencias/0 errores; la ejecución local de pruebas .NET quedó impedida por la directiva de Control de aplicaciones del host al cargar `InventoryApp.Tests.dll`, sin resultado PASS inventado.

## 2026-09-15 — N8.1.G — evidencia de UAT delegado y reconciliación pendiente

**Responsable:** ChatGPT/VAEP, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se registró el intento de UAT delegado sobre `c3e2b5fc34d2f51738a33a04d092104ecf2bf1c7`: la sesión CUA disponible quedó en `/login` después del redeploy y no se fabricaron capturas, DOM ni aceptación humana. La evidencia conserva el PASS existente de Inventario, los PASS técnicos previos de los otros grupos, el alcance interno provider-neutral/fail-closed de Integrations y el barrido de diálogos nativos en cero. La reconciliación del Sheet queda explícitamente pendiente por falta de acceso conectado.

## 2026-09-15 — N8.1.G — validación técnica de los seis grupos restantes

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se validaron Compras, Ventas/Facturación, Tesorería/Finanzas, BI/Reportes y Multiempresa/RBAC con UAT autenticado, contratos dirigidos y regresión completa. La evidencia consolidada está en `vaep/evidence/reviews/N8.1.G_CODEX_TECHNICAL_UAT_REMAINING_6_20260915T074247Z.json`; la preparación de sign-off está en `vaep/evidence/reviews/N8.1.G_READY_FOR_FINAL_HUMAN_SIGNOFF_20260915T074247Z.json`.

Se corrigió el defecto causal `N8.1.G-FIN-001`: al registrar o anular un pago de factura, `FacturaService` ahora sincroniza el estado del movimiento financiero automático de la Venta y cuenta con regresión dirigida. No se modificaron `main`, Producción, secretos, dominios ni PR #2.

Validación real: build backend 0 advertencias/0 errores; 2269 pruebas backend no-Integration superadas; filtros dirigidos 294/82/131 superados; lint y build productivo frontend superados. Integrations queda `BLOCKED_EXTERNAL` sólo por falta de sandbox/proveedores externos; no se inventaron credenciales. Falta la aceptación humana autorizada para los seis grupos restantes, por lo que N8.1.G no se marca `LISTO_REAL`.

## 2026-09-11 — ERP-N6.2.H — recuperación documental y REVIEW_FIRST

**Responsable:** CHATGPT_VAEP como first detector/correction owner.

El ATTEMPT1 J1 terminó con patch sobre `CHANGELOG_AI.md` y `TASKS.md`, pero el contrato terminal falló: `TASKS.md` es control-plane prohibido para Jules y faltaban las evidencias de self-review/tests. REVIEW_FIRST descartó íntegramente el fragmento de `TASKS.md`, corrigió la narrativa de cierre y materializó la reconciliación canónica en `docs/N6.2_TENANT_AWARE_DATA_MODEL_PREFLIGHT.md`.

La evidencia A–G quedó enlazada de forma source-backed; N6.3 (Empresa→sucursales), N6.4 (membresías/roles por empresa) y N6.5 (aislamiento anti-leakage completo) permanecen explícitamente fuera de N6.2. No hubo cambio de comportamiento productivo, esquema, migración, API, frontend, workflow, secrets, `main`, Producción ni PR #2.

Estado: `REVIEW_FIRST_ACCEPTED_AFTER_CONTROLLER_DIRECT_FIX__P0_0__P1_0__PENDING_EXACT_HEAD_GATES__NOT_LISTO_REAL`. No se consumió R2; R3 permanece prohibido.

## 2026-09-08 — Chat B + reconciliación canónica de estado VAEP

**Responsable:** Codex, por orden explícita del propietario.

**Equipo:** se incorporó Chat B (ChatGPT Business) como peer controller/QA full-access dentro de `Desarrollo`, con REVIEW_FIRST, QA_TAKEOVER, corrección, integración, CI, certificación, rollup y failover bajo `docs/VAEP_AUTHORITY.md`. No es una quinta lane Jules y no puede saltar gates, crear R3+, tocar `main`/Producción/secretos ni falsear evidencia.

**Repositorio:** se actualizó el MAESTRO y los documentos colaborativos para que Chat B consuma la misma autoridad única. El remoto avanzó concurrentemente a `0d0ba5e9` con `dispatch-admission=CLOSED`; se corrigió a `FROZEN`, el único valor contractual válido para contención por REVIEW_FIRST pendiente, conservando `allowExistingActiveSessions=true`.

**Drive compartido:** se reconciliaron CONFIG, DASHBOARD, COLA, PLAN_MAESTRO, TAREAS_PROGRAMADAS, EJECUCION_MANUAL y LEYENDA. El estado vigente es `CURRENT_PARENT=N4.11.H`, `FUNCTIONAL_HEAD=b30b949e`, `HEAD=5ed9b3d6` como descendiente de control-plane, `dispatch-admission=FROZEN`, `N4.11.B–G=LISTO_REAL` con evidencia, y `N4.11.H=VALIDANDO` por REVIEW_FIRST/QA_TAKEOVER pendiente. No se despachó ningún Jules manual ni se fabricó backlog.

**Documento rector compartido:** se añadió un bloque de vigencia al inicio de `Plan Maestro ERP V5 — VariApp — FUENTE RECTORA VAEP`, con chip nativo de fecha y precedencia explícita del MAESTRO versionado sobre contenido histórico.

**Verificación:** readback de todas las celdas objetivo, readback nativo del documento (incluido `dateElement` y estilos), `git diff --check`, rama default GitHub `Desarrollo`, `main` sin cambios. Pendiente externo: no se concedió una cuenta GitHub/Drive adicional porque no existe un email/login verificable de Chat B; el rol operativo quedó registrado sin inventar credenciales.

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

**Documentación:** `docs/ERP_N2_2_ORDEN_COMPRA.md`, `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`, `docs/ADR_N2_2_ORDEN_COMPRA_AUTORIDAD_DOCUMENTAL.md`, `docs/OPENAPI_N2_2_ORDEN_COMPRA.md` y `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`, más el preflight histórico `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md`.

**Control:** `N2.2.A–H` quedan formalmente cerrados. El siguiente foco FINISH_FIRST elegible es `N2.3.A — Recepción de mercancía — Auditoría y preflight`, donde recién debe materializarse el incremento de stock por recepción real. El scope Jules no fue editado ni integrado. `main`, Producción, merge/auto-merge del PR #2, ramas nuevas, force-push, secretos e infraestructura productiva permanecen intactos.

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

## 2026-09-04 02:31:00Z - Cierre N4.6 Plan de cuentas

Responsable: Codex local autorizado en `Desarrollo`; cierre documental append-only sobre el exact-head funcional `9d649bbbb4279e41e8cf5b7f5f9b84c26cc362bf`.

N4.6.A-H queda `LISTO_REAL` con persistencia jerárquica EF, repositorio, API protegida, UI Angular, RBAC/auditoría y certificación en `docs/CERTIFICACION_N4_6_PLAN_CUENTAS.md`. Gates reales: Development `#33828121004`, aceptación `#33828121038`, Fase 8 `#33828121029`, M13 `#33828121086` y M10 `#33828121034`, todos `SUCCESS`; `VariApp CI` `SKIPPED` no se usa como PASS. P0/P1 atribuibles al alcance: `0/0`.

La ejecución Fase 2 terminó `FAILURE` únicamente por HTTP 503 de `registry.npmjs.org` durante `npm audit`; se clasifica `EXTERNAL_INFRA`, sin evidencia de regresión causal y sin rerun artificial. No se modificaron producción, secretos, dominios, Vercel ni workflows históricos N0.2-N0.5. El siguiente parent dependency-valid es `N4.7.A`; este changeset no inicia su scope.

## 2026-09-04 — ERP-N4.7 Asientos Contables — ROLLUP DOCUMENTAL

**Responsable:** ChatGPT/VAEP bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** reconciliar de forma aditiva/history-preserving el cierre documental de N4.7 sin modificar producto ni adelantar N4.8. La certificación canónica es `docs/CERTIFICACION_N4_7_ASIENTOS.md`; el baseline funcional certificado de A-G es `c8d1e373ba8ea008bf773e69afa10f5f18d6de8b`.

**Evidencia previa al rollup:** N4.7.F `LISTO_REAL @09:49 -06`, N4.7.G `LISTO_REAL @09:50 -06`; REVIEW_FIRST del certificado canónico PASS; exact-head documental previo `6986874048985e4746d21e23254479b391220445` con gates aplicables terminales SUCCESS; `VariApp CI=SKIPPED` excluido; P0=0/P1=0.

**Control fail-closed:** este changeset completa exclusivamente el rollup documental `TASKS.md` + `CHANGELOG_AI.md`. `N4.7.H` no se declara `LISTO_REAL` por este texto: permanece pendiente de gates aplicables terminales y revalidación P0/P1=0 sobre el exact-head resultante del rollup. Solo después VAEP puede cerrar H y promover `N4.8.A`. No se modifica `main`, Producción, secretos, deploy, ramas ni PR #2.

## 2026-09-04 — Cierre Fase 0 y Fase 1 de migración VAEP MASTER

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se cerraron formalmente las fases de migración `FASE 0 — EXCLUSIVIDAD` y `FASE 1 — FREEZE + RECONCILIACIÓN`. La autoridad operativa continúa en `docs/VAEP_AUTHORITY.md`; ChatGPT/VAEP conserva controller/REVIEW_FIRST/QA/certificación. No se otorgó autoridad operativa a Codex ni AntiG.

## 2026-09-04 — Cierre Fase 2 de migración VAEP MASTER — Política Machine-Readable

Responsable: AntiG/Antigravity (intervención puntual autorizada por Javier exclusivamente para Fase 2).

Se ejecutó la consolidación de la política operativa machine-readable dentro del MAESTRO único `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`).

## 2026-09-04 — Corrección final de cierre Fase 2 VAEP MASTER

REVIEW_FIRST posterior al handoff AntiG detectó que la primera publicación de Fase 2 aún conservaba dos defectos de consolidación y fueron corregidos fail-closed bajo MASTER.

## 2026-09-04 — Cierre Fase 3 VAEP MASTER — Runtime Jules

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se implementó el hardening aprobado del runtime Jules A/B/C/D, con `NO_OP`, fail-closed multi-manifest, timeout/supersession y late-result guard.

## 2026-09-04 — Cierre Fase 4 VAEP MASTER — Historical writers

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se retiraron los writers históricos aprobados de Fase 4, manteniendo CI de producto y eliminando commit/push automáticos.

## 2026-09-04 — Cierre Fase 5 VAEP MASTER — Cleanup documental y manifests

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se completó el semantic-diff y cleanup de protocolos/manifests históricos activos, preservando Git history.

## 2026-09-04 — Cierre Fase 6 VAEP MASTER — AntiG RESERVED_INACTIVE

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se formalizó técnicamente el estado reservado de AntiG sin eliminar su infraestructura.

## 2026-09-04 — Certificación final VAEP MASTER — Fases 0 a 7

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se certifica el cierre integral de la migración F0-F7 hacia el MAESTRO único `docs/VAEP_AUTHORITY.md`.

## 2026-09-04 — Remediación post-auditoría Codex

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

La auditoría externa de Codex detectó tres deudas reales de endurecimiento y fueron remediadas preservando la autoridad MASTER.

## 2026-09-11 — ERP-N6.4 Usuarios por empresa — cierre documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver de forma estrictamente aditiva y byte-preserving el único P1 documental abierto de `N6.4.H`. `TASKS.md` y `docs/CERTIFICACION_N6_4_USUARIOS_POR_EMPRESA.md` conservan la evidencia funcional y de certificación; esta entrada no reabre código de producto ni adelanta `N6.5`.

**Evidencia:** `N6.4.A–G=LISTO_REAL`; functional head final `3f788ec78b03b820592c7a514fa3a63e345b3754`; baseline documental `375c7f76625a2b23b1acbd839e368b3d0e5a11e3`; REVIEW_FIRST bloqueante `vaep/evidence/reviews/N6.4.H_REVIEW_FIRST_20260911T2041Z_SUP24.json`. La lectura conectada confirmó el cuerpo completo de `CHANGELOG_AI.md` y el blob exacto `f30be5c7d586e74fc7ba945f8ab95b60a3cb1cd9` antes del append.

**Control:** esta publicación cierra únicamente el P1 de append history-preserving. No declara por sí sola `N6.4.H=LISTO_REAL`: requiere REVIEW_FIRST fresco P0=0/P1=0, gates causales terminales exact-head o equivalencia demostrada, receipt H y reconciliación canónica antes de promover `N6.5.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-12 — ERP-N6.8 Storage aislado — cierre documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :00.

**Objetivo/alcance:** reconciliar de forma estrictamente aditiva/history-preserving el cierre documental de `N6.8.H`, sin reabrir código runtime ni adelantar `N6.9`. La certificación canónica es `docs/CERTIFICACION_N6_8_STORAGE_AISLADO.md`; el functional storage candidate permanece en la evidencia ya certificada de `N6.8.G`.

**Evidencia:** `N6.8.A–G=LISTO_REAL`; candidate de storage/TEST_CI `f5d29e7471762a3fe6fe735ba98c2ad1f0188727`; causal storage-isolation run `34714029058`, job `103607801664=SUCCESS`; material DOC_CERT `dae43dfc5a3211643565c7002abcc67a845ef179`; REVIEW_FIRST bloqueante `6acb02e87d7cbde013fe2bed2ff79a7a9b068482` / `vaep/evidence/reviews/N6.8.H_REVIEW_FIRST_20260912T2002Z.json` con `P0=0/P1=2` exclusivamente por `CHANGELOG_AI.md` y `TASKS.md`.

**Control:** esta publicación resuelve el P1 de `CHANGELOG_AI.md` de forma append-only. No declara por sí sola `N6.8.H=LISTO_REAL`: requiere reconciliar `TASKS.md`, rerun REVIEW_FIRST con P0=0/P1=0, DoD PASS y receipt persistido/releído antes de promover `N6.9.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.1 Outbox Pattern — cierre documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver de forma estrictamente aditiva y byte-preserving el único P1 documental abierto de `N7.1.H`, sin reabrir el runtime de Outbox ni adelantar `N7.2`.

**Evidencia:** `N7.1.A–G=LISTO_REAL`; baseline seguro previo `6a4a8df9b4397a8028c74b50870295ca7d940cd7`; blob fuente exacto de `CHANGELOG_AI.md` `e48e7f339e09f385df329591eb1806fc33978323`. El append se publica sobre el árbol restaurado `5d186f061a3b7bc1545e27002b3fa455aa582bc6` y debe verificarse como único archivo modificado, con `deletions=0`.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. No declara por sí sola `N7.1.H=LISTO_REAL`: requiere REVIEW_FIRST fresco `P0=0/P1=0`, receipt H persistido/releído y reconciliación canónica antes de promover `N7.2.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.2 Retry controlado del Outbox — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.2.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ni adelantar `N7.3`.

**Evidencia:** `N7.2.A–G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_2_RETRY.md` en `3fda2525d0a9f0931e25cfa146c372c2e48fb5f5`; N7.2.G candidate congelado `6f297cd6107312ad9e301491d47d9f88b5497539`, gate exact-head run `34757554438`, job `103724457658=SUCCESS`, receipt `vaep/evidence/fragments/N7.2.G_LISTO_REAL_20260913T124538Z.json`. `TASKS.md` quedó reconciliado aditivamente en `e3bc89479493b163c99ccf53e7a52d8047342c44`; REVIEW_FIRST vigente `vaep/evidence/reviews/N7.2.H_REVIEW_FIRST_20260913T140109Z_SUP48.json` sobre control-head `695adaf21df85b872b27d51d9f18822c381793c4` dejó P0=0/P1=1 exclusivamente por `CHANGELOG_AI_ADDITIVE_RECONCILIATION`.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. `N7.2.H` no se declara `LISTO_REAL` hasta REVIEW_FIRST fresco P0=0/P1=0, gates causales exact-head aplicables PASS y receipt persistido/releído. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.3 Dead-letter del Outbox — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :48.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.3.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ni adelantar el sucesor.

**Evidencia:** `N7.3.A–G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_3_DEAD_LETTER.md`; N7.3.G functional candidate `2c19221fdbb0a3cdfdcf8afdc839999bf8e80265`, REVIEW_FIRST `67be3a7aff8f6ce77bb6de27f1a96187452565ad` con `P0=0/P1=0`, receipt `vaep/evidence/receipts/N7.3.G_LISTO_REAL_20260913T191310Z.json` en `1f5a62910b3a84b99ee90b45d204ee2fcf240f75`; `TASKS.md` quedó reconciliado de forma aditiva/history-preserving en `4ed8cfce16ad00b735194280b73f42c5e28e58a2`. La certificación N7.3 registra 17/17 criterios de aceptación satisfechos.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. `N7.3.H` no se declara `LISTO_REAL` hasta REVIEW_FIRST fresco `P0=0/P1=0`, gates causales exact-head aplicables PASS y receipt persistido/releído. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.4 Idempotencia — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :48.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.4.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ni adelantar `N7.5`.

**Evidencia:** `N7.4.A–G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_4_IDEMPOTENCIA.md` en `6097f60ff68d163878d5d3d85c590c59f0cd2f75`; functional candidate `1c366562a90c590cc2925a153298fd1b758e8dab`; causal gates `34782712890/103792518381`, `34782712890/103792518352`, `34782712890/103792518396`, `34782712884/103792518243` y `34782712884/103792518046` en `SUCCESS`; receipt G `vaep/evidence/receipts/N7.4.G_LISTO_REAL_20260913T220100Z_SUP48.json`.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. `N7.4.H` no se declara `LISTO_REAL` hasta resolver también `TASKS_ADDITIVE_STATE_RECONCILIATION`, repetir REVIEW_FIRST con `P0=0/P1=0`, demostrar equivalencia funcional y persistir/releer receipt H. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-14 — ERP-N7.5 Webhooks seguros — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / recovery byte-exacto N7.5.H.

**Objetivo/alcance:** resolver `TASKS_ADDITIVE_STATE_RECONCILIATION` y `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.5.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime de webhooks ni adelantar `N7.6`.

**Evidencia:** `N7.5.A-G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_5_WEBHOOKS.md`; functional candidate `dbd515909d98893f4924bb73d0a48f74fe9b97c3`; gates causales exact-head `34800735379/103842737649=SUCCESS` y `34800735411/103842739664=SUCCESS`; receipt G `vaep/evidence/receipts/N7.5.G_LISTO_REAL_20260914T030250Z_SUP36.json`. El probe off-ref `350f27db5040be75f7d84e78cbaa8e802f5cbf20` fue rechazado y nunca publicado porque mutaba historia; este recovery usa append de bytes al EOF sobre los blobs exactos vigentes.

**Control:** esta publicación resuelve únicamente los dos P1 documentales mediante append byte-exacto. No declara por sí sola `N7.5.H=LISTO_REAL`: exige REVIEW_FIRST fresco `P0=0/P1=0`, equivalencia funcional, compare de `TASKS.md` y `CHANGELOG_AI.md` con `additions>0/deletions=0`, receipt H persistido/releído y sólo entonces promoción de `N7.6.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-14 — ERP-N7.6 Integración API de WhatsApp Business — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.6.H.

**Objetivo/alcance:** cerrar documentalmente la cadena N7.6 sin reabrir lógica ya certificada ni ampliar alcance. La reconciliación preserva byte-for-byte toda historia previa de `TASKS.md` y `CHANGELOG_AI.md`; el estado machine-readable continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

**Evidencia funcional:** `N7.6.A-G=LISTO_REAL`; candidate funcional final `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b`; receipts B-G en `vaep/evidence/receipts/`. El alcance certificado incluye configuración tenant-scoped con referencias opacas a secretos, persistencia/migración, boundary API fail-closed, frontend con tenant verificado/RBAC reactivo/estado truthful, controles de seguridad y regresión exact-head. N7.6.G certificó gates `34864310838/104044575850`, `34864310860/104044621837`, `34864311032/104044562880`, `34864310684/104044559652` y `34864310645/104044379586` en `SUCCESS`, reutilizando además la migración causal N7.6.C `34841294854` sin schema delta posterior.

**Control:** esta publicación resuelve únicamente `TASKS_ADDITIVE_STATE_RECONCILIATION` y `CHANGELOG_AI_ADDITIVE_RECONCILIATION` mediante append byte-exacto. No declara por sí sola `N7.6.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional, certificación canónica releída y receipt H persistido/releído antes de promover `N7.7.A`. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.7 Email empresarial — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.7.H.

**Objetivo/alcance:** cerrar documentalmente N7.7 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.7.A-G=`LISTO_REAL`; candidate funcional `7a0765aa37533b8df2e52807f0a63800872c009d`; receipt G `vaep/evidence/receipts/N7.7.G_LISTO_REAL_20260914T181734Z_SUP12.json`; certificación `docs/CERTIFICACION_N7_7_EMAIL_EMPRESARIAL.md`. N7.7.H no introduce delta de producto ni schema.

**Control:** esta publicación resuelve los P1 documentales mediante append byte-exacto. No declara por sí sola `N7.7.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover N7.8.A. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.8 Pagos online — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.8.H.

**Objetivo/alcance:** cerrar documentalmente N7.8 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.8.A-G=`LISTO_REAL`; candidate funcional `b217cc00bfa9bfc452674f0bdacbef52e50186a7`; receipt G `vaep/evidence/receipts/N7.8.G_LISTO_REAL_20260914T212600Z_SUP48.json`; certificación `docs/CERTIFICACION_N7_8_PAGOS_ONLINE.md`. N7.8.F resolvió el P1 de checkout inseguro exigiendo URL absoluta HTTPS y añadió prueba negativa dirigida. N7.8.G certificó los gates aplicables; el fallo del run integral `34896693703` quedó probado no causal por pertenecer a suites legacy/global de frontend sin delta frontend atribuible a N7.8. N7.8.H no introduce delta de producto ni schema.

**Control:** esta publicación resuelve los P1 documentales mediante append byte-exacto. No declara por sí sola `N7.8.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover el sucesor dependency-valid. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.9 Ecommerce — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.9.H.

**Objetivo/alcance:** cerrar documentalmente N7.9 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.9.A-G=`LISTO_REAL`; functional candidate final `5de96492290218639f18c1648668215339c34a1e`; receipt G `vaep/evidence/receipts/N7.9.G_LISTO_REAL_20260914T230920Z_SUP00.json`; REVIEW_FIRST G P0=0/P1=0/P2=0. El workflow causal `34905794077` terminó con backend Release/pruebas, Docker, frontend producción, higiene y MySQL/migraciones en `SUCCESS`; backend registró 2245 passed, 0 failed, 0 skipped. Los commits posteriores al candidate hasta el receipt G son exclusivamente evidencia y no introducen delta funcional de producto/schema.

**Control:** esta publicación resuelve los P1 documentales mediante append byte-exacto. No declara por sí sola `N7.9.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover `N7.10.A`. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.10 Facturación fiscal/electrónica — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.10.H.

**Objetivo/alcance:** cerrar documentalmente N7.10 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.10.A-G=`LISTO_REAL`; functional candidate final `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b`; receipt F `vaep/evidence/receipts/N7.10.F_LISTO_REAL_20260915T011651Z_SUP00.json`; receipt G `vaep/evidence/receipts/N7.10.G_LISTO_REAL_20260915T012330Z_SUP00.json`; REVIEW_FIRST F/G P0=0/P1=0/P2=0. El workflow causal `34916355954` terminó con backend Release/pruebas, Docker, frontend producción, higiene y MySQL/migraciones/integración en `SUCCESS`; backend registró 2263 passed, 0 failed, 0 skipped. El gate suplementario `34916355934` terminó `SUCCESS` para restore aislado y seguridad/tenant/secrets. Los commits posteriores al candidate hasta el receipt G son exclusivamente evidencia.

**Seguridad y operación:** la emisión fiscal permanece autenticada, permission-gated, tenant-aware, idempotente y fail-closed ante configuración/proveedor inválido. La auditoría evita claves de idempotencia, hash de snapshot, payloads del proveedor, referencias externas y excepciones crudas. No se introdujo webhook fiscal ni se certifica una superficie inexistente.

**Control:** esta publicación resuelve los rollups documentales mediante append byte-exacto. No declara por sí sola `N7.10.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover `GATE-N7`. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-15 — ERP-N8.2 Compatibilidad de dispositivos — reconciliación documental H

**Responsable:** Tarea Supervisión :48 bajo `docs/VAEP_AUTHORITY.md`.

Se cerró la cobertura causal de N8.2 para escritorio, laptop, tablet, Android e iPhone. El P1 inicial por falta de perfiles explícitos se resolvió same-run con `frontend/e2e/n82-device-compatibility.spec.ts` y el gate dedicado `.github/workflows/n8-2-device-compatibility.yml`. El workflow `N8.2 - Compatibilidad de dispositivos` run `34941980085`, job `104292533908`, terminó `success` sobre `1f7008f464f195aab3020a5d0102a85428c8919a`; lint, build de producción y Playwright dirigido quedaron PASS.

N8.2.E, N8.2.F y N8.2.G quedaron `LISTO_REAL` mediante receipts VAEP con REVIEW_FIRST P0=0/P1=0/P2=0. No hubo delta de runtime backend, persistencia, migraciones, RBAC, límites tenant, deploy, Producción, secretos ni PR #2. La certificación canónica se materializó en `docs/CERTIFICACION_N8_2_DISPOSITIVOS.md`.

Este registro es histórico y no falsea H: N8.2.H sólo es `LISTO_REAL` cuando exista su receipt final tras REVIEW_FIRST documental y readback/reconciliación de control-plane.

## 2026-09-15 — ERP-N8.3 Compatibilidad de navegadores — reconciliación documental H

**Responsable:** Tarea Supervisión :48 bajo `docs/VAEP_AUTHORITY.md`.

Se cerró la cobertura causal de compatibilidad del storefront en Chromium, Firefox y WebKit sobre el functional candidate `b833e44a976f92a426fff7f969a3f3f757234f20`. El workflow `N8.3 - Compatibilidad de navegadores`, run `34943377733`, job `104296994230`, terminó `success`; lint, build de producción y Playwright dirigido quedaron PASS en los tres motores.

N8.3.F y N8.3.G quedaron `LISTO_REAL` con REVIEW_FIRST P0=0/P1=0/P2=0. El workflow usa permisos `contents: read`, no consume secretos y sirve Angular únicamente en `127.0.0.1`. N8.3 no introduce delta backend, persistencia, migraciones, autenticación, RBAC, Producción ni deploy. La certificación canónica se materializó en `docs/CERTIFICACION_N8_3_NAVEGADORES.md`.

Este registro es histórico y no falsea H: N8.3.H sólo es `LISTO_REAL` cuando exista su REVIEW_FIRST documental, receipt final y readback/reconciliación del control-plane.

## 2026-09-15 — ERP-N8.4 POS físico — reconciliación documental H

**Responsable:** Tarea Supervisión :24 bajo `docs/VAEP_AUTHORITY.md`.

Se certificó la cadena N8.4.A-F con sus receipts canónicos y se reconcilió N8.4.G mediante la aceptación explícita del propietario registrada en `vaep/evidence/receipts/OWNER_RECONCILIATION_N8.1.G_N8.1.H_N8.4.G_20260915T193239Z.json`. La aceptación confirma que la validación de impresora fue revisada/corroborada y aprobada, con P0=0/P1=0 y sin generar evidencia sintética ni afirmar observación física adicional por el controller.

El historial previo de bloqueo físico permanece intacto. El contrato canónico se conserva en `docs/N8_4_POS_PHYSICAL_CAPABILITY_CONTRACT.md` y la certificación final de alcance se materializó en `docs/CERTIFICACION_N8_4_POS_FISICO.md`. N8.4.H no introduce delta de runtime, esquema, datos, secretos, Producción ni deploy.

Este registro es histórico y no falsea H: N8.4.H sólo es `LISTO_REAL` cuando exista su REVIEW_FIRST documental, receipt final y readback/reconciliación del control-plane.

## 2026-09-15 — Auditoría forense N8.6–N8.8

Se contrastaron receipts con commits, diffs, revisiones, el control plane y runtime de Desarrollo. N8.6.G/H quedan confirmados por el contrato funcional de WhatsApp y la prueba dirigida actual (4/4). La corrección append-only de N8.7.E actualizó sólo `COLA!N676:O676` a los instantes canónicos demostrados por Git. N8.7.G conserva un blocker auténtico de workload autenticado no productivo; N8.8.G se reabre para certificar el proveedor y un backup/restore fresco mediante la ruta M11 existente. La evidencia completa está en `docs/evidencias/auditoria-forense-n8/2026-09-15_1532/`.

## 2026-09-15 — M11: certificado seguro de proveedor

El workflow operativo M11 ahora emite un certificado limitado a motor, versión, proveedor clasificado, sufijo de host, TLS y capacidad de restore lógico. El workflow no imprime ni publica usuario, contraseña, host completo ni nombre de base; el metadata externo del artefacto también redacciona ese nombre. Esta mejora prepara una ejecución real de backup cifrado y restore aislado sobre Desarrollo.

## 2026-09-15 — ERP-N8.11 Seguridad — reconciliación documental H

**Responsable:** Tarea Supervisión :48 bajo `docs/VAEP_AUTHORITY.md`.

Se certificó la cadena N8.11.A-G. El delta material de backend quedó cubierto por contratos de seguridad dirigidos; la auditoría SEC_AUDIT cerró con P0=0/P1=0/P2=0 y gates de hardening, npm high/critical y vulnerabilidades .NET en PASS. TEST_CI confirmó backend, frontend y security gates causales sobre el functional candidate `91a7051bdb75c858af08d0e28368d827c4c0f6b8`.

La certificación final se materializó en `docs/CERTIFICACION_N8_11_SEGURIDAD.md`. N8.11.H es documental y no introduce delta de runtime, esquema, datos, secretos, Producción ni deploy. Fallos/cancelaciones de workflows no causales no se utilizaron para fabricar PASS.

Este registro es histórico y no falsea H: N8.11.H sólo es `LISTO_REAL` cuando exista su REVIEW_FIRST documental, receipt final y readback/reconciliación del control-plane.

## 2026-09-17 — ERP-N8.6 WhatsApp real — revalidación current-standard append-only

**Responsable:** VAEP :48 Debt bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N8.6.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ya revalidado ni ampliar el alcance de WhatsApp hacia una API de proveedor inexistente.

**Evidencia current-standard:** `N8.6.A-G=LISTO` bajo revalidación vigente; `N8.6.G` receipt `vaep/evidence/receipts/N8.6.G_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T084800Z_SUP36.json`, REVIEW_FIRST P0=0/P1=0 y gate causal de handoff `wa.me` en PASS. El contrato certificado es `USER_INITIATED_HANDOFF`: no se afirma envío server-side, delivery ni read receipt de Meta/Twilio. `TASKS.md` ya fue reconciliado de forma aditiva con compare `additions=11/deletions=0`.

**Control:** esta publicación resuelve únicamente el P1 documental de `CHANGELOG_AI.md`. No declara por sí sola `N8.6.H=LISTO`: todavía exige REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional, receipt H persistido/releído y reconciliación de `COLA/CONFIG` antes de promover `N8.7.A`. Sin cambios a `main`, Producción, deploys, secretos, DNS/certificados ni PR #2.

## 2026-09-17 — ERP-N8.7 Performance — certificación current-standard append-only

**Responsable:** `CHATGPT_CONTROLLER` bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** reconciliar documentalmente N8.7.H sin reabrir producto ni convertir una corrida Development en SLA/SLO de Producción. Esta entrada preserva byte-for-byte toda la historia previa y agrega únicamente la evidencia current-standard de N8.7.

**Evidencia:** functional head `45f069e9997e0cb9de4682844ff64dc9addc8674`; workflow `N8.7.G - Performance proof` run `35219847763=SUCCESS`; REVIEW_FIRST G `12cb5b02bad4a5d1dc22581d276738525fff9d3f`; receipt G `cb4ed1ce54ff2767a0c8c0080d55395173cac74c`; artifact `10496399028` con digest `sha256:68f2912c06d84feb0c2c25d148748c6daf02aa61f14b132151aef810f2ecbd9c`; pruebas dirigidas 18/18; 18,594 requests materiales y 0 fallos; contrato de seguridad PASS con Production traffic/data=0, sin auth bypass ni rate-limit disable y datos únicamente sintéticos.

**Documentación:** `docs/evidencias/performance/N8.7_CURRENT_STANDARD_MATERIAL_PERFORMANCE_CERTIFICATION_20260917.md`, junto con los preflight/guards A/F ya existentes. No aplica nuevo OpenAPI, ADR ni ERD porque el cierre no introduce contrato HTTP, decisión arquitectónica ni cambio de dominio/esquema.

**Control:** esta entrada resuelve el changelog append-only requerido por DOC_CERT, pero no declara por sí sola `N8.7.H=LISTO`; el cierre depende todavía de REVIEW_FIRST final P0=0/P1=0, receipt y write/readback. No se tocó `main`, Producción, PR #2, secretos, DNS ni certificados.

## 2026-09-17 — ERP-N8.8 Backup y restauración — revalidación current-standard append-only

**Responsable:** Tarea Supervisión :00 bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** resolver de forma estrictamente aditiva/history-preserving los dos P1 documentales de `N8.8.H`, sin reabrir runtime ya certificado ni ejecutar un restore, fork, upgrade o acción pagada sobre Aiven. El estado operativo vigente continúa en `CONFIG/COLA`.

**Evidencia:** `N8.8.A-G=LISTO` bajo revalidación vigente; M11 backup operativo run `35223693841=SUCCESS` y backup/restauración run `35223693868=SUCCESS` sobre tested head `ab7b5a35cdc91312254b8a96b13ac24c53e28f44`; Aiven DEV read-only proof run `35223698590`, attempt 4, `SUCCESS`, con MySQL `RUNNING`, tres backups observados, PITR y política de retención accesibles y capacidad/autorización de restore identificada. El blocker histórico por token Aiven expirado quedó resuelto antes del cierre de G. La superficie ejecutable de backup/restore mantiene equivalencia causal por blobs invariantes, documentada en `docs/CERTIFICACION_N8_8_BACKUP_CURRENT_STANDARD.md`.

**Control:** esta publicación resuelve únicamente los P1 de reconciliación documental de `TASKS.md` y `CHANGELOG_AI.md`. No declara por sí sola `N8.8.H=LISTO`: todavía exige hard verify append-only, REVIEW_FIRST final P0=0/P1=0, receipt H persistido/releído y reconciliación `COLA/CONFIG` antes de promover `N8.9.A`. No se tocó `main`, Producción, PR #2, secretos, DNS/certificados, planes ni datos productivos.
