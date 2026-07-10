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
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  vendedorNombreUsuario: string;
  generadaPorNombreUsuario?: string;
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  metodoPago: string;
  estadoPago: string;
  observaciones?: string;
  detalles: FacturaDetalle[];
  fechaAnulacion?: string;
  anuladaPorNombreUsuario?: string;
  motivoAnulacion?: string;
}
