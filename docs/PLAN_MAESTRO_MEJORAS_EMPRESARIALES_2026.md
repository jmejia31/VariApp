# Plan Maestro de Mejoras Empresariales — VariApp

Versión: 4.0 — Saneamiento relacional integral + variantes multidimensionales
Fecha: 2026-08-08
Rama exclusiva: `Desarrollo`
PR oficial: `#2 Desarrollo -> main` (debe permanecer abierto y en borrador)
Producción: congelada

## 1. Principios obligatorios

1. No modificar `main`, no crear ramas nuevas, no fusionar PR #2 y no habilitar auto-merge.
2. No ejecutar migraciones, seeds, restauraciones, despliegues ni cambios de configuración sobre Producción.
3. Antes de ampliar funcionalidad se saneará primero la estructura relacional que sirve de base al sistema.
4. Toda regla crítica de integridad se reforzará en backend y, cuando corresponda, también en MySQL mediante PK, FK, UNIQUE, CHECK, índices o columnas generadas.
5. Ninguna evolución puede reescribir históricos confirmados.
6. Los snapshots históricos deliberados NO se consideran un defecto de normalización y se conservarán.
7. No se crearán tablas únicamente por estética: cada separación deberá responder a identidad, cardinalidad, integridad, mantenibilidad o necesidad administrativa real.
8. Las operaciones de stock, compra, venta, factura, anulación y ajuste se resolverán contra la variante exacta después de M2.
9. Los estados técnicos de máquinas de estado permanecerán como códigos/enums estables salvo requisito funcional explícito.
10. Los gastos financieros no se modelarán como productos; los insumos administrativos permanecen separados de la mercadería vendible.
11. Cada fase se cerrará con build, pruebas pertinentes, migraciones descartables cuando apliquen, regresión, CI real, documentación y evidencia.
12. No se declarará `completo`, `funciona`, `certificado` o `100 %` sin evidencia técnica real.

---

## 2. Resultado de la auditoría estructural de base de datos

La base versionada de VariApp es funcional y tiene varias decisiones correctas, pero NO se considera todavía una base relacional final suficientemente saneada para construir encima el motor multidimensional de variantes.

La auditoría se realizó sobre:

- entidades de Domain;
- `AppDbContext`;
- configuraciones EF Core;
- historial de migraciones;
- `AppDbContextModelSnapshot`;
- relaciones, índices, restricciones y delete behaviors representados por el modelo versionado.

Esta auditoría describe el esquema que el código de `Desarrollo` pretende aplicar. No equivale a afirmar que una instancia externa concreta de MySQL/Aiven tenga exactamente ese estado hasta verificarla mediante un entorno de Desarrollo autorizado o una base descartable.

### 2.1 Hallazgos que requieren corrección antes de M2

#### A. Catálogo polimórfico `CatalogosProducto`

Marca, Modelo, Color y Talla comparten actualmente una tabla genérica con discriminador `Tipo` y Modelo usa `CatalogoPadreId` para representar su Marca.

Aunque una tabla genérica con discriminador no constituye por sí sola una violación automática de 3FN, en VariApp impide expresar con suficiente fuerza semántica el dominio objetivo y obliga a FKs genéricas. Para el modelo empresarial aprobado se reemplazará como fuente de verdad por:

- `Marcas`;
- `Modelos`;
- `Colores`;
- `Tallas`.

Cada una tendrá entidad, tabla, mantenimiento, API y permisos propios.

#### B. `Producto` conserva fuentes duplicadas/transitorias

`Producto` mantiene simultáneamente:

- `Marca` texto + `MarcaId`;
- `Modelo` texto + `ModeloId`;
- `ColorId`;
- `TallaId`;
- `Cantidad`, `Costo`, `Precio` agregados;
- colección de variantes.

Esta coexistencia fue válida como compatibilidad, pero después de M2 no puede seguir siendo una fuente operativa paralela. Marca/Modelo/Color/Talla pasarán a la variante y `Cantidad/Costo/Precio` de Producto serán únicamente resumen derivado cuando se mantengan.

#### C. RBAC mantiene dos modelos simultáneos

`Usuario` conserva `Rol` legado y `RolId` dinámico. `RolPermiso` conserva simultáneamente `Rol/Modulo/Accion` y `RolId/PermisoId`.

La transición debe cerrarse para que las fuentes de verdad sean:

- `Usuarios.RolId -> Roles.Id`;
- `RolPermisos.RolId -> Roles.Id`;
- `RolPermisos.PermisoId -> Permisos.Id`.

Las columnas legacy se retirarán únicamente después del backfill y de comprobar compatibilidad de autorización.

#### D. Tablas puente de descuentos incompletas en integridad referencial

`DescuentoProductos`, `DescuentoCategorias`, `DescuentoClientes` y `DescuentoRoles` necesitan reforzar:

- FK hacia el objeto destino además de FK hacia `Descuentos`;
- UNIQUE por pareja para impedir asignaciones duplicadas.

Objetivos conceptuales:

- UNIQUE(`DescuentoId`, `ProductoId`);
- UNIQUE(`DescuentoId`, `CategoriaId`);
- UNIQUE(`DescuentoId`, `ClienteId`);
- UNIQUE(`DescuentoId`, `RolId`).

#### E. Tablas puente de impuestos incompletas en integridad referencial

`ImpuestoProductos`, `ImpuestoCategorias`, `ImpuestoClientes`, `ImpuestoProveedores` e `ImpuestoOperaciones` requieren el mismo hardening:

- FKs hacia las entidades destino;
- UNIQUE por pareja;
- UNIQUE(`ImpuestoId`, `Operacion`) para operaciones.

#### F. Relaciones históricas/documentales con IDs no protegidos por FK

Se revisarán y reforzarán, después de preflight, relaciones como:

- `FacturaDetalle.ProductoId`;
- `FacturaDetalle.ProductoVarianteId`;
- `EnlacePublicoFactura.FacturaId`;
- `HistorialEnvioFactura.FacturaId`;
- `VentaDescuento.DescuentoId`;
- `VentaImpuesto.ImpuestoId`;
- `CompraImpuesto.ImpuestoId`;
- referencias de historial que puedan tener FK segura.

La política preferida para datos históricos será `Restrict/NoAction` o `SetNull` cuando corresponda, no borrado en cascada destructivo.

#### G. Referencias polimórficas de Finanzas e Inventario

`MovimientoFinanciero` mantiene `ModuloOrigen + ReferenciaId` y además `CompraId/VentaId/FacturaId`. Esto permite estados contradictorios y no ofrece integridad completa.

Se normalizará hacia FKs tipadas, con una regla explícita de exactamente un origen cuando sea automático y sin origen documental cuando sea manual.

`MovimientoInventario` mantiene `ReferenciaTipo + ReferenciaId`. Se evaluará migrarlo a referencias tipadas a:

- Compra;
- Venta;
- Consumo de insumo;
- Ajuste de inventario;
- otras fuentes justificadas.

Para ajustes manuales se incorporará una entidad/documento `AjusteInventario` si el preflight confirma que actualmente no existe una cabecera relacional equivalente.

`HistorialAplicacionImpuesto` también será revisado para decidir entre FKs tipadas a Compra/Venta o consolidación con los snapshots `CompraImpuesto/VentaImpuesto`, evitando dos historiales contradictorios.

#### H. Reglas de unicidad de negocio que necesitan corrección

La unicidad actual basada únicamente en `Cliente.Nombre` y `Proveedor.Nombre` es demasiado restrictiva: personas o empresas distintas pueden compartir nombre.

Objetivo:

- `Nombre` será índice de búsqueda, no identidad universal;
- `Cliente.IdentidadORTN` podrá ser único cuando exista, previo preflight;
- `Proveedor.Documento` podrá ser único cuando exista, previo preflight;
- correo/teléfono no se declararán únicos automáticamente sin requisito de negocio.

`Categoria` deberá contar con nombre normalizado y unicidad segura si el negocio confirma que dos categorías vigentes no pueden compartir el mismo nombre.

#### I. Predeterminados/singletons protegidos solo por lógica de aplicación

Se reforzarán a nivel MySQL cuando la regla sea realmente única:

- un solo `CostoEnvio` predeterminado activo/no eliminado;
- una sola `EmpresaConfiguracion` activa si el contrato funcional confirma que la aplicación trabaja con una configuración activa global.

Se reutilizará el patrón ya validado de columna generada + índice único utilizado por `TipoCliente` predeterminado.

#### J. Precisión decimal inconsistente

El snapshot muestra columnas monetarias/de cálculo que quedaron con precisión implícita amplia (`decimal(65,30)`) mientras otros módulos usan `decimal(18,2)` o `decimal(18,4)`.

Se estandarizará:

- dinero final/pagado/costo/precio/total: normalmente `decimal(18,2)`;
- bases fiscales y montos internos que requieren precisión intermedia: `decimal(18,4)`;
- tasas: `decimal(9,4)`;
- redondeo final seguirá la política monetaria ya certificada.

Toda reducción de precisión tendrá preflight de datos y prueba de no pérdida material antes de migrar.

#### K. Longitudes/tipos de texto inconsistentes

Se revisarán columnas que actualmente terminan como `longtext` por falta de configuración explícita aunque semánticamente sean correo, teléfono, RTN, usuario, URL o nombre.

Se aplicarán límites razonables únicamente cuando exista contrato claro. JSON de auditoría, JSON de staging y textos realmente extensos conservarán tipos adecuados.

#### L. Delete behaviors de documentos contables

La relación `Venta -> Factura` no debe depender de un cascade destructivo como defensa final del histórico. Se revisará hacia `Restrict/NoAction` y se mantendrán anulaciones/soft delete como mecanismo funcional.

Los cascades legítimos sobre detalles internos de un agregado podrán conservarse si el agregado raíz está protegido contra eliminación física.

#### M. Métodos de pago con representación inconsistente

Ventas/Compras persisten `MetodoPago` con una representación y `FacturaPago` utiliza otra. Además, el plan empresarial exige mantenimiento futuro de métodos de pago.

Se decidirá una única arquitectura:

- tabla `MetodosPago` con `Codigo` técnico estable, nombre administrable, orden, activo y auditoría;
- FK para operaciones nuevas;
- snapshot/código histórico para documentos confirmados;
- compatibilidad temporal con enums existentes.

No se romperán facturas históricas al renombrar/desactivar un método.

### 2.2 Desnormalizaciones intencionales que deben conservarse

No se eliminarán por intentar alcanzar una normalización académica ciega:

- snapshots de Cliente/Proveedor en Venta/Compra confirmadas;
- snapshots de Empresa/Cliente/Vendedor en Factura;
- snapshots de Producto/Variante en CompraDetalle, VentaDetalle, FacturaDetalle y movimientos;
- snapshots de impuesto/descuento aplicado;
- totales monetarios de documentos confirmados;
- JSON de auditoría;
- JSON temporal de cargas masivas;
- hash del token en enlaces públicos.

Estas duplicaciones son deliberadas para inmutabilidad histórica, auditoría y reproducibilidad documental.

---

## 3. Arquitectura comercial definitiva

### Producto

Representa la familia o concepto comercial y conservará solo información general:

- Nombre;
- Categoría;
- Tipo de inventario;
- Descripción;
- imágenes generales;
- estado;
- auditoría.

### Variante

Representa la unidad física exacta de inventario y podrá combinar:

- Marca;
- Modelo;
- Color;
- Talla/Tamaño;
- SKU;
- Código de barras;
- Cantidad;
- Costo;
- Precio;
- Umbral de stock bajo;
- imágenes propias;
- estado.

La identidad comercial será:

`Producto + Marca + Modelo + Color + Talla`

Modelo exige Marca y debe pertenecer a ella.

Ejemplos:

- Cobertor SPACE + Samsung + S24 Ultra + Negro + Sin talla = 12 unidades.
- Cobertor SPACE + Samsung + S24 Ultra + Azul + Sin talla = 7 unidades.
- Cobertor SPACE + Samsung + S23 Ultra + Negro + Sin talla = 4 unidades.
- Camiseta + Nike + Modelo A + Negro + M = 6 unidades.
- Camiseta + Nike + Modelo A + Negro + L = 9 unidades.

---

# FASE M0 — Auditoría y mapa de impacto

Estado: COMPLETADA.

Entregable: `docs/FASE_M0_AUDITORIA_MAPA_IMPACTO.md`.

# FASE M0.B — Auditoría estructural de normalización

Estado: COMPLETADA A NIVEL DE ESQUEMA VERSIONADO.

Objetivo cumplido: revisar el modelo relacional antes de continuar con variantes.

Resultado: se detectó deuda estructural suficiente para convertir M1 en una fase obligatoria de saneamiento integral, no únicamente de catálogos.

No se realizaron cambios funcionales ni migraciones externas durante M0.B.

---

# FASE M1 — Saneamiento relacional integral de MySQL/EF Core

Objetivo: dejar la base estructural lista para que M2 no nazca sobre fuentes duplicadas, FKs débiles o restricciones incompletas.

**Gate obligatorio:** M2 NO puede iniciar hasta que M1 esté verde en migración descartable, snapshot EF, backend, frontend afectado e integración MySQL.

## M1.A — Baseline y preflight de datos

- congelar el baseline de `AppDbContextModelSnapshot`;
- inventariar todas las entidades/tablas/FKs/índices;
- generar consultas de preflight para duplicados y huérfanos;
- comprobar `CatalogosProducto` por Tipo;
- comprobar Modelo -> Marca;
- comprobar SKU/barcode duplicados;
- comprobar tablas puente duplicadas/huérfanas;
- comprobar clientes/proveedores afectados por nuevas reglas de identidad;
- comprobar configuraciones/predeterminados múltiples;
- comprobar valores decimales incompatibles con las precisiones objetivo;
- abortar fail-closed si una transformación no puede probarse segura.

## M1.B — Normalización de Marcas, Modelos, Colores y Tallas

Crear entidades y tablas independientes:

- `Marca` / `Marcas`;
- `Modelo` / `Modelos`;
- `Color` / `Colores`;
- `Talla` / `Tallas`.

Reglas:

- `Modelo.MarcaId` obligatorio;
- nombre de Modelo único dentro de Marca;
- no activar Modelo con Marca inactiva;
- nombres normalizados con unicidad vigente donde corresponda;
- soft delete;
- auditoría;
- permisos independientes.

Migración desde `CatalogosProducto`:

1. copiar preservando IDs cuando sea seguro;
2. convertir `CatalogoPadreId` a `Modelo.MarcaId`;
3. verificar conteos y mapeos;
4. migrar consumidores;
5. retirar dependencia runtime de `CatalogosProducto`;
6. retirar tabla genérica solo cuando no existan referencias funcionales y CI sea verde.

Mantenimientos independientes obligatorios:

- Marcas;
- Modelos;
- Colores;
- Tallas.

## M1.C — Normalización del RBAC legado

Objetivo final:

- `Usuarios.RolId` como fuente de verdad;
- `RolPermisos(RolId, PermisoId)` como relación normalizada;
- permisos definidos en `Permisos`.

Trabajo:

- preflight de usuarios sin RolId;
- backfill desde enum legado;
- reconciliar duplicados de RolPermiso;
- validar autorización equivalente antes/después;
- retirar dependencia runtime de `Usuario.Rol` y `RolPermiso.Rol/Modulo/Accion` cuando sea seguro;
- mantener compatibilidad solo durante la transición.

## M1.D — Integridad de tablas puente

### Descuentos

Agregar FKs y unicidad por pareja para:

- DescuentoProducto;
- DescuentoCategoria;
- DescuentoCliente;
- DescuentoRol.

### Impuestos

Agregar FKs y unicidad por pareja para:

- ImpuestoProducto;
- ImpuestoCategoria;
- ImpuestoCliente;
- ImpuestoProveedor;
- ImpuestoOperacion.

Antes de constraints:

- eliminar únicamente duplicados demostrados semánticamente equivalentes mediante migración determinista;
- no borrar históricos de uso;
- fallar si existen huérfanos cuyo destino no pueda resolverse.

## M1.E — Integridad documental e histórica

Agregar/reforzar FKs seguras para IDs actualmente desprotegidos, incluyendo según preflight:

- FacturaDetalle -> Producto;
- FacturaDetalle -> ProductoVariante;
- EnlacePublicoFactura -> Factura;
- HistorialEnvioFactura -> Factura;
- VentaDescuento -> Descuento;
- VentaImpuesto -> Impuesto;
- CompraImpuesto -> Impuesto;
- historiales de impuesto/descuento -> sus documentos/clientes/usuarios cuando proceda.

Política:

- históricos normalmente `Restrict/NoAction`;
- `SetNull` solo cuando perder la referencia viva sea aceptable pero se conserve snapshot;
- evitar cascade destructivo de documentos contables.

## M1.F — Normalización de orígenes financieros e inventario

### MovimientoFinanciero

Reemplazar la ambigüedad entre `ModuloOrigen/ReferenciaId` y múltiples IDs tipados por un contrato único.

Objetivo:

- FKs a Compra/Venta/Factura cuando correspondan;
- regla de exclusividad de origen para movimientos automáticos;
- movimientos manuales sin FK documental obligatoria;
- reversión explícita y trazable.

### MovimientoInventario

Sustituir gradualmente `ReferenciaTipo/ReferenciaId` por referencias tipadas.

Crear `AjusteInventario` como agregado/documento de ajuste si el preflight confirma que no existe estructura equivalente.

Cada movimiento deberá poder demostrar su origen relacional o su carácter manual autorizado.

### HistorialAplicacionImpuesto

Decidir mediante evidencia entre:

- FKs tipadas Compra/Venta;
- o consolidación con `CompraImpuesto/VentaImpuesto`.

No mantener dos fuentes históricas contradictorias.

## M1.G — Identidad y unicidad de entidades maestras

### Clientes

- retirar UNIQUE de Nombre;
- índice de búsqueda por nombre normalizado;
- evaluar UNIQUE condicional de `IdentidadORTN` cuando exista;
- no imponer unicidad a correo/teléfono sin requisito.

### Proveedores

- retirar UNIQUE de Nombre;
- índice de búsqueda por nombre normalizado;
- evaluar UNIQUE condicional de Documento cuando exista.

### Categorías

- incorporar `NombreNormalizado`;
- UNIQUE vigente si el negocio mantiene categoría única por nombre;
- preparar jerarquía únicamente si M1 confirma requisito real de subcategorías.

## M1.H — Singletons y predeterminados

Reforzar mediante restricciones MySQL:

- un solo CostoEnvio predeterminado vigente;
- una sola EmpresaConfiguracion activa si ese es el contrato real;
- conservar el patrón robusto ya existente de TipoCliente predeterminado.

Debe existir prueba de concurrencia que intente crear dos predeterminados simultáneos.

## M1.I — Normalización de métodos de pago

Crear, si el preflight confirma la administración dinámica prevista:

`MetodosPago`

Campos mínimos:

- Id;
- Codigo técnico estable;
- Nombre;
- Descripcion;
- Orden;
- Activo;
- Eliminado;
- auditoría.

Migrar gradualmente Compras, Ventas y FacturaPagos a una representación consistente, conservando snapshots/códigos históricos.

## M1.J — Tipos, precisión, longitudes e índices

- estandarizar moneda a `decimal(18,2)`;
- bases fiscales/cálculos intermedios a `decimal(18,4)` cuando corresponda;
- tasas a `decimal(9,4)`;
- eliminar precisiones implícitas `decimal(65,30)` donde no estén justificadas;
- definir longitudes explícitas para correo, teléfono, RTN, nombres, URLs y usuarios;
- conservar `json` y textos largos donde sí estén justificados;
- revisar índices por consultas reales, no por intuición;
- eliminar índices redundantes únicamente con evidencia.

## M1.K — Delete behavior y protección histórica

- cambiar Venta -> Factura a `Restrict/NoAction` si la regresión confirma compatibilidad;
- revisar cascades de entidades maestras hacia históricos;
- permitir cascades solo dentro de agregados cuyo root no sea físicamente eliminable;
- mantener soft delete como política de maestros/documentos cuando aplique.

## M1.L — Migraciones y compatibilidad

Las transformaciones se harán en migraciones pequeñas y reversibles lógicamente:

- preflight;
- expandir esquema;
- backfill;
- dual-read/compatibilidad temporal solo si es necesaria;
- cambiar fuente de verdad;
- validar;
- retirar columnas/tabla legacy al final.

No se mezclará en una única migración gigante la creación, backfill y destrucción de todas las estructuras.

## M1.M — Certificación de base saneada

Obligatorio:

- `dotnet restore`;
- backend Release;
- unitarias;
- integración MySQL;
- aplicar todo el historial de migraciones en base vacía descartable;
- migrar una base descartable con estructura/datos legacy representativos;
- comprobar `AppDbContextModelSnapshot`;
- generar/revisar SQL forward;
- pruebas de FKs, UNIQUE y CHECK/columnas generadas;
- pruebas de concurrencia de predeterminados;
- pruebas RBAC después del backfill;
- frontend lint/build de mantenimientos afectados;
- E2E de catálogos/RBAC cuando aplique;
- GitHub Actions reales.

Criterio de salida:

**No quedan P0/P1 relacionales abiertos que puedan contaminar M2.**

---

# FASE M2 — Motor de variantes multidimensionales

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-09)**.

Entregable: `docs/FASE_M2_VARIANTES_MULTIDIMENSIONALES.md`.

Objetivo: convertir `ProductoVariante` en la unidad exacta de inventario para Marca + Modelo + Color + Talla sobre la base saneada de M1.

## M2.A — Dominio de Variante

Agregar a `ProductoVariante`:

- `MarcaId`;
- `ModeloId`;
- `ColorId`;
- `TallaId`;
- navegaciones a las cuatro tablas normalizadas.

Mantener:

- SKU;
- código de barras;
- cantidad;
- costo;
- precio;
- umbral;
- estado;
- soft delete;
- variante técnica.

## M2.B — Integridad MySQL

- FK Variante -> Marca;
- FK Variante -> Modelo;
- FK Variante -> Color;
- FK Variante -> Talla;
- garantía Modelo pertenece a Marca;
- índice único multidimensional comercial;
- SKU único global;
- código de barras único global cuando exista;
- variante técnica única por Producto;
- tratamiento determinista de NULL y soft delete en la clave comercial.

## M2.C — Backfill no destructivo de variantes

- conservar `ProductoVariante.Id`;
- conservar Color actual;
- heredar Marca/Modelo/Talla legacy desde Producto cuando corresponda;
- preservar SKU/barcode/stock/costo/precio;
- detectar colisiones antes de migrar;
- abortar fail-closed ante combinaciones ambiguas.

## M2.D — DTO/API/Servicios

Actualizar contratos de Producto/Variante para devolver y validar Marca, Modelo, Color y Talla.

SKU:

- manual permitido;
- vacío -> backend genera uno único;
- frontend no es autoridad de unicidad.

## M2.E — Constructor de variantes en Productos

Reemplazar `Colores y existencias` por `Variantes y existencias`.

Cada fila:

- Marca;
- Modelo dependiente de Marca;
- Color;
- Talla/Tamaño;
- Cantidad;
- SKU;
- código de barras;
- costo;
- precio;
- umbral.

Mejoras:

- `Agregar variante`;
- copiar fila anterior;
- validación de duplicados;
- etiqueta canónica en vivo;
- resumen total y por dimensión;
- errores por fila;
- responsive;
- teclado/accesibilidad.

### Generador de combinaciones

Permitir seleccionar múltiples Modelos/Colores/Tallas y generar una vista previa de combinaciones válidas antes de confirmar.

No guardar combinaciones automáticamente sin confirmación.

## M2.F — Administrador de variantes

- alta;
- edición de atributos sin sobrescribir stock;
- activar/desactivar;
- ajuste de stock separado;
- soft delete con stock cero;
- filtros Marca/Modelo/Color/Talla/SKU/barcode/estado;
- historial/auditoría;
- indicadores de stock bajo/agotado.

## M2.G — Imágenes por variante

Extender imagen de Producto para soportar asociación a Variante o estructura equivalente normalizada.

- múltiples imágenes por variante;
- principal por ámbito;
- orden;
- preview;
- reemplazo/eliminación segura;
- fallback a imagen general del Producto;
- seguridad de imágenes 2F preservada.

## M2.H — Compras

- seleccionar variante exacta;
- etiqueta `Producto · Marca · Modelo · Color · Talla · SKU`;
- sumar stock solo a esa variante;
- costo por variante;
- snapshots Marca/Modelo/Color/Talla/SKU;
- anulación revierte exactamente esa variante;
- concurrencia/locks sobre fila correcta.

## M2.I — Ventas

- seleccionar variante exacta;
- validar stock exclusivamente de esa combinación;
- descontar solo esa variante;
- precio/costo/utilidad de variante;
- snapshots completos;
- anulación devuelve stock a la misma variante;
- impedir sobreventa concurrente.

## M2.J — Facturación/PDF/impresión/compartición

Agregar/preservar snapshots de:

- Marca;
- Modelo;
- Color;
- Talla;
- SKU/código.

Actualizar:

- factura;
- detalle;
- PDF;
- impresión oficina/POS;
- correo;
- WhatsApp/compartición.

## M2.K — Inventario y movimientos

- fuente de verdad: `ProductoVariante.Cantidad`;
- Producto.Cantidad = resumen derivado;
- movimientos apuntan a variante exacta;
- snapshots completos;
- valoración sin doble conteo Producto + Variante;
- ajustes usando el documento normalizado de M1 cuando aplique.

## M2.L — Producto y vistas

- Producto deja de imponer una sola Marca/Modelo/Color/Talla;
- detalle muestra variantes completas;
- filtros Marca -> Modelo -> Color -> Talla;
- agrupación/resúmenes sin ocultar combinaciones distintas.

## M2.M — Escáner/autocomplete/búsqueda

Resolver siempre una variante exacta por SKU/barcode.

Mostrar etiqueta canónica:

`Marca · Modelo · Color · Talla · SKU`

## M2.N — Cargas masivas

Extender `VariantesInventario` con:

- Producto;
- Marca;
- Modelo;
- Color;
- Talla;
- SKU;
- Barcode;
- Cantidad;
- Costo;
- Precio;
- Umbral.

Validar Modelo->Marca, duplicados, catálogos inactivos, SKU/barcode y combinación comercial antes de confirmar.

## M2.O — Reportes/Dashboard

Analítica por:

- Producto;
- Marca;
- Modelo;
- Color;
- Talla;
- variante exacta.

## M2.P — Auditoría/permisos

Auditar cambios de:

- dimensiones;
- stock;
- costo;
- precio;
- estado;
- imágenes.

## M2.Q — Certificación

Casos mínimos:

- producto simple;
- variante técnica;
- un Modelo/múltiples Colores;
- múltiples Modelos;
- Color + Talla;
- Marca + Modelo + Color + Talla;
- compras/anulaciones;
- ventas/anulaciones;
- concurrencia;
- facturación;
- PDF;
- cargas;
- escáner;
- imágenes;
- históricos;
- permisos;
- responsive/accesibilidad.

---

# FASE M3 — Configuración fiscal ISV/ISC

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-09)**.

Entregable: `docs/FASE_M3_CONFIGURACION_FISCAL_ISV_ISC.md`.

Objetivo: certificar y completar la persistencia fiscal ya existente.

- auditar Impuestos después de M1;
- verificar ISV/ISC persistidos;
- seed idempotente;
- snapshots históricos;
- permisos/auditoría;
- cierre/reinicio de sesión y API;
- no reactivar decisiones administrativas mediante seed.

---

# FASE M4 — Estado persistente de filtros y navegación

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-09)**.

Entregable: `docs/FASE_M4_PERSISTENCIA_FILTROS_NAVEGACION.md`.

- búsqueda;
- filtros;
- página;
- pageSize;
- orden;
- retorno desde detalle/edición;
- `Limpiar filtros`;
- query params + sessionStorage según necesidad;
- aislamiento por usuario cuando corresponda.

Cobertura completada: Productos, Ventas, Compras, Clientes, Inventario y Finanzas.

Evidencia enfocada: Playwright M4 sobre MySQL 8.4 descartable, run `31337474683` — **success**.

---

# FASE M5 — Clientes y segmentación

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-09)**.

Entregable: `docs/FASE_M5_CLIENTES_SEGMENTACION.md`.

- `TipoCliente` existente como base;
- filtros y estadísticas por tipo;
- reportes/exportaciones;
- tipos administrables;
- `SIN_CLASIFICAR` protegido;
- segmentación extensible sin hardcodear etiquetas subjetivas.

Cobertura completada: mantenimiento dinámico de `TipoCliente`, filtro persistente por clasificación, estadísticas comparativas, exportación CSV filtrada, protección `SIN_CLASIFICAR` y conservación de las reglas de identidad de M1.

Evidencia enfocada: MySQL 8.4 + backend 17/17 + Playwright M5 3/3, run `31339633125` — **success**.

La identidad de Cliente sigue las reglas corregidas en M1.

---

# FASE M6 — Mercadería, insumos administrativos y gastos

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-09)**.

Entregable: `docs/FASE_M6_MERCADERIA_INSUMOS_GASTOS.md`.

Separación completada:

- mercadería vendible;
- insumo administrativo físico no vendible;
- gasto financiero sin stock.

Incluye permisos, vistas, reportes, valoración, consumos y bloqueo backend contra venta de insumos.

---

# FASE M7 — Costos de envío profesionales

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-09)**.

Entregable: `docs/FASE_M7_COSTOS_ENVIO_PROFESIONALES.md`.

Sobre la restricción de predeterminado ya reforzada en M1:

- zona;
- ciudad;
- departamento;
- modalidad/tipo de envío;
- precio;
- prioridad;
- vigencia;
- activo;
- predeterminado;
- historial;
- snapshots de Venta/Factura;
- eliminación/desactivación segura.

---

# FASE M8 — Búsqueda inteligente y rendimiento

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-10)**.

Entregable: `docs/FASE_M8_BUSQUEDA_INTELIGENTE_RENDIMIENTO.md`.

- SKU/barcode;
- Producto;
- Marca;
- Modelo;
- Color;
- Talla;
- categoría;
- Cliente/Proveedor;
- teléfono/correo;
- observaciones pertinentes;
- DTOs ligeros;
- paginación/cancelación/debounce;
- p50/p95;
- índices basados en medición.

---

# FASE M9 — Cargas masivas profesionales

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-10)**.

Entregable: `docs/FASE_M9_CARGAS_MASIVAS_PROFESIONALES.md`.

Cobertura cerrada sobre la infraestructura existente:

- preview;
- validación;
- progreso por etapas;
- correctos/errores/omitidos;
- descarga de errores;
- códigos consistentes;
- procesamiento por lotes;
- plantillas versionadas;
- auditoría;
- variantes multidimensionales normalizadas.

---

# FASE M10 — UI/UX empresarial y accesibilidad

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-10)**.

Entregable: `docs/FASE_M10_UI_UX_EMPRESARIAL_ACCESIBILIDAD.md`.

- tokens visuales;
- tipografía;
- iconografía;
- botones;
- tarjetas;
- encabezados;
- estados loading/error/success/empty;
- responsive;
- teclado;
- foco;
- labels;
- contraste WCAG;
- componentes reutilizables.

---

# FASE M11 — Backups y restauración en Desarrollo

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-10)**.

Entregable: `docs/FASE_M11_BACKUPS_RESTAURACION_DESARROLLO.md`.

- inventario de activos respaldables;
- MySQL;
- configuración;
- documentos;
- referencias/activos de imágenes;
- checksum;
- cifrado/control de acceso;
- retención;
- metadata;
- restore en entorno descartable;
- validación de integridad.

Producción queda fuera de alcance.

---

# FASE M12 — Automatización transversal

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-10)**.

Entregable: `docs/FASE_M12_AUTOMATIZACION_TRANSVERSAL.md`.

Revisar Productos, Compras, Ventas, Inventario, Clientes, Facturación, Finanzas, Cargas y Configuración para:

- defaults administrables;
- autocompletado;
- cálculos;
- preferencias;
- acciones masivas seguras;
- reducción de captura duplicada;
- sugerencias y recordatorios operativos.

Toda automatización financiera/inventario debe ser determinista y auditable.

---

# FASE M13 — Auditoría integral, hardening y certificación final

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE (2026-08-10)**.

Entregable: `docs/FASE_M13_AUDITORIA_INTEGRAL_HARDENING_CERTIFICACION_FINAL.md`.

HEAD funcional certificado: `19539c72d3a617d95bb3c03dfbde5f6b212ca1de`.

Dictamen automatizado: **APROBADO — P0/P1 abiertos = 0; Producción tocada = false; validación externa/física permanece separada y pendiente**.

Revisar:

- arquitectura;
- normalización e integridad relacional;
- migraciones/snapshot;
- seguridad;
- autenticación/RBAC;
- transacciones;
- concurrencia;
- datos históricos;
- búsquedas/rendimiento;
- UX/UI/accesibilidad;
- backups;
- logs/secrets;
- código duplicado/muerto;
- dependencias/vulnerabilidades;
- facturación/inventario/clientes/finanzas.

Validaciones mínimas:

- backend Release;
- unitarias;
- integración MySQL;
- historial de migraciones desde cero;
- upgrade desde esquema anterior representativo;
- SQL forward;
- EF snapshot;
- frontend lint/TypeScript/build;
- Playwright/E2E;
- permisos;
- seguridad;
- Docker cuando forme parte del baseline;
- GitHub Actions reales.

Informe final separará:

- AUTOMATIZADO Y COMPROBADO;
- VALIDACIÓN EXTERNA/FÍSICA PENDIENTE.

---

## Cierre del Plan Maestro M0–M13

El Plan Maestro de Mejoras Empresariales M0–M13 queda **COMPLETADO Y CERTIFICADO AUTOMÁTICAMENTE en `Desarrollo`** con M13.

Este cierre **no autoriza** merge del PR #2, cambios en `main`, auto-merge, despliegue productivo ni modificación de recursos de Producción. Las validaciones externas/físicas permanecen como proceso separado cuando correspondan.

No existe una fase M14 dentro del plan vigente; cualquier evolución posterior deberá abrir un nuevo plan o proceso formal de liberación/mantenimiento.

---

## Orden obligatorio de ejecución

`M0 ✅ -> M0.B ✅ -> M1 ✅ -> M2 ✅ -> M3 ✅ -> M4 ✅ -> M5 ✅ -> M6 ✅ -> M7 ✅ -> M8 ✅ -> M9 ✅ -> M10 ✅ -> M11 ✅ -> M12 ✅ -> M13 ✅`

### Gate crítico

`M1` es ahora prerequisito estructural obligatorio de `M2`.

No se seguirá ampliando el motor de variantes sobre `CatalogosProducto`, RBAC dual, tablas puente sin integridad completa, orígenes polimórficos ambiguos o precisiones monetarias inconsistentes.

### Política de compatibilidad

El saneamiento será evolutivo y no destructivo:

`preflight -> expandir -> backfill -> validar -> cambiar fuente de verdad -> regresión -> retirar legacy`.

No se tocará Producción durante este ciclo.