using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryApp.Infrastructure.Services;

/// Genera el único PDF oficial de la aplicación. El mismo documento se usa
/// para descarga, impresión, WhatsApp y correo, evitando formatos distintos
/// según el canal.
public class QuestPdfFacturaService : IFacturaPdfService
{
    private const string LogoPublicoPredeterminado =
        "https://varistorehn.vercel.app/assets/varistorehn-logo.png";

    private readonly IConfiguration _configuration;
    private readonly ILogger<QuestPdfFacturaService> _logger;

    static QuestPdfFacturaService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public QuestPdfFacturaService(
        IConfiguration configuration,
        ILogger<QuestPdfFacturaService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]> GenerarPdfAsync(FacturaDto factura)
    {
        var logoBytes = await ObtenerLogoAsync(factura.EmpresaLogoUrl);
        var colorPrimario = Colors.Blue.Darken2;
        var colorAcento = Colors.Orange.Darken1;
        var grisFondo = Colors.Grey.Lighten4;

        // Estas cifras provienen del snapshot fiscal de la venta. El PDF no
        // vuelve a calcular impuestos ni descuentos y, por tanto, coincide con
        // la pantalla, WhatsApp y el adjunto enviado por correo.
        var importeBruto = factura.ImporteBruto > 0
            ? factura.ImporteBruto
            : factura.Detalles.Sum(d => d.Subtotal);
        var subtotalNeto = Math.Max(0, factura.Subtotal);
        var impuestoIncluido = Math.Max(0, factura.ImpuestoIncluido);
        var impuestoAdicional = Math.Max(0, factura.ImpuestoAdicional);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.35f, Unit.Centimetre);
                page.MarginVertical(1.15f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Row(empresaRow =>
                        {
                            empresaRow.ConstantItem(74).Height(74).Element(c => DibujarLogo(c, logoBytes, colorPrimario, colorAcento));
                            empresaRow.RelativeItem().PaddingLeft(12).AlignMiddle().Column(empresa =>
                            {
                                empresa.Item().Text(factura.EmpresaNombre).FontSize(19).Bold().FontColor(colorPrimario);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaEslogan))
                                    empresa.Item().Text(factura.EmpresaEslogan).FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                                empresa.Item().PaddingTop(3).Text("Comprobante comercial interno").FontSize(8).SemiBold().FontColor(colorAcento);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaTelefono))
                                    empresa.Item().Text($"Teléfono: {factura.EmpresaTelefono}").FontSize(8);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaCorreo))
                                    empresa.Item().Text(factura.EmpresaCorreo).FontSize(8);
                            });
                        });

                        row.ConstantItem(190).AlignMiddle().Column(meta =>
                        {
                            meta.Item().AlignRight().Text("FACTURA").FontSize(18).Bold().FontColor(colorPrimario);
                            meta.Item().AlignRight().Text(factura.NumeroFactura).FontSize(15).Bold().FontColor(colorAcento);
                            meta.Item().AlignRight().Text($"Fecha: {factura.FechaEmision:dd/MM/yyyy HH:mm}").FontSize(8);
                            meta.Item().AlignRight().Text($"Venta origen: {factura.NumeroVentaOrigen}").FontSize(8);
                            meta.Item().PaddingTop(4).AlignRight().Text(factura.Estado.ToUpperInvariant())
                                .FontSize(9).Bold()
                                .FontColor(factura.Estado == "Anulada" ? Colors.Red.Darken2 : Colors.Green.Darken2);
                        });
                    });

                    header.Item().PaddingTop(10).LineHorizontal(2).LineColor(colorPrimario);
                });

                page.Content().PaddingVertical(12).Column(content =>
                {
                    content.Spacing(10);

                    if (factura.Estado == "Anulada")
                    {
                        content.Item().Background(Colors.Red.Lighten4).Border(1).BorderColor(Colors.Red.Lighten1)
                            .Padding(8).AlignCenter().Text("DOCUMENTO ANULADO").FontSize(13).Bold().FontColor(Colors.Red.Darken2);
                    }

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Background(grisFondo).Padding(10).Column(cliente =>
                        {
                            cliente.Item().Text("CLIENTE").FontSize(8).Bold().FontColor(colorPrimario);
                            cliente.Item().PaddingTop(3).Text(factura.ClienteNombre).FontSize(11).SemiBold();
                            if (!string.IsNullOrWhiteSpace(factura.ClienteIdentidadORTN))
                                cliente.Item().Text($"Identidad/RTN: {factura.ClienteIdentidadORTN}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteTelefono))
                                cliente.Item().Text($"Teléfono: {factura.ClienteTelefono}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteCorreo))
                                cliente.Item().Text(factura.ClienteCorreo).FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteDireccion))
                                cliente.Item().Text(factura.ClienteDireccion).FontSize(8);
                        });

                        row.ConstantItem(10);

                        row.RelativeItem().Background(grisFondo).Padding(10).Column(pago =>
                        {
                            pago.Item().Text("DATOS DE LA OPERACIÓN").FontSize(8).Bold().FontColor(colorPrimario);
                            pago.Item().PaddingTop(3).Text($"Método de pago: {factura.MetodoPago}").FontSize(8);
                            pago.Item().Text($"Estado de pago: {factura.EstadoPago}").FontSize(8);
                            pago.Item().Text($"Atendido por: {factura.VendedorNombreUsuario}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.GeneradaPorNombreUsuario))
                                pago.Item().Text($"Generada por: {factura.GeneradaPorNombreUsuario}").FontSize(8);
                        });
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(34);
                            columns.RelativeColumn(3.1f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(1.35f);
                            columns.RelativeColumn(1.35f);
                        });

                        table.Header(headerRow =>
                        {
                            EncabezadoCelda(headerRow.Cell(), "Cant.", colorPrimario);
                            EncabezadoCelda(headerRow.Cell(), "Producto", colorPrimario);
                            EncabezadoCelda(headerRow.Cell(), "Marca", colorPrimario);
                            EncabezadoCelda(headerRow.Cell(), "Modelo", colorPrimario);
                            EncabezadoCelda(headerRow.Cell(), "Precio unit.", colorPrimario);
                            EncabezadoCelda(headerRow.Cell(), "Importe", colorPrimario);
                        });

                        foreach (var d in factura.Detalles)
                        {
                            CeldaTexto(table.Cell(), d.Cantidad.ToString(), true);
                            CeldaTexto(table.Cell(), d.ProductoNombre);
                            CeldaTexto(table.Cell(), d.ProductoMarca);
                            CeldaTexto(table.Cell(), d.ProductoModelo);
                            CeldaTexto(table.Cell(), $"L. {d.PrecioUnitario:N2}", true);
                            CeldaTexto(table.Cell(), $"L. {d.Subtotal:N2}", true);
                        }
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(detalleFiscal =>
                        {
                            if (factura.DescuentosAplicados.Any())
                            {
                                detalleFiscal.Item().Text("DESCUENTOS APLICADOS").FontSize(8).Bold().FontColor(colorPrimario);
                                foreach (var d in factura.DescuentosAplicados)
                                {
                                    var codigo = string.IsNullOrWhiteSpace(d.Codigo) ? string.Empty : $" ({d.Codigo})";
                                    detalleFiscal.Item().Text($"• {d.Nombre}{codigo}: - L. {d.Monto:N2}").FontSize(8);
                                }
                            }

                            if (factura.ImpuestosAplicados.Any())
                            {
                                detalleFiscal.Item().PaddingTop(6).Text("IMPUESTOS APLICADOS").FontSize(8).Bold().FontColor(colorPrimario);
                                foreach (var i in factura.ImpuestosAplicados)
                                {
                                    detalleFiscal.Item().Text(
                                        $"• {i.Nombre} ({i.Tasa:N2}%): L. {i.Monto:N2} {(i.IncluidoEnPrecio ? "incluido" : "adicional")}")
                                        .FontSize(8);
                                }
                            }
                        });

                        row.ConstantItem(14);

                        row.ConstantItem(245).Background(grisFondo).Padding(10).Column(totales =>
                        {
                            FilaTotal(totales, "Importe bruto", importeBruto, false);
                            if (factura.Descuento > 0)
                                FilaTotal(totales, "Descuento", -factura.Descuento, false, Colors.Green.Darken2);
                            FilaTotal(totales, "Subtotal sin impuesto", subtotalNeto, false);
                            if (impuestoIncluido > 0)
                                FilaTotal(totales, "Impuesto incluido", impuestoIncluido, false);
                            if (impuestoAdicional > 0)
                                FilaTotal(totales, "Impuesto adicional", impuestoAdicional, false);
                            FilaTotal(totales, "TOTAL A PAGAR", factura.Total, true, colorPrimario);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(factura.Observaciones))
                    {
                        content.Item().Background(Colors.Grey.Lighten5).Padding(8).Column(obs =>
                        {
                            obs.Item().Text("OBSERVACIONES").FontSize(8).Bold().FontColor(colorPrimario);
                            obs.Item().Text(factura.Observaciones).FontSize(8);
                        });
                    }

                    if (factura.Estado == "Anulada")
                    {
                        content.Item().Background(Colors.Red.Lighten4).Padding(8).Column(anulada =>
                        {
                            anulada.Item().Text("INFORMACIÓN DE ANULACIÓN").FontSize(9).Bold().FontColor(Colors.Red.Darken2);
                            if (!string.IsNullOrWhiteSpace(factura.MotivoAnulacion))
                                anulada.Item().Text($"Motivo: {factura.MotivoAnulacion}").FontSize(8);
                            if (factura.FechaAnulacion.HasValue)
                                anulada.Item().Text($"Fecha: {factura.FechaAnulacion:dd/MM/yyyy HH:mm}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(factura.AnuladaPorNombreUsuario))
                                anulada.Item().Text($"Por: {factura.AnuladaPorNombreUsuario}").FontSize(8);
                        });
                    }

                    content.Item().PaddingTop(5).AlignCenter().Column(textos =>
                    {
                        textos.Item().Text(factura.EmpresaTextoFactura ?? "Gracias por su compra.")
                            .FontSize(9).SemiBold().FontColor(colorPrimario);
                        textos.Item().Text(
                            "Documento comercial interno de VariStorehn. No constituye comprobante fiscal autorizado por el SAR.")
                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(factura.EmpresaTextoLegal))
                            textos.Item().Text(factura.EmpresaTextoLegal).FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text(factura.EmpresaCopyright ?? "© VariStorehn. Todos los derechos reservados.").FontSize(7);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" de ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static void DibujarLogo(IContainer contenedor, byte[]? logoBytes, string colorPrimario, string colorAcento)
    {
        if (logoBytes is not null && logoBytes.Length > 0)
        {
            contenedor.Image(logoBytes).FitArea();
            return;
        }

        contenedor.Background(colorPrimario).Border(3).BorderColor(colorAcento)
            .AlignCenter().AlignMiddle().Text("VS").FontSize(24).Bold().FontColor(Colors.White);
    }

    private static void EncabezadoCelda(IContainer contenedor, string texto, string color) =>
        contenedor.Background(color).PaddingVertical(6).PaddingHorizontal(4)
            .Text(texto).FontSize(7.5f).Bold().FontColor(Colors.White);

    private static void CeldaTexto(IContainer contenedor, string texto, bool alinearDerecha = false)
    {
        var celda = contenedor.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(4);
        if (alinearDerecha)
            celda.AlignRight().Text(texto).FontSize(8);
        else
            celda.Text(texto).FontSize(8);
    }

    private static void FilaTotal(
        ColumnDescriptor columna,
        string etiqueta,
        decimal monto,
        bool esTotal,
        string? color = null)
    {
        columna.Item().PaddingVertical(esTotal ? 5 : 2).Row(fila =>
        {
            var estiloEtiqueta = TextStyle.Default.FontSize(esTotal ? 11 : 8.5f);
            var estiloMonto = TextStyle.Default.FontSize(esTotal ? 12 : 8.5f);
            if (esTotal)
            {
                estiloEtiqueta = estiloEtiqueta.Bold();
                estiloMonto = estiloMonto.Bold();
            }
            if (!string.IsNullOrWhiteSpace(color))
            {
                estiloEtiqueta = estiloEtiqueta.FontColor(color);
                estiloMonto = estiloMonto.FontColor(color);
            }

            fila.RelativeItem().Text(etiqueta).Style(estiloEtiqueta);
            fila.ConstantItem(100).AlignRight().Text($"L. {monto:N2}").Style(estiloMonto);
        });
    }

    private async Task<byte[]?> ObtenerLogoAsync(string? logoConfigurado)
    {
        var candidatos = new[]
        {
            logoConfigurado,
            _configuration["AppSettings:LogoPublicUrl"],
            LogoPublicoPredeterminado
        }
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Select(url => url!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var url in candidatos)
        {
            var bytes = await IntentarDescargarLogoAsync(url);
            if (bytes is { Length: > 0 }) return bytes;
        }

        _logger.LogWarning("No fue posible descargar el logo de VariStorehn desde ninguna URL configurada.");
        return null;
    }

    private async Task<byte[]?> IntentarDescargarLogoAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.IsLoopback ||
            uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("URL de logo rechazada por seguridad: {Url}", url);
            return null;
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var respuesta = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Logo no disponible en {Url}. HTTP {StatusCode}", uri, (int)respuesta.StatusCode);
                return null;
            }

            var contentType = respuesta.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("La URL de logo {Url} respondió con tipo {ContentType}.", uri, contentType);
                return null;
            }

            var longitud = respuesta.Content.Headers.ContentLength;
            if (longitud.HasValue && longitud.Value > 5 * 1024 * 1024)
            {
                _logger.LogWarning("El logo en {Url} supera el límite de 5 MB.", uri);
                return null;
            }

            var bytes = await respuesta.Content.ReadAsByteArrayAsync();
            return bytes.Length <= 5 * 1024 * 1024 ? bytes : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo descargar el logo desde {Url}.", uri);
            return null;
        }
    }
}