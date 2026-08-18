export enum MetodoCosteoInventario {
  PromedioPonderado = 1,
  FIFO = 2,
  Estandar = 3
}

export interface MetodoCosteoInventarioOption {
  id: MetodoCosteoInventario;
  nombre: string;
  descripcion?: string | null;
}

export interface PoliticaCosteoInventario {
  id: number;
  empresaConfiguracionId: number;
  metodo: MetodoCosteoInventario;
  metodoNombre: string;
  vigenteDesdeUtc: string;
  vigenteHastaUtc?: string | null;
  observacion?: string | null;
  creadoPorUsuarioId?: number | null;
}

export interface PoliticaCosteoInventarioQuery {
  page?: number;
  pageSize?: number;
  metodo?: MetodoCosteoInventario;
  desdeUtc?: string;
  hastaUtc?: string;
}

export interface CambiarPoliticaCosteoInventarioRequest {
  metodo: MetodoCosteoInventario;
  observacion?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
