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
- [x] N0.5.06 Eliminar doble autoridad enum/string, subdividido para evitar un changeset transversal gigante:
  - [x] N0.5.06 A1: preparar `IVentaRepository`/`VentaRepository` para resolver y cargar `MetodoPagoCatalogo`. Funcional `d987cb669de6dfbd00b8691a46e27f566e32138c`; backend y certificación ERP-N0.5 en verde.
  - [x] N0.5.06 A2: migrar escrituras de `VentaService` hacia `MetodoPagoId`/catálogo. Funcional `32feca8840122c7eccd58246a6db7196730d8491`; pruebas dirigidas `e00e20c614c8c66c34f726c82ef4922d48dc21d8`.
  - [x] N0.5.06 A3: migrar lecturas/propagación de Venta hacia la relación. Funcional `c024cc7c96da45f6d2b21867950de3c4dce49fd4`; pruebas dirigidas `05687cffcf9d34b3fdd8efd9becf9d158b61f028`.
  - [x] N0.5.06 B: retirar autoridad legacy de `FacturaPago`. Implementación hasta `d5e9a98c17848001fc64387c709a72ce0e379cd3`; fixtures relacionales `e8ab2b733affea70ba47b3ea8a7ff450c6b7766f`; CI general `31567189353` y ERP-N0.5 `31567189393` en verde.
  - [x] N0.5.06 C: retirar autoridad legacy de `MovimientoFinanciero`. Funcional `0f14b9b9f5248a01cb6c98fa456cd306fe38ae19`; CI general `31568099446` y ERP-N0.5 `31568099373` en verde.
- [ ] N0.5.07 Reglas operativas — subdividido para mantener cambios pequeños:
  - [x] N0.5.07A: elegibilidad `Activo` + preservación histórica. Funcional `11c958ead2a7a8cc5a3b1db4b502cbe63e8efba7`; CI general `31571200414` y ERP-N0.5 `31571200316` en verde.
  - [ ] N0.5.07B: `RequiereReferencia` + `RequiereBanco` — subdividida; trabajo concurrente controlado por `COLA`.
  - [ ] N0.5.07C: `PermiteCambio` + `Orden` + `Metadata`.
- [ ] N0.5.08 Backend/API/CRUD/DTOs.
- [ ] N0.5.09 Frontend administrable/selectores dinámicos.
- [ ] N0.5.10 RBAC + auditoría.
- [ ] N0.5.11 Reportes/facturas/PDFs.
- [ ] N0.5.12 Tests de regresión.
- [ ] N0.5.13 Workflow CI dedicado — **reconciliar primero con evidencia GitHub existente; no duplicar**.
- [ ] N0.5.14 Recertificación M13.
- [ ] N0.5.15 Documentación formal y cierre.

## ERP-N0.6 — Referencias polimórficas críticas

- [x] N0.6.A Auditoría y preflight: autoridad legacy de inventario, estado relacional parcial de finanzas, productores/consumidores, riesgos, transición, rollback y plan de pruebas documentados en `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md`.
- [x] N0.6.B Dominio y contratos: origen tipado puro `Compra`/`Venta`/`ConsumoInsumo` con invariante de exactamente un origen. Funcional `5fe605cc93470a4f4b90f73185016b9e15bc622e`; CI general `31575657900` Backend Release/pruebas `SUCCESS`.
- [x] N0.6.C Persistencia/migración — transición fail-closed cerrada en cambios pequeños:
  - [x] N0.6.C1 Preflight histórico read-only: valida tipos legacy admitidos, IDs positivos y documentos origen existentes antes de cualquier backfill. Funcional hasta `8b1ca4ceae848280cea59ba7103e6cd7ef227170`; workflow dedicado `31577099764` y CI general `31577099759` en verde.
  - [x] N0.6.C2 FKs tipadas nullable + migración/backfill determinista, preservando columnas legacy durante transición. Funcional `7375a61165b7e9e32feb6054e843937963472e67`; ERP-N0.6 `31579173571` y CI general `31579173553` en verde.
  - [x] N0.6.C3 Postcheck, constraints e integridad histórica. Corrección final `01c1116e6db4e839b56176333251e3992fa09d77`; ERP-N0.6 `31581993553` y CI general `31581993565` en verde.
- [ ] N0.6.D Aplicación, servicios y API — subdividida para mantener autoridad tipada por concern:
  - [x] N0.6.D1 Repositorio/consultas de `MovimientoInventario` migradas a `CompraId` como autoridad relacional. Funcional `2a2e093f66899b9c02c18026ecd3f270b6a730c1`; corrección de fixture `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`; CI general `31585718867` en verde completo.
  - [ ] N0.6.D2 Productores de `MovimientoInventario` — subdivididos:
    - [ ] N0.6.D2A Mapping de `CompraId/VentaId/ConsumoInsumoId` en dominio/EF — implementación publicada; `VALIDANDO`.
    - [ ] N0.6.D2B `CompraService`/`VentaService`/`ConsumoInsumoService` escriben FKs tipadas.
  - [ ] N0.6.D3 Contrato DTO/API de inventario + verificación de autoridad tipada en finanzas.
- [ ] N0.6.E–H Frontend si aplica, seguridad/auditoría, QA/CI y certificación según `COLA`.

N0.6 puede avanzar de forma independiente del trabajo todavía abierto de N0.5 cuando sus dependencias propias estén satisfechas; N0.7 sí depende de N0.6 según el Plan Maestro.

Después, VAEP continúa automáticamente con N0.6, N0.7, N0.8 y `GATE-N0`. Si una tarea queda bloqueada, puede saltar solo a una tarea sin dependencia directa/transitiva de la bloqueada.

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

Protocolo: `PLAN_EJECUCION_AUTONOMA.md`.

## Continuidad

En nueva conversación/sesión: confirmar proyecto/repo/rama; leer `AGENTS.md`, `PROJECT_CONTEXT.md`, este archivo y última entrada relevante de `CHANGELOG_AI.md`. Si es ejecución autónoma, leer además `PLAN_EJECUCION_AUTONOMA.md` y el tablero VAEP. Revisar solo commits/archivos que cambiaron y continuar sin reindexar el proyecto.