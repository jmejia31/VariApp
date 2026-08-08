# Plan Maestro de Mejoras Empresariales — VariApp

Fecha: 2026-08-08
Rama exclusiva: `Desarrollo`
PR oficial: `#2 Desarrollo -> main` (debe permanecer abierto y en borrador)
Producción: congelada

## Principios de ejecución

1. No modificar `main`, no crear ramas nuevas, no fusionar PR #2 y no habilitar auto-merge.
2. No ejecutar migraciones, seeds, restauraciones, despliegues ni cambios de configuración sobre Producción.
3. Antes de implementar cada fase: auditar lo existente, identificar reutilización, dependencias, riesgos y pruebas.
4. No duplicar funcionalidades existentes. Si existe parcialmente, extenderla; si está correcta, conservarla.
5. Al cerrar cada fase: build backend/frontend, pruebas pertinentes, regresión, refactor del código nuevo, informe técnico y evidencia real.
6. Los estados técnicos que forman parte de máquinas de estado o invariantes del dominio no se convertirán en catálogos arbitrariamente editables. Para esos casos se mantendrá un código técnico estable y, cuando aplique, se administrarán metadatos de presentación (nombre, color, icono, orden, descripción) de forma segura.
7. Los gastos operativos financieros no se modelarán como productos. Los insumos físicos de consumo administrativo permanecen separados como inventario no vendible; alquiler, energía, internet, publicidad y similares permanecen en Finanzas.

## Estado base confirmado antes de este ciclo

- Fases 2C a 2G del ciclo funcional complementario están documentadas como cerradas en `Desarrollo`.
- Existen backend y frontend de escáner, variante técnica, autocomplete remoto, redondeo monetario, anulación conservadora de compras, seguridad de imágenes y logs seguros.
- `TipoCliente` ya existe y debe auditarse/extenderse, no recrearse.
- Este ciclo maestro es posterior y complementario; no invalida cierres previos.

---

# FASE M0 — Auditoría y mapa de impacto

Objetivo: obtener el inventario técnico real de las 16 áreas antes de modificar código.

Entregables:
- matriz `Existe / Parcial / No existe / Requiere refactor`;
- dependencias por módulo, entidad, endpoint, componente, migración, permiso y prueba;
- deuda técnica priorizada P0/P1/P2/P3;
- mapa de listas desplegables y clasificación entre catálogo configurable vs enum técnico;
- mapa de filtros actuales y navegación;
- mapa de variantes, tallas, colores e imágenes;
- mapa de ISV/ISC y su persistencia;
- mapa de backups y proveedores externos;
- baseline de CI y pruebas.

No se implementan cambios funcionales durante M0 salvo correcciones bloqueantes de compilación o seguridad necesarias para poder auditar.

# FASE M1 — Catálogos maestros y metadatos administrables

Objetivo: eliminar listas de negocio codificadas donde sea técnicamente correcto y crear una infraestructura reutilizable de catálogos.

Incluye:
- métodos de pago;
- tipos de envío;
- prioridades;
- etiquetas;
- categorías/subcategorías cuando corresponda;
- otros catálogos detectados en M0.

Regla crítica:
- estados de máquina (`Borrador`, `Confirmado`, `Anulado`, tipos de movimiento, etc.) mantienen código técnico estable si participan en lógica, FKs, permisos o transiciones;
- su presentación puede ser administrable sin permitir romper la máquina de estados.

Cada catálogo tendrá CRUD, activar/desactivar, orden, color, icono, descripción, auditoría y validación de uso antes de eliminar.

# FASE M2 — Variantes 2.0: tallas/tamaños + imágenes por variante

Objetivo: generalizar variantes sin duplicar la arquitectura actual de colores.

Incluye:
- múltiples tallas/tamaños por producto;
- alta, edición, activación, desactivación y eliminación controlada;
- UX dinámica equivalente a colores;
- combinación de atributos cuando aplique (`Color + Talla`), evitando crear dos sistemas de variantes paralelos;
- SKU/código de barras/stock/costo/precio por variante;
- imágenes independientes por variante;
- múltiples imágenes, principal, orden, vista previa, reemplazo y eliminación;
- drag & drop donde sea accesible y mantenible;
- integración transversal en productos, inventario, compras, ventas, facturación, cargas masivas, escáner y autocomplete;
- migración/backfill no destructivo y compatibilidad con variante técnica.

# FASE M3 — Persistencia de configuración ISV/ISC y preferencias

Objetivo: eliminar cualquier reinicio de configuración por sesión o frontend.

Incluye:
- localizar la fuente actual del estado ISV/ISC;
- persistencia en base de datos;
- API y permisos de configuración;
- restauración del estado al iniciar sesión;
- idempotencia de seeds: nunca reactivar una configuración que el usuario desactivó;
- auditoría del cambio;
- pruebas de cierre/inicio de sesión y reinicio de API.

# FASE M4 — Estado de navegación y filtros persistentes

Objetivo: conservar filtros hasta que el usuario pulse `Limpiar filtros`.

Incluye:
- servicio reutilizable de estado por vista;
- búsqueda, filtros, página, pageSize y orden;
- retorno desde detalle/edición sin perder estado;
- botón `Limpiar filtros`;
- política de expiración y aislamiento por usuario cuando sea necesario;
- aplicación inicial en Productos, Ventas, Compras, Clientes, Inventario y Gastos/Finanzas y posterior extensión a todas las listas detectadas.

Preferencia técnica: query params para estado navegable/compartible + almacenamiento de sesión para valores auxiliares, evitando estado global opaco.

# FASE M5 — Clientes y segmentación administrativa

Objetivo: completar el mantenimiento existente de `TipoCliente` y convertirlo en base de segmentación.

Incluye:
- auditoría del CRUD ya existente;
- filtros y estadísticas por tipo;
- integración en reportes y exportaciones;
- puntos de extensión para campañas futuras;
- tipos adicionales creados por administración, no hardcodeados innecesariamente.

`SIN_CLASIFICAR` continúa como fallback técnico protegido. Etiquetas subjetivas como `Enojado`, `Moroso` o `Recuperado` serán datos administrables del negocio, no invariantes de código.

# FASE M6 — Separación inventario comercial / insumos administrativos / gastos financieros

Objetivo: completar la separación conceptual y de reportes.

Modelo:
- Mercadería de venta: inventario vendible.
- Insumo administrativo: inventario físico no vendible, con stock y consumo interno.
- Gasto operativo: transacción financiera sin stock, administrada en Finanzas.

Incluye:
- vistas separadas;
- permisos;
- búsquedas y reportes separados;
- valoración diferenciada;
- bloqueo backend de venta de insumos;
- consumo interno sin doble egreso financiero.

# FASE M7 — Costos de envío profesionales

Objetivo: extender el mantenimiento existente de costos de envío, no duplicarlo.

Incluye, según auditoría:
- zona;
- ciudad;
- departamento;
- tipo de envío;
- estado;
- precio;
- prioridad;
- vigencia;
- predeterminado único;
- historial y auditoría;
- selección obligatoria desde catálogo en ventas;
- eliminación/desactivación segura con históricos preservados;
- snapshots de factura/venta.

# FASE M8 — Búsqueda inteligente y rendimiento operacional

Objetivo: ampliar el autocomplete remoto existente y las búsquedas de Ventas/Compras.

Incluye:
- código/SKU/código de barras;
- nombre/descripción;
- categoría/marca/modelo;
- cliente/proveedor;
- teléfono/correo;
- observaciones y otros campos útiles;
- índices DB medidos, no añadidos a ciegas;
- DTOs ligeros, paginación y cancelación;
- p50/p95 en desarrollo;
- protección contra consultas demasiado amplias.

# FASE M9 — Cargas masivas profesionales

Objetivo: elevar UX, validación, accesibilidad, rendimiento y trazabilidad.

Incluye:
- preview/validación previa;
- progreso real por etapas;
- resumen de correctos/errores/omitidos;
- descarga de errores;
- códigos de error consistentes;
- cancelación segura cuando la arquitectura lo permita;
- procesamiento por lotes;
- plantillas versionadas;
- compatibilidad con talla, color e imágenes/variantes cuando corresponda;
- auditoría y límites de archivo.

# FASE M10 — Configuración visual empresarial y estandarización UI

Objetivo: separar tokens visuales y unificar todas las interfaces.

Incluye tokens independientes para:
- iconos;
- texto;
- botones;
- tarjetas;
- encabezados.

Se implementará mediante variables/tokens de tema reutilizables, evitando estilos inline y duplicación. Incluye responsive, contraste WCAG, navegación por teclado, estados focus/hover/disabled, feedback de carga y consistencia de componentes.

# FASE M11 — Backups y restauración profesional en Desarrollo

Objetivo: diseñar e implementar un sistema seguro sin tocar Producción.

Incluye:
- inventario de activos respaldables: MySQL, documentos, configuración y referencias/activos del almacenamiento de imágenes;
- backups manuales y programables;
- política de retención;
- checksum/integridad;
- cifrado y control de acceso;
- metadata de tamaño, fecha, usuario, estado y observaciones;
- puntos de restauración;
- restauración ensayada únicamente sobre infraestructura descartable/desarrollo durante este ciclo;
- logs y auditoría;
- estrategia compatible con proveedores externos sin asumir que un único ZIP puede restaurar recursos que el proveedor administra por separado.

Producción requerirá un procedimiento y autorización independientes.

# FASE M12 — Automatización transversal

Objetivo: reducir pasos repetitivos sin perder control administrativo.

Se revisarán productos, compras, ventas, inventario, clientes, facturación, finanzas, cargas y configuración para detectar:
- autocompletado;
- valores por defecto administrables;
- recordatorio de preferencias;
- cálculos automáticos;
- sugerencias;
- acciones masivas seguras;
- reducción de capturas duplicadas.

Toda automatización debe ser determinista/auditable cuando impacte inventario, facturación o finanzas.

# FASE M13 — Auditoría integral, hardening y certificación final

Objetivo: cerrar el ciclo con evidencia técnica.

Informe final:
- problemas encontrados;
- riesgos;
- código duplicado;
- rendimiento;
- UX/UI;
- automatizaciones;
- arquitectura;
- deuda técnica;
- vulnerabilidades;
- obsolescencia;
- prioridad, impacto y solución.

Validaciones mínimas de cierre por fase y final:
- backend build Release;
- pruebas unitarias;
- pruebas de integración MySQL cuando aplique;
- frontend lint/TypeScript;
- frontend production build;
- E2E sobre flujos modificados;
- migraciones sobre base descartable;
- verificación de snapshot EF;
- auditoría de secretos/logs;
- regresión de permisos;
- evidencia CI real antes de declarar una fase cerrada.

## Orden de ejecución

`M0 -> M1 -> M2 -> M3 -> M4 -> M5 -> M6 -> M7 -> M8 -> M9 -> M10 -> M11 -> M12 -> M13`

No se inicia una fase funcional posterior si una dependencia estructural anterior deja regresiones bloqueantes.
