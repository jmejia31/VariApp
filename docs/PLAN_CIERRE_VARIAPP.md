# Plan obligatorio de trabajo por fases — VariApp / VariStorehn

Rama exclusiva de trabajo: `Desarrollo`.

Pull Request: `Desarrollo -> main`, en borrador hasta autorización expresa de Javier Mejía.

## Entornos oficiales

Solo existen dos entornos lógicos autorizados:

```text
varistorehn_producción (Producción)
varistorehn_desarrollo
```

Los nombres técnicos actuales de proyectos, servicios, dominios, usuarios y claves se conservan cuando renombrarlos o recrearlos pueda afectar funcionamiento. Cada recurso debe estar asignado documentalmente a uno de los dos entornos; un nombre técnico diferente no constituye un tercer entorno por sí solo.

## Reglas generales

Antes de iniciar cada fase se debe:

1. analizar el alcance completo;
2. identificar riesgos;
3. verificar dependencias;
4. confirmar que ningún cambio afecta Producción;
5. definir pruebas y evidencia de cierre.

No se avanza a la fase siguiente mientras la fase actual conserve un requisito pendiente.

Producción queda congelada durante todo el plan. No se modifican `main`, variables, credenciales, dominios, servicios, despliegues, bases, activos, claves, usuarios administrativos ni migraciones productivas.

## FASE 1 — Entornos y recursos — COMPLETA

### Resultado

Se estandarizaron los dos entornos oficiales sin modificar Producción:

| Plataforma | varistorehn_producción (Producción) | varistorehn_desarrollo |
|---|---|---|
| GitHub | rama `main`, solo lectura | rama única `Desarrollo` |
| Aiven | recursos productivos existentes; `avnadmin` se conserva | usuario de aplicación y base `varistorehn_desarrollo` |
| Cloudinary | claves, activos y variables productivas existentes | clave etiquetada `varistorehn_desarrollo` y prefijo `varistorehn_desarrollo/` |
| Render | entorno y servicio productivos existentes, sin cambios | entorno Desarrollo y servicio técnico existente `variapp-api-desarrollo` |
| Vercel | proyecto y dominio productivos existentes, sin cambios | proyecto técnico existente `variapp-desarrollo`, rama `Desarrollo` |

### Protecciones cerradas

- `Desarrollo` es la única rama de cambios.
- No se crean ramas adicionales.
- Todo commit se publica en `origin/Desarrollo`.
- `main` no se modifica ni se utiliza como rama de trabajo.
- Las variables de Producción y Desarrollo se mantienen.
- `avnadmin` se mantiene.
- Las claves `Raíz`, moderación y flujos de medios de Cloudinary se mantienen.
- No se elimina ningún recurso por su nombre.
- Solo se elimina un duplicado de Desarrollo después de demostrar que está sin uso, sin dependencias y con autorización expresa.
- `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo` está versionado y protegido por CI.
- Las migraciones automáticas permanecen deshabilitadas.

## FASE 2 — Auditoría general — COMPLETA Y CERTIFICADA

### Correcciones cerradas

- Rate limiting por IP para `POST /auth/login`.
- Validación temprana de secreto, issuer y audience JWT.
- `ForwardLimit=1` para encabezados del proxy.
- HSTS y encabezados defensivos para la API.
- Endpoints separados `/health` y `/health/ready`.
- Contenedor Docker ejecutado como usuario no privilegiado.
- `AllowedHosts` restringido al host de Render Desarrollo.
- Logo de Desarrollo servido desde Vercel Desarrollo, sin dependencia productiva.
- Resolución de la vulnerabilidad crítica transitiva `System.Text.Encodings.Web 4.5.0`.
- Auditoría npm productiva sin vulnerabilidades altas o críticas.

### Certificación

Commit funcional auditado: `20e5bbc917c02946433948355c5c20697b0fe259`.

- `Desarrollo - Compilación y pruebas`, run `30263028300`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30263028360`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30263028335`: **success**.

El detalle completo está en `docs/FASE2_AUDITORIA_GENERAL.md`.

### Riesgos residuales documentados

- JWT almacenado en `localStorage`; migrarlo a cookies HttpOnly requiere una fase de arquitectura y CSRF.
- Cloudinary puede compartir product environment; el aislamiento actual usa clave, prefijo y bloqueo de borrado.
- SMTP no tiene cola persistente ni reintento; se resolverá en Fase 7.
- No existe observabilidad centralizada externa; queda como recomendación futura.
- El proyecto Vercel productivo puede generar Preview de `Desarrollo`; desactivarlo exigiría modificar Producción y no se realizó.

## FASE 3 — Corrección de interfaz — COMPLETA Y CERTIFICADA

### Correcciones cerradas

- Altura dinámica para ayudas y errores de Angular Material.
- Textos multilínea sin superposición ni elipsis destructiva.
- Cuadrículas con `min-width: 0` y separación vertical suficiente.
- Jerarquía clara en Usuario y Producto.
- Galería accesible con teclado y dispositivos táctiles.
- Tabla de Productos con semántica nativa y desplazamiento propio.
- Tarjetas móviles con nombres, clasificación y acciones completas.
- Cabecera y rol adaptables a anchos reducidos.

### Certificación

Commit funcional final: `0bbc73f00bb8024e72a5837310456311d23f8740`.

- `Desarrollo - Compilación y pruebas`, run `30270049875`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30270049661`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30270049562`: **success**.

La aceptación integral ejecutó 27 pruebas sin fallos ni errores. El detalle está en `docs/FASE3_CERTIFICACION_INTERFAZ.md`.

## FASE 4 — Responsive — COMPLETA Y CERTIFICADA

### Matriz ejecutada

Se certificaron 320 × 568, 430 × 932, 768 × 1024, 1024 × 768, 1366 × 768, 1920 × 1080, 2560 × 1440 y 3840 × 2160.

Cada resolución navegó 30 rutas de módulos y formularios, para un total de 240 navegaciones responsive específicas.

### Defecto corregido

La tabla de Auditoría desbordaba 63 px en 1024 × 768. Se añadió un contenedor de desplazamiento horizontal accesible que conserva la semántica de tabla.

### Certificación

Commit funcional final: `898ada1d9e4c5c22353ba0fbed5589c52a0366de`.

- `Desarrollo - Compilación y pruebas`, run `30276203737`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30276203714`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30276203753`: **success**.

La aceptación integral ejecutó 43 pruebas sin fallos. El detalle está en `docs/FASE4_CERTIFICACION_RESPONSIVE.md`.

## FASE 5 — Imágenes — COMPLETA Y CERTIFICADA

### Alcance ejecutado

Se estandarizó la imagen principal y su fallback en Productos, Compras, Ventas y Movimientos de inventario.

### Correcciones cerradas

- Componente reutilizable `app-producto-imagen`.
- Fallback visible y accesible para imagen ausente o URL rota.
- Texto alternativo contextual.
- Dimensiones intrínsecas y relación de aspecto estable.
- Carga diferida para listas e historial.
- Carga prioritaria para la imagen principal del detalle.
- Galería y lightbox operables con teclado.
- Imagen principal propagada por los DTO de Compra, Venta y Movimiento.
- Miniaturas integradas sin alterar reglas de inventario, compra o venta.
- Vistas de escritorio y móvil adaptadas.

No se requirió migración de base de datos.

### Certificación

Commit funcional final: `90eb4ff4c9b7b4a8ed66561fa092f7521ebe7630`.

- `Desarrollo - Compilación y pruebas`, run `30289511599`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30289510773`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30289511930`: **success**.

```text
47 pruebas totales
47 aprobadas
0 inesperadas
0 inestables
0 omitidas
```

Artefacto `desarrollo-aceptacion-integral`, id `8662414676`, con diez capturas específicas.

El detalle completo está en `docs/FASE5_CERTIFICACION_IMAGENES.md`.

### Validaciones externas pendientes

La carga, sustitución y eliminación real de activos, el bloqueo frente a recursos productivos y las pruebas con cámara o conexión móvil lenta se realizarán únicamente en `varistorehn_desarrollo` durante la validación externa autorizada.

## FASE 6 — Facturación e impresión — COMPLETA Y CERTIFICADA

### Perfiles implementados

- A4 — 210 × 297 mm.
- Carta — 215.9 × 279.4 mm.
- Legal — 215.9 × 355.6 mm.
- Oficio — 215.9 × 330.2 mm.
- A5 — 148 × 210 mm.
- POS 58 mm — rollo continuo.
- POS 80 mm — rollo continuo.

### Correcciones cerradas

- Catálogo backend de perfiles y alias controlados.
- Generador QuestPDF específico para página fija y rollo térmico.
- A4 preservado como documento oficial para correo, WhatsApp y enlaces públicos.
- Endpoint `GET /facturas/formatos-pdf`.
- Descarga `GET /facturas/{id}/pdf?formato=...` con validación, nombre y auditoría.
- Selector de papel, dimensiones y uso recomendado en la interfaz.
- Preferencia local del formato seleccionado.
- Descarga e impresión del perfil elegido.
- Vista previa proporcional para A5, POS 58 y POS 80.
- Desplazamiento interno para papeles grandes en teléfono.
- Sin recalcular ni modificar el snapshot fiscal.
- Sin migración de base de datos.

### Certificación

Commit funcional final:

```text
14bf32069f9d87f731e59f230b9e9f5f16ade14e
```

- `Desarrollo - Compilación y pruebas`, run `30295557180`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30295557155`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30295557157`: **success**.

```text
51 pruebas totales
51 aprobadas
0 fallos
0 errores
0 omitidas
```

Artefacto final:

```text
desarrollo-aceptacion-integral
artifact id: 8664760725
7 PDFs físicos
8 capturas de interfaz
```

Los PDFs certifican firma, encabezados y `MediaBox` físico. Los siete fueron renderizados e inspeccionados sin recortes, superposiciones o pérdida visible de datos.

El detalle completo está en `docs/FASE6_CERTIFICACION_FACTURACION_IMPRESION.md`.

### Validaciones físicas pendientes

Solo sobre Desarrollo: impresoras reales de oficina y térmicas, drivers, USB/red/Bluetooth, márgenes no imprimibles, densidad, corte, avance y diálogos de impresión por navegador/dispositivo.

La Fase 6 no modificó Producción y no autoriza merge ni despliegue.

## FASE 7 — Envío de correo — SIGUIENTE, NO INICIADA

Problema confirmado en Desarrollo: intentos con resultado `Error` y mensaje `No se pudo enviar el correo`.

Se revisarán SMTP, variables de Render Desarrollo, autenticación, TLS, certificados, remitente, timeout, logs, errores sanitizados, plantillas, PDF adjunto, reintentos e idempotencia.

La fase solo se cierra con entrega real y verificación de bandeja de entrada y spam.

## FASE 8 — Validación completa — BLOQUEADA

Se repetirá la auditoría de interfaz, responsive, impresión, imágenes, correo, configuración, rendimiento, consola, logs, advertencias, seguridad y accesibilidad.

No debe quedar ningún defecto crítico o alto conocido.

## FASE 9 — Informe final — BLOQUEADA

El informe final contendrá:

1. cambios realizados;
2. problemas encontrados y solución aplicada;
3. riesgos identificados;
4. mejoras recomendadas no implementadas sin autorización.

## Regla de publicación

Completar estas fases no autoriza automáticamente el merge ni el despliegue productivo. Antes de cualquier operación sobre Producción se exige respaldo verificable, estrategia de migración única, ventana de mantenimiento, responsables, rollback y autorización expresa de Javier Mejía.
