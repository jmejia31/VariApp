export interface VentaDetalle {
  id: number;
  productoId: number;
  productoNombre: string;
  productoMarca: string;
  productoModelo: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  utilidadBruta: number;
}

export interface Venta {
  id: number;
  numeroVenta: string;
  fecha: string;
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  estado: 'Borrador' | 'Confirmada' | 'Anulada';
  estadoPago: 'Pendiente' | 'Pagado' | 'Parcial';
  metodoPago: 'Efectivo' | 'Transferencia' | 'Tarjeta' | 'Otro';
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  costoTotal: number;
  utilidadBruta: number;
  notas?: string;
  detalles: VentaDetalle[];
  facturaId?: number;
  numeroFactura?: string;
  creadoPorNombreUsuario?: string;
  fechaCreacion: string;
  confirmadoPorNombreUsuario?: string;
  fechaConfirmacion?: string;
  anuladoPorNombreUsuario?: string;
  fechaAnulacion?: string;
  motivoAnulacion?: string;
}

export interface VentaDetalleInput {
  productoId: number;
  cantidad: number;
  precioUnitario: number;
}

export interface VentaFormValue {
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  metodoPago: string;
  estadoPago: string;
  descuento: number;
  impuesto: number;
  notas?: string;
  detalles: VentaDetalleInput[];
}
