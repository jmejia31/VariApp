export interface ProductoImagen {
  id: number;
  url: string;
  orden: number;
  esPrincipal: boolean;
}

export interface ProductoVariante {
  id: number;
  productoId: number;
  productoNombre: string;
  colorId: number;
  colorNombre: string;
  colorCodigoVisual?: string;
  sku: string;
  codigoBarras?: string;
  cantidad: number;
  umbralStockBajo: number;
  costo: number;
  precio: number;
  activo: boolean;
  tieneStockBajo: boolean;
  estaAgotada: boolean;
  estadoInventario: string;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface ProductoVarianteFormValue {
  id?: number;
  colorId: number;
  sku?: string;
  codigoBarras?: string;
  cantidad: number;
  umbralStockBajo: number;
  costo: number;
  precio: number;
  activo?: boolean;
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
  precioMinimo: number;
  precioMaximo: number;
  umbralStockBajo: number;
  tieneStockBajo: boolean;
  estaAgotado: boolean;
  estadoInventario: string;
  activo: boolean;
  categoriaId?: number;
  categoriaNombre?: string;
  colorId?: number;
  colorNombre?: string;
  colorCodigoVisual?: string;
  tallaId?: number;
  tallaNombre?: string;
  marcaId?: number;
  marcaNombre?: string;
  modeloId?: number;
  modeloNombre?: string;
  imagenPrincipalUrl?: string;
  imagenes: ProductoImagen[];
  totalImagenes: number;
  variantes: ProductoVariante[];
  totalVariantes: number;
  usaVariantes: boolean;
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
  colorId?: number | null;
  tallaId?: number | null;
  marcaId?: number | null;
  modeloId?: number | null;
  variantes: ProductoVarianteFormValue[];
  imagenesNuevas?: File[];
  imagenesAEliminarIds?: number[];
  imagenPrincipalId?: number | null;
}


export interface AjusteStockRequest {
  cantidadActualEsperada: number;
  cantidadNueva: number;
  motivo: string;
}

export interface AjusteStockResultado {
  productoId: number;
  productoVarianteId?: number;
  cantidadAnterior: number;
  cantidadNueva: number;
  diferencia: number;
  motivo: string;
}
