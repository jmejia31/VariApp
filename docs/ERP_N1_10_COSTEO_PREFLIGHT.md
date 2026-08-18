# ERP-N1.10 — Costeo empresarial — Auditoría y preflight

## 1. Propósito

Definir el punto de partida real para ERP-N1.10 antes de introducir dominio, persistencia o algoritmos nuevos. El objetivo rector es disponer de **una política de costeo coherente y auditable para la empresa**, contemplando Promedio Ponderado Móvil, FIFO y Costo Estándar cuando corresponda, sin romper históricos ni crear una segunda autoridad de stock.

Este documento es PRE: no introduce todavía entidades, migraciones, endpoints ni UI funcionales de N1.10.B–H.

## 2. Dependencias y límites

- Dependencia VAEP directa: `N1.5.H` — LISTO.
- `N1.9.H` fue cerrado antes de abrir este punto y no se reabre.
- `ExistenciaVariante` sigue siendo la única autoridad cuantitativa del stock vivo por Variante + Almacén + Ubicación.
- ERP-N1.10 define **valoración**, no una segunda cantidad.
- ERP-N6 continúa siendo la fase responsable de multiempresa/SaaS. N1.10 no debe inventar una entidad tenant ni una FK `Empresa` inexistente.
- El sistema sí posee `EmpresaConfiguracion` activa única; en el contexto actual single-company esa configuración representa el ámbito empresarial efectivo. La evolución tenant-aware se hará en N6 sin reescribir históricos de costeo.

## 3. Estado real encontrado

### 3.1 Autoridades actuales de costo

Hoy existen dos proyecciones persistidas de costo corriente:

- `Producto.Costo` — costo consolidado de compatibilidad a nivel producto.
- `ProductoVariante.Costo` — costo operativo a nivel variante.

La compra confirmada aplica actualmente **promedio ponderado móvil**:

- para una variante: `(valor anterior + valor de entrada) / stock nuevo`;
- para el producto consolidado: `(costo anterior * stock anterior + valor entrada) / stock nuevo`;
- redondeo actual: 2 decimales, `MidpointRounding.AwayFromZero`.

Por tanto, el sistema ya implementa de facto una política de Promedio Ponderado, pero la política está embebida dentro de `CompraService`; no existe un contrato de costeo explícito ni una selección empresarial administrable.

### 3.2 Stock y valoración están parcialmente desacoplados

`ExistenciaVariante` contiene `StockFisico`, `StockReservado`, `StockDisponible`, `StockTransito`, mínimos y máximos, pero **no contiene costo**. Esto es correcto como principio: la existencia física no debe transformarse en una autoridad paralela de valoración.

`MovimientoInventario` sí conserva `CostoUnitario` nullable como snapshot del evento, además de cantidad, stock anterior/nuevo, origen tipado y `CorrelationId`. El Kardex ya ofrece la base de trazabilidad para reconstruir eventos, pero ese snapshot por sí solo no implementa FIFO ni costo estándar.

### 3.3 Compras

`CompraDetalle.CostoUnitario` conserva el costo real de entrada. Además existen campos históricos de valoración (`CostoProductoAnteriorSnapshot`, `CostoProductoNuevoSnapshot`, `CostoVarianteAnteriorSnapshot`, `CostoVarianteNuevoSnapshot`, stocks anterior/nuevo), pero el flujo actual inspeccionado de `CompraService.ConfirmarAsync` no materializa esos snapshots antes/después.

El costo corriente se recalcula directamente dentro de `CompraService`, lo que mezcla workflow documental, inventario y política de valoración.

### 3.4 Ventas

`VentaDetalle` persiste:

- `CostoUnitarioSnapshot`;
- `UtilidadBruta`;
- precio y snapshots comerciales.

El costo de venta se toma hoy desde `ProductoVariante.Costo` en `ArmarDetallesAsync`, es decir **al crear/editar el borrador**, no al confirmar físicamente la salida. Si una compra o ajuste modifica el costo mientras una venta permanece en borrador, el `CostoUnitarioSnapshot`, el COGS y la utilidad pueden quedar obsoletos al confirmar.

Este comportamiento debe corregirse en N1.10: el costo contable de la salida debe congelarse en el momento autoritativo de confirmación, bajo el mismo control transaccional/concurrencia del inventario.

### 3.5 Anulación de compras

La anulación actual protege la secuencia física: falla cerrada si existen movimientos posteriores de inventario. Sin embargo, después de restar cantidades **no restaura explícitamente el costo anterior de Producto/Variante**. En un esquema de promedio ponderado, si la compra anulada fue el último movimiento, la reversión debe restaurar determinísticamente la valoración previa o calcular una reversión matemáticamente equivalente.

Los campos snapshot ya presentes en `CompraDetalle` sugieren una intención histórica de soportar esta reversibilidad, pero no están conectados al flujo inspeccionado. N1.10 debe resolverlo y cubrirlo con pruebas causales.

### 3.6 Ámbito empresa

No existe actualmente `Domain/Entities/Empresa.cs`. `Sucursal.EmpresaId` está expresamente reservado para ERP-N6 y no constituye todavía una relación tenant-aware.

Sí existe `EmpresaConfiguracion`, con unicidad activa y datos operativos/fiscales. Para no adelantar N6, la política N1.10 debe operar inicialmente en el único ámbito empresarial activo. El diseño debe permitir que N6 agregue aislamiento por empresa sin cambiar el significado de movimientos históricos.

## 4. Problemas y riesgos confirmados

### R1 — Política implícita y acoplada

El promedio ponderado está codificado dentro de `CompraService`; cambiar de método requeriría tocar workflows documentales. Riesgo: divergencia entre compras, ventas, ajustes, transferencias, conteos y reportes.

### R2 — Doble proyección Producto/Variante

`Producto.Costo` y `ProductoVariante.Costo` pueden divergir si un flujo actualiza uno sin reconciliar el otro. La autoridad de valoración de N1.10 debe declararse explícitamente. Recomendación: la Variante es la unidad mínima valorable; Producto debe ser proyección consolidada/compatibilidad, nunca una autoridad independiente.

### R3 — Costo de venta congelado demasiado pronto

Una venta en borrador captura costo antes de la confirmación. Esto puede producir COGS/utilidad históricos incorrectos aunque el stock físico se confirme correctamente.

### R4 — Reversión de compra no restaura valoración

Una anulación físicamente segura puede dejar el costo promedio posterior a la compra anulada. Riesgo de valoración residual incorrecta.

### R5 — FIFO no tiene capas

No existe una entidad de capas/lotes contables de costo ni asignaciones de consumo. Los lotes N1.9 son **identidad logística**, no capas contables FIFO; no deben reutilizarse como autoridad contable porque un producto sin control de lote también debe poder usar FIFO.

### R6 — Costo estándar no tiene vigencia ni variación

No existe contrato de costo estándar efectivo ni registro de variaciones compra-real vs estándar. Sobrescribir `ProductoVariante.Costo` perdería historia y es insuficiente.

### R7 — Transferencias y ajustes

Una transferencia entre almacenes no debe crear utilidad ni cambiar costo empresarial; con FIFO debe trasladar la identidad de capas o una asignación equivalente. Ajustes positivos requieren una fuente explícita de valoración; ajustes negativos deben consumir costo según la política vigente.

### R8 — Históricos

No se puede recalcular retrospectivamente COGS/utilidad de documentos cerrados sin una política explícita de migración. Los snapshots existentes deben preservarse. El cutover de N1.10 debe ser forward-only y determinista.

## 5. Decisiones de arquitectura para N1.10.B–H

### 5.1 Principio de autoridad

- **Cantidad:** `ExistenciaVariante`.
- **Unidad mínima de valoración:** `ProductoVariante`.
- **Producto.Costo:** proyección consolidada de compatibilidad, no autoridad primaria.
- **Costo histórico de salida:** snapshot/asignación persistida al confirmar el movimiento, nunca recalculado desde el costo corriente al consultar.
- **Política:** una única política activa para el ámbito empresarial actual; preparada para tenantización en N6.

### 5.2 Métodos objetivo

#### Promedio Ponderado Móvil

Debe preservar el comportamiento actual como default de migración para evitar reinterpretar históricos. La lógica debe salir de `CompraService` hacia un servicio/estrategia de costeo transaccional reutilizable.

#### FIFO

Requiere capas contables separadas de `LoteInventario`, como concepto propuesto `CapaCostoInventario`:

- Variante;
- contexto físico Almacén/Ubicación cuando corresponda;
- cantidad original/restante;
- costo unitario;
- fecha/orden durable;
- origen tipado/correlación;
- referencia a compra/ajuste/transferencia de origen.

Cada salida debe persistir su asignación a una o más capas para hacer el COGS auditable e inmutable.

#### Costo Estándar

Requiere valor estándar vigente por Variante y registro explícito de variaciones. El costo real de compra no debe perderse. La venta usa el estándar vigente congelado según la regla empresarial; las diferencias deben quedar auditables.

### 5.3 Política empresarial sin adelantar N6

Opción recomendada para N1.10.B/C:

- definir `MetodoCosteoInventario` como contrato estable (`PromedioPonderado`, `FIFO`, `Estandar`);
- persistir la selección en el ámbito de `EmpresaConfiguracion` activa única o en una configuración de costeo 1:1 con ella;
- default/backfill: `PromedioPonderado` porque reproduce el comportamiento actual;
- prohibir múltiples políticas activas simultáneas en el contexto single-company;
- documentar la migración de clave empresarial a N6 como evolución de aislamiento, no como cambio semántico de históricos.

## 6. Flujos que N1.10 debe cubrir

1. **Compra confirmada** — entrada valorada según política; snapshot auditable.
2. **Compra anulada** — reversión determinística si la secuencia lo permite.
3. **Venta confirmada** — COGS congelado al confirmar, no al crear borrador.
4. **Venta anulada** — restauración física y valoración compatible con el costo histórico original.
5. **Transferencia** — traslado sin generar ganancia/pérdida; FIFO conserva procedencia de capa.
6. **Ajuste negativo** — consumo por política.
7. **Ajuste positivo** — costo explícito/justificado; nunca costo cero implícito salvo regla documentada.
8. **Conteo físico** — cualquier diferencia que materialice Ajuste debe obedecer el punto anterior.
9. **Kardex/reportes** — costo histórico del movimiento y COGS deben ser reproducibles sin consultar costo corriente.
10. **Reservas** — no reconocen COGS mientras no exista salida física; no deben mutar capas/costo.
11. **Lotes/series N1.9** — identidad logística puede correlacionarse con valoración, pero no sustituye capas contables.

## 7. Persistencia/cutover esperado para N1.10.C

El diseño definitivo se cierra en B, pero C deberá mantener estas guardas:

- migraciones aditivas y forward-only;
- default de política a Promedio Ponderado para preservar semántica actual;
- no backfill inventado de capas FIFO sobre stock histórico sin evidencia suficiente;
- si se habilita FIFO con stock preexistente, crear una **capa de apertura explícita** usando costo corriente certificado y registrar su origen/cutover;
- costo estándar requiere alta explícita antes de activarse cuando exista stock;
- índices y constraints para impedir capas negativas, consumo superior a saldo y ambigüedad de política;
- snapshot/modelo EF sin drift;
- preflight/postcheck y rollback documentados antes de cualquier activación.

## 8. API/UX/RBAC esperados

N1.10.D–F deberá separar:

- consulta de política y estado de costeo;
- cambio de política con preflight y restricciones de cutover;
- mantenimiento/versión de costo estándar cuando aplique;
- consulta auditable de capas/asignaciones/variaciones;
- reportes de costo/COGS/utilidad.

Los cambios de política o costo estándar son operaciones sensibles: RBAC explícito, auditoría estricta y correlation obligatorios. Una política no puede cambiarse silenciosamente si existen existencias/capas incompatibles.

## 9. Matriz mínima de pruebas

### Dominio

- enum/contratos de política válidos;
- promedio ponderado exacto y redondeo definido;
- FIFO orden estable y consumo parcial/multicapa;
- costo estándar y variación;
- cantidades/costos negativos fail-closed.

### Integración/MySQL

- concurrencia de dos entradas/salidas;
- unicidad de política activa;
- capas FIFO sin saldo negativo;
- transferencias preservan valor;
- migración desde baseline real y snapshot EF sin drift.

### Servicios

- compra promedio y reversión restauran costo;
- venta en borrador no congela COGS definitivo;
- confirmación de venta congela costo actual bajo lock;
- FIFO asigna capas y persiste COGS;
- estándar persiste variación;
- ajuste positivo exige valoración válida;
- reservas no alteran costo.

### E2E/contrato

- consulta y cambio autorizado de política;
- bloqueo de cambio incompatible;
- venta muestra utilidad consistente con COGS confirmado;
- reportes/Kardex reproducen costo histórico.

## 10. Rollback

Antes de N1.10.C el rollback es documental: este preflight no modifica runtime.

Para la implementación posterior:

- no eliminar `Producto.Costo`, `ProductoVariante.Costo`, `VentaDetalle.CostoUnitarioSnapshot` ni `MovimientoInventario.CostoUnitario` durante el cutover;
- mantener compatibilidad de lectura mientras las nuevas autoridades se certifican;
- cambios de política deben tener preflight y condición de retorno explícita;
- una vez existan movimientos bajo FIFO/Estándar, volver a otro método no puede reinterpretar históricos: debe ser un nuevo corte temporal documentado.

## 11. Criterio de cierre de N1.10.A

N1.10.A queda listo cuando:

- autoridad actual y algoritmo implícito están identificados;
- divergencias de compra/venta/anulación están documentadas;
- alcance Promedio/FIFO/Estándar está separado de stock y de N6;
- consumidores causales y riesgos están enumerados;
- transición/rollback y matriz de pruebas están definidos;
- N1.10.B puede diseñar dominio sin volver a escanear globalmente el repositorio.
