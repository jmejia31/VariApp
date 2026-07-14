export interface Perfil {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  fechaCreacion: string;
}

export interface ActualizarPerfilValue {
  nombreCompleto: string;
}

export interface CambiarPasswordValue {
  passwordActual: string;
  passwordNueva: string;
}
