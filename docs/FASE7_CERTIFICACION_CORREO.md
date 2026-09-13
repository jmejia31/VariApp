# FASE 7 — Envío de correo

Fecha de cierre técnico aislado: 27 de julio de 2026.

Rama modificada: `Desarrollo`.

Estado: **implementación y certificación SMTP aislada completas; aceptación externa pendiente**.

Producción permaneció congelada. No se modificaron `main`, credenciales, variables, dominios, servicios, bases, activos, migraciones ni despliegues productivos.

## 1. Alcance ejecutado

Se revisó y corrigió el flujo completo de envío de facturas por correo:

- configuración SMTP;
- validación temprana de variables;
- autenticación;
- TLS y validación de certificados del sistema operativo;
- remitente y dirección de respuesta;
- timeout;
- manejo de errores;
- logs sanitizados;
- plantilla HTML y alternativa de texto plano;
- PDF oficial A4 adjunto;
- reintentos ante fallos transitorios;
- idempotencia frente a doble clic o repetición HTTP;
- historial de envíos;
- estado de configuración para la interfaz;
- pruebas unitarias y end-to-end con SMTP real efímero.

## 2. Backend

### Servicio SMTP

`backend/src/Infrastructure/Services/SmtpEmailService.cs` ahora:

- valida host, puerto, usuario, contraseña y remitente antes de conectar;
- no acepta valores `CHANGE_ME`, `REPLACE_ME` o marcadores sin configurar;
- permite declarar si el servidor requiere autenticación;
- utiliza `EnableSsl` y la validación de certificados del sistema operativo;
- no incluye bypass de certificados;
- normaliza contraseñas de aplicación con espacios;
- establece timeout configurable;
- reintenta únicamente errores considerados transitorios;
- aplica backoff exponencial acotado;
- limita la cantidad y tamaño total de adjuntos;
- genera cuerpo `text/plain` y `text/html`;
- asigna `X-VariApp-Message-Id` para trazabilidad;
- enmascara destinatario, remitente y host en diagnósticos;
- no registra contraseñas ni credenciales;
- devuelve códigos técnicos controlados sin exponer excepciones internas.

Códigos principales:

```text
ENVIADO
SMTP_NO_CONFIGURADO
DESTINATARIO_INVALIDO
ADJUNTOS_INVALIDOS
SMTP_TIMEOUT
SMTP_AUTENTICACION
SMTP_DESTINATARIO_RECHAZADO
SMTP_TEMPORAL
SMTP_RECHAZADO
SMTP_ERROR
```

### Factura y adjunto

El envío genera el PDF oficial A4 de la factura y lo adjunta como:

```text
{NumeroFactura}.pdf
Content-Type: application/pdf
```

La plantilla contiene:

- nombre comercial;
- cliente;
- número de factura;
- fecha;
- total;
- explicación del PDF adjunto;
- versión HTML responsive;
- alternativa de texto plano.

A4 se mantiene como formato oficial para correo, independientemente del perfil seleccionado para impresión en la interfaz.

## 3. Reintentos

Variables:

```text
Smtp__TimeoutSeconds=30
Smtp__MaxAttempts=3
Smtp__RetryBaseDelayMilliseconds=500
```

Se reintentan errores temporales de red, timeout, servicio no disponible, buzón ocupado, almacenamiento insuficiente, error local de procesamiento o transacción fallida. No se reintentan destinatarios inválidos, configuración ausente ni rechazos permanentes.

Cuando un envío requiere más de un intento, el historial registra, por ejemplo:

```text
Enviado (2 intentos)
```

## 4. Idempotencia

El endpoint acepta:

```http
Idempotency-Key: <valor único de hasta 128 caracteres>
```

La clave se combina con factura, destinatario y usuario, y se almacena únicamente como hash. Una repetición exitosa con la misma clave:

- no vuelve a enviar el correo;
- no duplica el historial;
- devuelve `YaProcesado=true` y el mismo `MessageId`.

Ventana configurable:

```text
AppSettings__CorreoFacturaIdempotenciaMinutos=15
```

### Límite conocido

La caché de idempotencia es local al proceso. Protege doble clic y reintentos HTTP en la instancia actual, pero no sobrevive un reinicio ni coordina varias réplicas. Una implementación persistente distribuida requeriría tabla o caché compartida y se documenta como mejora futura; no se añadió una migración en esta fase.

## 5. API

### Estado SMTP

```http
GET /facturas/correo/estado
```

Requiere `Facturacion:Compartir` y devuelve únicamente información segura:

- configurado o no configurado;
- host enmascarado;
- puerto;
- TLS;
- autenticación;
- remitente enmascarado;
- máximo de intentos;
- timeout;
- mensaje de diagnóstico sin secretos.

### Envío

```http
POST /facturas/{id}/compartir/correo
Idempotency-Key: <clave>
Content-Type: application/json

{
  "destinatario": "cliente@correo.com"
}
```

Respuestas controladas:

- `200`: enviado o solicitud ya procesada;
- `400`: destinatario, PDF o clave inválidos;
- `503`: SMTP no configurado o fallo transitorio, con `Retry-After` cuando corresponde;
- `502`: rechazo SMTP permanente.

## 6. Frontend

La vista de factura:

- consulta el estado SMTP al abrir el panel;
- utiliza una clave de idempotencia por intento del usuario;
- conserva la misma clave cuando el transporte HTTP falla;
- genera una clave nueva después de un envío exitoso o al cambiar/cerrar el panel;
- mantiene editable el destinatario;
- presenta mensajes de error sanitizados;
- actualiza el historial si estaba abierto;
- evita llamadas duplicadas mientras el envío está en curso.

## 7. Configuración declarativa de Desarrollo

`render.yaml` declara exclusivamente para `variapp-api-desarrollo`:

```text
Smtp__Host
Smtp__Port
Smtp__UsuarioSmtp
Smtp__PasswordSmtp
Smtp__UsarSsl
Smtp__RequiereAutenticacion
Smtp__CorreoRemitente
Smtp__CorreoRespuesta
Smtp__NombreRemitente
Smtp__TimeoutSeconds
Smtp__MaxAttempts
Smtp__RetryBaseDelayMilliseconds
AppSettings__CorreoFacturaIdempotenciaMinutos
```

Los secretos usan `sync: false`; no están versionados. Producción no fue modificada.

## 8. Pruebas automatizadas

### Unitarias

Se añadieron pruebas para:

- diagnóstico sin exposición de secretos;
- SMTP AUTH LOGIN;
- mensaje MIME con texto plano y HTML;
- PDF adjunto;
- primer fallo `451` transitorio;
- segundo intento exitoso;
- destinatario inválido sin conexión;
- historial con cantidad de intentos;
- error transitorio seguro;
- idempotencia sin segundo envío.

### SMTP efímero

`scripts/fase7_fake_smtp.py` levanta un servidor SMTP real de laboratorio que:

- exige autenticación;
- captura el mensaje `.eml`;
- provoca deliberadamente un fallo temporal en el primer `DATA`;
- acepta el segundo intento;
- registra cantidad de conexiones, intentos y mensajes.

`scripts/fase7_validate_email.py` analiza el correo capturado y certifica:

- destinatario;
- asunto;
- `X-VariApp-Message-Id`;
- cuerpo de texto plano;
- cuerpo HTML;
- exactamente un PDF;
- nombre del archivo;
- firma `%PDF`;
- tamaño mínimo del adjunto;
- exactamente un correo pese a repetir la solicitud.

### End-to-end

`frontend/e2e/fase7-correo.spec.ts` valida:

- endpoint de estado SMTP;
- creación de producto, venta y factura;
- envío de factura;
- reintento después de fallo temporal;
- respuesta con dos intentos;
- idempotencia;
- un único registro histórico;
- rechazo de clave demasiado larga;
- panel móvil sin desbordamiento.

## 9. Certificación aislada final

Commit funcional certificado:

```text
53db49dff838779a707c360bc3b0294939407387
```

Ejecuciones:

- `Desarrollo - Compilación y pruebas`, run `30302605498`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30302605307`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30302605671`: **success**.

Resultado Playwright:

```text
52 pruebas totales
52 aprobadas
0 fallos
0 errores
0 omitidas
```

Evidencia:

```text
desarrollo-aceptacion-integral
artifact id: 8667374072
SHA-256: a1b022885921276b765c16020f1aea39606a0f597e44b5ae5abc26a7ad522a16
```

Resumen SMTP certificado:

```text
Destinatario: fase7@example.com
Asunto: Factura FAC-000002 - VariStorehn
Adjunto: FAC-000002.pdf
PDF: 126302 bytes
Conexiones: 3
Intentos DATA: 2
Fallos temporales: 1
Mensajes guardados: 1
```

El artefacto incluye `.eml`, estado del servidor, resumen de validación, logs, reporte Playwright y captura móvil del panel.

## 10. Aceptación externa pendiente

La prueba aislada demuestra que el código envía correctamente por SMTP, reintenta y adjunta un PDF válido. No demuestra recepción en un proveedor externo ni reputación del dominio.

Para cerrar completamente la Fase 7, el propietario debe realizar únicamente en el servicio Render de Desarrollo `variapp-api-desarrollo`:

1. Configurar `Smtp__Host`, `Smtp__Port`, `Smtp__UsuarioSmtp` y `Smtp__PasswordSmtp`.
2. Usar una contraseña de aplicación o credencial SMTP dedicada; no la contraseña normal de la cuenta.
3. Configurar `Smtp__CorreoRemitente` y, opcionalmente, `Smtp__CorreoRespuesta`.
4. Mantener `Smtp__UsarSsl=true` cuando el proveedor utilice STARTTLS/SSL según su puerto.
5. Desplegar únicamente `Desarrollo`.
6. Abrir una factura de prueba en `variapp-desarrollo.vercel.app`.
7. Confirmar que `/facturas/correo/estado` muestra `configurado=true` sin revelar secretos.
8. Enviar a un buzón controlado.
9. Verificar bandeja de entrada y spam.
10. Abrir el PDF y comprobar número, cliente, totales y formato A4.
11. Confirmar remitente, Reply-To, asunto y contenido HTML/texto.
12. Pulsar dos veces o repetir con la misma clave y verificar que llega un solo correo.
13. Revisar Historial de envíos y logs de Render Desarrollo.
14. Registrar capturas y hora de recepción.

Si el proveedor utiliza un dominio propio, revisar SPF, DKIM y DMARC en ese dominio antes de considerar entregabilidad certificada.

No deben utilizarse ni modificarse credenciales, variables o servicios de Producción.

## 11. Criterio de estado

- Implementación: **completa**.
- Compilación, pruebas y SMTP aislado: **certificados**.
- Entrega a proveedor externo, bandeja de entrada y spam: **pendiente del propietario**.
- Fase 7 completa en sentido estricto: **no**, hasta aportar esa evidencia externa.
- Fase 8: permanece bloqueada hasta cerrar la aceptación externa o registrar formalmente una excepción aprobada.

Este estado no autoriza merge, despliegue productivo ni modificación de Producción.
