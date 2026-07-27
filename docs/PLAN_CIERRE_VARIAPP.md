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

Se estandarizaron los entornos `varistorehn_producción` y `varistorehn_desarrollo`. `main` permanece de solo lectura, `Desarrollo` es la única rama de cambios, `avnadmin` se conserva, Cloudinary usa prefijo de Desarrollo y las migraciones productivas permanecen deshabilitadas.

## FASE 2 — Auditoría general — COMPLETA Y CERTIFICADA

Se cerraron rate limiting, validación JWT, proxy, HSTS, health/readiness, ejecución Docker no privilegiada, AllowedHosts, aislamiento del logo, vulnerabilidades .NET y auditoría npm.

Commit: `20e5bbc917c02946433948355c5c20697b0fe259`.

- Compilación `30263028300`: **success**.
- Aceptación `30263028360`: **success**.
- Auditoría `30263028335`: **success**.

Detalle: `docs/FASE2_AUDITORIA_GENERAL.md`.

## FASE 3 — Corrección de interfaz — COMPLETA Y CERTIFICADA

Se corrigieron ayudas, errores, textos extensos, jerarquía visual, formularios, galería, tabla de Productos, tarjetas móviles, cabecera y rol.

Commit: `0bbc73f00bb8024e72a5837310456311d23f8740`.

- Compilación `30270049875`: **success**.
- Aceptación `30270049661`: **success**.
- Auditoría `30270049562`: **success**.

27 pruebas aprobadas. Detalle: `docs/FASE3_CERTIFICACION_INTERFAZ.md`.

## FASE 4 — Responsive — COMPLETA Y CERTIFICADA

Se certificaron ocho resoluciones entre 320 × 568 y 3840 × 2160, con 240 navegaciones específicas. Se corrigió el desbordamiento de Auditoría.

Commit: `898ada1d9e4c5c22353ba0fbed5589c52a0366de`.

- Compilación `30276203737`: **success**.
- Aceptación `30276203714`: **success**.
- Auditoría `30276203753`: **success**.

43 pruebas aprobadas. Detalle: `docs/FASE4_CERTIFICACION_RESPONSIVE.md`.

## FASE 5 — Imágenes — COMPLETA Y CERTIFICADA

Se implementó el componente reutilizable de imágenes, fallback accesible, texto alternativo, carga diferida, hero prioritario, galería y lightbox por teclado, e imagen principal en Productos, Compras, Ventas y Movimientos.

Commit: `90eb4ff4c9b7b4a8ed66561fa092f7521ebe7630`.

- Compilación `30289511599`: **success**.
- Aceptación `30289510773`: **success**.
- Auditoría `30289511930`: **success**.

47 pruebas aprobadas. Artefacto `8662414676`. Detalle: `docs/FASE5_CERTIFICACION_IMAGENES.md`.

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
- Generador QuestPDF para página fija y rollo térmico.
- A4 preservado para correo, WhatsApp y enlaces públicos.
- Endpoint de catálogo y descarga seleccionable.
- Encabezado, nombre de archivo y auditoría por formato.
- Selector de papel y preferencia local.
- Vista previa proporcional para A5 y POS.
- Descarga e impresión del perfil elegido.
- Desplazamiento interno para papeles grandes en teléfono.
- Snapshot fiscal preservado sin recálculo.
- Sin migración de base de datos.

### Certificación

Commit funcional:

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
7 PDFs
8 capturas
```

Los PDFs certifican firma, encabezados y dimensiones físicas mediante `MediaBox`; fueron renderizados e inspeccionados sin recortes ni superposiciones.

Detalle: `docs/FASE6_CERTIFICACION_FACTURACION_IMPRESION.md`.

### Validaciones físicas pendientes

Solo en Desarrollo: impresoras reales de oficina y térmicas, drivers, USB/red/Bluetooth, márgenes, densidad, corte, avance y diálogos de impresión por dispositivo.

La Fase 6 no modificó Producción y no autoriza merge ni despliegue.

## FASE 7 — Envío de correo — SIGUIENTE, NO INICIADA

Problema confirmado en Desarrollo: intentos con resultado `Error` y mensaje `No se pudo enviar el correo`.

Se revisarán SMTP, variables de Render Desarrollo, autenticación, TLS, certificados, remitente, timeout, logs, errores sanitizados, plantillas, PDF adjunto, reintentos e idempotencia.

La fase solo se cierra con entrega real y verificación de bandeja de entrada y spam.

## FASE 8 — Validación completa — BLOQUEADA

Se repetirá la auditoría de interfaz, responsive, impresión, imágenes, correo, configuración, rendimiento, consola, logs, advertencias, seguridad y accesibilidad. No debe quedar ningún defecto crítico o alto conocido.

## FASE 9 — Informe final — BLOQUEADA

El informe final contendrá cambios realizados, problemas y soluciones, riesgos y mejoras recomendadas no implementadas sin autorización.

## Regla de publicación

Completar estas fases no autoriza automáticamente el merge ni el despliegue productivo. Antes de cualquier operación sobre Producción se exige respaldo verificable, estrategia de migración única, ventana de mantenimiento, responsables, rollback y autorización expresa de Javier Mejía.
