# PROJECT_CONTEXT — VariApp

> Fuente principal de contexto técnico compartido para ChatGPT/VAEP, Chat B, Codex cuando sea autorizado y cualquier colaborador futuro. No reconstruir el proyecto desde cero ni confiar en snapshots antiguos.

## 1. Estado canónico

- PROJECT_ID: VARIAPP
- Repositorio: `jmejia31/VariApp`
- Rama de trabajo: `Desarrollo`
- `main`: congelada; no tocar sin autorización expresa.
- PR oficial `Desarrollo -> main`: #2, abierto y Draft; no merge automático.
- Entornos lógicos: `varistorehn_producción` y `varistorehn_desarrollo`.
- Plan rector: **Plan Maestro ERP V5 — VariApp**.
- Orden estricto: ERP-N0 -> N1 -> N2 -> N3 -> N4 -> N5 -> N6 -> N7 -> N8 -> N9.
- Tracks obligatorios: T0–T12.

Plan rector en Drive:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero operativo:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

## 2. Regla de lectura del estado vivo

Ningún colaborador debe tomar este archivo, un prompt viejo, una conversación, un Issue, una fila de Sheet o un artifact como prueba suficiente de estado actual.

Antes de actuar, releer en este orden:

1. `docs/VAEP_AUTHORITY.md` — autoridad única de reglas.
2. HEAD vivo de `Desarrollo`.
3. `vaep/control/jules-autorefill-catalog.json` — `currentParent`, `lastClosedParent`, `closureReceipts`, lanes y `throughputPlan`.
4. `vaep/control/dispatch-admission.json` — `OPEN/FROZEN` y razón.
5. Recibo `LISTO_REAL` del parent recién cerrado y sus `runId/headSha` causales.
6. GitHub Actions del HEAD/control relevante y el último `VAEP scheduled checkpoints`.
7. `COLA`, `PLAN_MAESTRO`, `CONFIG`, `DASHBOARD`, `TAREAS_PROGRAMADAS` y `CONTROL_TOWER` en el Sheet cuando se necesite estado operativo o extender roadmap.

Si dos superficies difieren, GitHub/CI prueba actividad técnica real y el Sheet debe reconciliarse; no se inventa un PASS/LISTO para hacer coincidir reportes.

## 3. Arquitectura resumida

VariApp/VariStorehn evoluciona hacia un ERP empresarial. Backend ASP.NET Core 8 Web API con capas Domain/Application/Infrastructure/API, EF Core 8 + Pomelo/MySQL, JWT/BCrypt, RBAC relacional, auditoría e integraciones Cloudinary/QuestPDF/SMTP. Frontend Angular 20 standalone con Signals, Angular Material, guards de autenticación/permisos, servicios y features lazy. E2E con Playwright.

Áreas principales: productos/variantes/catálogos, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, descuentos, impuestos, envíos, cargas masivas, auditoría, reportes y tienda pública VariStorehn.

Consultar `PROJECT_INDEX.md` para localizar responsabilidades. Abrir `ARCHITECTURE.md` solo ante cambio estructural/transversal; cambios del mapa se registran en `ARCHITECTURE_CHANGELOG.md`.

## 4. Persistencia, seguridad e invariantes

- MySQL mediante EF Core/Pomelo y migraciones forward-only cuando aplique.
- JWT y permisos relacionales; no reintroducir bypass de administrador legacy.
- No tocar Producción ni `main` desde el flujo de Desarrollo.
- No exponer secretos ni inventar validaciones externas.
- No force-push.
- Preservar commits ajenos y reconciliar HEAD antes de publicar.

## 5. Gobierno colaborativo vigente

`AGENTS.md` es vinculante. ChatGPT/VAEP y Chat B operan como controller/QA bajo el MAESTRO. El runtime canónico de implementación/autorefill es **J1–J6**; referencias Jules A/B/C/D son legado/histórico y no deben usarse para crear trabajo nuevo. Codex participa solo por orden explícita del usuario. AntiG/Antigravity permanece `RESERVED_INACTIVE`: puede leer este handoff, pero no tiene scheduler, procesamiento de handoffs ni autoridad `LISTO_REAL` hasta autorización explícita futura.

Todo changeset intencional deja evidencia en `CHANGELOG_AI.md`. `TASKS.md` cambia cuando cambia el estado operativo. Índice/arquitectura/contexto solo cambian cuando cambia la realidad que describen.

## 6. VAEP — ejecución autónoma integral

Protocolo: `PLAN_EJECUCION_AUTONOMA.md`.

Autoridad operativa única: `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`). Toda regla se edita allí; no se seleccionan protocolos por etiquetas históricas.

El Sheet representa el Plan Maestro ERP V5 completo y lo traduce a `COLA` granular. GitHub/CI prueban actividad real; el Sheet describe y reconcilia estado operativo.

Reglas esenciales: `PARENT_CLOSE_FIRST`, `REVIEW_FIRST`, causalidad real, P0/P1=0 antes de `LISTO_REAL`, trabajo material no duplicado, no busywork, máximo ATTEMPT1+R2 y R3 prohibido salvo cambio explícito de política.

Los gates `GATE-N0` ... `GATE-N9` hacen cumplir orden de fases y DoD. Una tarea `BLOQUEADO` no paraliza toda la cola: solo puede saltarse hacia una tarea sin dependencia directa/transitiva de la bloqueada.

## 7. Handoff operativo — 2026-09-09

Estado verificado durante la reparación:

- `N5.1.H` quedó certificado `LISTO_REAL` y cerrado.
- Recibo usado por el catálogo: `vaep/evidence/fragments/N5.1.H_LISTO_REAL_20260909T1236Z.json`.
- Functional HEAD causal: `65406f48f071367a368e75b32f82fda3fc915f5b`.
- Gate funcional: `Desarrollo - aceptación funcional integral`, run `34339662570`, `SUCCESS`, Playwright `100/100`.
- Promoción atómica H -> N5.2.A: commit `4a6dfa7a59d46afcd9589a380d07eee4f61cc42c`.
- `N5.2.A` es el `currentParent`; `lastClosedParent=N5.1.H`.
- `dispatch-admission=OPEN`, razón `VERIFIED_ROADMAP_PROMOTION__N5.1.H__N5.2.A`.
- Scope material vigente: `N5.2.A.1.REPORTES_INVENTARIO_PREFLIGHT_QA`, J5, archivo `docs/N5.2_REPORTES_INVENTARIO_PREFLIGHT_QA.md`.
- Dispatch materializado: commit `1803d747bd9e1f669e57c6b9b55cfc5e284081ff`.
- El catálogo fue reconciliado para que `throughputPlan`, roadmap note y reason de J5 también reflejen N5.2.A; no fabricar sucesor si el extracto de roadmap todavía no lo contiene.

Reparaciones de automatización realizadas el 2026-09-09:

- Se recrearon y habilitaron las 5 tareas externas canónicas de ChatGPT: `:00`, `:12`, `:24`, `:36`, `:48`, zona `America/Tegucigalpa`.
- Se corrigió `.github/scripts/vaep-parent-close.sh`: ya no exige como causal un workflow hardcodeado no aplicable; verifica los `runId/headSha` declarados por el recibo y conserva fallback legacy.
- Commits de hardening relevantes: `3fa2015aad49d7bcdf80dc74c98012362beafb6c` y `efff38fcefe5d7f06cc4e1dcbd5f22234ad8ae56`.
- Se corrigió `scripts/vaep/parent_transition.py` para mantener coherentes `currentParent`, `throughputPlan`, roadmap note y razones de lane en futuras promociones.

Base observada justo antes de esta resincronización de contexto: `0990d7ff736fff48b195208faae180a1db127a0c`. **No tratar esa SHA como HEAD eterno:** cada colaborador debe reconsultar `Desarrollo` al comenzar.

## 8. Qué debe buscar cualquier colaborador al reanudar

- Si `currentParent` cambió desde N5.2.A, localizar primero el nuevo recibo de cierre y la transición que lo promovió.
- Si `admission=FROZEN`, leer la razón exacta antes de despachar; una freeze de seguridad/manual no se despeja automáticamente.
- Si `VAEP_CLOSE=BLOCKED`, buscar el diagnóstico causal exacto en el job, no generar trabajo para ocultar el bloqueo.
- Si no hay scope material seguro, detener refill y extender roadmap solo desde Plan/COLA frescos; nunca por orden léxico ni por llenar J1–J6.
- Verificar que PR #2 siga `open + draft + unmerged` y que `main` no haya cambiado por este flujo.
- Mantener visibles como deuda no-P0/P1 los gaps aceptados de N5.1 (B4, C1–C4, D1–D2, E4–E5); no convertirlos silenciosamente en PASS.

## 9. Regla de actualización

Actualizar este archivo ante cambio real de arquitectura, gobierno transversal, fuente de verdad, roadmap rector o flujo autónomo. Para avances ordinarios usar `TASKS.md`, `CHANGELOG_AI.md` y el tablero VAEP; si un avance cambia el handoff que otros agentes necesitan para operar sin ambigüedad, actualizar también esta sección de estado compartido.
