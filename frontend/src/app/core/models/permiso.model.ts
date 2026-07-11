export interface PermisoMatrizItem {
  rol: string;
  modulo: string;
  accion: string;
  permitido: boolean;
}

export interface MisPermisos {
  rol: string;
  esAdministrador: boolean;
  permisos: string[]; // formato "Modulo:Accion"
}
