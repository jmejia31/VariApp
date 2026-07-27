# FASE 6 — Certificación de facturación e impresión

Fecha de cierre técnico: 27 de julio de 2026.

Rama modificada y certificada: `Desarrollo`.

Producción permaneció congelada. No se modificaron `main`, variables, credenciales, dominios, servicios, bases, activos, migraciones ni despliegues productivos.

## 1. Objetivo

La Fase 6 implementó perfiles explícitos de generación, descarga, previsualización e impresión de facturas para:

- A4;
- Carta;
- Legal;
- Oficio;
- A5;
- POS 58 mm;
- POS 80 mm.

La solución cubre impresoras convencionales de oficina y perfiles térmicos continuos utilizados por equipos POS, móviles, handheld e industriales.

## 2. Principio fiscal y compatibilidad

El cambio de papel no modifica la factura persistida ni recalcula importes. Todos los formatos consumen el mismo snapshot fiscal de la venta confirmada:

- empresa y cliente;
- fecha y número de factura;
- vendedor y método de pago;
- productos, cantidades y precios;
- descuentos;
- impuestos incluidos y adicionales;
- subtotal y total;
- observaciones y anulación.

A4 se conserva como documento oficial predeterminado para:

- correo electrónico;
- WhatsApp;
- enlaces públicos;
- consumidores anteriores del método `GenerarPdfAsync(FacturaDto)`;
- solicitudes al endpoint PDF que omitan el parámetro de formato.

Esto evita alterar enlaces existentes, adjuntos de correo o integraciones anteriores.

## 3. Catálogo de perfiles

Se añadió:

```text
backend/src/Application/DTOs/FacturaFormatoPdf.cs
```

| Código | Perfil | Dimensión física | Tipo |
|---|---|---:|---|
| `a4` | A4 | 210 × 297 mm | Página fija |
| `carta` | Carta | 215.9 × 279.4 mm | Página fija |
| `legal` | Legal | 215.9 × 355.6 mm | Página fija |
| `oficio` | Oficio | 215.9 × 330.2 mm | Página fija |
| `a5` | A5 | 148 × 210 mm | Página fija compacta |
| `pos58` | POS 58 mm | 58 mm de ancho | Rollo continuo |
| `pos80` | POS 80 mm | 80 mm de ancho | Rollo continuo |

El parser acepta alias controlados como `letter`, `58 mm`, `ticket-80` y `folio`. Cualquier valor desconocido devuelve HTTP 400 con los códigos permitidos.

## 4. Generador QuestPDF

Se añadió:

```text
backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs
```

La implementación anterior conserva su nombre de registro y delega al nuevo generador para mantener compatibilidad con la inyección de dependencias.

### Papeles de página fija

A4, Carta, Legal y Oficio usan una composición corporativa completa con:

- logo o monograma seguro de respaldo;
- identificación de la empresa;
- metadatos de factura;
- cliente y operación;
- tabla detallada;
- desglose fiscal;
- totales;
- observaciones;
- información de anulación;
- textos legales;
- paginación.

A5 utiliza tipografía y columnas compactas. Marca y modelo se integran en la descripción del producto para conservar legibilidad sin perder información.

### Perfiles térmicos

POS 58 y POS 80 usan páginas continuas con:

- ancho físico exacto;
- altura calculada por el contenido;
- estructura vertical;
- productos con clasificación completa;
- importes alineados;
- descuentos e impuestos;
- total destacado;
- observaciones y datos legales.

POS 58 reduce columnas y tamaño tipográfico respecto de POS 80, pero conserva todo el contenido necesario.

## 5. API

Se añadió:

```text
GET /facturas/formatos-pdf
GET /facturas/{id}/pdf?formato=a4|carta|legal|oficio|a5|pos58|pos80
```

La descarga incluye:

- `Content-Type: application/pdf`;
- nombre de archivo con el perfil solicitado;
- encabezado `X-Factura-Formato`;
- encabezados privados de no caché;
- auditoría del formato exportado.

Las descargas públicas continúan protegidas por token, expiración, revocación y límite de accesos, y permanecen en A4.

## 6. Interfaz

La vista de factura incorpora un panel explícito de perfil de impresión con:

- selector de los siete formatos;
- dimensiones físicas;
- tipo de página o rollo;
- uso recomendado;
- persistencia local de la preferencia;
- aviso de que correo, WhatsApp y enlaces públicos usan A4.

Los botones indican el formato que se descargará o abrirá para imprimir. La impresión abre el PDF real generado por el backend en el visor del navegador, evitando que el HTML de pantalla dependa de reglas CSS o de la detección automática de la impresora.

La vista previa mantiene proporciones diferenciadas para A5, POS 58 y POS 80. En teléfonos, los papeles grandes usan desplazamiento dentro del área de previsualización y no producen desbordamiento horizontal del documento.

## 7. Pruebas backend

Se añadió:

```text
backend/tests/InventoryApp.Tests/QuestPdfFacturaPerfilesServiceTests.cs
```

Las pruebas verifican:

- firma `%PDF`;
- tamaño mínimo del documento;
- `MediaBox` físico de A4, Carta, Legal, Oficio y A5;
- ancho exacto y altura continua de POS 58 y POS 80;
- A4 como formato predeterminado;
- alias válidos;
- rechazo de formatos desconocidos;
- existencia de los siete perfiles.

## 8. Aceptación end-to-end

Se añadió:

```text
frontend/e2e/fase6-facturacion-impresion.spec.ts
```

La prueba crea productos y una venta en MySQL descartable, confirma la venta y certifica la factura resultante.

Comprueba:

1. catálogo completo de perfiles;
2. generación real de los siete PDFs;
3. firma PDF y encabezados HTTP;
4. dimensiones físicas mediante `MediaBox`;
5. formato inválido con HTTP 400;
6. selector y vista previa de los siete perfiles;
7. ausencia de desbordamiento del documento;
8. POS 58 en teléfono;
9. descarga del perfil seleccionado;
10. apertura del perfil seleccionado para impresión.

## 9. Evidencia

Artefacto final:

```text
desarrollo-aceptacion-integral
artifact id: 8664760725
SHA-256: f7e2ca67e311f43f9e13fedcce57fc43cb9b93da314827587226de922694b640
```

### PDFs incluidos

```text
FAC-000001-a4.pdf
FAC-000001-carta.pdf
FAC-000001-legal.pdf
FAC-000001-oficio.pdf
FAC-000001-a5.pdf
FAC-000001-pos58.pdf
FAC-000001-pos80.pdf
```

### Capturas incluidas

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

Commit documental de cierre:

```text
693559785c05172a1d354e51866445c420603366
```

Ejecuciones sobre el commit funcional:

- `Desarrollo - Compilación y pruebas`, run `30295557180`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30295557155`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30295557157`: **success**.

Resultado Playwright:

```text
51 pruebas totales
51 aprobadas
0 fallos
0 errores
0 omitidas
```

La prueba específica de Fase 6 ejecutó cuatro casos y los cuatro terminaron correctamente.

## 11. Validaciones físicas pendientes

La certificación automatizada demuestra dimensiones, contenido, descarga e integración de navegador, pero no puede certificar mecánica, drivers o calibración de cada impresora física.

Antes de una adopción operativa general deben probarse exclusivamente en `varistorehn_desarrollo`:

- impresora de oficina con A4, Carta, Legal, Oficio y A5;
- impresora térmica real de 58 mm;
- impresora térmica real de 80 mm;
- conexión USB, red y Bluetooth cuando corresponda;
- diálogo de impresión en Chrome, Edge, Android e iOS según dispositivos disponibles;
- márgenes no imprimibles de cada modelo;
- densidad, velocidad, corte automático y avance de papel;
- escalado configurado en 100 %, sin “ajustar a página” para POS;
- rollos de ancho real compatible con el área imprimible del equipo.

Estas pruebas pueden producir recomendaciones específicas por modelo, pero no justifican modificar automáticamente los perfiles certificados para todos los dispositivos.

## 12. Criterio de cierre

La Fase 6 queda completa y certificada en código, API, generación PDF, interfaz, pruebas backend, aceptación end-to-end y evidencia visual.

La siguiente etapa es la Fase 7 — Envío de correo.

Completar esta fase no autoriza merge, despliegue ni modificación de Producción.
