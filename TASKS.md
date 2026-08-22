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
- [x] N0.5.07 Reglas operativas — cerrada en hijos pequeños y certificada por CI.
- [x] N0.5.08 Backend/API/CRUD/DTOs.
- [x] N0.5.09 Frontend administrable/selectores dinámicos.
- [x] N0.5.10 RBAC + auditoría.
- [x] N0.5.11 Reportes/facturas/PDFs.
- [x] N0.5.12 Tests de regresión.
- [x] N0.5.13 Workflow CI dedicado.
- [x] N0.5.14 Recertificación M13.
- [x] N0.5.15 Documentación formal y cierre.

## ERP-N0.6 — Referencias polimórficas críticas

- [x] N0.6.A-H completados y certificados.

## ERP-N0.7 — AjusteInventario formal

- [x] N0.7.A-H completados y certificados.

## ERP-N0.8 — Migraciones y limpieza

- [x] N0.8.A-H completados y certificados.

## ERP-N1.1 — Sucursales

- [x] N1.1.A-H completados y certificados.

## ERP-N1.3 — Ubicaciones internas de almacén

- [x] N1.3.A-H completados y certificados.

## ERP-N1.2 — Almacenes empresariales

- [x] N1.2.A-H completados y certificados.

## ERP-N1.5 — Kardex empresarial

- [x] N1.5.A-H completados y certificados.

## ERP-N1.6 — Transferencias entre almacenes

- [x] N1.6.A-H completados y certificados.

## ERP-N1.7 — Conteos físicos

- [x] N1.7.A-H completados y certificados.

## ERP-N1.8 — Reservas de inventario

- [x] N1.8.A-H completados y certificados.

## ERP-N1.9 — Series, lotes y vencimientos

- [x] N1.9.A-H completados y certificados.

## ERP-N1.10 — Costeo empresarial

- [x] N1.10.A-H completados y certificados.

## ERP-N2.1 — Solicitud de compra

- [x] N2.1.A-H completados y certificados.

## ERP-N2.2 — Orden de compra

- [x] N2.2.A-H completados y certificados por evidencia autoritativa VAEP.

## ERP-N2.7 — Nota de crédito de proveedor

- [x] N2.7.A-H completados — paquete documental canónico `c466ec3099c2a498c2353af82b99ce0be9d46e29`. Baseline funcional certificado `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`; Development, Acceptance, Fase 8 y M13 en SUCCESS. Sin defectos bloqueantes P0/P1 conocidos.

**ERP-N2.7 queda formalmente cerrado.**

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit
