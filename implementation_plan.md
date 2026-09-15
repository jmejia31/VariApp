# Ciclo funcional complementario — Ajustes técnicos de implementación

Este documento constituye el plan de implementación técnico y de control de regresiones para la fase de estabilidad de VariApp. Detalla los ajustes de diseño requeridos y las acciones para solucionar las regresiones existentes en la rama `Desarrollo`.

---

## CONGELAMIENTO DE PRODUCCIÓN Y ENTORNO DE TRABAJO (CRÍTICO)

> [!IMPORTANT]
> - Esta implementación se ejecutará exclusivamente en entornos locales y en `varistorehn_desarrollo`.
> - Queda prohibido modificar `main`, crear ramas adicionales, o fusionar el PR #2.
> - No se ejecutará ninguna migración, despliegue o cambio destructivo sobre Producción.

---

## 1. Plan de Corrección de Regresiones (Prioridad Inmediata)

Antes de avanzar con las secciones funcionales 2 a 6, se corregirán las regresiones transversales introducidas en el commit `4a69ae3e` que hacen fallar las pruebas de aceptación:
1. **Error 503 en Creación de Ventas:** Corregido. Inyectado `ITipoClientePredeterminadoResolver` para asignar la clasificación por defecto o fallback en la creación de clientes al vuelo.
2. **Carga Masiva de Clientes `ConErrores`:** Corregido. Declaradas las columnas `TipoCliente` y `TipoInventario` como opcionales en el importador para admitir plantillas sin esas columnas y aplicar la asignación automática por defecto.
3. **Dependencias de Angular y Vulnerabilidades Altas:** Corregido. Actualizados todos los paquetes del framework Angular a la versión segura `20.3.27`, resolviendo las alertas altas de `HttpTransferCache` y XSS sin vulnerabilidades productivas restantes.
4. **Fijación de Playwright:** Corregido. Fijada la versión exacta `@playwright/test: 1.49.1` en `package.json` y removida la instalación dinámica de `@latest` en los workflows.

---

## 2. Ajustes Técnicos de Diseño para Módulos Complementarios

### A. Mantenimiento de Clientes (`TipoCliente`) y Predeterminado Único
* **Nueva Migración Independiente:** No se modificará la migración inicial ya publicada `20260803121112_AddTipoCliente`. Se creará una migración nueva llamada `AddTipoClientePredeterminadoUnico`.
* **Columna Generada Almacenada (`STORED`):** Creará la columna utilizando la sintaxis de MySQL 8.0+:
  ```sql
  ALTER TABLE TipoClientes
  ADD COLUMN EsPredeterminadoUnico VARCHAR(10)
  GENERATED ALWAYS AS (
      IF(
          EsPredeterminado = 1
          AND Activo = 1
          AND Eliminado = 0,
          'DEFAULT',
          NULL
      )
  ) STORED;

  CREATE UNIQUE INDEX IX_TipoClientes_EsPredeterminadoUnico ON TipoClientes(EsPredeterminadoUnico);
  ```
* **Control Transaccional en el Servicio:** Dado que el índice único solo garantiza como máximo un predeterminado, la lógica en `TipoClienteService` implementará una transacción explícita con reintento ante concurrencia. Se desmarcará el predeterminado anterior, se marcará el nuevo, y se capturará específicamente la excepción de violación de índice único devolviendo un error de negocio controlado.

### B. Control de Concurrencia de Inventarios (`FOR UPDATE`)
* **Lógica Parametrizada y Bloqueo Ordenado de Variantes:** Se evitará la concatenación de IDs en cadenas SQL. El bloqueo de filas se ejecutará de forma secuencial y determinista dentro de una transacción robusta con reintento de la unidad transaccional completa:
  ```csharp
  var strategy = _context.Database.CreateExecutionStrategy();
  await strategy.ExecuteAsync(async () =>
  {
      await using var transaction = await _context.Database.BeginTransactionAsync();
      
      // 1. Ordenar ProductoId de menor a mayor y bloquear Productos
      foreach (var productoId in productoIds.OrderBy(x => x))
      {
          await _context.Database
              .SqlQueryRaw<int>(
                  "SELECT Id AS Value FROM Productos WHERE Id = {0} FOR UPDATE", 
                  productoId)
              .SingleAsync();
      }

      // 2. Ordenar ProductoVarianteId de menor a mayor y bloquear ProductoVariantes
      foreach (var varianteId in varianteIds.OrderBy(x => x))
      {
          await _context.Database
              .SqlQueryRaw<int>(
                  "SELECT Id AS Value FROM ProductoVariantes WHERE Id = {0} FOR UPDATE", 
                  varianteId)
              .SingleAsync();
      }
      
      // 3. Validar el estado de la operación y el stock de insumos/productos
      // 4. Aplicar descuentos de inventario y registrar movimientos correspondientes
      await _context.SaveChangesAsync();
      await transaction.CommitAsync();
  });
  ```
* **Idempotencia:** La lógica de consumo y venta aplicará un cambio de estado atómico (ej. `Borrador -> Confirmado`) para que cualquier reintento de la transacción o petición HTTP duplicada no descuente el inventario dos veces.

### C. Compatibilidad del Escáner con Productos Simples
* **Variante Técnica Predeterminada:** Los códigos SKU y Código de Barras residen y se administran únicamente en `ProductoVariante`.
* Para dar soporte de escaneo a productos simples (sin variantes comerciales reales), el sistema creará de forma transparente una variante predeterminada técnica. Las búsquedas del escáner coincidirán de forma directa sobre `ProductoVariante` evitando búsquedas directas en la tabla principal `Productos`.

### D. Redondeo y Distribución de Centavos en Facturas
* **Fórmula de Facturación:** El total del documento incluirá el flete:
  $$\text{TotalProductos} = \sum(\text{SubtotalLinea} - \text{DescuentoLinea} + \text{ImpuestoLinea})$$
  $$\text{TotalFactura} = \text{TotalProductos} + \text{CostoEnvio}$$
* **Política de Redondeo:** Los cálculos intermedios de líneas e impuestos se redondearán explícitamente utilizando `MidpointRounding.AwayFromZero`. El flete de envío se sumará exactamente una vez sobre el total final. Las diferencias por distribución se persistirán.

### E. Anulación de Compras
* **Bloqueo Conservador:** Para evitar distorsionar el costo promedio ponderado histórico sin un módulo completo de trazabilidad de lotes, se aplicará un bloqueo estricto si existe **cualquier movimiento posterior** (compra posterior, venta, ajuste, consumo o reversión) sobre la combinación de `ProductoId` y `ProductoVarianteId` de la compra que se pretende anular.

### F. Validación de Imágenes en Backend (Seguridad Crítica)
* **Biblioteca y Versión:** Se añadirá la biblioteca `SixLabors.ImageSharp` (versión exacta fijada en `3.1.5`) al proyecto de infraestructura.
* **Límites de Seguridad y Decodificación:** Al subir una imagen de producto o perfil, el backend:
  1. Validará el tamaño máximo del archivo (10 MB).
  2. Validará dimensiones de resolución máximas (4096 px de ancho por 4096 px de alto).
  3. Restringirá la cantidad máxima de píxeles (16 megapíxeles) para evitar bombas de descompresión.
  4. Inspeccionará los *magic numbers* de bytes para coincidir con JPG/JPEG, PNG o WEBP.
  5. Decodificará completamente la imagen en memoria liberando correctamente todos los streams.
  6. Sanitizará y guardará una copia recodificada limpia, eliminando todos los metadatos EXIF sensibles antes de transferirla a Cloudinary.

### G. Logs Seguros de Búsqueda
* Se eliminará el registro de los términos de búsqueda textuales completos en los logs de auditoría de rendimiento.
* Se registrarán únicamente: `Ruta`, `DuracionMs`, `LongitudTermino` (entero), `CantidadResultados`, `EstadoHTTP` y `CorrelationId`.

---

## 3. Plan de Verificación Ampliado

### Verificación Técnica de CI
Toda actualización de la rama `Desarrollo` se someterá al pipeline local/remoto de pruebas de integración completa:
```bash
# 1. Restauración y compilación en modo Release
dotnet restore backend/InventoryApp.sln
dotnet build backend/InventoryApp.sln --configuration Release

# 2. Ejecución de suite de pruebas unitarias
dotnet test backend/InventoryApp.sln --configuration Release

# 3. Instalación y validación estática del Frontend
npm --prefix frontend ci
npm --prefix frontend run lint

# 4. Compilación de producción del Frontend
npm --prefix frontend run build:prod
```

### Escenarios de Pruebas Funcionales Obligatorios
- **Ejecución Total de Tests de Backend:** Se correrán y completarán sin fallos ni omisiones la totalidad de las pruebas unitarias y de integración del backend descubiertas.
- **Predeterminado Único:** Probar concurrencia de marcado de predeterminado en `TipoCliente` validando que la segunda petición capture el error de índice único controlado.
- **Concurrencia de Stock:** Simular peticiones paralelas de consumo sobre el mismo stock para certificar la consistencia del control transaccional.
- **Exclusión comercial de insumos:** Intentar incluir un insumo administrativo en ventas o borrador de ventas y verificar el rechazo 400.
- **Flete y Componentes Financieros:** Validar que compras mixtas dividan correctamente fletes capitalizados o generen componentes financieros separados de gasto operativo según corresponda.
- **Filtro de Imágenes:** Subir archivos renombrados maliciosos y comprobar el bloqueo y la generación de logs de advertencia.
