import { expect, Page, test } from '@playwright/test';

const conteoBase = {
  id: 701,
  numero: 'CNT-E2E-701',
  tipo: 5,
  tipoNombre: 'Ciego',
  estado: 2,
  estadoNombre: 'En proceso',
  almacenId: 11,
  almacenNombre: 'Bodega E2E',
  ubicacionAlmacenId: null,
  ubicacionNombre: null,
  categoriaId: null,
  categoriaNombre: null,
  esCiego: true,
  observaciones: 'Conteo físico determinista E2E',
  fechaInicio: '2026-08-16T20:00:00Z',
  iniciadoPorUsuarioId: 1,
  fechaCierre: null,
  cerradoPorUsuarioId: null,
  fechaAprobacion: null,
  aprobadoPorUsuarioId: null,
  fechaCancelacion: null,
  canceladoPorUsuarioId: null,
  motivoCancelacion: null,
  cantidadLineas: 1,
  cantidadCapturadas: 0,
  cantidadConDiferencia: 0,
  diferenciaNeta: 0,
  detalles: [{
    id: 9001,
    conteoInventarioId: 701,
    productoVarianteId: 44,
    almacenId: 11,
    ubicacionAlmacenId: null,
    stockEsperado: 8,
    cantidadContada: null,
    diferencia: null,
    fechaConteo: null,
    contadoPorUsuarioId: null,
    ajusteInventarioId: null,
    productoSku: 'SKU-E2E-44',
    productoMarca: 'Marca E2E',
    productoModelo: 'Modelo E2E',
    productoColor: 'Negro',
    productoTalla: 'M'
  }]
};

async function autenticar(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('inventoryapp_token', 'e2e-token-conteos');
    localStorage.setItem('inventoryapp_user', 'e2e-conteos');
    localStorage.setItem('inventoryapp_nombre_completo', 'E2E Conteos');
    localStorage.setItem('inventoryapp_rol', 'Administrador');
    localStorage.setItem('inventoryapp_expira_en', '2099-12-31T23:59:59Z');
  });
  await page.route('**/permisos/mis-permisos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      message: 'Permisos cargados',
      errors: [],
      data: {
        permisos: [
          'MovimientosInventario:Ver',
          'MovimientosInventario:Crear',
          'MovimientosInventario:Editar',
          'MovimientosInventario:CambiarEstado',
          'MovimientosInventario:Cerrar',
          'MovimientosInventario:Aprobar',
          'MovimientosInventario:Anular'
        ],
        esAdministrador: false
      }
    })
  }));
}

async function seleccionarMatOption(page: Page, label: string, opcion: string): Promise<void> {
  await page.getByRole('combobox', { name: label }).click();
  await page.getByRole('option', { name: opcion }).click();
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
}

test.describe('N1.7.E - Conteos físicos', () => {
  test.beforeEach(async ({ page }) => autenticar(page));

  test('lista conteos con filtros, estado y métricas operativas', async ({ page }) => {
    await page.route('**/conteos-inventario?**', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'OK',
        errors: [],
        data: { items: [conteoBase], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
      })
    }));

    await page.goto('/inventario/conteos');
    await expect(page.getByRole('heading', { name: 'Conteos físicos' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Conteos físicos' })).toHaveAttribute('href', '/inventario/conteos');
    const row = page.locator('tbody tr').filter({ hasText: 'CNT-E2E-701' });
    await expect(row).toHaveCount(1);
    await expect(row).toContainText('Ciego');
    await expect(row).toContainText('En proceso');
    await expect(row).toContainText('0 / 1');
    await row.getByRole('button', { name: 'Ver conteo' }).click();
    await expect(page).toHaveURL(/\/inventario\/conteos\/701$/);
  });

  test('crea conteo por ubicación usando catálogos activos y scope físico válido', async ({ page }) => {
    await page.route('**/almacenes/activos', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'OK', errors: [], data: [{ id: 11, sucursalId: 1, sucursalCodigo: 'S1', sucursalNombre: 'Principal', codigo: 'BOD', nombre: 'Bodega E2E', tipo: 'Bodega', activo: true, fechaCreacion: '2026-08-16T00:00:00Z', fechaActualizacion: '2026-08-16T00:00:00Z' }] })
    }));
    await page.route('**/categorias/activas', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'OK', errors: [], data: [{ id: 5, nombre: 'Accesorios', activa: true, totalProductos: 1, fechaCreacion: '2026-08-16T00:00:00Z', fechaActualizacion: '2026-08-16T00:00:00Z' }] })
    }));
    await page.route('**/ubicaciones-almacen/activas?**', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'OK', errors: [], data: [{ id: 23, almacenId: 11, almacenCodigo: 'BOD', almacenNombre: 'Bodega E2E', codigo: 'A-01', nombre: 'Pasillo A', tipo: 'Pasillo', activa: true, fechaCreacion: '2026-08-16T00:00:00Z', fechaActualizacion: '2026-08-16T00:00:00Z' }] })
    }));
    await page.route('**/conteos-inventario', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      const payload = route.request().postDataJSON();
      expect(payload.tipo).toBe(3);
      expect(payload.almacenId).toBe(11);
      expect(payload.ubicacionAlmacenId).toBe(23);
      expect(payload.productoVarianteIds).toEqual([44]);
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Creado', errors: [], data: { ...conteoBase, tipo: 3, tipoNombre: 'PorUbicacion', ubicacionAlmacenId: 23, ubicacionNombre: 'Pasillo A' } }) });
    });

    await page.goto('/inventario/conteos/nuevo');
    await seleccionarMatOption(page, 'Tipo', 'Por ubicación');
    await seleccionarMatOption(page, 'Almacén', 'BOD · Bodega E2E');
    await seleccionarMatOption(page, 'Ubicación', 'A-01 · Pasillo A');
    await page.getByRole('textbox', { name: 'Variantes específicas' }).fill('44');
    await page.getByRole('button', { name: 'Crear conteo' }).click();
    await expect(page).toHaveURL(/\/inventario\/conteos\/701$/);
  });

  test('no habilita persistencia cuando la captura cargada no fue modificada', async ({ page }) => {
    const actual = {
      ...structuredClone(conteoBase),
      cantidadCapturadas: 1,
      detalles: [{ ...conteoBase.detalles[0], cantidadContada: 8, diferencia: 0 }]
    };
    await page.route('**/conteos-inventario/701', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'OK', errors: [], data: actual })
    }));

    await page.goto('/inventario/conteos/701');
    const guardar = page.getByRole('button', { name: 'Guardar capturas' });
    await expect(guardar).toBeDisabled();
    await page.getByLabel('Cantidad contada para SKU-E2E-44').fill('7');
    await expect(guardar).toBeEnabled();
  });

  test('modo ciego oculta stock esperado y captura lote sin exponerlo', async ({ page }) => {
    let actual: any = structuredClone(conteoBase);
    await page.route('**/conteos-inventario/701', async route => {
      if (route.request().method() !== 'GET') return route.continue();
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', errors: [], data: actual }) });
    });
    await page.route('**/conteos-inventario/701/detalles/captura-lote', async route => {
      expect(route.request().method()).toBe('PUT');
      const payload = route.request().postDataJSON();
      expect(payload.lineas).toEqual([{ detalleId: 9001, cantidadContada: 6 }]);
      actual = {
        ...actual,
        cantidadCapturadas: 1,
        cantidadConDiferencia: 1,
        diferenciaNeta: -2,
        detalles: [{ ...actual.detalles[0], cantidadContada: 6, diferencia: -2 }]
      };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Captura registrada', errors: [], data: actual }) });
    });

    await page.goto('/inventario/conteos/701');
    await expect(page.getByText('Oculto')).toBeVisible();
    const input = page.getByLabel('Cantidad contada para SKU-E2E-44');
    await input.fill('6');
    await page.getByRole('button', { name: 'Guardar capturas' }).click();
    await expect(page.getByText('Oculto')).toBeVisible();
    await expect(page.locator('tbody').getByText('-2', { exact: true })).toBeVisible();
  });

  test('cierra y aprueba el lifecycle sólo mediante acciones explícitas', async ({ page }) => {
    let actual: any = {
      ...structuredClone(conteoBase),
      cantidadCapturadas: 1,
      cantidadConDiferencia: 1,
      diferenciaNeta: -2,
      detalles: [{ ...conteoBase.detalles[0], cantidadContada: 6, diferencia: -2 }]
    };
    await page.route('**/conteos-inventario/701', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', errors: [], data: actual }) }));
    await page.route('**/conteos-inventario/701/cerrar', async route => {
      actual = { ...actual, estado: 3, estadoNombre: 'Cerrado', fechaCierre: '2026-08-16T20:30:00Z', cerradoPorUsuarioId: 1 };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Cerrado', errors: [], data: actual }) });
    });
    await page.route('**/conteos-inventario/701/aprobar', async route => {
      actual = { ...actual, estado: 4, estadoNombre: 'Aprobado', fechaAprobacion: '2026-08-16T20:31:00Z', aprobadoPorUsuarioId: 1 };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Aprobado', errors: [], data: actual }) });
    });

    await page.goto('/inventario/conteos/701');
    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Cerrar' }).click();
    await expect(page.getByText('Cerrado', { exact: true })).toBeVisible();

    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Aprobar' }).click();
    await expect(page.getByText('Aprobado', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Generar ajuste' })).toBeVisible();
  });
});
