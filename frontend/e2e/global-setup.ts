import { expect, request as playwrightRequest } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'];
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'];
const TENANT_ID = 1;

function dataOf(payload: any): any {
  return payload?.data ?? payload?.Data;
}

export default async function globalSetup(): Promise<void> {
  if (!ADMIN_USERNAME || !ADMIN_PASSWORD) {
    throw new Error('PHASE7_ADMIN_USERNAME y PHASE7_ADMIN_PASSWORD son obligatorios para el bootstrap E2E.');
  }

  const api = await playwrightRequest.newContext({ baseURL: API_URL });

  try {
    const login = await api.post('/auth/login', {
      data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
    });
    expect(login.status(), await login.text()).toBe(200);
    const loginData = dataOf(await login.json());
    const token = loginData?.token;
    expect(token).toBeTruthy();

    const authHeaders = { Authorization: `Bearer ${token}` };

    const empresas = await api.get('/empresas', { headers: authHeaders });
    expect(empresas.status(), await empresas.text()).toBe(200);
    const empresasData = dataOf(await empresas.json());
    let empresa = Array.isArray(empresasData)
      ? empresasData.find((item: any) => item?.id === TENANT_ID)
      : null;

    if (!empresa) {
      const crearEmpresa = await api.post('/empresas', {
        headers: authHeaders,
        data: { nombre: 'Empresa E2E Acceptance' }
      });
      expect(crearEmpresa.status(), await crearEmpresa.text()).toBe(201);
      empresa = dataOf(await crearEmpresa.json());
    }

    expect(empresa?.id).toBe(TENANT_ID);

    const usuarios = await api.get('/usuarios', { headers: authHeaders });
    expect(usuarios.status(), await usuarios.text()).toBe(200);
    const usuariosData = dataOf(await usuarios.json());
    const admin = Array.isArray(usuariosData)
      ? usuariosData.find((usuario: any) => usuario?.nombreUsuario === ADMIN_USERNAME)
      : null;

    expect(admin?.id).toBeGreaterThan(0);
    expect(admin?.rolId).toBeGreaterThan(0);

    const membresias = await api.get(`/usuarios/${admin.id}/empresas`, { headers: authHeaders });
    expect(membresias.status(), await membresias.text()).toBe(200);
    const membresiasData = dataOf(await membresias.json());
    const existente = Array.isArray(membresiasData)
      ? membresiasData.find((item: any) => item?.empresaId === TENANT_ID)
      : null;

    if (!existente) {
      const asignar = await api.post(`/usuarios/${admin.id}/empresas`, {
        headers: authHeaders,
        data: { empresaId: TENANT_ID, rolId: admin.rolId }
      });
      expect(asignar.status(), await asignar.text()).toBe(200);
      const membresia = dataOf(await asignar.json());
      expect(membresia?.empresaId).toBe(TENANT_ID);
      expect(membresia?.usuarioId).toBe(admin.id);
      expect(membresia?.activa).toBe(true);
    }
  } finally {
    await api.dispose();
  }
}
