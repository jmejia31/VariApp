export interface Producto {
  id: number;
  nombre: string;
  marca: string;
  modelo: string;
  descripcion?: string;
  cantidad: number;
  costo: number;
  precio: number;
  imagenUrl?: string;
  umbralStockBajo: number;
  tieneStockBajo: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface ProductoFormValue {
  nombre: string;
  marca: string;
  modelo: string;
  descripcion?: string;
  cantidad: number;
  costo: number;
  precio: number;
  umbralStockBajo: number;
  imagen?: File | null;
  eliminarImagen?: boolean;
}
