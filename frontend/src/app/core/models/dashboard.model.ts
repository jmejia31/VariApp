import { Producto } from './producto.model';

export interface DashboardResumen {
  totalProductos: number;
  totalUnidades: number;
  valorTotalInventario: number;
  valorPotencialVenta: number;
  productosStockBajo: Producto[];
  ultimosAgregados: Producto[];
}
