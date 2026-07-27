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
| Aiven | recursos productivos existentes; `avnadmin` se conserva | usuario de aplicación `varistorehn_desarrollo` y variables de Desarrollo existentes |
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
- No se identificó en la evidencia un tercer entorno permanente que pudiera eliminarse de forma segura.
- `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo` está versionado y protegido por CI.
- Las migraciones automáticas permanecen deshabilitadas.
- Los workflows de compilación y aceptación terminaron correctamente para la estandarización inicial.

### Decisión sobre nombres técnicos

No se renombra Producción. Los nombres técnicos existentes, como dominios o nombres de servicio, se mantienen para evitar interrupciones. En documentación, gobierno y nuevas configuraciones se utilizan los nombres lógicos oficiales `varistorehn_producción` y `varistorehn_desarrollo`.

### Cierre

La Fase 1 queda cerrada con la confirmación del propietario de que:

- Producción y Desarrollo deben conservar sus variables actuales;
- los recursos productivos indicados no se eliminan;
- únicamente pueden retirarse duplicados ajenos a ambos entornos y previamente verificados;
- todo trabajo continúa exclusivamente en `Desarrollo`.

Las comprobaciones profundas de conexiones, permisos, variables, certificados y dependencias pasan a la Fase 2.

## FASE 2 — Auditoría general — SIGUIENTE, NO INICIADA

### Alcance

Revisar de extremo a extremo:

- configuraciones;
- servicios y despliegues;
- variables y secretos sin revelar sus valores;
- conexiones e integraciones;
- bases de datos y almacenamiento;
- autenticación y APIs;
- colas y tareas programadas;
- permisos y certificados;
- dominios, DNS y CORS;
- logs, alertas y observabilidad.

### Criterio

Producción y Desarrollo deben ser consistentes en arquitectura, pero independientes en datos, credenciales y ejecución. La auditoría no autoriza ningún cambio productivo.

## FASE 3 — Corrección de interfaz — BLOQUEADA

Corregir textos cortados, superpuestos, fuera del contenedor o desalineados. Prioridades observadas:

- formulario administrativo de Usuario;
- Perfil;
- formulario de Producto;
- lista y tabla de Productos;
- cabecera, rol y acciones;
- ayudas y errores de formularios.

Criterio: ningún texto puede montarse, cortarse o desbordarse en los viewports certificados.

## FASE 4 — Responsive — BLOQUEADA

Certificar el sistema en:

- teléfonos pequeños y grandes;
- tablets;
- laptops;
- Full HD;
- 2K;
- 4K.

Se revisarán tipografía fluida, grids, tablas, paneles, diálogos, navegación, formularios, acciones y áreas táctiles.

## FASE 5 — Imágenes — BLOQUEADA

Mostrar la imagen principal cuando exista, con fallback accesible y carga eficiente, especialmente en:

- lista y detalle de Productos;
- Compras;
- Ventas;
- detalles e historial.

## FASE 6 — Facturación e impresión — BLOQUEADA

Certificar PDF e impresión para:

- Carta;
- Legal;
- Oficio;
- A4;
- A5;
- POS 58 mm;
- POS 80 mm;
- impresoras móviles, handheld, industriales y convencionales.

La factura debe conservar información, alineación, logo, tablas, códigos, datos fiscales y totales. Se implementarán perfiles explícitos cuando el navegador no pueda detectar automáticamente el medio físico.

## FASE 7 — Envío de correo — BLOQUEADA

Problema confirmado en Desarrollo: intentos con resultado `Error` y mensaje `No se pudo enviar el correo`.

Se revisarán SMTP, variables de Render Desarrollo, autenticación, TLS, certificados, remitente, timeout, logs, errores sanitizados, plantillas, PDF adjunto, reintentos e idempotencia.

La fase solo se cierra con entrega real y verificación de bandeja de entrada y spam.

## FASE 8 — Validación completa — BLOQUEADA

Se repetirá la auditoría de:

- interfaz;
- responsive;
- impresión;
- imágenes;
- correo;
- configuración;
- rendimiento;
- consola, logs y advertencias;
- seguridad y accesibilidad.

No debe quedar ningún defecto crítico o alto conocido.

## FASE 9 — Informe final — BLOQUEADA

El informe final contendrá:

1. cambios realizados;
2. problemas encontrados y solución aplicada;
3. riesgos identificados;
4. mejoras recomendadas no implementadas sin autorización.

## Regla de publicación

Completar estas fases no autoriza automáticamente el merge ni el despliegue productivo. Antes de cualquier operación sobre Producción se exige respaldo verificable, estrategia de migración única, ventana de mantenimiento, responsables, rollback y autorización expresa de Javier Mejía.
