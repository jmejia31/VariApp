export interface Usuario {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  rolId?: number;
  activo: boolean;
  bloqueado: boolean;
  fechaCreacion: string;
}

export interface UsuarioDetalle {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  rolId?: number;
  rolNombre?: string;
  activo: boolean;
  bloqueado: boolean;
  motivoBloqueo?: string;
  fechaBloqueo?: string;
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface CreateUsuarioValue {
  nombreUsuario: string;
  nombreCompleto: string;
  password: string;
  rol: 'Administrador' | 'Vendedor';
  rolId?: number;
}

export interface UpdateUsuarioValue {
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  rolId?: number;
  nuevaPassword?: string;
}
