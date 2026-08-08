# Plan Maestro de Mejoras Empresariales — VariApp

Versión: 2.0 — Variantes multidimensionales
Fecha: 2026-08-08
Rama exclusiva: `Desarrollo`
PR oficial: `#2 Desarrollo -> main` (debe permanecer abierto y en borrador)
Producción: congelada

## 1. Principios de ejecución

1. No modificar `main`, no crear ramas nuevas, no fusionar PR #2 y no habilitar auto-merge.
2. No ejecutar migraciones, seeds, restauraciones, despliegues ni cambios de configuración sobre Producción.
3. Antes de implementar cada fase: auditar lo existente, identificar reutilización, dependencias, riesgos y pruebas.
4. No duplicar funcionalidades existentes. Si existe parcialmente, extenderla; si está correcta, conservarla.
5. Al cerrar cada fase: build backend/frontend, pruebas pertinentes, regresión, refactor del código nuevo, informe técnico y evidencia real.
6. Los estados técnicos que forman parte de máquinas de estado o invariantes del dominio no se convertirán en catálogos arbitrariamente editables.
7. Los gastos operativos financieros no se modelarán como productos. Los insumos físicos de consumo administrativo permanecen separados como inventario no vendible; alquiler, energía, internet, publicidad y similares permanecen en Finanzas.
8. Una variante de inventario representa una combinación física exacta. Toda compra, venta, ajuste, movimiento, factura y carga masiva deberá operar contra la variante exacta cuando el producto utilice variantes.
9. Ninguna evolución de variantes puede reescribir históricos. Las operaciones ya confirmadas conservarán snapshots y referencias existentes.
10. Se priorizarán restricciones de integridad a nivel de base de datos para reglas que no deben depender únicamente del frontend o de una carrera entre solicitudes.

## 2. Estado base confirmado

- M0 quedó cerrada en `docs/FASE_M0_AUDITORIA_MAPA_IMPACTO.md`.
- Fases 2C a 2G del ciclo funcional complementario permanecen como baseline y no se reconstruyen.
- Existen escáner, variante técnica, autocomplete remoto, redondeo monetario, anulación conservadora de compras, seguridad de imágenes, logs seguros y aislamiento de entornos.
- `CatalogoProducto` ya administra Color, Talla, Marca y Modelo.
- Modelo ya depende de Marca mediante `CatalogoPadreId`.
- `ProductoVariante` existe, pero actualmente solo almacena `ColorId` como dimensión comercial.
- `Producto` mantiene `TallaId`, `MarcaId` y `ModeloId` a nivel global.
- Compras, ventas, movimientos y facturas ya referencian `ProductoVarianteId`, pero sus snapshots de variante están centrados principalmente en Color/SKU.
- Cargas masivas y escáner ya entienden variantes, por lo que deben evolucionarse, no sustituirse.

## 3. Aclaración funcional incorporada después de M0

El modelo objetivo queda definido así:

### Producto

Es la familia o concepto comercial principal. Ejemplos: `Cobertor SPACE`, `Cargador`, `Cable USB-C`, `Camiseta deportiva`.

### Variante

Es la unidad inventariable exacta. Puede combinar:

- Marca;
- Modelo;
- Color;
- Talla/Tamaño;
- SKU;
- Código de barras;
- costo;
- precio;
- stock;
- umbral de stock bajo;
- imágenes propias;
- estado activo/inactivo.

Ejemplos válidos dentro del mismo Producto:

- Samsung + S24 Ultra + Negro + Sin talla;
- Samsung + S24 Ultra + Azul + Sin talla;
- Samsung + S23 Ultra + Negro + Sin talla;
- Apple + iPhone 16 Pro Max + Transparente + Sin talla;
- Marca X + Modelo Y + Negro + M;
- Marca X + Modelo Y + Negro + L.

Por tanto, la cantidad deja de interpretarse como `cantidad por color` y pasa a ser `cantidad por combinación exacta de variante`.

## 4. Regla de identidad de variante

La identidad comercial se modelará sobre:

`Producto + Marca + Modelo + Color + Talla`

Cada dimensión podrá ser opcional cuando el tipo de producto no la utilice, pero no se permitirá registrar dos variantes activas/no eliminadas con la misma combinación normalizada.

Reglas:

- si existe Modelo, debe existir Marca;
- Modelo debe pertenecer a Marca;
- una variante no puede duplicar una combinación ya existente;
- SKU es único globalmente;
- código de barras, cuando exista, es único globalmente;
- la variante técnica seguirá siendo una representación interna y no competirá con las variantes comerciales;
- una variante con stock no se elimina físicamente: se desactiva o se lleva a cero antes del soft delete;
- históricos conservan referencias y snapshots incluso si catálogos o variantes cambian después.

La unicidad multidimensional se reforzará en MySQL mediante clave generada/normalizada o mecanismo equivalente que trate los `NULL` de forma determinista y permita soft delete sin colisiones.

## 5. Fuente de verdad y compatibilidad

Después de M2, la fuente de verdad del inventario será `ProductoVariante`.

Los campos históricos/globales de `Producto` (`ColorId`, `TallaId`, `MarcaId`, `ModeloId`, `Cantidad`, `Costo`, `Precio`) no se eliminarán de forma destructiva durante la transición. Se mantendrán como compatibilidad y/o resumen derivado:

- si todas las variantes comparten una Marca, `Producto.MarcaId` puede conservarla; si hay varias, será resumen múltiple/null según contrato final;
- mismo criterio para Modelo, Color y Talla;
- `Producto.Cantidad` será la suma física de variantes no eliminadas;
- costo consolidado seguirá una regla explícita de valoración;
- precio consolidado será informativo; la operación utiliza el precio de la variante seleccionada.

## 6. Etiqueta canónica de variante

Se creará una única regla reutilizable para mostrar una variante en toda la aplicación, evitando que cada pantalla concatene atributos por su cuenta.

Formato conceptual:

`Marca · Modelo · Color · Talla · SKU`

Solo se muestran dimensiones presentes. La misma etiqueta deberá utilizarse en:

- Producto;
- detalle de producto;
- administrador de variantes;
- Compras;
- Ventas;
- Inventario;
- Facturación;
- PDF/impresión;
- correo/WhatsApp de factura cuando incluya detalle;
- escáner;
- autocomplete;
- cargas masivas;
- reportes;
- auditoría y mensajes operativos.

---

# FASE M0 — Auditoría y mapa de impacto

Estado: COMPLETADA.

Entregable principal: `docs/FASE_M0_AUDITORIA_MAPA_IMPACTO.md`.

La aclaración de variantes multidimensionales de esta versión actúa como addendum funcional a M0 y reemplaza cualquier interpretación anterior de `variante = color`.

# FASE M1 — Catálogos maestros y metadatos administrables

Objetivo: consolidar los catálogos necesarios antes de convertir Marca/Modelo/Color/Talla en dimensiones de inventario.

## M1.1 Color, Talla, Marca y Modelo

No se recrean. Se conserva `CatalogoProducto`.

Se certificará:

- CRUD;
- activar/desactivar;
- soft delete;
- orden;
- descripción;
- código visual cuando aplique;
- auditoría;
- permisos;
- Marca -> múltiples Modelos;
- bloqueo de Modelo cuando su Marca esté inactiva;
- validación de uso antes de eliminar/desactivar cuando pueda romper nuevas operaciones.

## M1.2 Marca y Modelo

Modelo continuará dependiendo de Marca. Se reforzará que:

- una Marca puede tener muchos Modelos;
- un mismo Producto puede tener variantes de uno o varios Modelos;
- un mismo Modelo puede tener varios Colores/Tallas;
- las cantidades se almacenan en la combinación de variante, no en el catálogo Modelo;
- renombrar Marca/Modelo no reescribe documentos históricos confirmados.

## M1.3 Otros catálogos

Evaluar y completar:

- métodos de pago;
- tipos/modalidades de envío;
- prioridades;
- etiquetas administrativas;
- categorías/subcategorías cuando aporten valor;
- metadatos de estados técnicos sin convertir su código en editable.

Métodos de pago podrán evolucionar desde enum a configuración administrable únicamente con compatibilidad histórica y código estable.

# FASE M2 — Motor de variantes multidimensionales 2.0

Objetivo: convertir la arquitectura existente en un motor de inventario por combinación exacta Marca + Modelo + Color + Talla, preservando datos, referencias y flujos existentes.

M2 es estructural y se divide en subfases obligatorias.

## M2.A — Dominio, entidades e integridad MySQL

Modificar `ProductoVariante` para incorporar:

- `MarcaId` nullable;
- `ModeloId` nullable;
- `ColorId` nullable;
- `TallaId` nullable;
- navegación a los cuatro catálogos;
- SKU;
- código de barras;
- stock;
- umbral;
- costo;
- precio;
- estado;
- variante técnica.

Agregar índices de búsqueda apropiados después de medir consultas.

Crear una garantía de unicidad comercial que impida duplicar una combinación activa/no eliminada, normalizando valores nulos y respetando soft delete.

Mantener por separado la restricción única de variante técnica por producto.

## M2.B — Migración y backfill no destructivo

Antes de migrar:

- preflight de duplicados;
- detectar referencias inválidas;
- detectar modelos cuyo padre no coincida con la marca;
- detectar variantes huérfanas;
- detectar SKU/códigos de barras duplicados;
- abortar de forma fail-closed ante inconsistencias.

Backfill:

- `ColorId` se conserva desde la variante actual;
- Marca/Modelo/Talla de variantes existentes se heredan inicialmente desde Producto;
- IDs de `ProductoVariante` no cambian;
- referencias de Compra/Venta/Movimiento/Factura no cambian;
- no se toca Producción;
- migración y rollback se validan en MySQL descartable.

## M2.C — Contratos DTO/API y reglas de negocio

Actualizar:

- DTO de variante;
- create/update;
- producto DTO;
- scanner DTO;
- autocomplete DTO;
- requests de Compras/Ventas;
- movimientos de inventario;
- cargas masivas;
- reportes.

Reglas:

- Modelo exige Marca;
- Modelo pertenece a Marca;
- catálogos usados en nuevas operaciones deben estar activos;
- stock nunca negativo;
- costo no negativo;
- precio mayor que cero para mercadería vendible;
- combinación no duplicada;
- cambios de stock solo mediante operaciones autorizadas.

## M2.D — SKU y código de barras

Corregir la inconsistencia actual entre UI y backend.

Contrato final:

- SKU puede ingresarse manualmente;
- si se omite, backend genera uno único y estable;
- frontend nunca será responsable de garantizar unicidad;
- código de barras es opcional y único cuando exista;
- escáner resuelve siempre a una variante exacta;
- conflictos retornan error explícito y auditable.

## M2.E — Formulario de Producto / Constructor de variantes

La sección actual `Colores y existencias` se convertirá en `Variantes y existencias`.

Cada fila permitirá definir, según aplique:

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

Mejoras UX:

- botón `Agregar variante` en lugar de `Agregar otro color`;
- copiar datos de la variante anterior para reducir captura;
- impedir combinaciones duplicadas antes de enviar;
- mostrar etiqueta canónica en tiempo real;
- resumen de variantes y stock total;
- resumen por Marca, Modelo, Color y Talla cuando sea útil;
- errores de combinación visibles en la fila correspondiente;
- selección Marca -> Modelo dependiente;
- no obligar a inventar una talla/color cuando el producto no utiliza esa dimensión;
- responsive y navegación por teclado.

## M2.F — Administración posterior de variantes

La pantalla `Administrar variantes` permitirá:

- crear;
- editar atributos sin alterar stock;
- activar/desactivar;
- ajustar stock mediante flujo dedicado;
- eliminar lógicamente solo con stock cero;
- consultar historial;
- buscar/filtrar por Marca, Modelo, Color, Talla, SKU, barcode y estado;
- ordenar;
- detectar agotado/stock bajo;
- visualizar el impacto en stock consolidado.

## M2.G — Imágenes generales e imágenes por variante

Extender `ProductoImagen` con asociación nullable a `ProductoVariante` o estructura equivalente compatible.

Reglas:

- imágenes generales del Producto continúan válidas;
- una variante puede tener múltiples imágenes;
- principal por producto y principal por variante;
- orden;
- preview;
- reemplazo;
- eliminación segura en almacenamiento;
- fallback: variante sin imagen usa imagen principal del Producto;
- al vender/comprar/listar se prioriza miniatura de la variante;
- no se eliminan activos históricos por cambiar una variante.

La integridad de `principal` por ámbito se reforzará donde sea técnicamente viable.

## M2.H — Compras por variante exacta

Compras deberá buscar y seleccionar una variante mostrando al menos:

`Producto · Marca · Modelo · Color · Talla · SKU`.

Al confirmar:

- aumenta stock exclusivamente de esa variante;
- actualiza costo de esa variante según la política vigente;
- consolida Producto sin perder detalle;
- movimientos inventario referencian `ProductoVarianteId`;
- snapshots capturan Marca/Modelo/Color/Talla/SKU reales de la variante;
- editar/anular una compra revierte exactamente la variante afectada;
- concurrencia usa lock sobre la fila correcta.

## M2.I — Ventas por variante exacta

Ventas deberá seleccionar la combinación exacta y mostrar stock de esa combinación.

Al confirmar:

- descuenta únicamente la variante elegida;
- nunca usa stock de otro color/talla/modelo para completar una venta;
- valida stock bajo lock/concurrencia;
- usa precio/costo de la variante;
- calcula utilidad sobre la variante real;
- snapshots capturan Marca/Modelo/Color/Talla/SKU;
- anulación devuelve stock exactamente a la variante original.

## M2.J — Facturación, impresión y compartición

`FacturaDetalle` conservará el atributo exacto vendido.

Agregar/preservar snapshots de:

- Marca;
- Modelo;
- Color;
- Talla;
- SKU/código cuando corresponda.

Actualizar:

- vista de factura;
- PDF;
- impresión oficina/POS;
- correo;
- WhatsApp/compartición;
- duplicación cuando exista.

Una factura histórica debe verse igual aunque después se renombre un catálogo o una variante.

## M2.K — Inventario y movimientos

`MovimientoInventario` ampliará snapshots de variante para Marca/Modelo/Talla además de Color/SKU.

Se cubrirá:

- compras;
- ventas;
- anulaciones;
- ajustes;
- consumos administrativos cuando aplique;
- reversión;
- conteo físico futuro.

Toda modificación de stock debe dejar:

- variante exacta;
- stock anterior;
- stock nuevo;
- diferencia;
- causa;
- referencia;
- usuario;
- fecha;
- snapshots legibles.

## M2.L — Detalle, listas y filtros de Producto

La vista actual `Inventario por color y SKU` se convertirá en `Inventario por variante`.

Mostrar:

- Marca;
- Modelo;
- Color;
- Talla;
- SKU;
- stock;
- costo;
- precio;
- estado;
- imagen de variante.

Agregar filtros por:

- Marca;
- Modelo dependiente;
- Color;
- Talla;
- estado;
- agotado/stock bajo;
- SKU/barcode.

Producto mostrará valores agregados sin ocultar la distribución real.

## M2.M — Escáner, autocomplete y búsqueda

Todos los resultados deberán incluir dimensiones completas.

Búsqueda por:

- SKU;
- barcode;
- producto;
- descripción;
- Marca;
- Modelo;
- Color;
- Talla;
- categoría.

La etiqueta canónica evita opciones ambiguas como dos `Azul` pertenecientes a modelos diferentes.

## M2.N — Cargas masivas multidimensionales

Evolucionar `VariantesInventario` para incluir:

- Producto;
- Marca;
- Modelo;
- Color;
- Talla;
- SKU;
- barcode;
- cantidad;
- costo;
- precio;
- umbral;
- estado cuando aplique.

La validación detectará:

- combinación duplicada en archivo;
- combinación ya existente;
- Marca/Modelo inválidos;
- Modelo de otra Marca;
- catálogos inactivos;
- SKU/barcode duplicado;
- cantidades/precios inválidos.

Preview y confirmación seguirán transaccionales.

## M2.O — Reportes, Dashboard y valoración

Actualizar métricas para que no pierdan la granularidad:

- stock por Marca;
- stock por Modelo;
- stock por Color;
- stock por Talla;
- stock por combinación;
- rotación;
- agotados;
- stock bajo;
- valoración al costo;
- potencial de venta;
- utilidad por variante/modelo cuando exista información.

Evitar doble conteo entre Producto consolidado y sus Variantes.

## M2.P — Auditoría y permisos

Auditar:

- creación de variante;
- edición de atributos;
- cambio de estado;
- eliminación lógica;
- ajustes de stock;
- cambios de precio/costo;
- imagen principal;
- operaciones por carga masiva.

Los permisos existentes de Productos/Inventario se reutilizarán salvo que M0/M2 demuestre necesidad real de una acción adicional.

## M2.Q — Compatibilidad y regresión

Pruebas obligatorias:

- producto simple/variante técnica;
- producto de un solo color;
- múltiples colores;
- múltiples tallas;
- múltiples modelos de una Marca;
- múltiples Marcas cuando el producto lo permita;
- combinaciones Color + Talla + Modelo;
- compra;
- venta;
- anulación;
- factura/PDF;
- escáner;
- autocomplete;
- carga masiva;
- ajuste stock;
- imágenes;
- búsqueda/filtros;
- concurrencia;
- soft delete;
- históricos.

No se cierra M2 con una prueba solamente visual.

# FASE M3 — Persistencia y certificación ISV/ISC

M0 confirmó que `Impuesto` y `SeedFiscalService` ya cubren la persistencia principal. M3 se convierte en fase de certificación/regresión:

- persistencia tras login/logout;
- persistencia tras reinicio API;
- seeds idempotentes;
- no reactivar impuestos desactivados;
- snapshots históricos;
- integración correcta con variantes y sus precios;
- permisos/auditoría.

Solo se modificará código si la certificación detecta una brecha real.

# FASE M4 — Estado de navegación y filtros persistentes

Objetivo: conservar filtros hasta que el usuario pulse `Limpiar filtros`.

Incluye:

- servicio reutilizable de estado por vista;
- búsqueda, filtros, página, pageSize y orden;
- query params como estado navegable;
- sessionStorage para auxiliares;
- retorno desde detalle/edición sin perder estado;
- aislamiento por usuario cuando aplique;
- Productos, Variantes, Ventas, Compras, Clientes, Inventario, Finanzas y demás listas.

Los nuevos filtros Marca/Modelo/Color/Talla de M2 participan en esta persistencia.

# FASE M5 — Clientes y segmentación administrativa

Objetivo: completar `TipoCliente` y segmentación.

Incluye:

- filtros por tipo;
- KPIs;
- total vendido/compras si aplica;
- frecuencia;
- última compra;
- reportes/exportación;
- integración Dashboard;
- tipos administrables;
- fallback protegido `SIN_CLASIFICAR`.

No convertir el módulo en CRM fuera de alcance.

# FASE M6 — Inventario comercial / insumos administrativos / gastos

Objetivo: completar la separación ya existente.

- Mercadería de venta: inventario vendible con variantes multidimensionales.
- Insumo administrativo: inventario físico no vendible; puede usar variantes solo si aporta valor real.
- Gasto operativo: Finanzas sin stock.

Incluye:

- vistas;
- permisos;
- consumos;
- búsqueda;
- valoración separada;
- reportes;
- bloqueo backend;
- E2E.

# FASE M7 — Costos de envío profesionales

Objetivo: extender `CostoEnvio` sin duplicarlo.

Incluye:

- zona;
- ciudad;
- departamento;
- tipo/modalidad de envío;
- precio;
- prioridad;
- vigencia;
- activo;
- predeterminado único;
- historial;
- auditoría;
- selección desde venta;
- exoneración controlada;
- snapshots.

Obligatorio: reforzar en MySQL la unicidad/integridad del predeterminado frente a concurrencia.

# FASE M8 — Búsqueda inteligente y rendimiento operacional

Objetivo: ampliar búsquedas y medirlas.

Incluye:

- atributos de variante completos;
- cliente/proveedor;
- teléfono/correo;
- observaciones útiles;
- índices solo con evidencia;
- DTOs ligeros;
- paginación;
- cancelación de requests;
- medición p50/p95;
- límites contra consultas amplias;
- pruebas con volumen representativo en Desarrollo.

# FASE M9 — Cargas masivas profesionales

Objetivo: elevar el módulo ya existente.

Incluye:

- preview;
- validación;
- progreso real por etapa cuando se procese en lotes;
- correctos/errores/omitidos;
- descarga errores;
- códigos consistentes;
- lotes;
- plantillas versionadas;
- cancelación segura cuando sea viable;
- dimensiones completas de M2;
- auditoría;
- límites y seguridad de archivo.

# FASE M10 — UI empresarial y accesibilidad

Objetivo: normalizar visualmente la aplicación.

Incluye tokens para:

- iconos;
- texto;
- botones;
- tarjetas;
- encabezados;
- formularios;
- chips/estados;
- tablas.

Cierre:

- responsive;
- contraste WCAG;
- teclado;
- focus visible;
- hover/disabled;
- loading/empty/error;
- encabezados con contraste correcto;
- formularios de variantes legibles aun con muchas combinaciones;
- consistencia móvil/tablet/escritorio.

# FASE M11 — Backups y restauración profesional en Desarrollo

Objetivo: implementar respaldo/restauración sin tocar Producción.

Incluye:

- MySQL;
- metadata/configuración;
- documentos;
- inventario de referencias/activos Cloudinary;
- backup manual/programable;
- retención;
- checksum;
- cifrado;
- permisos;
- estados;
- puntos de restauración;
- restauración ensayada en entorno descartable;
- auditoría.

Producción requerirá autorización independiente.

# FASE M12 — Automatización transversal

Objetivo: reducir fricción sin perder trazabilidad.

Incluye:

- SKU automático cuando se omita;
- valores por defecto administrables;
- copia inteligente de atributos al crear otra variante;
- autocompletado Marca -> Modelo;
- selección de imagen fallback;
- cálculos automáticos;
- consolidación de stock;
- recordatorio de preferencias;
- acciones masivas seguras;
- sugerencias no destructivas.

Toda automatización que impacte inventario/facturación/finanzas será determinista y auditable.

# FASE M13 — Auditoría integral, hardening y certificación final

Objetivo: cerrar con evidencia real.

Informe final:

- problemas;
- riesgos;
- duplicación;
- rendimiento;
- UX/UI;
- accesibilidad;
- arquitectura;
- deuda técnica;
- vulnerabilidades;
- obsolescencia;
- integridad de variantes;
- consistencia Producto -> Compra -> Inventario -> Venta -> Factura;
- prioridad/impacto/solución.

Validaciones mínimas:

- backend build Release;
- unit tests;
- integración MySQL;
- frontend lint/TypeScript;
- production build;
- E2E;
- migraciones sobre base descartable;
- snapshot EF;
- preflight y rollback cuando aplique;
- secretos/logs;
- permisos;
- pruebas de concurrencia;
- facturación/PDF;
- cargas masivas;
- evidencia CI real.

## 7. Mejoras adicionales incorporadas en V2

Estas mejoras no estaban suficientemente explícitas en V1 y pasan a ser obligatorias:

1. Snapshot histórico de Talla en Compra, Venta, Movimiento y Factura.
2. Snapshot de Marca/Modelo desde la variante real, no asumir siempre el Producto global.
3. Etiqueta canónica de variante reutilizable en todo el sistema.
4. Garantía única multidimensional a nivel MySQL.
5. Imágenes por variante con fallback a imagen general.
6. Corrección contrato SKU opcional/auto-generado.
7. Filtros y reportes por combinación, evitando doble conteo.
8. Resumen de stock por Marca/Modelo/Color/Talla.
9. Precio, costo y umbral a nivel variante como fuente operativa.
10. Validación Marca -> Modelo en backend y frontend.
11. Migración fail-closed con preflight de integridad.
12. Regresión explícita de variante técnica/producto simple.
13. Anulación de compra/venta siempre sobre la variante exacta.
14. PDF, impresión, correo y WhatsApp con información inequívoca de variante.
15. Búsqueda/autocomplete/escáner sin resultados ambiguos.
16. Accesibilidad y responsive del constructor de variantes.
17. Prevención de combinaciones duplicadas tanto en UI como backend/BD.
18. Auditoría de cambios de atributos, precio/costo, stock e imágenes.

## 8. Orden de ejecución

`M0 [COMPLETADA] -> M1 -> M2.A -> M2.B -> M2.C -> M2.D -> M2.E -> M2.F -> M2.G -> M2.H -> M2.I -> M2.J -> M2.K -> M2.L -> M2.M -> M2.N -> M2.O -> M2.P -> M2.Q -> M3 -> M4 -> M5 -> M6 -> M7 -> M8 -> M9 -> M10 -> M11 -> M12 -> M13`

No se inicia una subfase estructural posterior si la anterior deja una regresión bloqueante.

## 9. Criterio de cierre por fase

Cada fase/subfase se considera terminada únicamente si:

1. código publicado en `Desarrollo`;
2. build backend correcto;
3. pruebas backend pertinentes verdes;
4. lint/build frontend correctos;
5. E2E del flujo modificado cuando aplique;
6. migraciones probadas en MySQL descartable cuando aplique;
7. snapshot EF consistente;
8. permisos/auditoría revisados;
9. no se exponen secretos;
10. CI real revisado;
11. documento de cierre actualizado;
12. PR #2 permanece Draft, abierto y sin merge;
13. `main` permanece congelada;
14. Producción no fue modificada.
