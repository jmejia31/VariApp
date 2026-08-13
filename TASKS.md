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
- [x] N0.5.07 Reglas operativas — cerrada en hijos pequeños y certificada por CI:
  - [x] N0.5.07A: elegibilidad `Activo` + preservación histórica. Funcional `11c958ead2a7a8cc5a3b1db4b502cbe63e8efba7`; CI general `31571200414` y ERP-N0.5 `31571200316` en verde.
  - [x] N0.5.07B: `RequiereReferencia` + `RequiereBanco`; B1/B2 certificados, incluyendo Banco normalizado y fail-closed. Cierre de B2 hasta `16fdf809ced379dff8d1b970ba684644668ec1e5`; CI ERP-N0.5 `31633767440` y general `31633767490` en verde.
  - [x] N0.5.07C: `PermiteCambio` + `Orden` + `Metadata`; C1/C2 certificados hasta `ce3b218b296f3a7de417870659a6ce08de428e40`; CI ERP-N0.5 `31638441486` y general `31638441384` en verde.
- [x] N0.5.08 Backend/API/CRUD/DTOs. DTO/repositorio/servicio/API/DI/RBAC/auditoría completados; permisos `MetodosPago:*` incorporados al catálogo en `b94aa0d9346f6efafe73b7911f07673ef07aceee`; pruebas dirigidas del servicio cerradas en `5827e610cf9cae1b6a3d5745d10e1cee59df6c78`; ERP-N0.5 `31650122695` y CI general `31650122667` en verde completo.
- [x] N0.5.09 Frontend administrable/selectores dinámicos. Cierre `7da9cc73f75598dedbf7630f8b131d7dc5f72af8`; ERP-N0.5 `31662728534`, Desarrollo `31662728587` y M10 `31662728555` SUCCESS.
- [x] N0.5.10 RBAC + auditoría. Cierre `fe669fd0f3138193b04bcbbad96934d4e93b8ccb`; ERP-N0.5 `31671574303` y Desarrollo `31671574330` SUCCESS.
- [x] N0.5.11 Reportes/facturas/PDFs. Cierre `fd841429d04d4663278cf0605be54b13d5b0178b`; ERP-N0.5 `31737978596` y Desarrollo `31737978473` SUCCESS.
- [x] N0.5.12 Tests de regresión. Cierre `eaa52c4b92c6932b33afa8eb2b334ed8dec3593f`; ERP-N0.5 `31745717643`, build `31745717778`, aceptación integral `31745717860` y Fase 8 `31745717633` SUCCESS.
- [x] N0.5.13 Workflow CI dedicado — reconciliado sin duplicar `.github/workflows/erp-n0-5-ci.yml`; run `31745717643` SUCCESS.
- [x] N0.5.14 Recertificación M13. SHA `1bbccd9cccdcc181ab8c1e842ea0ff8343831197`; N0.5 `31753406161`, recovery MySQL `31753406119`, M11 `31753406267`, build `31753406190`, aceptación integral `31753406328` y M13 `31753406059` attempt 2 SUCCESS.
- [ ] N0.5.15 Documentación formal y cierre — documento canónico publicado en `docs/ERP_N0_5_METODOS_PAGO.md`; pendiente únicamente reconciliar changelog/tablero final de esta microtarea.

## ERP-N0.6 — Referencias polimórficas críticas

- [x] N0.6.A Auditoría y preflight: autoridad legacy de inventario, estado relacional parcial de finanzas, productores/consumidores, riesgos, transición, rollback y plan de pruebas documentados en `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md`.
- [x] N0.6.B Dominio y contratos: origen tipado puro `Compra`/`Venta`/`ConsumoInsumo` con invariante de exactamente un origen. Funcional `5fe605cc93470a4f4b90f73185016b9e15bc622e`; CI general `31575657900` Backend Release/pruebas `SUCCESS`.
- [x] N0.6.C Persistencia/migración — transición fail-closed cerrada en cambios pequeños:
  - [x] N0.6.C1 Preflight histórico read-only: valida tipos legacy admitidos, IDs positivos y documentos origen existentes antes de cualquier backfill. Funcional hasta `8b1ca4ceae848280cea59ba7103e6cd7ef227170`; workflow dedicado `31577099764` y CI general `31577099759` en verde.
  - [x] N0.6.C2 FKs tipadas nullable + migración/backfill determinista, preservando columnas legacy durante transición. Funcional `7375a61165b7e9e32feb6054e843937963472e67`; ERP-N0.6 `31579173571` y CI general `31579173553` en verde.
  - [x] N0.6.C3 Postcheck, constraints e integridad histórica. Corrección final `01c1116e6db4e839b56176333251e3992fa09d77`; ERP-N0.6 `31581993553` y CI general `31581993565` en verde.
- [ ] N0.6.D Aplicación, servicios y API — subdividida para mantener autoridad tipada por concern:
  - [x] N0.6.D1 Repositorio/consultas de `MovimientoInventario` migradas a `CompraId` como autoridad relacional. Funcional `2a2e093f66899b9c02c18026ecd3f270b6a730c1`; corrección de fixture `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`; CI general `31585718867` en verde completo.
  - [x] N0.6.D2 Productores de `MovimientoInventario` — boundary typed-first y los tres productores documentales certificados:
    - [x] N0.6.D2A boundary de escritura `typed-first` certificado; corrección final `6eadf19a27a0c7c90b0cec54262070f896209738`; CI general `31587640123` en verde completo.
    - [x] N0.6.D2B productores tipados:
      - [x] N0.6.D2B1 `CompraService` confirma/anula mediante `AddConOrigenTipadoAsync` + `OrigenMovimientoInventario.DesdeCompra`; funcional `e62b0667f4faace2d8d6520f753547b3e2624a1d`, pruebas `c76124980914edbea57ad7ff97eaa705171a2d58`, CI general `31589093189` en verde completo.
      - [x] N0.6.D2B2 `VentaService` confirma/anula mediante origen tipado; funcional `bac4d61b34813168b087fd7e9caf740a518c354a`, pruebas `06dea3390e0c40bef94e80f2e0ce30f482cac1f2`, CI general `31589968458` en verde completo.
      - [x] N0.6.D2B3 `ConsumoInsumoService` confirma/anula mediante origen tipado; funcional `8648cc61f29a878d213ff2ddcce4e3731a81ff43`, prueba de integración corregida hasta `ed570bb842ae4fbeb57b981bd596dfafbecf6072`, CI general `31594243722` en verde completo. Los fallos previos `31593684786` y `31593975660` fueron defectos de prueba/build y quedaron corregidos sin modificar el servicio funcional.
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
