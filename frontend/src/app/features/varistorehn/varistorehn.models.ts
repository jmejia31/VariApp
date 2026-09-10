/**
 * Contratos canonicos del escaparate publico de VariStoreHn.
 * Independientes de componentes Angular y de modelos administrativos.
 */
export interface ImagenCatalogo {
  url: string;
  orden: number;
  esPrincipal: boolean;
}

export interface ModeloCatalogoPublico {
  modeloId?: number;
  modeloNombre?: string;
  marcaNombre?: string;
  sku?: string | null;
  precio: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  imagenes: ImagenCatalogo[];
}

/** Frontera HTTP publica. Los campos comerciales reservados no se inventan. */
export interface ProductoCatalogoPublico {
  id: number;
  slug?: string;
  nombre: string;
  descripcion?: string;
  categoriaId?: number | null;
  categoriaNombre?: string;
  marcaNombre?: string;
  modeloNombre?: string;
  precio: number;
  precioOferta?: number | null;
  cantidadDisponible: number;
  estaAgotado: boolean;
  sku?: string | null;
  activo?: boolean;
  esDestacado?: boolean;
  fechaCreacion?: string;
  imagenPrincipalUrl?: string;
  imagenes: ImagenCatalogo[];
  modelos: ModeloCatalogoPublico[];
}

export interface CategoriaCatalogoPublico {
  id: number;
  slug: string;
  nombre: string;
  descripcion?: string | null;
  /** Null significa que la fuente publica aun no calculo el conteo; nunca equivale a cero. */
  totalProductos: number | null;
}

export interface ModeloTienda {
  clave: string;
  modeloId: number | null;
  nombre: string;
  marca: string;
  sku: string;
  precio: number;
  stock: number;
  disponible: boolean;
  imagenes: string[];
}

/** Modelo unico que deben consumir todas las paginas publicas de producto. */
export interface ProductoTienda {
  id: number;
  slug: string;
  nombre: string;
  descripcion: string;
  categoriaId: number | null;
  categoria: string;
  marca: string;
  sku: string;
  precio: number;
  precioOferta: number | null;
  stock: number;
  disponible: boolean;
  activo: boolean;
  destacado: boolean;
  fechaCreacion: string | null;
  imagenes: string[];
  modelos: ModeloTienda[];
  ilustracion?: string;
}

/** Modelo unico que deben consumir todas las paginas publicas de categoria. */
export interface CategoriaTienda {
  id: number;
  nombre: string;
  slug: string;
  descripcion: string;
  imagenUrl?: string;
  /** Null preserva la diferencia entre 'sin productos' y 'conteo no disponible'. */
  cantidadProductos: number | null;
}

export interface ItemCarrito {
  clave: string;
  productoId: number;
  modeloClave: string;
  modeloId: number | null;
  nombre: string;
  modelo: string;
  precio: number;
  stock: number;
  unidades: number;
  imagen: string;
  ilustracion: string;
}

export interface ReferenciaCarrito {
  productoId: number;
  modeloClave: string;
  unidades: number;
}

export type OrdenCatalogo = 'destacados' | 'precio-asc' | 'precio-desc' | 'nombre';

export interface FiltrosCatalogo {
  busqueda: string;
  categoria: string;
  soloDisponibles: boolean;
  precioMaximo: number | null;
  orden: OrdenCatalogo;
}

export type EstadoConsultaPublica = 'loading' | 'empty' | 'error' | 'success';
export type EstadoDisponibilidad = 'available' | 'lowStock' | 'outOfStock';
export type EstadoPromocion = 'none' | 'active' | 'expired';
