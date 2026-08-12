# TASKS — VariApp

Fuente resumida de pendientes. El detalle operativo vive en VAEP v2; no duplicar aquí el Plan Maestro completo.

## Reglas

- Todo changeset intencional debe quedar en `CHANGELOG_AI.md`.
- GitHub es autoridad técnica; el Sheet VAEP es tablero operativo.
- No iniciar ERP-N1 hasta `GATE-N0=LISTO`.
- No releer archivos ya documentados salvo que hayan cambiado.

## Gobierno y productividad

- [x] Memoria canónica y gobierno colaborativo.
- [x] Gate de identidad `PROJECT_ID=VARIAPP`.
- [x] Guardas pre/post commit locales.
- [x] VAEP v1 creado.
- [x] VAEP v2: Plan Maestro ERP V5 completo cargado, granularizado, con gates, tracks, dashboard y regla de bloqueo transitivo.
- [x] Plan Maestro original convertido a Google Docs como fuente rectora permanente para el runner.
- [x] Cola granular v2 creada con 778 microtareas y 131 filas de plan/gobierno.
- [ ] Javier/Codex/AntiG: sincronizar checkout local y ejecutar `scripts/configurar-colaboracion.ps1` cuando corresponda.
- [x] `VAEP-001`: optimizar triggers redundantes de GitHub Actions sin reducir cobertura necesaria. Cerrado en `d2466a3047e7cd2001f1cf998faa08c4ae229c1b`.

## ERP-N0.5 — MetodoPago

Estado operativo controlado por `COLA`:

- [x] N0.5.01 Análisis y diagnóstico.
- [x] N0.5.02 Diseño funcional.
- [x] N0.5.03 Auditoría legacy.
- [x] N0.5.04 Entidad/persistencia relacional.
- [x] N0.5.05 Seed/preflight/backfill histórico.
- [ ] N0.5.06 Eliminar doble autoridad enum/string, subdividido para evitar un changeset transversal gigante:
  - [ ] N0.5.06 A1: preparar `IVentaRepository`/`VentaRepository` para resolver y cargar `MetodoPagoCatalogo`; implementación incluida en el changeset funcional actual y pendiente de certificación CI antes de marcar `LISTO`.
  - [ ] N0.5.06 A2: migrar escrituras de `VentaService` hacia `MetodoPagoId`/catálogo.
  - [ ] N0.5.06 A3: migrar lecturas/propagación de Venta hacia la relación.
  - [ ] N0.5.06 B: retirar autoridad legacy de `FacturaPago`.
  - [ ] N0.5.06 C: retirar autoridad legacy de `MovimientoFinanciero`.
- [ ] N0.5.07 Reglas operativas.
- [ ] N0.5.08 Backend/API/CRUD/DTOs.
- [ ] N0.5.09 Frontend administrable/selectores dinámicos.
- [ ] N0.5.10 RBAC + auditoría.
- [ ] N0.5.11 Reportes/facturas/PDFs.
- [ ] N0.5.12 Tests de regresión.
- [ ] N0.5.13 Workflow CI dedicado — **reconciliar primero con evidencia GitHub existente; no duplicar**.
- [ ] N0.5.14 Recertificación M13.
- [ ] N0.5.15 Documentación formal y cierre.

Después, VAEP continúa automáticamente con N0.6, N0.7, N0.8 y `GATE-N0`. Si una tarea queda bloqueada, puede saltar solo a una tarea sin dependencia directa/transitiva de la bloqueada.

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

Protocolo: `PLAN_EJECUCION_AUTONOMA.md`.

## Continuidad

En nueva conversación/sesión: confirmar proyecto/repo/rama; leer `AGENTS.md`, `PROJECT_CONTEXT.md`, este archivo y última entrada relevante de `CHANGELOG_AI.md`. Si es ejecución autónoma, leer además `PLAN_EJECUCION_AUTONOMA.md` y el tablero VAEP. Revisar solo commits/archivos que cambiaron y continuar sin reindexar el proyecto.
