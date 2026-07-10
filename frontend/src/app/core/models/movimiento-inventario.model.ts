export interface MovimientoInventario {
  id: number;
  productoId: number;
  productoNombre: string;
  tipo: 'Entrada' | 'Salida' | 'Ajuste' | 'Reversion';
  cantidad: number;
  stockAnterior: number;
  stockNuevo: number;
  referenciaTipo: string;
  referenciaId: number;
  descripcion?: string;
  creadoPorNombreUsuario?: string;
  fecha: string;
}
