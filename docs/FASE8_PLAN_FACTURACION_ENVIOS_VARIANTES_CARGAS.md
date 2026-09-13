# Fase 8 — Plan técnico de facturación, envíos, variantes y cargas masivas

Fecha: 2026-07-28
Rama autorizada: `Desarrollo`
PR oficial: #2 (`Desarrollo -> main`, abierto y en borrador)

## Estado de seguridad confirmado

- `main` no se modifica.
- No se crean ramas adicionales.
- No se habilita auto-merge.
- No se ejecutan despliegues ni migraciones contra Producción.
- Las migraciones nuevas se validarán únicamente en MySQL temporal/aislado mediante CI.

## Arquitectura encontrada

- Backend: ASP.NET Core con capas Domain, Application, Infrastructure y API.
- Persistencia: Entity Framework Core sobre MySQL.
- Frontend: Angular con rutas por módulo, servicios HTTP, guards por permiso y pruebas Playwright.
- Facturación actual: se deriva de ventas y ya dispone de PDF, impresión A4/POS-80, enlace público, correo SMTP y auditoría.
- Impuestos: administrables, con impuestos incluidos o adicionales, relaciones por producto/categoría y snapshots históricos.
- Descuentos: administrables, por porcentaje o monto, acumulables, con vigencia, límites y relaciones.
- Productos: actualmente conservan un único `ColorId`, `Cantidad`, `Costo` y `Precio`; esta estructura no permite existencias simultáneas por varios colores.
- Inventario: los movimientos actuales se relacionan con producto, no con una variante concreta.
- Clientes y proveedores: CRUD existente, sin flujo general de importación masiva con vista previa e historial.

## Hallazgos principales

1. El cálculo de impuesto incluido ya usa la fórmula correcta: `base = importe / (1 + tasa)`.
2. El subtotal actual descuenta impuestos incluidos, pero todavía no contempla un costo de envío incluido en el total comercial.
3. La factura existe como documento asociado a venta; debe ampliarse sin duplicar el flujo de ventas ni generar doble afectación de inventario.
4. `Producto.ColorId` debe conservarse temporalmente como compatibilidad, pero la fuente futura será `ProductoVariante`.
5. La cantidad total del producto deberá derivarse de la suma de variantes activas.
6. Las facturas históricas deben conservar snapshots; no se recalcularán con configuraciones futuras.

## Diseño objetivo

### Costos de envío

Entidades previstas:

- `CostoEnvio`
- `VentaCostoEnvio` o snapshot equivalente en `Venta`
- `HistorialAplicacionCostoEnvio`

Reglas:

- Un solo costo por factura/venta.
- Valor inicial sembrado: L. 80.00.
- Solo un registro predeterminado activo.
- Permiso separado para administrar y para exonerar.
- El backend será la fuente oficial del cálculo.
- El costo aplicado quedará persistido como snapshot.

Fórmula objetivo para precios con impuesto y envío incluidos:

- `montoSujetoImpuesto = importeBruto - costoEnvioIncluido`
- `subtotalNeto = montoSujetoImpuesto - impuestoIncluido - descuento`
- `totalFinal = subtotalNeto + impuestoIncluido + impuestoAdicional + costoEnvioIncluido`

El descuento reduce el total pagado. El envío incluido se resta para obtener el subtotal y se suma en el desglose final exactamente una vez.

### Variantes de producto

Entidades previstas:

- `ProductoVariante`
- ampliación de movimientos, detalles de compra y detalles de venta con `ProductoVarianteId`

Campos mínimos:

- ProductoId
- ColorId
- SKU
- Código de barras
- Cantidad
- Umbral de stock
- Costo opcional
- Precio opcional
- Activo
- auditoría

Restricciones:

- unicidad de producto + color para variantes activas;
- cantidades no negativas;
- la cantidad consolidada del producto se deriva de variantes;
- ventas y compras afectan exclusivamente la variante seleccionada;
- variantes con historial no se eliminan físicamente.

Migración de compatibilidad:

- Cada producto existente recibirá una variante inicial.
- Si tiene `ColorId`, se reutilizará ese color.
- Si no tiene color, la variante será temporalmente “Sin especificar”.
- La cantidad inicial de la variante será la cantidad actual del producto.
- No se duplicará inventario.

### Facturación

Se ampliará el flujo existente, evitando crear un segundo motor de facturación. Alcance:

- listado y filtros;
- estados y transiciones controladas;
- pagos totales/parciales y saldo;
- anulación con reversión transaccional;
- variante/color en cada línea;
- costo de envío y snapshots;
- exportación y reportes;
- PDF, correo e impresión reutilizando los servicios existentes.

### Cargas masivas

Entidades previstas:

- `CargaMasiva`
- `CargaMasivaError`

Primera cobertura:

- clientes;
- proveedores;
- productos;
- variantes e inventario inicial;
- colores.

Flujo:

1. plantilla CSV/Excel;
2. carga y validación sin persistir;
3. vista previa;
4. confirmación explícita;
5. transacción;
6. resumen e informe de errores;
7. auditoría e idempotencia.

## Mapa de impacto

### Domain

- nuevas entidades de envío, variantes, pagos y cargas;
- relaciones nuevas en producto, venta, compra y movimientos;
- estados de factura/pago cuando no existan equivalentes.

### Application

- DTOs y validadores;
- ampliación de `CalculoService`;
- servicios de costo de envío, variantes, pagos e importación;
- permisos y auditoría;
- pruebas unitarias.

### Infrastructure

- configuraciones EF Core;
- repositorios;
- migración aditiva;
- seed de L. 80.00;
- snapshots y restricciones únicas.

### API

- controladores/endpoints de costos de envío, variantes, pagos y cargas;
- ampliación de facturas, ventas, compras, productos e inventario.

### Frontend

- mantenimiento de costos de envío;
- editor de variantes en producto;
- selección de variante en compras/ventas;
- desglose de envío en factura;
- interfaz de cargas masivas;
- permisos, navegación y reportes.

### Pruebas

- cálculo matemático;
- costo aplicado una sola vez;
- stock por color;
- migración sin duplicación;
- permisos;
- carga masiva e idempotencia;
- regresión de PDF, correo e impresión.

## Secuencia de implementación

1. Completar el modelo y cálculo de costos de envío.
2. Integrar envío en venta/factura, PDF y frontend.
3. Crear variantes y migración de compatibilidad.
4. Integrar variantes en producto, compra, venta e inventario.
5. Completar mantenimiento ampliado de facturas y pagos.
6. Implementar cargas masivas.
7. Completar permisos, auditoría y reportes.
8. Ejecutar CI aislado, pruebas E2E y documentación final.

## Riesgos controlados

- Compatibilidad con datos históricos.
- Evitar doble descuento de inventario entre venta y factura.
- Redondeo al separar impuesto y envío incluidos.
- Productos sin color actual.
- Concurrencia al vender la última unidad de una variante.
- Archivos masivos maliciosos o duplicados.

## Criterio de cierre

La fase no se declarará completa hasta que backend, pruebas, migración aislada y build Angular estén aprobados en CI y el PR #2 continúe abierto y en borrador.
