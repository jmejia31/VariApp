export interface Usuario {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  activo: boolean;
  fechaCreacion: string;
}

export interface CreateUsuarioValue {
  nombreUsuario: string;
  nombreCompleto: string;
  password: string;
  rol: 'Administrador' | 'Vendedor';
}

export interface UpdateUsuarioValue {
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  nuevaPassword?: string;
}
