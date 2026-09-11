import { CheckoutLineaValidada, DatosCompradorCheckout } from './varistorehn.models';

export function urlCheckoutPermitida(url: string, origenesPermitidos: readonly string[]): string {
  try {
    const destino = new URL(url);
    const permitidos = new Set(origenesPermitidos.map(origen => {
      try { return new URL(origen).origin; } catch { return ''; }
    }).filter(Boolean));
    return destino.protocol === 'https:' && permitidos.has(destino.origin) ? destino.toString() : '';
  } catch {
    return '';
  }
}

export function normalizarDatosComprador(datos: DatosCompradorCheckout): DatosCompradorCheckout {
  return {
    nombre: datos.nombre.trim().replace(/\s+/g, ' ').slice(0, 120),
    telefono: datos.telefono.replace(/[^0-9+]/g, '').slice(0, 24),
    correo: datos.correo?.trim().toLowerCase().slice(0, 160) || undefined,
    notas: datos.notas?.trim().slice(0, 600) || undefined
  };
}

export function mensajeWhatsappCheckout(
  comercio: string,
  comprador: DatosCompradorCheckout,
  referencia: string,
  moneda: string,
  lineas: readonly CheckoutLineaValidada[],
  total: number
): string {
  const formato = new Intl.NumberFormat('es-HN', { style: 'currency', currency: moneda || 'HNL' });
  const detalle = lineas.map(linea => {
    const variante = linea.modelo ? ` — ${linea.modelo}` : '';
    const sku = linea.sku ? ` [${linea.sku}]` : '';
    return `• ${linea.unidades} × ${linea.nombre}${variante}${sku}: ${formato.format(linea.total)}`;
  }).join('\n');
  const contacto = comprador.telefono ? `\nTeléfono: ${comprador.telefono}` : '';
  const correo = comprador.correo ? `\nCorreo: ${comprador.correo}` : '';
  const notas = comprador.notas ? `\nNotas: ${comprador.notas}` : '';

  return `Hola ${comercio || 'VariStoreHN'}, quiero solicitar esta compra.\nReferencia: ${referencia}\nCliente: ${comprador.nombre}${contacto}${correo}\n\n${detalle}\n\nTotal validado: ${formato.format(total)}${notas}`;
}
