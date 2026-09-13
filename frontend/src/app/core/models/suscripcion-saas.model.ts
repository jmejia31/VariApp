export interface SuscripcionSaaS {
  id: number;
  planCodigo: string;
  planNombre: string;
  estado: number | string;
  inicioUtc: string;
  finUtc?: string | null;
}

export interface LimiteSuscripcionSaaS {
  clave: string;
  valorMaximo?: number | null;
}

export interface PaginaSuscripcionSaaS<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  total: number;
}

export interface OnboardingSuscripcionSaaS {
  planCodigo: string;
  inicioUtc: string;
}
