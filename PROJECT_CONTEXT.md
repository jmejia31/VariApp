# PROJECT_CONTEXT — VariApp

> Fuente principal de contexto técnico para agentes. No reconstruir el proyecto desde cero en cada sesión.

## 1. Estado canónico

- PROJECT_ID: VARIAPP
- Repositorio: `jmejia31/VariApp`
- Rama de trabajo: `Desarrollo`
- `main`: congelada
- PR oficial `Desarrollo -> main`: #2, abierto y Draft hasta autorización expresa de Javier Mejía
- Entornos lógicos: `varistorehn_producción` y `varistorehn_desarrollo`
- Plan rector: **Plan Maestro ERP V5 — VariApp**.
- Orden estricto: ERP-N0 -> N1 -> N2 -> N3 -> N4 -> N5 -> N6 -> N7 -> N8 -> N9.
- Tracks obligatorios: T0–T12.

Plan rector en Drive:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

## 2. Arquitectura resumida

VariApp/VariStorehn evoluciona hacia un ERP empresarial. Backend ASP.NET Core 8 Web API con capas Domain/Application/Infrastructure/API, EF Core 8 + Pomelo/MySQL, JWT/BCrypt, RBAC relacional, auditoría e integraciones Cloudinary/QuestPDF/SMTP. Frontend Angular 20 standalone con Signals, Angular Material, guards de autenticación/permisos, servicios y features lazy. E2E con Playwright.

Áreas principales: productos/variantes/catálogos, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, descuentos, impuestos, envíos, cargas masivas, auditoría, reportes y tienda pública VariStorehn.

Consultar `PROJECT_INDEX.md` para localizar responsabilidades y su índice de decisión. Abrir `ARCHITECTURE.md` solo ante cambio estructural/transversal; todo cambio del mapa se registra en `ARCHITECTURE_CHANGELOG.md`.

## 3. Persistencia, seguridad e invariantes

- MySQL mediante EF Core/Pomelo y migraciones forward-only cuando aplique.
- JWT y permisos relacionales; no reintroducir bypass de administrador legacy.
- Mantener compatibilidad controlada durante retiro legacy ERP-N0.
- No tocar Producción ni `main` desde el flujo de Desarrollo.
- No exponer secretos ni inventar validaciones externas.

## 4. Gobierno colaborativo

`AGENTS.md` es vinculante. Cada sesión comienza verificando identidad del proyecto y leyendo únicamente contexto/tareas/changelog necesarios. **No releer archivos ya documentados a menos que hayan cambiado.** ChatGPT trabaja remotamente mediante conectores autorizados; Javier/Codex/AntiG pueden operar el checkout local autorizado.

Todo changeset intencional deja evidencia en `CHANGELOG_AI.md`; `TASKS.md` cambia cuando cambia el estado operativo. Índice/arquitectura/contexto solo cambian cuando cambia la realidad que describen.

## 5. VAEP — ejecución autónoma integral

Protocolo: `PLAN_EJECUCION_AUTONOMA.md`.

Autoridad operativa única: `docs/VAEP_AUTHORITY.md` es el MAESTRO permanente (AUTOMATION_AUTHORITY=MASTER). Toda regla se edita allí mismo; no se seleccionan protocolos por etiquetas numéricas históricas. El Sheet describe estado operativo; el sistema de tareas ejecuta y GitHub/CI prueban actividad real.

Tablero operativo:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

VAEP representa el **Plan Maestro ERP V5 completo** en `PLAN_MAESTRO` y lo traduce a una `COLA` granular de **778 microtareas**. El workbook también incluye `DASHBOARD`, `CONFIG`, `BITACORA` y `LEYENDA`.

Cada punto funcional se divide normalmente en microtareas `PRE`, `DOMAIN`, `DB_MIG`, `BACKEND_API`, `FRONTEND_UX`, `SEC_AUDIT`, `TEST_CI` y `DOC_CERT`; si una de ellas sigue siendo grande, debe subdividirse antes de editar. El runner puede cerrar hasta 3 microtareas pequeñas por corrida, deteniéndose antes si el riesgo o alcance crecen.

Los gates `GATE-N0` ... `GATE-N9` hacen cumplir el orden de fases y Definition of Done. T0–T12 acompañan toda la ejecución. Los módulos futuros no-core están registrados pero `NO_AUTORIZADO` y no son autoejecutables.

Una tarea `BLOQUEADO` no paraliza toda la cola: solo puede saltarse hacia una tarea que no dependa directa ni transitivamente de la bloqueada. GitHub sigue siendo autoridad técnica; el Sheet es estado operativo y debe reconciliarse con el HEAD real.

## 6. Continuidad técnica vigente

M0–M13 permanecen como baseline histórico cerrado. ERP-N0 sigue activo.

ERP-N0.5 MetodoPago está cargado con checklist especializado: N0.5.01–N0.5.05 `LISTO`; N0.5.06–N0.5.15 inicialmente `PENDIENTE`, con reconciliación obligatoria antes de duplicar trabajo ya existente en GitHub. Después siguen N0.6, N0.7, N0.8 y `GATE-N0`.

El repositorio también contiene trabajo concurrente de escaparate/tienda VariStorehn. Todo agente debe preservar commits ajenos y reconciliar HEAD antes de publicar; nunca force-push.

## 7. Regla de actualización

Actualizar este archivo solo ante cambio real de arquitectura, stack, gobierno transversal, fuente de verdad, roadmap rector o flujo autónomo. Cuando cambien arquitectura, módulos, integraciones, rutas/API, datos o comandos, actualizar también el mapa de `PROJECT_INDEX.md` y agregar una entrada a `ARCHITECTURE_CHANGELOG.md`. Para avances ordinarios usar `TASKS.md`, `CHANGELOG_AI.md` y el tablero VAEP.
