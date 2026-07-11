export interface RegistroAuditoria {
  id: number;
  fecha: string;
  nombreUsuario: string;
  modulo: string;
  accion: string;
  referenciaId?: number;
  descripcion: string;
}
