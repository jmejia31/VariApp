import { DescuentoAplicado, ImpuestoAplicado } from './venta.model';

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
