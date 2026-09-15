import { Injectable } from '@angular/core';

/** Estados que describen el handoff iniciado por el usuario; nunca delivery/read. */
export const WHATSAPP_HANDOFF_GENERATED = 'WHATSAPP_HANDOFF_GENERATED' as const;
export const WHATSAPP_CLIENT_OPEN_REQUESTED = 'WHATSAPP_CLIENT_OPEN_REQUESTED' as const;
export type WhatsAppHandoffStatus =
  | typeof WHATSAPP_HANDOFF_GENERATED
  | typeof WHATSAPP_CLIENT_OPEN_REQUESTED;

const DEFAULT_COUNTRY_PREFIX = '504';
const INVALID_PHONE = /^[0]+$/;

/**
 * Converts a local or international phone into the digits accepted by wa.me.
 * Honduras is only the documented fallback for this tenant/market; callers can
 * provide another country prefix when their tenant has one configured.
 */
export function normalizarTelefonoWhatsApp(value: string | null | undefined, defaultCountryPrefix = DEFAULT_COUNTRY_PREFIX): string {
  const raw = (value ?? '').trim();
  if (!raw || !/^[+\d\s().-]+$/.test(raw)) return '';

  let digits = raw.replace(/\D/g, '');
  if (digits.startsWith('00')) digits = digits.slice(2);
  const prefix = defaultCountryPrefix.replace(/\D/g, '');
  if (digits.length === 8 && prefix) digits = `${prefix}${digits}`;
  if (prefix && digits.startsWith(`${prefix}${prefix}`) && digits.length === prefix.length * 2 + 8) return '';
  if (INVALID_PHONE.test(digits) || !/^[1-9]\d{9,14}$/.test(digits)) return '';
  return digits;
}

export function enmascararTelefonoWhatsApp(value: string): string {
  const digits = normalizarTelefonoWhatsApp(value);
  return digits ? `${'*'.repeat(Math.max(0, digits.length - 4))}${digits.slice(-4)}` : '';
}

export function construirEnlaceWhatsApp(numero: string, mensaje: string): string {
  const normalizado = normalizarTelefonoWhatsApp(numero);
  const texto = mensaje.trim();
  if (!normalizado || !texto || texto.length > 6000) return '';
  return `https://wa.me/${normalizado}?text=${encodeURIComponent(texto)}`;
}

@Injectable({ providedIn: 'root' })
export class WhatsAppShareService {
  normalizarTelefono(value: string | null | undefined, defaultCountryPrefix = DEFAULT_COUNTRY_PREFIX): string {
    return normalizarTelefonoWhatsApp(value, defaultCountryPrefix);
  }

  construirEnlace(numero: string, mensaje: string): string {
    return construirEnlaceWhatsApp(numero, mensaje);
  }

  /** Opens the official client/web URL and reports whether the browser accepted it. */
  abrir(numero: string, mensaje: string): boolean {
    const url = this.construirEnlace(numero, mensaje);
    if (!url) return false;
    return Boolean(window.open(url, '_blank', 'noopener,noreferrer'));
  }
}
