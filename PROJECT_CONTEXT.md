# PROJECT_CONTEXT — Solqaryn

> Fuente principal de contexto técnico compartido para ChatGPT/VAEP, Chat B, Codex cuando sea autorizado y cualquier colaborador futuro. No reconstruir el proyecto desde cero ni confiar en snapshots antiguos.

## 1. Estado canónico

- PROJECT_ID: SOLQARYN
- Repositorio: `solqaryn/Solqaryn`
- Rama de trabajo: `dev`
- `main`: congelada; no tocar sin autorización expresa.
- PR histórico de la antigua rama DEV -> `main`: #2 está `CLOSED + MERGED` por la liberación ERP-N9 ya ejecutada; no debe reabrirse. Cualquier nuevo merge a `main` o cambio productivo requiere autorización nueva y explícita del propietario.
- Environments GitHub canónicos: `PROD` y `DEV`; VariStoreHN es cliente de la plataforma y no define la identidad de estos environments.
- Identidad corporativa primaria de la plataforma para cuentas/proveedores: `solqaryn.platform@outlook.com`.
- Gobierno GitHub ratificado 2026-09-25: el repositorio canónico permanece en la organización `solqaryn`; `jmejia31` se conserva **intencionalmente como Owner secundario/de recuperación** de la organización. Por ese motivo su permiso efectivo `admin` sobre `solqaryn/Solqaryn` es esperado y **NO es deuda ni acceso residual a retirar**.
- `morales35alex` se conserva como colaborador externo con permiso `write` sobre el repositorio, sin rol Owner/Admin organizacional.
- La permanencia de `jmejia31` como Owner es una excepción de continuidad/recuperación al objetivo general de centralizar proveedores en SOLQARYN; no convierte la cuenta personal en repositorio canónico ni autoriza mover recursos fuera de la organización. No remover ni degradar a `jmejia31` sin una nueva decisión explícita del propietario.
- Plan rector: **Plan Maestro ERP V5 — Solqaryn**.
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
2. HEAD vivo de `dev`.
3. `vaep/control/jules-autorefill-catalog.json` — `currentParent`, `lastClosedParent`, `closureReceipts`, lanes y `throughputPlan`.
4. `vaep/control/dispatch-admission.json` — contrato global **OPEN_ONLY**. `newDispatchAdmission` debe ser exactamente `OPEN`; cualquier otro valor es corrupción operativa que se repara a `OPEN` antes de decidir dispatch. Los bloqueos reales viven en tarea/lane (`dispatchEligible=false`, hold causal, cuarentena o REVIEW_FIRST/QA_TAKEOVER).
5. Recibo `LISTO_REAL` del parent recién cerrado y sus `runId/headSha` causales.
6. GitHub Actions del HEAD/control relevante y el último `VAEP scheduled checkpoints`.
7. `COLA`, `PLAN_MAESTRO`, `CONFIG`, `DASHBOARD`, `TAREAS_PROGRAMADAS` y `CONTROL_TOWER` en el Sheet cuando se necesite estado operativo o extender roadmap.

Si dos superficies difieren, GitHub/CI prueba actividad técnica real y el Sheet debe reconciliarse; no se inventa un PASS/LISTO para hacer coincidir reportes. Ningún snapshot histórico puede reintroducir un estado global distinto de `OPEN`.

## 3. Arquitectura resumida

Solqaryn/VariStorehn evoluciona hacia un ERP empresarial. Backend ASP.NET Core 8 Web API con capas Domain/Application/Infrastructure/API, EF Core 8 + Pomelo/MySQL, JWT/BCrypt, RBAC relacional, auditoría e integraciones Cloudinary/QuestPDF/SMTP. Frontend Angular 20 standalone con Signals, Angular Material, guards de autenticación/permisos, servicios y features lazy. E2E con Playwright.

Áreas principales: productos/variantes/catálogos, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, descuentos, impuestos, envíos, cargas masivas, auditoría, reportes y tienda pública VariStorehn.

Consultar `PROJECT_INDEX.md` para localizar responsabilidades. Abrir `ARCHITECTURE.md` solo ante cambio estructural/transversal; cambios del mapa se registran en `ARCHITECTURE_CHANGELOG.md`.

## 4. Persistencia, seguridad e invariantes

- MySQL mediante EF Core/Pomelo y migraciones forward-only cuando aplique.
- Aiven canónico: proyecto `solqaryn`, servicio único `solqaryn-mysql`, bases `solqaryn_dev`/`solqaryn_prod` y usuarios aislados `solqaryn_dev_user`/`solqaryn_prod_user`; `avnadmin` queda solo para administración.
- Aiven DEV certificado el 2026-09-25 por GitHub Actions run `36175275439`: el proyecto `solqaryn` y servicio `solqaryn-mysql` pertenecen a la organización Aiven que incluye `solqaryn.platform@outlook.com`; el endpoint DEV coincide con ese servicio; MySQL efectivo usa `solqaryn_dev` con `solqaryn_dev_user`, MySQL 8.4.8, 137 tablas base, 107 migraciones EF, 8 productos y 2 categorías. No se tocaron secretos ni Producción.
- Los Environments GitHub canónicos son `DEV` y `PROD`; secretos y variables no se cruzan entre ambos.
- JWT y permisos relacionales; no reintroducir bypass de administrador legacy.
- No tocar PROD ni `main` desde el flujo de DEV.
- No exponer secretos ni inventar validaciones externas.
- No force-push.
- Preservar commits ajenos y reconciliar HEAD antes de publicar.

## 5. Gobierno colaborativo vigente

`AGENTS.md` es vinculante. ChatGPT/VAEP y Chat B operan como controller/QA bajo el MAESTRO. El runtime canónico son exclusivamente las **diez automatizaciones programadas** definidas por `docs/VAEP_AUTHORITY.md`; J1–J6/Jules y las referencias Jules A/B/C/D son legado/histórico y no deben usarse para crear trabajo nuevo. Codex participa solo por orden explícita del usuario. AntiG/Antigravity permanece `RESERVED_INACTIVE`: puede leer este handoff, pero no tiene scheduler, procesamiento de handoffs ni autoridad `LISTO_REAL` hasta autorización explícita futura.

Todo changeset intencional deja evidencia en `CHANGELOG_AI.md`. `TASKS.md` cambia cuando cambia el estado operativo. Índice/arquitectura/contexto solo cambian cuando cambia la realidad que describen.

## 6. VAEP — ejecución autónoma integral

Protocolo: `PLAN_EJECUCION_AUTONOMA.md`.

Autoridad operativa única: `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`). Toda regla se edita allí; no se seleccionan protocolos por etiquetas históricas.

El Sheet representa el Plan Maestro ERP V5 completo y lo traduce a `COLA` granular. GitHub/CI prueban actividad real; el Sheet describe y reconcilia estado operativo.

Reglas esenciales: `PARENT_CLOSE_FIRST`, `REVIEW_FIRST`, `GLOBAL_DISPATCH_ADMISSION=OPEN_ONLY`, causalidad real, P0/P1=0 antes de `LISTO_REAL`, trabajo material no duplicado, no busywork, máximo ATTEMPT1+R2 y R3 prohibido salvo cambio explícito de política.

Los gates `GATE-N0` ... `GATE-N9` hacen cumplir orden de fases y DoD. Una tarea `BLOQUEADO` no paraliza toda la cola: solo puede saltarse hacia una tarea sin dependencia directa/transitiva de la bloqueada. Ningún gate de tarea, review o hardening puede cerrar la admisión global.

## 7. Handoffs históricos

Los snapshots y handoffs fechados anteriores se conservan únicamente en Git history, receipts, artifacts y BITACORA como **EVIDENCIA_NO_EJECUTABLE**. No se duplica aquí ningún `currentParent`, HEAD, admission, sesión ni asignación de lane, porque esos valores caducan y podían contaminar decisiones posteriores.

Para reanudar trabajo, usar exclusivamente la secuencia de lectura viva de la sección 2. El estado global de admisión vigente se deriva únicamente del MAESTRO `OPEN_ONLY` y del JSON actual, nunca de una captura histórica.

## 8. Qué debe buscar cualquier colaborador al reanudar

- Localizar el `currentParent` directamente en el catálogo vivo y comprobar su último recibo de cierre/promoción.
- Exigir `newDispatchAdmission=OPEN`. Cualquier otro valor global es inválido y debe repararse a `OPEN`; la causa técnica se conserva únicamente en la tarea/lane afectada.
- Si `VAEP_CLOSE=BLOCKED`, buscar el diagnóstico causal exacto en el job, no generar trabajo para ocultar el bloqueo.
- Si no hay scope material seguro, detener refill y extender roadmap solo desde Plan/COLA frescos; nunca por orden léxico ni por llenar slots/automatizaciones con trabajo artificial.
- Tratar PR #2 como evidencia histórica `closed + merged` y no reabrirlo. Verificar que `main` no cambie por el flujo ordinario y que no exista un nuevo merge o cambio productivo sin autorización fresca del propietario.
- Mantener visibles como deuda no-P0/P1 los gaps aceptados de N5.1 (B4, C1–C4, D1–D2, E4–E5); no convertirlos silenciosamente en PASS.

## 9. Regla de actualización

Actualizar este archivo ante cambio real de arquitectura, gobierno transversal, fuente de verdad, roadmap rector o flujo autónomo. Para avances ordinarios usar `TASKS.md`, `CHANGELOG_AI.md` y el tablero VAEP; si un avance cambia el handoff que otros agentes necesitan para operar sin ambigüedad, actualizar también esta sección de estado compartido.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Regla vinculante: este archivo solo puede interpretarse con contexto de SOLQARYN. Está prohibido consultar o usar skills, documentación, chats, repositorios, memorias o reglas fuera de SOLQARYN salvo autorización explícita del propietario para la fuente/alcance concreto o una entrada `ACTIVE` en la allowlist versionada. La disponibilidad técnica no equivale a permiso. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/Solqaryn`. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.


