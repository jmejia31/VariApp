export interface ProductoImagen {
  id: number;
  url: string;
  orden: number;
  esPrincipal: boolean;
}

export interface Producto {
  id: number;
  nombre: string;
  marca: string;
  modelo: string;
  descripcion?: string;
  cantidad: number;
  costo: number;
  precio: number;
  umbralStockBajo: number;
  tieneStockBajo: boolean;
  activo: boolean;
  categoriaId?: number;
  categoriaNombre?: string;
  imagenPrincipalUrl?: string;
  imagenes: ProductoImagen[];
  totalImagenes: number;
  creadoPorNombreUsuario?: string;
  actualizadoPorNombreUsuario?: string;
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
  categoriaId?: number | null;
  imagenesNuevas?: File[];
  imagenesAEliminarIds?: number[];
  imagenPrincipalId?: number | null;
}
