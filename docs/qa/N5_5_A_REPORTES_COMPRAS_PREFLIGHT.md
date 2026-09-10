# N5.5.A — Reportes de compras: auditoría y preflight

Authority: `docs/VAEP_AUTHORITY.md`.

## Alcance inspeccionado

El roadmap de N5.5 pide analizar proveedor, precio, variación, recepción, devoluciones y cumplimiento. La inspección dirigida del HEAD vivo de `Desarrollo` confirma que VariApp ya tiene primitivas operativas de compras, pero no un subsistema dedicado `ReporteCompras` que pueda ampliarse de forma segura sin definir primero su contrato.

Fuentes inspeccionadas:

- `OrdenesCompraController` / `IOrdenCompraService`: listado y detalle de órdenes, creación/edición, aprobación y cancelación; RBAC bajo `Compras`.
- `OrdenCompraDto` y detalles: proveedor, moneda, condiciones, fecha esperada, precios unitarios, descuentos, impuestos, subtotales/totales y snapshots de producto; filtros existentes por proveedor, estado, solicitud, número y fechas.
- `RecepcionesCompraController` / `IRecepcionCompraService`: listado/detalle de recepciones, saldo por orden, creación/edición, confirmación y anulación; RBAC bajo `Compras`.
- Contratos/tests existentes de N2.2/N2.3 para órdenes y recepciones, incluidos idempotencia, snapshots, seguridad/auditoría y reversión de stock.

## Estado real y límites

1. **Proveedor**: source-backed por `OrdenCompra.ProveedorId` y snapshot/nombre expuesto por DTO. Es dimensión segura para reporting.
2. **Precio**: source-backed por `OrdenCompraDetalle.PrecioUnitario`, descuentos/impuestos/subtotal/total y snapshots históricos de producto. La base de análisis debe preservar snapshots; no recalcular historia con maestros actuales.
3. **Recepción**: source-backed por recepciones vinculadas a órdenes y por el saldo de orden. Puede medirse cantidad/estado/tiempo cuando el dato exista en las entidades actuales.
4. **Variación**: no existe todavía un contrato canónico único para “variación”. N5.5.B debe definir explícitamente si significa variación entre órdenes, precio ordenado vs. recibido/facturado, o variación temporal; no se inventa aquí.
5. **Devoluciones**: la búsqueda dirigida no encontró un agregado/endpoint `DevolucionCompra` dedicado. Anulación/cancelación de orden o recepción no debe renombrarse como devolución. Si el roadmap exige devoluciones reales, queda como gap de dominio para N5.5.B o un hijo material posterior.
6. **Cumplimiento**: existen fecha esperada, estados de orden/recepción y saldo; no existe aún un KPI canónico de cumplimiento. N5.5.B debe fijar fórmula y granularidad antes de persistencia/API.
7. **Seguridad**: los endpoints operativos usan `[Authorize]` + permisos relacionales de `ModuloSistema.Compras`. Un reporte futuro debe conservar ese acceso y no crear permisos nuevos sin evidencia/contrato autorizado.
8. **Producción/datos**: N5.5.A no requiere migración, backfill ni escritura de datos; no se toca Producción.

## Dependencias y estrategia de ejecución

- `N5.1.H` y `GATE-N4` ya están certificados; N5.4 A-H también están cerrados antes de esta promoción.
- N5.5.B debe ser el siguiente parent: dominio/contratos primero. Debe mantener las entidades operativas N2.2/N2.3 como fuente y definir DTO/query semantics de reporting sin modificar esquemas si no es necesario.
- N5.5.C sólo podrá decidir persistencia/migración después de B. Si el reporte puede derivarse de datos históricos existentes, C puede documentar `N/A` con evidencia y evitar una migración artificial.
- N5.5.D implementará servicio/API únicamente tras B/C aceptados.

## Riesgos y gates

- No confundir cancelación/anulación con devolución de compra.
- No calcular variación o cumplimiento con una fórmula no autorizada.
- No perder snapshots históricos de precio/producto.
- No crear escrituras o tablas de reporting si una consulta read-only satisface el contrato.
- Evitar N+1 y materialización de grafos operativos completos; preferir proyección server-side y paginación/filtros explícitos.
- Mantener RBAC `Compras` y auditoría/correlation conforme a los patrones existentes cuando aplique.

## Criterios de aceptación para N5.5.B

N5.5.B debe producir un contrato source-backed y verificable para las dimensiones que sí están modeladas, declarar explícitamente los gaps de devolución/variación/cumplimiento, y no inventar semántica. Debe dividirse si un único changeset deja de ser coherente y verificable.

## Rollback

Este preflight es documentación/evidencia únicamente. Rollback: revertir el commit del documento en `Desarrollo`; no hay migración, cambio de datos, `main`, Producción, secrets ni merge de PR #2.

## REVIEW_FIRST disposition

`PASS_PRE`: inspección dirigida suficiente para habilitar N5.5.B. No constituye PASS de B ni autoriza implementación de semántica no resuelta.
