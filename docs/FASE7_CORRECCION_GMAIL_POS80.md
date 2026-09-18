# Fase 7 — Corrección real de Gmail SMTP y POS‑80

Fecha: 27 de julio de 2026  
Rama exclusiva: `Desarrollo`  
Producción: congelada; `main` no se modificó.

## Hallazgos aportados desde varistorehn_desarrollo

Las capturas del propietario confirmaron dos problemas diferentes:

1. Render Desarrollo tenía declarados `smtp.gmail.com`, puerto `587`, autenticación, usuario y remitente, pero el envío fallaba sin explicar si la causa era conexión, STARTTLS o credenciales.
2. Chrome mostraba el ticket POS‑80 dentro de una hoja `80(72.1) × 297 mm`, con un gran espacio en blanco y el pie al final.

## Diagnóstico de impresión

El generador ya utiliza `ContinuousSize` para POS‑58 y POS‑80. El PDF certificado anterior de POS‑80 tenía un `MediaBox` aproximado de `79.99 × 191.73 mm`, no `80 × 297 mm`.

Por tanto, los `297 mm` observados no procedían del PDF generado por VariApp. El controlador `POS-80 (copy 2)` estaba imponiendo un tamaño fijo de papel en el diálogo de impresión.

### Corrección aplicada

- Las pruebas ahora rechazan que un ticket corto tenga altura A4/297 mm.
- Se valida que la altura del rollo crece cuando aumenta el contenido.
- La interfaz lee el `MediaBox` real del PDF descargado.
- Para POS‑58/POS‑80, antes de abrir el visor se muestra una preparación de impresión con:
  - ancho y altura reales;
  - advertencia cuando el driver muestra 297 mm;
  - indicación de papel `Receipt`, `Rollo` o `Continuous`;
  - escala 100 % y márgenes desactivados;
  - tamaño personalizado sugerido cuando el driver exige altura fija;
  - acceso al diálogo del sistema mediante `Ctrl+Shift+P`.

La aplicación web no puede modificar por sí sola el controlador de impresora instalado en Windows. Ahora distingue claramente el tamaño del PDF del tamaño impuesto por el driver.

## Corrección Gmail SMTP

Se reemplazó el transporte basado en `System.Net.Mail` por MailKit.

El nuevo servicio:

- usa STARTTLS obligatorio con puerto 587;
- usa SSL/TLS directo con puerto 465;
- mantiene validación real de certificados;
- comprueba conexión, negociación TLS y autenticación antes de habilitar el botón de envío;
- elimina mecanismos OAuth no configurados y utiliza usuario + contraseña de aplicación;
- aumenta el timeout predeterminado de 30 a 60 segundos;
- diferencia errores de conexión, TLS, timeout, autenticación, rechazo y fallo temporal;
- muestra instrucciones específicas cuando Gmail devuelve rechazo de credenciales;
- no devuelve ni registra contraseñas.

### Diagnóstico seguro

```http
POST /facturas/correo/probar
```

Requiere `Facturacion:Compartir`. No envía mensajes. Comprueba:

1. conexión a host y puerto;
2. negociación STARTTLS/SSL;
3. autenticación;
4. comando SMTP `NOOP`;
5. desconexión limpia.

La respuesta contiene solamente:

- código seguro;
- host enmascarado;
- puerto;
- modo de seguridad;
- autenticación aprobada o rechazada;
- duración;
- explicación sin secretos.

## Configuración requerida en Render Desarrollo

Servicio exclusivo: `variapp-api-desarrollo`.

```text
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__UsuarioSmtp=varistorehn@gmail.com
Smtp__PasswordSmtp=<contraseña de aplicación de Gmail>
Smtp__UsarSsl=true
Smtp__RequiereAutenticacion=true
Smtp__CorreoRemitente=varistorehn@gmail.com
Smtp__CorreoRespuesta=varistorehn@gmail.com
Smtp__NombreRemitente=VariStorehn Desarrollo
Smtp__TimeoutSeconds=60
Smtp__MaxAttempts=3
Smtp__RetryBaseDelayMilliseconds=500
```

`Smtp__PasswordSmtp` debe ser una contraseña de aplicación de Google vigente. No debe ser la contraseña normal de la cuenta. El servicio elimina espacios accidentales antes de autenticar.

Después de reemplazar una credencial en Render Desarrollo se requiere guardar y desplegar o reiniciar el servicio para que el proceso lea el nuevo valor.

## Flujo visible en la factura

Al abrir «Enviar por correo»:

1. se valida la configuración local;
2. se ejecuta el diagnóstico real del proveedor;
3. se muestran host enmascarado, puerto, seguridad y timeout;
4. si Gmail rechaza la autenticación, el botón permanece deshabilitado y se muestra la causa;
5. solo después de `SMTP_OK` se habilita «Enviar».

Esto evita el mensaje genérico «No se pudo enviar el correo» y permite corregir la variable exacta.

## Pruebas incorporadas

- Estado SMTP sin exposición de secretos.
- Modo STARTTLS explícito.
- Diagnóstico de conexión y autenticación sin enviar correo.
- Envío MIME con HTML, texto y PDF.
- Reintento ante `451` temporal.
- Idempotencia.
- POS‑58/POS‑80 sin altura 297 mm fija.
- Crecimiento de la altura térmica según contenido.
- Lectura del `MediaBox` desde la interfaz.
- Pantalla de preparación del controlador POS‑80.
- Panel SMTP responsive con diagnóstico previo.

## Estado de cierre

La corrección de código se certifica mediante CI aislado. La recepción real en Gmail y la impresión física dependen todavía de:

- una contraseña de aplicación válida cargada en Render Desarrollo;
- el nuevo despliegue de `Desarrollo`;
- el controlador POS‑80 configurado como rollo continuo o con el tamaño personalizado sugerido;
- la prueba física del propietario.

No se avanza a la siguiente fase hasta registrar esas comprobaciones externas. Ninguna de ellas autoriza tocar Producción.
