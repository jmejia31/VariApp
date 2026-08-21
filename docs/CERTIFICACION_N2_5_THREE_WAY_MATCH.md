# CERTIFICACIÓN — N2.5 Three-Way Match (DRAFT / VALIDANDO)

**ESTADO GLOBAL N2.5**: **VALIDANDO — NO LISTO AÚN**

Este documento es el expediente de certificación de N2.5. La certificación final solo puede cambiar a `LISTO` cuando N2.5.E/F/G/H hayan sido reconciliadas, el HEAD funcional resultante tenga CI causal aplicable en verde y no existan P0/P1 abiertos. Ningún `COMPLETED` de Jules equivale por sí solo a certificación.

## Evidencia técnica actual

### 1. Dominio y persistencia — EVIDENCIADO
- `backend/src/Domain/Entities/ThreeWayMatchResult.cs` implementa evaluación exacta/fail-closed.
- `backend/src/Domain/ValueObjects/ThreeWayMatchLineDiscrepancy.cs` representa discrepancias de línea/cabecera.
- La persistencia materializada usa `ThreeWayMatchResultados` y `ThreeWayMatchDiscrepancias`.
- La migración `20260821053500_N2_5_ThreeWayMatchPersistencia.cs` está presente.
- `OrdenCompraDetalleId = 0` es el sentinela de discrepancia de cabecera; la migración no crea FK dura hacia `OrdenCompraDetalles` y sí aplica `CK_ThreeWayMatchDiscrepancias_OrdenDetalleSentinela`.
- El `Down()` elimina ambas tablas de N2.5; es destructivo para la evidencia Three-Way Match y no contiene DownGuard.

### 2. Aplicación y API — EVIDENCIADO
- `ConciliacionController` expone `GET /conciliacion/ordenes-compra/{ordenCompraId}/three-way-match`.
- El controller exige autenticación y permiso `Compras/Ver`.
- `ThreeWayMatchService` usa lectura paginada/fail-closed y rechaza evidencia inestable.
- No se introducen tolerancias, FX, CxP ni acciones transaccionales no aprobadas.

### 3. Frontend / UX — IMPLEMENTADO, PENDIENTE DE CERTIFICACIÓN CAUSAL
- El QA takeover de N2.5.E integró modelo, servicio y feature Angular bajo `frontend/src/app/features/compras/three-way-match/**`, más su ruta de acceso.
- La existencia del código no basta para `LISTO`: debe quedar cubierta por CI/QA causal sobre el HEAD final de N2.5.

### 4. Seguridad / observabilidad — QA TAKEOVER EN VALIDACIÓN
- El QA takeover de N2.5.F incorpora pruebas dirigidas de `[Authorize]`, ruta, método GET y `Compras/Ver`.
- Los claims ambientales sin evidencia causal permanecen como `causa no determinada`; no se convierten en contrato.
- La promoción de F depende de E y del CI causal del HEAD de takeover.

### 5. Regresión / CI — QA TAKEOVER EN VALIDACIÓN
- El QA takeover de N2.5.G incorpora regresiones sobre match exacto, elegibilidad `Recibida`/`Registrada`, discrepancia de moneda, determinismo y ausencia de tolerancias inventadas.
- La promoción de G depende de F y del CI causal del HEAD de takeover.

### 6. Documentación / rollback — EVIDENCIADO, CIERRE PENDIENTE
- ADR, OpenAPI, ERD, Runbook y Rollback se mantienen alineados al contrato vigente.
- El rollback estructural es destructivo para datos N2.5; exige backup/export verificable, quiescencia, aprobación explícita, postchecks y plan de restore/abort. No ejecutar Producción desde VAEP.

## Gate final
`N2.5 = LISTO` únicamente si:
1. E, F, G y H quedan `LISTO` bajo REVIEW-FIRST;
2. CI causal aplicable del HEAD final está en verde;
3. no existen P0/P1 abiertos ni scope/evidence mismatch;
4. la bitácora/cola/plan quedan reconciliados.

Hasta entonces: **DRAFT / VALIDANDO**.
