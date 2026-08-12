# PROJECT_CONTEXT — VariApp

> Fuente principal de contexto técnico para agentes. No reconstruir el proyecto desde cero en cada sesión.

## 1. Estado canónico

- `PROJECT_ID`: `VARIAPP`
- Repositorio: `jmejia31/VariApp`
- Rama de trabajo: `Desarrollo`
- `main`: congelada
- PR oficial `Desarrollo -> main`: #2, abierto y Draft hasta autorización expresa de Javier Mejía
- Entornos lógicos: `varistorehn_producción` y `varistorehn_desarrollo`
- Plan rector: ERP V5, con ERP-N0 antes de ERP-N1 y continuidad N1→N9; transversales T0–T12 cuando correspondan.

## 2. Arquitectura resumida

VariApp/VariStorehn evoluciona hacia un ERP empresarial. Backend ASP.NET Core 8 Web API con capas Domain/Application/Infrastructure/API, EF Core 8 + Pomelo/MySQL, JWT/BCrypt, RBAC relacional, auditoría e integraciones Cloudinary/QuestPDF/SMTP. Frontend Angular 20 standalone con Signals, Angular Material, guards de autenticación/permisos, servicios y features lazy. E2E con Playwright.

Áreas funcionales principales: productos/variantes/catálogos, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, descuentos, impuestos, envíos, cargas masivas, auditoría, reportes y tienda pública VariStorehn.

Consultar `ARCHITECTURE.md` únicamente cuando el cambio sea estructural/transversal y `PROJECT_INDEX.md` para localizar responsabilidades.

## 3. Persistencia, seguridad e invariantes

- MySQL mediante EF Core/Pomelo y migraciones forward-only cuando aplique.
- JWT y permisos relacionales; no reintroducir bypass de administrador legacy.
- Mantener compatibilidad controlada durante retiro legacy de ERP-N0.
- No tocar Producción ni `main` desde el flujo de Desarrollo.
- No exponer secretos ni inventar validaciones externas.

## 4. Gobierno colaborativo

`AGENTS.md` es vinculante. Cada conversación comienza verificando identidad del proyecto y leyendo únicamente `AGENTS.md`, este contexto, `TASKS.md` y la última entrada relevante de `CHANGELOG_AI.md`. No releer archivos ya documentados si no cambiaron. ChatGPT trabaja remotamente vía conectores autorizados; Javier/Codex/AntiG pueden operar el checkout local autorizado.

Cada changeset intencional deja evidencia en `CHANGELOG_AI.md`; `TASKS.md` se actualiza cuando cambia el estado operativo. Índice/arquitectura/contexto solo cambian cuando cambia la realidad que describen.

## 5. VAEP — ejecución autónoma

Desde 2026-08-11 existe `PLAN_EJECUCION_AUTONOMA.md` como protocolo VAEP v1 y el tablero Google Sheets `VariApp — Cola de Ejecución Autónoma VAEP`:

https://docs.google.com/spreadsheets/d/1RSgaF6q9wnvWT6cSO3bsxpesofompYUYUA7aohPMWTM/edit

Drive funciona como cola operativa editable; GitHub sigue siendo la autoridad técnica y de evidencia. ChatGPT puede consumir tareas de forma recurrente. Una tarea bloqueada no paraliza toda la cola: solo se salta hacia una tarea que no dependa directa ni transitivamente de la bloqueada. Estados: `PENDIENTE`, `EN_PROGRESO`, `VALIDANDO`, `LISTO`, `BLOQUEADO` y `CANCELADO` cuando esté expresamente justificado.

## 6. Continuidad técnica vigente

M0–M13 permanecen como baseline histórico cerrado. ERP-N0 está en ejecución. El Punto 5 de MetodoPago histórico fue cerrado y certificado; el siguiente punto debe determinarse desde el plan/documentación vigente, no inferirse por nombres de migración.

El repositorio también contiene trabajo concurrente de escaparate/tienda VariStorehn. Todo agente debe preservar commits ajenos y reconciliar el HEAD antes de publicar; nunca force-push.

## 7. Regla de actualización

Actualizar este archivo solo ante cambio real de arquitectura, stack, gobierno transversal, fuente de verdad, roadmap rector o flujo autónomo. Para avances ordinarios usar `TASKS.md` y `CHANGELOG_AI.md` sin reescribir este contexto completo.
