/**
 * Contratos canónicos del escaparate público de VariStoreHn.
 *
 * Los tipos *CatalogoPublico representan la frontera HTTP actual.
 * Los tipos *Tienda representan el modelo normalizado que consumen las páginas públicas.
 * Mantener estos contratos independientes de componentes Angular y de modelos administrativos.
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
  precio: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  imagenes: ImagenCatalogo[];
}

export interface ProductoCatalogoPublico {
  id: number;
  nombre: string;
  descripcion?: string;
  categoriaNombre?: string;
  marcaNombre?: string;
  modeloNombre?: string;
  precio: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  imagenPrincipalUrl?: string;
  imagenes: ImagenCatalogo[];
  modelos: ModeloCatalogoPublico[];
}

export interface ModeloTienda {
  clave: string;
  modeloId: number | null;
  nombre: string;
  marca: string;
  precio: number;
  stock: number;
  disponible: boolean;
  imagenes: string[];
}

/** Modelo normalizado actual. Slug/promoción se añadirán al cerrar el contrato backend de Fase 0. */
export interface ProductoTienda {
  id: number;
  nombre: string;
  descripcion: string;
  categoria: string;
  marca: string;
  precio: number;
  disponible: boolean;
  imagenes: string[];
  modelos: ModeloTienda[];
  ilustracion?: string;
}

/** Contrato objetivo para navegación pública; no debe rellenarse con slugs efímeros en el navegador. */
export interface CategoriaTienda {
  id: number;
  nombre: string;
  slug: string;
  descripcion: string;
  imagenUrl?: string;
  cantidadProductos?: number;
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
