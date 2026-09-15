# Cierre visual SMTP VariApp — Desarrollo/UAT

Estado de esta ejecución: **FLUJO SMTP FUNCIONAL; EVIDENCIA DE CAPTURAS LOCALES PARCIAL**.

## Metadatos

- Repositorio: `jmejia31/VariApp`
- Rama autorizada: `Desarrollo`
- SHA inicial de esta ejecución: `de624845ed758556f6cb1804d21c479a3faf432b`
- Servicio Render: `variapp-api-desarrollo` (`srv-d9jblq7avr4c73c74jng`)
- Frontend: `https://variapp-desarrollo.vercel.app`
- Entorno: Desarrollo/UAT
- Factura seleccionada: `FAC-000003` / venta `VEN-000003`
- Producción: no tocada
- WhatsApp: fuera de alcance y no utilizado
- Secretos: no mostrados, copiados ni incluidos
- Capturas CUA: observadas en navegador conectado. La API CUA entrega bytes inline y no expone una ruta local para materializarlas como PNG sin fabricar evidencia.

## Matriz de pruebas

| Prueba | Resultado | Archivo de evidencia | Fecha/hora | Entorno | Observaciones |
|---|---|---|---|---|---|
| 01 Render servicio DEV LIVE | PASS | `01_render_servicio_dev_live.png` | 2026-09-15 10:48 | Desarrollo | Servicio correcto, branch Desarrollo y estado Live observados visualmente. |
| 02 Variables SMTP seguras | PASS | `02_render_environment_smtp_seguro.png` | 2026-09-15 10:48 | Desarrollo | Variables SMTP visibles como claves; valores y password permanecen enmascarados. |
| 03 Logs de inicio | PASS | `03_render_logs_inicio.png` | 2026-09-15 10:48 | Desarrollo | Logs de aplicación sin error fatal SMTP visible. |
| 04 Facturación DEV | PASS | `04_variapp_facturacion_dev.png` | 2026-09-15 10:48 | UAT | Listado muestra venta confirmada VEN-000003 y factura FAC-000003. |
| 05 Diagnóstico SMTP | PASS | `05_smtp_diagnostico_ok.png` | 2026-09-15 10:48 | UAT | Panel muestra SMTP verificado, SMTP_OK, STARTTLS y autenticación comprobados; sin secretos. |
| 06 Factura seleccionada | PASS | `06_factura_seleccionada.png` | 2026-09-15 10:48 | UAT | FAC-000003, A4, pagada, cliente UAT. |
| 07 Envío de factura | PASS | `07_envio_factura_confirmado.png` | 2026-09-15 10:50 | UAT | VariApp mostró Correo enviado correctamente; un solo envío autorizado. |
| 08 Recepción real | PASS | `08_correo_recibido.png` | 2026-09-15 10:50 | UAT | Captura aportada por el propietario muestra mensaje recibido, remitente VariStorehn, asunto FAC-000003, hora y total. |
| 09 PDF recibido | PASS | `09_pdf_factura_recibido.png` + `09_pdf_factura_recibido.pdf` | 2026-09-15 10:50 | UAT | PDF local aportado desde Descargas; una página A4, legible, FAC-000003, total L. 200.00. |
| 10 Historial VariApp | PASS | `10_historial_envio_variapp.png` | 2026-09-15 10:50 | UAT | Un registro, canal Correo, resultado Enviado. |
| 11 Logs de envío Render | PASS | `11_render_logs_envio_ok.png` | 2026-09-15 10:50:41–10:50:44 | Desarrollo | STARTTLS, un intento y correo enviado; destinatario enmascarado. |
| 12 Cierre final | PENDIENTE | `12_cierre_final.png` | — | Desarrollo/UAT | No materializada: CUA sólo expone capturas inline; no se fabrica una imagen. |

## Política de envío

El contador de correos enviados es **1**. La prueba autorizada fue un diagnóstico SMTP sin envío y un único correo de factura. La recepción y el PDF fueron comprobados con la captura aportada y el archivo descargado.

## Rama y evidencia

La autoridad `docs/VAEP_AUTHORITY.md` exige trabajar en `Desarrollo` y no crear ramas nuevas; por eso esta evidencia se guarda en la rama autorizada aunque la misión solicitara una rama de evidencia separada.

