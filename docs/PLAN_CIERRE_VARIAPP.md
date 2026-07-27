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

## FASE 2 — Auditoría general — COMPLETA Y CERTIFICADA

### Alcance ejecutado

Se auditaron:

- configuración de backend, frontend, Docker, Render y Vercel;
- variables declarativas y ausencia de secretos versionados;
- conexión MySQL, estrategia de migraciones y readiness;
- autenticación JWT, renovación, permisos y alcance por usuario;
- CORS, host filtering, proxy inverso y encabezados HTTP;
- almacenamiento Cloudinary;
- SMTP y manejo de errores;
- dependencias .NET y npm;
- colas, tareas programadas y servicios en segundo plano;
- logs, auditoría, dominios, TLS y observabilidad.

### Correcciones cerradas

- Rate limiting por IP para `POST /auth/login`.
- Validación temprana de secreto, issuer y audience JWT.
- `ForwardLimit=1` para encabezados del proxy.
- HSTS y encabezados defensivos para la API.
- Endpoints separados `/health` y `/health/ready`.
- Contenedor Docker ejecutado como usuario no privilegiado.
- `AllowedHosts` restringido al host de Render Desarrollo.
- Logo de Desarrollo servido desde Vercel Desarrollo, sin dependencia productiva.
- Eliminación de la referencia obsoleta `Microsoft.AspNetCore.Http.Abstractions 2.2.0`.
- Resolución de la vulnerabilidad crítica transitiva `System.Text.Encodings.Web 4.5.0`.
- Auditoría npm productiva sin vulnerabilidades altas o críticas.
- Workflow permanente `.github/workflows/fase2-auditoria.yml`.

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

La auditoría no modificó Producción y no autoriza merge ni despliegue.

## FASE 3 — Corrección de interfaz — SIGUIENTE, NO INICIADA

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
