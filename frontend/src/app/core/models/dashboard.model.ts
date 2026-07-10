import { Producto } from './producto.model';

export interface CompraResumen {
  numeroCompra: string;
  proveedorNombre: string;
  total: number;
  estado: string;
  fecha: string;
}

export interface VentaResumen {
  numeroVenta: string;
  clienteNombre: string;
  total: number;
  estado: string;
  fecha: string;
}

export interface DashboardResumen {
  totalProductos: number;
  totalUnidades: number;
  valorTotalInventario: number;
  valorPotencialVenta: number;
  productosStockBajo: Producto[];
  ultimosAgregados: Producto[];

  comprasDelMes: number;
  ventasDelMes: number;
  ultimasCompras: CompraResumen[];
  ultimasVentas: VentaResumen[];

  ingresosDelMes: number;
  utilidadBruta: number;
  balanceOperativo: number;
  cuentasPorCobrar: number;
  cuentasPorPagar: number;

  ultimaVentaPor?: string;
  ultimaCompraPor?: string;
  ultimaRevisionFinancieraPor?: string;
  ultimoProductoRegistradoPor?: string;
}
