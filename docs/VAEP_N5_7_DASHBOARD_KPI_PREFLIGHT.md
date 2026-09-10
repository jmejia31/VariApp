# N5.7 — Dashboard ejecutivo · Preflight de KPIs configurables

Autoridad: `docs/VAEP_AUTHORITY.md`  
Rama: `Desarrollo`  
Parent: `N5.7.A`

## Estado vivo inspeccionado

El dashboard actual ya consume datos reales desde `DashboardService.GetResumenAsync`, `DashboardController GET /dashboard/resumen` y `DashboardResumenDto`. La UI `frontend/src/app/features/dashboard/dashboard.component.html` muestra indicadores fijos de inventario, ventas, compras, finanzas y auditoría. Los valores están source-backed y aplican alcance por usuario/administrador, pero la selección, orden y presentación de KPIs está hardcodeada: no existe un contrato de configuración de KPIs.

## Gap material autorizado

`N5.7` debe crear KPIs configurables sin inventar fórmulas financieras ni un motor de expresiones. La configuración se limita a un catálogo cerrado de métricas ya respaldadas por fuentes existentes. El usuario autorizado podrá habilitar/deshabilitar, ordenar y opcionalmente definir una etiqueta visible para KPIs soportados. Cada `metricKey` debe mapear a una métrica existente; claves desconocidas fallan cerrado.

Métricas inicialmente elegibles por evidencia viva: `INGRESOS_MES`, `VENTAS_MES`, `COMPRAS_MES`, `TOTAL_PRODUCTOS`, `TOTAL_UNIDADES`, `VALOR_INVENTARIO`, `UTILIDAD_BRUTA`, `BALANCE_OPERATIVO`, `CUENTAS_POR_COBRAR`, `CUENTAS_POR_PAGAR`, `PRODUCTOS_STOCK_BAJO`. La disponibilidad final sigue sujeta a permisos/rol y a las mismas autoridades que hoy alimentan `DashboardResumenDto`.

## Fuera de alcance

No se autorizan fórmulas arbitrarias, SQL/expresiones del usuario, nuevas métricas financieras sin fuente, acceso cruzado a datos restringidos, widgets remotos, producción, secretos ni cambios en `main`.

## Descomposición segura

- B — Dominio/contratos: contrato de configuración + catálogo cerrado de `metricKey`.
- C — Persistencia: configuración por usuario/rol únicamente después de aceptar B; migración reversible y sin tocar datos productivos.
- D — Application/API: lectura/escritura idempotente y validada de configuración, más resolución de KPIs desde autoridades existentes.
- E — Frontend/UX: selector/orden de KPIs, estados loading/empty/error y accesibilidad.
- F — RBAC/auditoría/seguridad: permisos y ownership de configuración.
- G — QA/regresión/CI.
- H — documentación/certificación.

## Estrategia Jules

B tiene un solo write-scope coherente y material: un archivo de contrato nuevo. Se asigna a J1. No se fabrican cinco scopes adicionales sólo para ocupar lanes. C/D/E se prearman dependency-gated; otros Jules quedan elegibles cuando esos parents sean materialmente dependency-valid.

## Aceptación A

Preflight completo cuando la frontera anterior queda registrada, dependencias N5.2.H/N5.3.H/N5.4.H/N5.5.H/N5.6.H están `LISTO_REAL`, y B puede despacharse con un scope exclusivo sin filler.

## Reconciliación de implementación y cierre

La implementación A–G quedó materializada y certificada bajo el mismo alcance del preflight. El contrato final mantiene el catálogo cerrado y no crea fórmulas arbitrarias ni una segunda autoridad de datos. La persistencia por usuario/rol, Application/API, frontend/UX, ownership/RBAC y QA/regresión/CI están respaldados por receipts `LISTO_REAL` en `vaep/evidence/fragments/`.

La certificación documental vigente está en `docs/CERTIFICACION_N5_7_DASHBOARD_KPI.md`. El functional head de la última delta funcional N5.7 es `14df0c1c9aeb045033eb8d3020a396c487d862dc`; los gates reutilizados por G terminaron `SUCCESS` y G quedó certificado en `vaep/evidence/fragments/N5.7.G_LISTO_REAL_20260910T1322Z.json`.

H permanece como cierre documental/control hasta reconciliar los registros colaborativos y el Sheet; no debe marcarse `LISTO_REAL` antes de esa reconciliación. Tras H, la continuidad obligatoria es `N5.8.A` y luego su cadena dependency-valid, sin filler y bajo `CLOSURE_CHAIN_SAME_RUN`.
