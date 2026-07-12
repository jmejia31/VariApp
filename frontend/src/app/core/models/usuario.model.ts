export interface Usuario {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  rolId?: number;
  activo: boolean;
  fechaCreacion: string;
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
