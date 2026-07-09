export interface LoginRequest {
  nombreUsuario: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: 'Administrador' | 'Vendedor';
  expiraEn: string;
}
