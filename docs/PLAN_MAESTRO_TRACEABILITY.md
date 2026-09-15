# Índice de trazabilidad del Plan Maestro — corte vivo 2026-09-10

Fuente rectora: Google Sheet `VariApp — PLAN MAESTRO DE AUTOMATIZACIONES`, spreadsheet `19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY`, rangos `PLAN_MAESTRO!A1:M130`, más evidencia Git bajo `Desarrollo`.

## Semántica fail-closed

- `LISTO` / `CERRADO_HISTORICO`: exige evidencia de implementación o decisión N/A justificada, prueba aplicable y aceptación/receipt conforme al MAESTRO. El estado de una fila por sí solo no certifica código.
- `EN_PROGRESO`: puede tener código/pruebas parciales, pero no aceptación final de la macro.
- `PLANIFICADO != ACEPTADO`: la trazabilidad registra la obligación, no inventa código ni receipt.
- `OBLIGATORIO`: track transversal que debe satisfacerse en cada parent donde aplique.
- `NO_AUTORIZADO != IMPLEMENTADO`: los FUT-* se indexan únicamente para impedir su ejecución accidental.

## Cadena de evidencia por requisito

Cada ID se resuelve contra cuatro fuentes: (1) fila del Plan Maestro; (2) código/configuración o decisión técnica documentada en Git; (3) pruebas/gates aplicables; (4) aceptación mediante REVIEW_FIRST/DoD/receipt cuando el requisito está cerrado. Para los requisitos aún planificados, los puntos 2–4 son `NOT_DUE` hasta que el roadmap los promueva.

| Grupo | Código / contratos | Pruebas / gates | Aceptación |
| --- | --- | --- | --- |
| ERP-N0 | `backend/src/**`, migraciones y docs ERP-N0 | `backend/tests/**`, workflows ERP-N0 | receipts/evidencia N0 + GATE-N0 |
| ERP-N1 | inventario, sucursales, almacenes, ubicaciones | tests N1 + CI | receipts N1 + GATE-N1 |
| ERP-N2 | compras, recepción, proveedor, CxP | tests N2 + CI | receipts N2 + GATE-N2 |
| ERP-N3 | ventas, pedidos, reservas, POS, CxC | tests N3 + E2E | receipts N3 + GATE-N3 |
| ERP-N4 | caja, bancos, contabilidad, centros de costo | tests N4 + regresión | receipts N4 + GATE-N4 |
| ERP-N5 | reportes, exportaciones, performance | tests N5 + N5.9 | receipts N5 + GATE-N5 |
| ERP-N6 | Empresa/tenant, ownership, persistencia, API | tests N6 + tenant negative tests según avance | receipts N6; GATE-N6 aún no debido |
| ERP-N7 | integración resiliente/outbox/idempotencia | NOT_DUE | NOT_DUE |
| ERP-N8 | UAT/dispositivos/performance/DR/security/observabilidad | NOT_DUE | NOT_DUE |
| ERP-N9 | release candidate y salida | NOT_DUE | NOT_DUE |
| T0–T12 | evidencia transversal en arquitectura, DB, seguridad, API, frontend, performance, observabilidad y DevOps | gates continuos | se hereda en cada cierre aplicable |

## Inventario completo de IDs y estado del corte

**ERP-N0:** N0.0 [LISTO], N0.1 [CERRADO_HISTORICO], N0.2 [CERRADO_HISTORICO], N0.3 [CERRADO_HISTORICO], N0.4 [CERRADO_HISTORICO], N0.5 [LISTO], N0.6 [LISTO], N0.7 [LISTO], N0.8 [LISTO].

**ERP-N1:** N1.1 [LISTO], N1.2 [LISTO], N1.3 [LISTO], N1.4 [LISTO], N1.5 [LISTO], N1.6 [LISTO], N1.7 [LISTO], N1.8 [LISTO], N1.9 [LISTO], N1.10 [LISTO].

**ERP-N2:** N2.1 [LISTO], N2.2 [LISTO], N2.3 [LISTO], N2.4 [LISTO], N2.5 [LISTO], N2.6 [LISTO], N2.7 [LISTO], N2.8 [LISTO], N2.9 [LISTO].

**ERP-N3:** N3.1 [LISTO], N3.2 [LISTO], N3.3 [LISTO], N3.4 [LISTO], N3.5 [LISTO], N3.6 [LISTO], N3.7 [LISTO], N3.8 [LISTO], N3.9 [LISTO], N3.10 [LISTO], N3.11 [LISTO].

**ERP-N4:** N4.1 [LISTO], N4.2 [LISTO], N4.3 [LISTO], N4.4 [LISTO], N4.5 [LISTO], N4.6 [LISTO], N4.7 [LISTO], N4.8 [LISTO], N4.9 [LISTO], N4.10 [LISTO], N4.11 [LISTO].

**ERP-N5:** N5.1 [LISTO], N5.2 [LISTO], N5.3 [LISTO], N5.4 [LISTO], N5.5 [LISTO], N5.6 [LISTO], N5.7 [LISTO], N5.8 [LISTO], N5.9 [LISTO].

**ERP-N6:** N6.1 [LISTO], N6.2 [EN_PROGRESO], N6.3 [PLANIFICADO], N6.4 [PLANIFICADO], N6.5 [PLANIFICADO], N6.6 [PLANIFICADO], N6.7 [PLANIFICADO], N6.8 [PLANIFICADO], N6.9 [PLANIFICADO], N6.10 [PLANIFICADO].

**ERP-N7:** N7.1 [PLANIFICADO], N7.2 [PLANIFICADO], N7.3 [PLANIFICADO], N7.4 [PLANIFICADO], N7.5 [PLANIFICADO], N7.6 [PLANIFICADO], N7.7 [PLANIFICADO], N7.8 [PLANIFICADO], N7.9 [PLANIFICADO], N7.10 [PLANIFICADO].

**ERP-N8:** N8.1 [PLANIFICADO], N8.2 [PLANIFICADO], N8.3 [PLANIFICADO], N8.4 [PLANIFICADO], N8.5 [PLANIFICADO], N8.6 [PLANIFICADO], N8.7 [PLANIFICADO], N8.8 [PLANIFICADO], N8.9 [PLANIFICADO], N8.10 [PLANIFICADO], N8.11 [PLANIFICADO], N8.12 [PLANIFICADO], N8.13 [PLANIFICADO], N8.14 [PLANIFICADO].

**ERP-N9:** N9.1 [PLANIFICADO], N9.2 [PLANIFICADO], N9.3 [PLANIFICADO], N9.4 [PLANIFICADO], N9.5 [PLANIFICADO], N9.6 [PLANIFICADO], N9.7 [PLANIFICADO].

**Gates:** GATE-N0 [LISTO], GATE-N1 [LISTO], GATE-N2 [LISTO], GATE-N3 [LISTO], GATE-N4 [LISTO], GATE-N5 [LISTO], GATE-N6 [PLANIFICADO], GATE-N7 [PLANIFICADO], GATE-N8 [PLANIFICADO], GATE-N9 [PLANIFICADO].

**Tracks:** T0 [OBLIGATORIO], T1 [OBLIGATORIO], T2 [OBLIGATORIO], T3 [OBLIGATORIO], T4 [OBLIGATORIO], T5 [OBLIGATORIO], T6 [OBLIGATORIO], T7 [OBLIGATORIO], T8 [OBLIGATORIO], T9 [OBLIGATORIO], T10 [OBLIGATORIO], T11 [OBLIGATORIO], T12 [OBLIGATORIO].

**Futuro no autorizado:** FUT-RRHH [NO_AUTORIZADO], FUT-CRM [NO_AUTORIZADO], FUT-MRP [NO_AUTORIZADO], FUT-AF [NO_AUTORIZADO], FUT-PROY [NO_AUTORIZADO], FUT-ST [NO_AUTORIZADO].

Total indexado: **129 IDs**. `scripts/quality/priority4_quality_audit.py` falla si uno de estos IDs desaparece del índice o si se eliminan las reglas fail-closed para trabajo planificado/no autorizado.

## Reconciliación realizada en este corte

La evidencia Git viva mostraba receipts para GATE-N4, GATE-N5 y N6.1.H, mientras el Sheet conservaba `PLANIFICADO/EN_PROGRESO`. Se reconciliaron las filas del Plan Maestro a GATE-N4=LISTO, GATE-N5=LISTO, N6.1=LISTO y N6.2=EN_PROGRESO. N6.2 no se marca LISTO: el trabajo actual está en N6.2.B y aún faltan persistencia/aplicación y aislamiento posterior.

## Regla de mantenimiento

Este archivo es un corte auditable, no una copia que sustituya al Sheet. Cada promoción/cierre debe actualizar primero la evidencia canónica y luego reconciliar el índice. Si Git y Sheet vuelven a divergir, el estado se considera `DRIFT` hasta revalidación; nunca se infiere aceptación sólo por documentación.
