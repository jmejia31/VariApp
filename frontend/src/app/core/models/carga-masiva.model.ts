export type TipoCargaMasiva =
  | 'Clientes'
  | 'Proveedores'
  | 'Colores'
  | 'Productos'
  | 'VariantesInventario';

export type EstadoCargaMasiva =
  | 'PendienteValidacion'
  | 'Validada'
  | 'ConErrores'
  | 'Confirmada'
  | 'Fallida'
  | 'Cancelada';

export interface CargaMasiva {
  id: number;
  tipo: TipoCargaMasiva;
  estado: EstadoCargaMasiva;
  nombreArchivo: string;
  tamanoBytes: number;
  totalFilas: number;
  filasValidas: number;
  filasConError: number;
  filasConAdvertencia: number;
  filasProcesadas: number;
  registrosCreados: number;
  registrosActualizados: number;
  fechaValidacion?: string | null;
  fechaConfirmacion?: string | null;
  creadoPorNombreUsuario?: string | null;
  confirmadoPorNombreUsuario?: string | null;
  errorGeneral?: string | null;
  fechaCreacion: string;
}

export interface CargaMasivaDetalle extends CargaMasiva {
  puedeConfirmarse: boolean;
  archivoReutilizado: boolean;
  filas: CargaMasivaFila[];
  errores: CargaMasivaError[];
}

export interface CargaMasivaFila {
  numeroFila: number;
  accion: 'Crear' | 'Actualizar' | string;
  esValida: boolean;
  datos: Record<string, string | null>;
  mensajes: string[];
}

export interface CargaMasivaError {
  numeroFila: number;
  campo?: string | null;
  codigo: string;
  mensaje: string;
  valorOriginal?: string | null;
  esAdvertencia: boolean;
}

export interface CargaMasivaConfiguracion {
  maximoBytes: number;
  maximoFilas: number;
  extensionesPermitidas: string[];
  tipos: CargaMasivaTipo[];
}

export interface CargaMasivaTipo {
  tipo: TipoCargaMasiva;
  nombre: string;
  descripcion: string;
  columnas: string[];
}
