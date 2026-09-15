export const VARISTOREHN_BASE_PATH = 'varistorehn' as const;

/**
 * URLs públicas canónicas definidas por el Plan Maestro v1.1.
 * Este módulo construye rutas; no genera slugs. Los slugs deben venir del contrato público.
 */
export const VARISTOREHN_PATHS = {
  inicio: '/varistorehn',
  productos: '/varistorehn/productos',
  categorias: '/varistorehn/categorias',
  ofertas: '/varistorehn/ofertas',
  carrito: '/varistorehn/carrito',
  checkout: '/varistorehn/checkout',
  categoria: (slug: string) => `/varistorehn/categoria/${encodeURIComponent(slug)}`,
  producto: (slug: string) => `/varistorehn/producto/${encodeURIComponent(slug)}`,
  pedido: (id: string | number) => `/varistorehn/pedido/${encodeURIComponent(String(id))}`
} as const;
