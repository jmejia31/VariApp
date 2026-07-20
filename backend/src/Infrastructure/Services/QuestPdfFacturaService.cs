using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryApp.Infrastructure.Services;

/// Generación real de PDF con QuestPDF (licencia Community: gratuita para
/// organizaciones pequeñas según sus términos, ver https://questpdf.com/license).
/// NOTA DE VERIFICACIÓN: este código no pudo compilarse en el sandbox de
/// desarrollo (sin acceso a nuget.org para restaurar el paquete). Se escribió
/// contra la API pública documentada de QuestPDF 2024.3.x con cuidado, pero
/// se recomienda revisar el primer `dotnet build` real antes de confiar en
/// que compila sin ajustes.
public class QuestPdfFacturaService : IFacturaPdfService
{
    static QuestPdfFacturaService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerarPdfAsync(FacturaDto factura)
    {
        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(factura.EmpresaLogoUrl))
        {
            logoBytes = await IntentarDescargarLogoAsync(factura.EmpresaLogoUrl);
        }

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        if (logoBytes is not null)
                        {
                            row.ConstantItem(60).Image(logoBytes).FitArea();
                        }

                        row.RelativeItem().Column(empresa =>
                        {
                            empresa.Item().Text(factura.EmpresaNombre).FontSize(16).Bold();
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaEslogan))
                                empresa.Item().Text(factura.EmpresaEslogan).FontSize(8).Italic();
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaRTN))
                                empresa.Item().Text($"RTN: {factura.EmpresaRTN}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaTelefono))
                                empresa.Item().Text($"Tel: {factura.EmpresaTelefono}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaCorreo))
                                empresa.Item().Text(factura.EmpresaCorreo).FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaDireccion))
                                empresa.Item().Text(factura.EmpresaDireccion).FontSize(8);
                        });

                        row.ConstantItem(160).Column(meta =>
                        {
                            meta.Item().AlignRight().Text("FACTURA").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            meta.Item().AlignRight().Text($"No. {factura.NumeroFactura}").FontSize(10).Bold();
                            meta.Item().AlignRight().Text($"Venta: {factura.NumeroVentaOrigen}").FontSize(8);
                            meta.Item().AlignRight().Text($"Fecha: {factura.FechaEmision:dd/MM/yyyy HH:mm}").FontSize(8);
                            if (factura.Estado == "Anulada")
                            {
                                meta.Item().AlignRight().Text("ANULADA").FontSize(12).Bold().FontColor(Colors.Red.Darken2);
                            }
                        });
                    });

                    header.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(12).Column(content =>
                {
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(cliente =>
                        {
                            cliente.Item().Text("Cliente").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            cliente.Item().Text(factura.ClienteNombre).FontSize(10);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteIdentidadORTN))
                                cliente.Item().Text($"Identidad/RTN: {factura.ClienteIdentidadORTN}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteTelefono))
                                cliente.Item().Text($"Tel: {factura.ClienteTelefono}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteCorreo))
                                cliente.Item().Text(factura.ClienteCorreo).FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteDireccion))
                                cliente.Item().Text(factura.ClienteDireccion).FontSize(8);
                        });

                        row.RelativeItem().Column(pago =>
                        {
                            pago.Item().Text("Datos de la operación").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            pago.Item().Text($"Método de pago: {factura.MetodoPago}").FontSize(8);
                            pago.Item().Text($"Estado de pago: {factura.EstadoPago}").FontSize(8);
                            pago.Item().Text($"Atendido por: {factura.VendedorNombreUsuario}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.GeneradaPorNombreUsuario))
                                pago.Item().Text($"Factura generada por: {factura.GeneradaPorNombreUsuario}").FontSize(8);
                        });
                    });

                    content.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(headerRow =>
                        {
                            EncabezadoCelda(headerRow.Cell(), "Producto");
                            EncabezadoCelda(headerRow.Cell(), "Marca");
                            EncabezadoCelda(headerRow.Cell(), "Modelo");
                            EncabezadoCelda(headerRow.Cell(), "Cant.");
                            EncabezadoCelda(headerRow.Cell(), "Precio unit.");
                            EncabezadoCelda(headerRow.Cell(), "Subtotal");
                        });

                        foreach (var d in factura.Detalles)
                        {
                            CeldaTexto(table.Cell(), d.ProductoNombre);
                            CeldaTexto(table.Cell(), d.ProductoMarca);
                            CeldaTexto(table.Cell(), d.ProductoModelo);
                            CeldaTexto(table.Cell(), d.Cantidad.ToString());
                            CeldaTexto(table.Cell(), $"L. {d.PrecioUnitario:N2}");
                            CeldaTexto(table.Cell(), $"L. {d.Subtotal:N2}");
                        }
                    });

                    content.Item().PaddingTop(16).AlignRight().Width(220).Column(totales =>
                    {
                        FilaTotal(totales, "Subtotal", factura.Subtotal, false);
                        if (factura.Descuento > 0) FilaTotal(totales, "Descuento", -factura.Descuento, false);
                        if (factura.Impuesto > 0) FilaTotal(totales, "Impuesto", factura.Impuesto, false);
                        FilaTotal(totales, "Total", factura.Total, true);
                    });

                    if (factura.DescuentosAplicados.Any() || factura.ImpuestosAplicados.Any())
                    {
                        content.Item().PaddingTop(16).Row(row =>
                        {
                            row.RelativeItem().Column(descuentos =>
                            {
                                descuentos.Item().Text("Descuentos aplicados").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                                if (!factura.DescuentosAplicados.Any())
                                    descuentos.Item().Text("Sin descuentos aplicados.").FontSize(8);

                                foreach (var d in factura.DescuentosAplicados)
                                {
                                    var codigo = string.IsNullOrWhiteSpace(d.Codigo) ? string.Empty : $" ({d.Codigo})";
                                    descuentos.Item().Text($"{d.Nombre}{codigo}: L. {d.Monto:N2}").FontSize(8);
                                }
                            });

                            row.RelativeItem().Column(impuestos =>
                            {
                                impuestos.Item().Text("Impuestos aplicados").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                                if (!factura.ImpuestosAplicados.Any())
                                    impuestos.Item().Text("Sin impuestos aplicados.").FontSize(8);

                                foreach (var i in factura.ImpuestosAplicados)
                                    impuestos.Item().Text($"{i.Nombre} {i.Codigo} {i.Tasa:N2}% | Base L. {i.BaseImponible:N2} | Monto L. {i.Monto:N2}").FontSize(8);
                            });
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(factura.Observaciones))
                    {
                        content.Item().PaddingTop(16).Column(obs =>
                        {
                            obs.Item().Text("Observaciones").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            obs.Item().Text(factura.Observaciones).FontSize(8);
                        });
                    }

                    if (factura.Estado == "Anulada")
                    {
                        content.Item().PaddingTop(16).Background(Colors.Red.Lighten4).Padding(8).Column(anulada =>
                        {
                            anulada.Item().Text("Esta factura fue anulada").FontSize(10).Bold().FontColor(Colors.Red.Darken2);
                            if (!string.IsNullOrWhiteSpace(factura.MotivoAnulacion))
                                anulada.Item().Text($"Motivo: {factura.MotivoAnulacion}").FontSize(8);
                            if (factura.FechaAnulacion.HasValue)
                                anulada.Item().Text($"Fecha: {factura.FechaAnulacion:dd/MM/yyyy HH:mm}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.AnuladaPorNombreUsuario))
                                anulada.Item().Text($"Por: {factura.AnuladaPorNombreUsuario}").FontSize(8);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(factura.EmpresaTextoFactura) || !string.IsNullOrWhiteSpace(factura.EmpresaTextoLegal))
                    {
                        content.Item().PaddingTop(16).Column(textos =>
                        {
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaTextoFactura))
                                textos.Item().Text(factura.EmpresaTextoFactura).FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.EmpresaTextoLegal))
                                textos.Item().Text(factura.EmpresaTextoLegal).FontSize(7).FontColor(Colors.Grey.Darken1);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    if (!string.IsNullOrWhiteSpace(factura.EmpresaCopyright))
                        text.Span($"{factura.EmpresaCopyright} ").FontSize(8);
                    else
                        text.Span("Gracias por su preferencia. ").FontSize(8);
                    text.Span("Pagina ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" de ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static void EncabezadoCelda(IContainer contenedor, string texto) =>
        contenedor.Background(Colors.Grey.Lighten3).Padding(4).Text(texto).FontSize(8).Bold();

    private static void CeldaTexto(IContainer contenedor, string texto) =>
        contenedor.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(texto).FontSize(8);

    private static void FilaTotal(ColumnDescriptor columna, string etiqueta, decimal monto, bool esTotal)
    {
        columna.Item().Row(fila =>
        {
            var estiloEtiqueta = TextStyle.Default.FontSize(esTotal ? 10 : 9);
            var estiloMonto = TextStyle.Default.FontSize(esTotal ? 10 : 9);
            if (esTotal)
            {
                estiloEtiqueta = estiloEtiqueta.Bold();
                estiloMonto = estiloMonto.Bold();
            }

            fila.RelativeItem().Text(etiqueta).Style(estiloEtiqueta);
            fila.ConstantItem(90).AlignRight().Text($"L. {monto:N2}").Style(estiloMonto);
        });
    }

    private static async Task<byte[]?> IntentarDescargarLogoAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            var respuesta = await httpClient.GetAsync(url);
            if (!respuesta.IsSuccessStatusCode) return null;
            return await respuesta.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            // Si el logo no se puede descargar, la factura se genera sin él
            // en vez de fallar por completo (sección 12: "define qué ocurre
            // si el registro existe pero el archivo no").
            return null;
        }
    }
}
