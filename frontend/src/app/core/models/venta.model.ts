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

export interface DescuentoAplicado {
  descuentoId: number;
  nombre: string;
  codigo?: string;
  tipo: string;
  valor: number;
  monto: number;
}

export interface ImpuestoAplicado {
  impuestoId: number;
  nombre: string;
  tasa: number;
  baseImponible: number;
  monto: number;
  incluidoEnPrecio?: boolean;
}

export interface ResultadoCalculo {
  subtotal: number;
  descuentosAplicados: DescuentoAplicado[];
  totalDescuento: number;
  impuestosAplicados: ImpuestoAplicado[];
  totalImpuesto: number;
  total: number;
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
  descuentosAplicados: DescuentoAplicado[];
  impuestosAplicados: ImpuestoAplicado[];
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
  /** Se envían en 0 por compatibilidad; el backend los ignora y recalcula. */
  descuento: number;
  impuesto: number;
  codigoPromocional?: string;
  notas?: string;
  detalles: VentaDetalleInput[];
}
