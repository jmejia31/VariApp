# N5.6.A — Reportes de finanzas · Preflight VAEP

Autoridad: `docs/VAEP_AUTHORITY.md`  
Rama: `Desarrollo`

## Objetivo

Delimitar el trabajo de N5.6 antes de implementar cambios. Este preflight no introduce reglas financieras nuevas ni toca Producción.

## Autoridades existentes verificadas

- `backend/src/API/Controllers/FinanzasController.cs`: entrada API del módulo Finanzas con patrón de autorización/permisos existente.
- `backend/src/Application/Services/FinanzasService.cs`: agregaciones financieras existentes, incluyendo cuentas por cobrar y cuentas por pagar.
- `backend/src/Application/DTOs/FinanzasDto.cs`: contrato de resumen financiero existente.
- `frontend/src/app/features/finanzas/`: UI financiera existente.
- `backend/src/API/Controllers/CuentasPorCobrarController.cs` y certificaciones N3.9: CxC sigue siendo proyección/autoridad existente; no crear un segundo source of truth de saldo.
- Autoridad ERP-N2.8/N4.5 de Cuentas por Pagar, incluida su persistencia y permisos relacionales: reutilizarla, no duplicar tablas ni saldos.

## Alcance N5.6

El roadmap solicita reportes sobre CxC, CxP, vencimientos, flujo/caja, bancos/conciliaciones, impuestos y estados financieros. Cada subdominio debe reutilizar la autoridad existente cuando exista. La ausencia de evidencia específica para bancos, conciliaciones, impuestos o estados financieros no autoriza a inventar entidades, cuentas contables, reglas fiscales ni saldos.

## Riesgos y límites

1. No duplicar `Factura.SaldoPendiente`, CxC ni CxP en nuevas tablas de reporting.
2. No inferir reglas fiscales, conciliación bancaria o contabilidad que no estén respaldadas por código/roadmap autoritativo.
3. Mantener autorización y permisos relacionales del módulo Finanzas.
4. Separar lectura/reporting de cualquier mutación financiera existente.
5. Cualquier persistencia nueva requiere microtarea C y preflight/migración/rollback propios; N5.6.A no crea schema.
6. No tocar `main`, Producción, secretos ni mergear PR #2.

## Estrategia de ejecución

- N5.6.B: fijar contratos/dominio estrictamente necesarios, priorizando reutilización de DTOs y autoridades existentes.
- N5.6.C: persistencia sólo si B demuestra una necesidad real no cubierta; de lo contrario certificar N/A con evidencia.
- N5.6.D: aplicación/API sobre contratos aceptados.
- N5.6.E: frontend/UX sobre API aceptada.
- N5.6.F: RBAC/auditoría/seguridad.
- N5.6.G: QA/regresión/CI.
- N5.6.H: documentación/certificación.

## Criterios de aceptación del preflight

Dependencias resueltas, autoridades existentes identificadas, riesgos/no-invención explícitos, rollback de A = revertir sólo este documento/evidencia, y estrategia de pruebas definida por microtarea. No se requiere cambio funcional ni migración en A.
