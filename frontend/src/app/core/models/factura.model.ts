import { DescuentoAplicado, ImpuestoAplicado } from './venta.model';

export type FacturaFormatoCodigo = 'a4' | 'carta' | 'legal' | 'oficio' | 'a5' | 'pos58' | 'pos80';

export interface FacturaFormatoPdf {
  codigo: FacturaFormatoCodigo;
  nombre: string;
  descripcion: string;
  anchoMm: number;
  altoMm?: number;
  esContinuo: boolean;
  usoRecomendado: string;
}

export interface FacturaDetalle {
  productoNombre: string;
  productoMarca: string;
  productoModelo: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  subtotal: number;
}

export interface Factura {
  id: number;
  ventaId: number;
  numeroVentaOrigen: string;
  numeroFactura: string;
  fechaEmision: string;
  estado: 'Emitida' | 'Anulada';
  empresaNombre: string;
  empresaRTN?: string;
  empresaTelefono?: string;
  empresaCorreo?: string;
  empresaDireccion?: string;
  empresaEslogan?: string;
  empresaTextoFactura?: string;
  empresaTextoLegal?: string;
  empresaCopyright?: string;
  empresaLogoUrl?: string;
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  vendedorNombreUsuario: string;
  generadaPorNombreUsuario?: string;
  importeBruto: number;
  subtotal: number;
  descuento: number;
  impuesto: number;
  impuestoIncluido: number;
  impuestoAdicional: number;
  total: number;
  metodoPago: string;
  estadoPago: string;
  observaciones?: string;
  detalles: FacturaDetalle[];
  descuentosAplicados: DescuentoAplicado[];
  impuestosAplicados: ImpuestoAplicado[];
  fechaAnulacion?: string;
  anuladaPorNombreUsuario?: string;
  motivoAnulacion?: string;
}

export interface EnlaceCompartir {
  urlPdfPublica: string;
  fechaExpiracion: string;
  mensajeWhatsApp: string;
  telefonoSugerido: string;
}

export interface HistorialEnvio {
  id: number;
  canal: 'WhatsApp' | 'Correo';
  destinatario: string;
  resultado: string;
  error?: string;
  usuarioNombre?: string;
  fecha: string;
}

export interface ResultadoEnvioCorreo {
  exito: boolean;
  mensaje: string;
  codigo: string;
  esTransitorio: boolean;
  yaProcesado: boolean;
  intentos: number;
  messageId?: string;
}

export interface EstadoConfiguracionSmtp {
  configurado: boolean;
  host: string;
  puerto: number;
  usaTls: boolean;
  requiereAutenticacion: boolean;
  remitenteEnmascarado: string;
  maximoIntentos: number;
  timeoutSegundos: number;
  mensaje: string;
}
