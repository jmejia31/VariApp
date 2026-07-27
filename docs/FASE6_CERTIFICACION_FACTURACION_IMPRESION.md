# FASE 6 — Certificación de facturación e impresión

Fecha de cierre técnico: 27 de julio de 2026.

Rama modificada y certificada: `Desarrollo`.

Producción permaneció congelada. No se modificaron `main`, variables, credenciales, dominios, servicios, bases, activos, migraciones ni despliegues productivos.

## 1. Objetivo

La Fase 6 implementó perfiles explícitos de generación, descarga, previsualización e impresión de facturas para A4, Carta, Legal, Oficio, A5, POS 58 mm y POS 80 mm.

La solución cubre impresoras convencionales de oficina y perfiles térmicos continuos utilizados por equipos POS, móviles, handheld e industriales.

## 2. Principio fiscal y compatibilidad

El cambio de papel no modifica la factura persistida ni recalcula importes. Todos los formatos consumen el mismo snapshot fiscal de la venta confirmada: empresa, cliente, fecha, número, vendedor, pago, productos, cantidades, precios, descuentos, impuestos, subtotal, total, observaciones y anulación.

A4 se conserva como documento oficial predeterminado para correo electrónico, WhatsApp, enlaces públicos, consumidores anteriores de `GenerarPdfAsync(FacturaDto)` y solicitudes PDF que omitan el parámetro de formato.

## 3. Catálogo de perfiles

Se añadió `backend/src/Application/DTOs/FacturaFormatoPdf.cs`.

| Código | Perfil | Dimensión física | Tipo |
|---|---|---:|---|
| `a4` | A4 | 210 × 297 mm | Página fija |
| `carta` | Carta | 215.9 × 279.4 mm | Página fija |
| `legal` | Legal | 215.9 × 355.6 mm | Página fija |
| `oficio` | Oficio | 215.9 × 330.2 mm | Página fija |
| `a5` | A5 | 148 × 210 mm | Página fija compacta |
| `pos58` | POS 58 mm | 58 mm de ancho | Rollo continuo |
| `pos80` | POS 80 mm | 80 mm de ancho | Rollo continuo |

El parser acepta alias controlados y rechaza cualquier formato desconocido con HTTP 400.

## 4. Generador QuestPDF

Se añadió `backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs`. La clase de servicio anterior conserva su nombre de registro y delega al nuevo generador.

A4, Carta, Legal y Oficio usan composición corporativa completa. A5 compacta tipografía y columnas. POS 58 y POS 80 utilizan ancho físico exacto, altura continua y una estructura vertical apropiada para rollo térmico.

Todos conservan datos empresariales, cliente, productos, clasificación, pago, descuentos, impuestos, totales, observaciones, anulación y textos legales.

## 5. API

```text
GET /facturas/formatos-pdf
GET /facturas/{id}/pdf?formato=a4|carta|legal|oficio|a5|pos58|pos80
```

La descarga incluye `application/pdf`, nombre con perfil, `X-Factura-Formato`, no caché y auditoría del formato exportado. Las descargas públicas permanecen en A4 y conservan token, expiración, revocación y límite de accesos.

## 6. Interfaz

La vista de factura incorpora selector, dimensiones, tipo de papel, uso recomendado, persistencia local y aviso de que los canales compartidos usan A4.

Los botones indican el perfil que se descargará o abrirá para imprimir. La impresión abre el PDF real del backend. La vista previa mantiene proporciones diferenciadas para A5, POS 58 y POS 80, mientras los papeles grandes usan desplazamiento interno en teléfonos.

## 7. Pruebas backend

`backend/tests/InventoryApp.Tests/QuestPdfFacturaPerfilesServiceTests.cs` verifica firma PDF, tamaño mínimo, `MediaBox`, rollos continuos, A4 predeterminado, alias y rechazo de formatos desconocidos.

## 8. Aceptación end-to-end

`frontend/e2e/fase6-facturacion-impresion.spec.ts` crea productos y una venta en MySQL descartable, confirma la venta y certifica catálogo, siete PDFs, firma, encabezados, dimensiones físicas, error 400, selector, vistas previas, responsive, descarga e impresión.

## 9. Evidencia

```text
desarrollo-aceptacion-integral
artifact id: 8664760725
SHA-256: f7e2ca67e311f43f9e13fedcce57fc43cb9b93da314827587226de922694b640
```

PDFs incluidos:

```text
FAC-000001-a4.pdf
FAC-000001-carta.pdf
FAC-000001-legal.pdf
FAC-000001-oficio.pdf
FAC-000001-a5.pdf
FAC-000001-pos58.pdf
FAC-000001-pos80.pdf
```

Capturas incluidas:

```text
preview-a4.png
preview-carta.png
preview-legal.png
preview-oficio.png
preview-a5.png
preview-pos58.png
preview-pos80.png
preview-pos58-mobile.png
```

Los siete PDFs fueron renderizados e inspeccionados sin texto recortado, superposiciones, glifos rotos ni pérdida visible de totales. Las capturas finales se regeneraron después de cerrar por completo la animación del selector.

## 10. Resultado final

Commit funcional certificado:

```text
14bf32069f9d87f731e59f230b9e9f5f16ade14e
```

La documentación de cierre se encuentra en este archivo y en `docs/PLAN_CIERRE_VARIAPP.md`.

Ejecuciones sobre el commit funcional:

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

La prueba específica de Fase 6 ejecutó cuatro casos y los cuatro terminaron correctamente.

## 11. Validaciones físicas pendientes

La certificación automatizada no puede certificar mecánica, drivers o calibración de cada impresora. Deben probarse exclusivamente en `varistorehn_desarrollo`:

- impresora de oficina con A4, Carta, Legal, Oficio y A5;
- impresoras térmicas reales de 58 y 80 mm;
- USB, red y Bluetooth;
- Chrome, Edge, Android e iOS según disponibilidad;
- márgenes no imprimibles;
- densidad, velocidad, corte y avance;
- escalado 100 % sin “ajustar a página” para POS;
- ancho imprimible real del equipo.

## 12. Criterio de cierre

La Fase 6 queda completa y certificada en código, API, generación PDF, interfaz, pruebas backend, aceptación end-to-end y evidencia visual. La siguiente etapa es la Fase 7 — Envío de correo.

Completar esta fase no autoriza merge, despliegue ni modificación de Producción.
