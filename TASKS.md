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
- [x] N0.5.15 Documentación formal y cierre — `docs/ERP_N0_5_METODOS_PAGO.md`, `CHANGELOG_AI.md`, `TASKS.md` y tablero VAEP reconciliados; ERP-N0.5 formalmente cerrado.

## ERP-N0.6 — Referencias polimórficas críticas

- [x] N0.6.A Auditoría y preflight: autoridad legacy de inventario, estado relacional parcial de finanzas, productores/consumidores, riesgos, transición, rollback y plan de pruebas documentados en `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md`.
- [x] N0.6.B Dominio y contratos: origen tipado puro `Compra`/`Venta`/`ConsumoInsumo` con invariante de exactamente un origen. Funcional `5fe605cc93470a4f4b90f73185016b9e15bc622e`; CI general `31575657900` Backend Release/pruebas `SUCCESS`.
- [x] N0.6.C Persistencia/migración — transición fail-closed cerrada en cambios pequeños:
  - [x] N0.6.C1 Preflight histórico read-only: valida tipos legacy admitidos, IDs positivos y documentos origen existentes antes de cualquier backfill. Funcional hasta `8b1ca4ceae848280cea59ba7103e6cd7ef227170`; workflow dedicado `31577099764` y CI general `31577099759` en verde.
  - [x] N0.6.C2 FKs tipadas nullable + migración/backfill determinista, preservando columnas legacy durante transición. Funcional `7375a61165b7e9e32feb6054e843937963472e67`; ERP-N0.6 `31579173571` y CI general `31579173553` en verde.
  - [x] N0.6.C3 Postcheck, constraints e integridad histórica. Corrección final `01c1116e6db4e839b56176333251e3992fa09d77`; ERP-N0.6 `31581993553` y CI general `31581993565` en verde.
- [x] N0.6.D Aplicación, servicios y API — autoridad tipada consolidada:
  - [x] N0.6.D1 Repositorio/consultas de `MovimientoInventario` migradas a `CompraId` como autoridad relacional. Funcional `2a2e093f66899b9c02c18026ecd3f270b6a730c1`; corrección de fixture `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`; CI general `31585718867` en verde completo.
  - [x] N0.6.D2 Productores de `MovimientoInventario` — boundary typed-first y los tres productores documentales certificados:
    - [x] N0.6.D2A boundary de escritura `typed-first` certificado; corrección final `6eadf19a27a0c7c90b0cec54262070f896209738`; CI general `31587640123` en verde completo.
    - [x] N0.6.D2B productores tipados:
      - [x] N0.6.D2B1 `CompraService` confirma/anula mediante `AddConOrigenTipadoAsync` + `OrigenMovimientoInventario.DesdeCompra`; funcional `e62b0667f4faace2d8d6520f753547b3e2624a1d`, pruebas `c76124980914edbea57ad7ff97eaa705171a2d58`, CI general `31589093189` en verde completo.
      - [x] N0.6.D2B2 `VentaService` confirma/anula mediante origen tipado; funcional `bac4d61b34813168b087fd7e9caf740a518c354a`, pruebas `06dea3390e0c40bef94e80f2e0ce30f482cac1f2`, CI general `31589968458` en verde completo.
      - [x] N0.6.D2B3 `ConsumoInsumoService` confirma/anula mediante origen tipado; funcional `8648cc61f29a878d213ff2ddcce4e3731a81ff43`, prueba de integración corregida hasta `ed570bb842ae4fbeb57b981bd596dfafbecf6072`, CI general `31594243722` en verde completo.
  - [x] N0.6.D3 Contrato DTO/API de inventario + autoridad tipada en finanzas: `MovimientoInventarioDto` expone `OrigenTipo/OrigenId/CompraId/VentaId/ConsumoInsumoId`; el servicio deriva esos campos desde FKs tipadas y finanzas mantiene `CompraId/VentaId/FacturaId` como autoridad relacional.
- [x] N0.6.E Frontend/UX — N/A verificado: no existe consumidor Angular del contrato legacy que requiera cambio.
- [x] N0.6.F RBAC/auditoría/seguridad/observabilidad — N/A verificado: no se añadió nueva superficie de autorización.
- [x] N0.6.G QA, regresión y CI — cobertura existente suficiente y ejecutada: ERP-N0.6 `31754907625`, build `31754907682`, Fase 8 `31754907626`, aceptación integral `31754907600` y M13 `31754907614` en SUCCESS.
- [x] N0.6.H Documentación y certificación — fuente canónica `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS.md`; cierre formal sobre SHA funcional `0e35a9f75c49b6ddfbd5ef21d426521e2b559c40`.

## ERP-N0.7 — AjusteInventario formal

- [x] N0.7.A Auditoría y preflight — fuente histórica `docs/ERP_N0_7_AJUSTE_INVENTARIO_PREFLIGHT.md`; autoridad única de stock, transición legacy, rollback y matriz de validación definidos.
- [x] N0.7.B Dominio y contratos formales.
- [x] N0.7.C Persistencia e integridad histórica.
- [x] N0.7.D Backend/API/reglas de negocio.
- [x] N0.7.E Frontend/UX.
- [x] N0.7.F RBAC, auditoría, seguridad y observabilidad — permisos relacionales, auditoría crítica transaccional y correlación HTTP certificados.
- [x] N0.7.G QA, regresión y CI.
- [x] N0.7.H Documentación y certificación — fuente canónica `docs/ERP_N0_7_AJUSTE_INVENTARIO.md`; SHA funcional `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`; CI principal `31808933744`, aceptación integral `31808933692` y M13 `31808933833` terminaron SUCCESS completo, incluido runtime/Playwright y dictamen automatizado final.

## ERP-N0.8 — Migraciones y limpieza

- [x] N0.8.A Auditoría y preflight — `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA_PREFLIGHT.md`; inventario read-only, riesgos, históricos, rollback y estrategia B–H definidos; CI principal `31815060021` SUCCESS.
- [x] N0.8.B Dominio y contratos — Compra expone contrato relacional de MetodoPago y MovimientoInventario orígenes tipados; `c20151391d696ebe1d172ae3341e579cc371c35f`.
- [x] N0.8.C Persistencia/migración/datos — `20260814155400_N0_8_PersistenciaLimpiezaTransicional`; `Compras.MetodoPagoId` backfilleado por código estable + FK; FKs tipadas de inventario reconciliadas con EF; snapshot/postcheck sin drift; `b7b1db8746beac2a6e3f25c68afcafd8768383c8`.
- [x] N0.8.D Aplicación/servicios/API — Compra usa MetodoPago relacional como autoridad; bridge legacy one-way bajo lock fail-closed; MovimientoInventario usa EF para orígenes tipados; cierre dirigido `633d8fc36e2b825a6362f418c01254c8886f37fe`, 375/375 backend tests SUCCESS.
- [x] N0.8.E Frontend/UX — Compras consume catálogo de pagos activo dinámico por código estable, sin lista hardcodeada; loading/error/inactivo fail-closed + reintento; `4693502282f54e3adfeee97669e0ca7ffa10b3ae`, M10/N0.5 E2E SUCCESS.
- [x] N0.8.F RBAC/auditoría/seguridad/observabilidad — N/A verificado para superficie nueva; permisos y auditoría existentes permanecen aplicables; N0.4 SUCCESS sobre el SHA E.
- [x] N0.8.G QA, regresión y CI — SHA funcional final `369158761ad05671b9a1859d17796c8ca4a09bf8`; CI `31821172124`, M10 `31821172381`, Fase 8 `31821172230`, Acceptance `31821172223` y M13 `31821172341` SUCCESS completos.
- [x] N0.8.H Documentación/certificación — fuente canónica `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA.md`; rollback forward-only, autoridad final y compatibilidad histórica deliberadamente preservada documentados.

ERP-N0.5, ERP-N0.6, ERP-N0.7 y ERP-N0.8 están formalmente cerrados. `GATE-N0=LISTO` fue revalidado antes de iniciar ERP-N1.

## ERP-N1.1 — Sucursales empresariales

- [x] N1.1.A Auditoría y preflight — no existía entidad/DbSet Sucursal; alcance, riesgos y compatibilidad futura definidos sin adelantar multiempresa.
- [x] N1.1.B Dominio y contratos — entidad y DTOs en `0a576db21e583a76418ce037ca53f8c30d3b7eb1`.
- [x] N1.1.C Persistencia/migración — tabla `Sucursales`, índices/constraints, preflight/rollback fail-closed y snapshot EF; cierre `65785999934d8f02ffdf947fa24f48ceb9059076`.
- [x] N1.1.D Aplicación/API/RBAC — repositorio, servicio, validadores, CRUD, estado idempotente, soft-delete, paginación y permisos relacionales; cobertura dirigida hasta `805818140ef78183e52a17d196f36c452d39ebc2`.
- [x] N1.1.E Frontend/UX — mantenimiento responsive, filtros/paginación server-side, rutas/menú protegidos y M10 verde; `d3009e051ffea91631673dc764e56fdf8cab70b2`.
- [x] N1.1.F RBAC/auditoría/seguridad/observabilidad — auditoría `Entidad=Sucursal`, correlation ID y métrica P50/P95 sin PII; `9ead42f594aea12c20612d7c15e21768c090f828`.
- [x] N1.1.G QA/regresión/CI — workflow dedicado `.github/workflows/n1-1-sucursales-ci.yml`; primer E2E detectó filtro de Auditoría no traducible por EF/MySQL, corregido en `b82c8d8325866fdf4408e22424fefe692965b8d9`; certificado final run `31830346962` SUCCESS sobre `42a241162dc54c8fddf040a7321d57dd229f7e5b`.
- [x] N1.1.H Documentación/certificación — fuente canónica `docs/ERP_N1_1_SUCURSALES.md`; rollback, ERD, API, RBAC, UX, observabilidad, defecto encontrado por E2E y DoD documentados.

**ERP-N1.1 queda formalmente cerrado.** Siguiente foco autorizado por VAEP: `N1.2.A — Almacenes / auditoría y preflight`.

## ✅ ERP-N1.3 — Ubicaciones internas de almacén — cierre certificado (2026-08-14)

- [x] N1.3.A Preflight y diseño — `docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md`; topología jerárquica aditiva definida sin stock, sin `SucursalId`/`EmpresaId` duplicados y con N1.4 como autoridad futura de existencias.
- [x] N1.3.B Dominio y contratos — `UbicacionAlmacen`, `TipoUbicacionAlmacen`, DTOs y guardas de contrato; backend Release/unitarias certificados.
- [x] N1.3.C Persistencia/migración — `20260814211647_N1_3_UbicacionAlmacenPersistencia`; FK a Almacén, jerarquía autorreferente compuesta del mismo Almacén, código activo único, constraints y triggers MySQL 8.4 para self-parent; snapshot sin drift e historial MySQL certificado.
- [x] N1.3.D Aplicación, servicios y API — repositorio/servicio/controller/DI, paginación y filtros, padre activo/mismo Almacén, prevención de ciclos y protección de descendientes; cierre funcional `4d2cc04b363df602f6de97b7f5ea876ea35a6196`; Backend Release/unitarias `31843085895` / `94903923345` SUCCESS.
- [x] N1.3.E Frontend/UX — listado responsive, filtros server-side, formulario jerárquico, selectores de Almacén/padre, rutas y menú principal RBAC; cierre `91f878ef3cbc56219b637e9b62c99bdd1109a9df`; Frontend producción `31846161956` / `94912936660` SUCCESS.
- [x] N1.3.F RBAC/auditoría/seguridad/observabilidad — módulo `UbicacionesAlmacen`, permisos por endpoint, auditoría de mutaciones y regresiones que congelan autorización/auditoría; baseline `4a6be38683f03fc2076f18a71115480c930ba79b`; backend `94913888850` SUCCESS.
- [x] N1.3.G QA, regresión y CI — run agregado `31846485117` SUCCESS: higiene `94913888918`, backend `94913888850`, frontend `94913888865`, Docker `94913888808` y MySQL 8.4/integración `94913888844`.
- [x] N1.3.H Documentación y certificación — fuente canónica `docs/ERP_N1_3_UBICACIONES_ALMACEN.md`, `TASKS.md`, `CHANGELOG_AI.md` y VAEP reconciliados preservando historial.

**Resultado:** ERP-N1.3 queda cerrado como topología interna de Almacenes. No introduce autoridad de cantidad ni stock. El siguiente foco es **N1.4.A — ExistenciaVariante — Preflight y diseño**, responsable de diseñar la autoridad de existencias por Almacén/Ubicación y la transición desde `ProductoVariante.Cantidad`.

## ERP-N1.2 — Almacenes empresariales

- [x] N1.2.A Auditoría y preflight — no existía implementación legacy Almacén/Bodega/Ubicación; Almacén definido como hijo obligatorio de Sucursal, sin adelantar stock N1.4 ni multiempresa N6.
- [x] N1.2.B Dominio y contratos — `Almacen`, `TipoAlmacen` estable Tienda/Bodega/Transito/Devolucion/Cuarentena y DTOs; autoridad jerárquica única `SucursalId`; corrección concurrente final `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8` retiró `EmpresaId` duplicada y añadió guarda arquitectónica.
- [x] N1.2.C Persistencia/migración — tabla `Almacenes`, FK Restrict a `Sucursales`, código activo único, índices/checks, preflight/postcheck y rollback fail-closed; HEAD `bebafe3abb2ddc66448c805b107f8d1f8ee3f3e9`; CI `31834214669` con MySQL/integración/snapshot sin drift SUCCESS.
- [x] N1.2.D Aplicación/API/RBAC — repositorio, servicio, validadores, CRUD, filtros/paginación, jerarquía fail-closed, estado idempotente, soft-delete, `ModuloSistema.Almacenes=29` y permisos seedables; `5a97bf3844069a565e1aecf39e4b8001c10f386b`.
- [x] N1.2.E Frontend/UX — mantenimiento responsive, filtros server-side, selector Sucursal activa, catálogo de tipos API, rutas/menú RBAC y formulario sin EmpresaId/stock; `3a1b8004f2120c4be6459bb46fd120eff8704fe9`; M10 `31835928799` SUCCESS.
- [x] N1.2.F RBAC/auditoría/seguridad/observabilidad — auditoría `Entidad=Almacen`, correlation/health globales y métrica P50/P95 `/almacenes` sin término ni PII; `30c7e9ff1dedf69eb860916b92b1d5bee0941084`.
- [x] N1.2.G QA/regresión/CI — workflow dedicado `.github/workflows/n1-2-almacenes-ci.yml`; fallos reales de harness puerto y orden de rutas corregidos en `3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f` y `053152ae51de3617bf30a4e9987574c7879e3049`; run final `31837394309` SUCCESS, Playwright 8/8.
- [x] N1.2.H Documentación/certificación — fuente canónica `docs/ERP_N1_2_ALMACENES.md`; arquitectura, migración/rollback, API/RBAC, UX, observabilidad, QA y DoD documentados.

**ERP-N1.2 queda formalmente cerrado.** Siguiente foco autorizado por VAEP: `N1.3.A — Ubicaciones internas / auditoría y preflight`.

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

Protocolo: `PLAN_EJECUCION_AUTONOMA.md`.

## Continuidad

En nueva conversación/sesión: confirmar proyecto/repo/rama; leer `AGENTS.md`, `PROJECT_CONTEXT.md`, este archivo y última entrada relevante de `CHANGELOG_AI.md`. Si es ejecución autónoma, leer además `PLAN_EJECUCION_AUTONOMA.md` y el tablero VAEP. Revisar solo commits/archivos que cambiaron y continuar sin reindexar el proyecto.
