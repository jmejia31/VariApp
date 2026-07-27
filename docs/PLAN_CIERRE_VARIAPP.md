# Plan obligatorio de trabajo por fases — VariApp / VariStorehn

Rama exclusiva de trabajo: `Desarrollo`.

Pull Request: `Desarrollo -> main`, en borrador hasta autorización expresa de Javier Mejía.

Identificador oficial del entorno de Desarrollo:

```text
varistorehn_desarrollo
```

## Reglas generales

Antes de iniciar cada fase se debe:

1. analizar el alcance completo;
2. identificar riesgos;
3. verificar dependencias;
4. confirmar que ningún cambio afecta Producción;
5. definir pruebas y evidencia de cierre.

Al finalizar cada fase se ejecutará una revisión completa. No se inicia la fase siguiente mientras exista un requisito, una validación o una evidencia pendiente de la fase actual.

Producción no se modifica bajo ninguna circunstancia durante estas fases. Un cambio de nombre visible solo se evaluará si es puramente estético, reversible y demostrablemente ajeno a dominios, variables, conexiones y despliegues; por defecto no se realizará.

## FASE 1 — Entornos y recursos — EN CURSO

### Objetivo

Estandarizar Desarrollo usando únicamente los recursos creados y designados por Javier Mejía, sin modificar Producción.

### Recursos autorizados observados

- Git: rama `Desarrollo`.
- Aiven: usuario de aplicación `varistorehn_desarrollo` dentro del servicio mostrado por el propietario.
- Cloudinary: clave de API etiquetada `varistorehn_desarrollo`.
- Render: entorno `Desarrollo`, servicio `variapp-api-desarrollo`.
- Vercel: proyecto `variapp-desarrollo`, Production Branch `Desarrollo`, dominio `variapp-desarrollo.vercel.app`.

### Trabajo realizado en el repositorio

- `render.yaml` usa el prefijo Cloudinary `varistorehn_desarrollo`.
- Las pruebas de aislamiento Cloudinary usan el identificador oficial.
- GitHub Actions rechaza el prefijo genérico anterior `desarrollo`.
- Se documentaron los recursos designados por el propietario y se retiraron instrucciones que pedían crear duplicados.
- Se mantiene el proxy de Vercel separado por host.
- Se mantienen las migraciones automáticas deshabilitadas.
- No se modificó `main` ni ninguna configuración productiva.

### Riesgos identificados

- La captura de Aiven no muestra el nombre de la base ni los privilegios del usuario; no se puede certificar todavía el aislamiento de datos.
- La captura de Render no muestra, ni debe mostrar, valores de secretos; falta confirmar que utiliza el usuario Aiven y la clave Cloudinary autorizados.
- Cloudinary comparte el mismo product environment; la separación depende de la clave autorizada, el prefijo y el bloqueo de eliminación.
- El cambio de prefijo requiere un nuevo despliegue de Desarrollo antes de validar cargas reales.
- El correo SMTP falla en Desarrollo, pero se tratará exclusivamente en la Fase 7.

### Evidencia pendiente para cerrar la fase

1. Nombre exacto de la base Aiven de Desarrollo.
2. Confirmación de que `varistorehn_desarrollo` solo tiene privilegios sobre esa base.
3. Confirmación con valores ocultos de que Render Desarrollo usa:
   - el usuario Aiven `varistorehn_desarrollo`;
   - la base exclusiva de Desarrollo;
   - la clave Cloudinary etiquetada `varistorehn_desarrollo`;
   - `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo`.
4. Respuesta HTTP 200 de `/health` después del despliegue.
5. Confirmación de que Vercel Desarrollo sigue enviando `/api` a Render Desarrollo.
6. Confirmación de que no existe un tercer servicio, proyecto, base o usuario de aplicación permanente para Desarrollo.
7. GitHub Actions en verde para el commit final de la fase.

### Criterio de cierre

Fase 1 se marca completa únicamente cuando todas las evidencias anteriores están verificadas. Hasta entonces Fase 2 permanece bloqueada.

## FASE 2 — Auditoría general — BLOQUEADA POR FASE 1

### Alcance

Revisar de extremo a extremo:

- configuraciones;
- servicios y despliegues;
- variables y secretos;
- conexiones e integraciones;
- bases de datos y almacenamiento;
- autenticación y APIs;
- colas y tareas programadas;
- permisos y certificados;
- dominios, DNS y CORS;
- logs, alertas y observabilidad.

### Criterio

Producción y Desarrollo deben ser consistentes en arquitectura, pero completamente independientes en datos, credenciales y recursos.

## FASE 3 — Corrección de interfaz — BLOQUEADA

### Alcance

Corregir todos los textos cortados, superpuestos, fuera del contenedor o desalineados. Prioridades observadas en la evidencia:

- formulario administrativo de usuario;
- perfil del usuario;
- formulario de Producto;
- lista y tabla de Productos;
- cabecera, rol y acciones;
- ayudas y errores de formularios.

### Criterio

Ningún texto puede montarse, cortarse o desbordarse en los viewports certificados.

## FASE 4 — Responsive — BLOQUEADA

### Alcance

Certificar todo el sistema en:

- teléfonos pequeños y grandes;
- tablets;
- laptops;
- Full HD;
- 2K;
- 4K.

Se revisarán tipografía fluida, grids, tablas, paneles, diálogos, navegación, formularios, acciones y áreas táctiles.

## FASE 5 — Imágenes — BLOQUEADA

### Alcance

Mostrar la imagen correspondiente cuando exista, especialmente en:

- lista y detalle de Productos;
- Compras;
- Ventas;
- detalles e historial.

Se utilizará imagen principal, fallback accesible y carga eficiente.

## FASE 6 — Facturación e impresión — BLOQUEADA

### Alcance mínimo

Certificar PDF, impresión convencional y térmica para:

- Carta;
- Legal;
- Oficio;
- A4;
- A5;
- POS 58 mm;
- POS 80 mm;
- impresoras móviles, handheld, industriales y convencionales.

La factura ajustará tipografía, márgenes, logo, tablas, códigos, datos fiscales y totales sin pérdida de información.

No se prometerá adaptación automática a un medio que el navegador no pueda detectar. Se implementarán perfiles de impresión explícitos y CSS/PDF determinista por formato.

## FASE 7 — Envío de correo — BLOQUEADA

### Problema confirmado

La evidencia muestra intentos reales con resultado `Error` y el mensaje `No se pudo enviar el correo`.

### Alcance

Revisar:

- SMTP y variables de Render Desarrollo;
- autenticación, TLS y certificados;
- remitente y credenciales;
- timeout, logs y errores sanitizados;
- plantillas y adjunto PDF;
- reintentos seguros e idempotencia;
- historial y observabilidad.

La fase solo se cerrará con entrega real a una cuenta externa y verificación de bandeja de entrada y spam.

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

Completar estas fases no autoriza automáticamente el merge ni el despliegue productivo. Antes de cualquier operación sobre Producción se exige:

- respaldo verificable;
- estrategia de migración única;
- ventana de mantenimiento;
- responsables;
- procedimiento de rollback;
- autorización expresa de Javier Mejía.
