# FASE 6 — Certificación de facturación e impresión

Fecha de cierre técnico: 27 de julio de 2026.

Rama modificada y certificada: `Desarrollo`.

Producción permaneció congelada. No se modificaron `main`, variables, credenciales, dominios, servicios, bases, activos, migraciones ni despliegues productivos.

## Objetivo y alcance

Se implementaron perfiles explícitos de generación, descarga, previsualización e impresión para A4, Carta, Legal, Oficio, A5, POS 58 mm y POS 80 mm. La solución cubre páginas convencionales y rollos térmicos continuos.

Cambiar el papel no altera ni recalcula la factura. Todos los perfiles consumen el mismo snapshot fiscal persistido. A4 continúa siendo el documento oficial predeterminado para correo, WhatsApp, enlaces públicos y consumidores anteriores.

## Perfiles

| Código | Perfil | Dimensión física | Tipo |
|---|---|---:|---|
| `a4` | A4 | 210 × 297 mm | Página fija |
| `carta` | Carta | 215.9 × 279.4 mm | Página fija |
| `legal` | Legal | 215.9 × 355.6 mm | Página fija |
| `oficio` | Oficio | 215.9 × 330.2 mm | Página fija |
| `a5` | A5 | 148 × 210 mm | Página fija compacta |
| `pos58` | POS 58 mm | 58 mm de ancho | Rollo continuo |
| `pos80` | POS 80 mm | 80 mm de ancho | Rollo continuo |

## Implementación

- `backend/src/Application/DTOs/FacturaFormatoPdf.cs`: catálogo y alias controlados.
- `backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs`: generador QuestPDF para página y rollo.
- `GET /facturas/formatos-pdf`: catálogo disponible para clientes.
- `GET /facturas/{id}/pdf?formato=...`: PDF validado, auditado y nombrado por perfil.
- Selector de papel, dimensiones, uso recomendado y preferencia local en la vista de factura.
- Descarga e impresión del perfil seleccionado.
- Vista previa proporcional para A5, POS 58 y POS 80.
- Desplazamiento interno para páginas grandes en teléfono.
- Sin migración de base de datos.

Todos los formatos conservan empresa, cliente, operación, productos, clasificación, pago, descuentos, impuestos, totales, observaciones, anulación y textos legales.

## Certificación automatizada

`backend/tests/InventoryApp.Tests/QuestPdfFacturaPerfilesServiceTests.cs` verifica firma, tamaño, `MediaBox`, rollos continuos, formato predeterminado, alias y rechazo de valores desconocidos.

`frontend/e2e/fase6-facturacion-impresion.spec.ts` crea una venta real en MySQL descartable y certifica catálogo, siete PDFs, encabezados, dimensiones físicas, error 400, selector, responsive, descarga e impresión.

Commit funcional certificado:

```text
14bf32069f9d87f731e59f230b9e9f5f16ade14e
```

Ejecuciones:

- Compilación y pruebas `30295557180`: **success**.
- Aceptación funcional integral `30295557155`: **success**.
- Auditoría de configuración y dependencias `30295557157`: **success**.

```text
51 pruebas totales
51 aprobadas
0 fallos
0 errores
0 omitidas
```

## Evidencia

```text
desarrollo-aceptacion-integral
artifact id: 8664760725
SHA-256: f7e2ca67e311f43f9e13fedcce57fc43cb9b93da314827587226de922694b640
```

Incluye siete PDFs:

```text
FAC-000001-a4.pdf
FAC-000001-carta.pdf
FAC-000001-legal.pdf
FAC-000001-oficio.pdf
FAC-000001-a5.pdf
FAC-000001-pos58.pdf
FAC-000001-pos80.pdf
```

Y ocho capturas:

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

Los PDFs fueron renderizados e inspeccionados sin recortes, superposiciones, glifos rotos ni pérdida visible de datos.

## Validaciones físicas pendientes

La automatización no certifica mecánica, drivers o calibración de equipos específicos. Deben probarse exclusivamente en `varistorehn_desarrollo`:

- impresoras de oficina con A4, Carta, Legal, Oficio y A5;
- impresoras térmicas de 58 y 80 mm;
- USB, red y Bluetooth;
- navegadores y dispositivos disponibles;
- márgenes no imprimibles;
- densidad, velocidad, corte y avance;
- escalado 100 % para POS;
- ancho imprimible real.

## Cierre

La Fase 6 queda completa y certificada. La siguiente etapa es la Fase 7 — Envío de correo.

Este cierre no autoriza merge, despliegue ni modificación de Producción.
