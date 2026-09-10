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
