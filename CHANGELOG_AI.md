# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT, Chat B (ChatGPT Business) y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

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

**Evidencia:** las microtareas fueron concluidas según su dominio y el cierre permanece sustentado por la certificación canónica ya versionada.

## 2026-08-24 — Codex — ejecutor Jules v3.25

- Se alineó `.github/scripts/vaep-jules-worker-v320.sh` con semántica v3.25 conservando el nombre por compatibilidad con cuatro workflows.
- Los lanes Jules A/B/C/D ahora identifican v3.25; se preservaron v4.6, ATTEMPT1+R2, R3 prohibido, QA takeover, doble revisión, artifacts/Issues, `Desarrollo` y prohibición de push/merge/deploy Jules.

## 2026-08-24 — Codex — autoridad VAEP/Jules v3.25

- Se unificó la gobernanza documental en `V3.25_CURRENT`, cierre por padre y checkpoints `:00/:15/:30/:45/:55`, preservando control-plane global v4.6.

## 2026-08-24 — Codex — reconciliación documental ChatGPT/VAEP

- Se amplió `docs/CONTEXTO_CHATGPT_VAEP.md` con roles, ciclo automático, mutex/actividad/CI/handoff, fuentes de verdad, consulta selectiva, estado local observable y mejoras priorizadas.

## 2026-08-24 — Codex — guía operativa por dominio

- Se amplió `PROJECT_INDEX.md` con mapa por capas, matriz por dominio, flujos transversales y límites de inspección para cambios locales.

## 2026-08-24 — Codex — contexto ChatGPT/VAEP

- Se incorporó `docs/CONTEXTO_CHATGPT_VAEP.md` como referencia histórica/operativa de VariApp.

## 2026-08-24 — Codex — mapa técnico persistente

- Se consolidó `PROJECT_INDEX.md` como mapa rápido con índice de decisión, puntos de entrada y comandos verificados.

## 2026-08-23 — ERP-N3.1 Cotizaciones — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante PARENT-CLOSURE-FIRST y QA takeover documental.

**Objetivo/alcance:** cerrar N3.1.A-H con Cotización como documento comercial previo al Pedido de Venta, snapshots de cliente/producto y lifecycle `Borrador → Enviada → Aceptada/Rechazada → Convertida`, sin adelantar el dominio de Pedidos N3.2.

## 2026-08-23 — ERP-N2.9 Evaluación de proveedores — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante QA takeover y cierre canónico parent-first.

## 2026-08-22 — ERP-N2.8 Cuentas por pagar — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante cierre canónico parent-first.

## 2026-08-22 — ERP-N2.7 NotaCreditoProveedor — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP mediante QA takeover v3.21.

## 2026-08-19 — ERP-N2.2 OrdenCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-18 — ERP-N2.1 SolicitudCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-17 — ERP-N1.9 Series, lotes y vencimientos — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-17 — ERP-N1.8 Reservas de inventario — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-14 — ERP-N1.3 Ubicaciones internas de almacén — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-14 — ERP-N1.2 Almacenes empresariales — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## ERP-N1.1 — Sucursales empresariales

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-14 — ERP-N0.8 Migraciones y limpieza — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-14 — ERP-N0.7 AjusteInventario formal — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-13 — ERP-N0.6 Referencias polimórficas críticas — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-08-13 — ERP-N0.5 MetodoPago — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

## 2026-09-11 — ERP-N6.4 Usuarios por empresa — cierre documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver de forma estrictamente aditiva y byte-preserving el único P1 documental abierto de `N6.4.H`. `TASKS.md` y `docs/CERTIFICACION_N6_4_USUARIOS_POR_EMPRESA.md` conservan la evidencia funcional y de certificación; esta entrada no reabre código de producto ni adelanta `N6.5`.

**Evidencia:** `N6.4.A–G=LISTO_REAL`; functional head final `3f788ec78b03b820592c7a514fa3a63e345b3754`; baseline documental `375c7f76625a2b23b1acbd839e368b3d0e5a11e3`; REVIEW_FIRST bloqueante `vaep/evidence/reviews/N6.4.H_REVIEW_FIRST_20260911T2041Z_SUP24.json`.

**Control:** esta publicación cierra únicamente el P1 de append history-preserving. No declara por sí sola `N6.4.H=LISTO_REAL`.

## 2026-09-12 — ERP-N6.8 Storage aislado — cierre documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :00.

**Objetivo/alcance:** reconciliar de forma estrictamente aditiva/history-preserving el cierre documental de `N6.8.H`, sin reabrir código runtime ni adelantar `N6.9`.

**Evidencia:** `N6.8.A–G=LISTO_REAL`; candidate de storage/TEST_CI `f5d29e7471762a3fe6fe735ba98c2ad1f0188727`; causal storage-isolation run `34714029058`, job `103607801664=SUCCESS`.

**Control:** esta publicación resuelve el P1 de `CHANGELOG_AI.md` de forma append-only. No declara por sí sola `N6.8.H=LISTO_REAL`.

## 2026-09-13 — ERP-N7.1 Libro digital + Outbox — cierre documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :12.

**Objetivo/alcance:** reconciliar de forma estrictamente aditiva/history-preserving el lado `CHANGELOG_AI.md` del cierre `N7.1.H`, sin modificar runtime ni adelantar `N7.2`. La certificación canónica es `docs/CERTIFICACION_N7_1_OUTBOX.md`; el alcance certificado mantiene contexto de empresa/tenant, idempotencia durable y registro Outbox transaccional, sin inventar dispatcher/retry de N7.2+.

**Evidencia:** `N7.1.A–G=LISTO_REAL`; receipt de G `vaep/evidence/fragments/N7.1.G_LISTO_REAL_20260913T070412Z.json`; functional candidate heredado `2bb8148c1119a0a6c433b60cd9a893a3933e4a3e`; material DOC_CERT `9741584e0c5439648dda26c1faad0eab69f768d7`; REVIEW_FIRST vigente `2c0ab6a38aeb065009fa585cc0f1e3430d0d3487` mantiene `P0=0/P1=1` exclusivamente por este append.

**Gates heredados causales:** run `34743908510`, job `103688163982=SUCCESS`; run `34743908521`, job `103688167028=SUCCESS`. `N7.1.H` introduce sólo delta documental, por lo que runtime build/tests permanece no aplicable salvo causalidad nueva.

**Control:** esta publicación no declara por sí sola `N7.1.H=LISTO_REAL`: exige REVIEW_FIRST fresco `P0=0/P1=0`, DoD PASS, receipt H persistido/releído y reconciliación de `COLA/CONFIG` antes de promover `N7.2.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.
