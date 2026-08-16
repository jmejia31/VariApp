import { test, expect } from '@playwright/test';

const transferencia = (estado = 'Borrador') => ({
  id: 41,
  numero: 'TR-000041',
  almacenOrigenId: 1,
  almacenOrigenNombre: 'Tienda Centro',
  almacenDestinoId: 2,
  almacenDestinoNombre: 'Bodega Norte',
  estado,
  observaciones: 'Reposición interna',
  fechaSolicitud: estado === 'Borrador' ? null : '2026-08-16T10:00:00Z',
  fechaAprobacion: null,
  fechaDespacho: null,
  fechaRecepcion: null,
  fechaCancelacion: null,
  motivoCancelacion: null,
  detalles: [{
    id: 501,
    productoVarianteId: 77,
    ubicacionOrigenId: 10,
    ubicacionDestinoId: 20,
    cantidadSolicitada: 3,
    cantidadAprobada: 0,
    cantidadDespachada: 0,
    cantidadRecibida: 0,
    cantidadFaltante: 0,
    cantidadSobrante: 0,
    cantidadDanada: 0,
    productoSkuSnapshot: 'SKU-77',
    productoMarcaSnapshot: 'Marca',
    productoModeloSnapshot: 'Modelo',
    productoColorSnapshot: 'Negro',
    productoTallaSnapshot: 'M'
  }]
});

test.describe('Transferencias de inventario - lifecycle UI', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('token', 'e2e-token');
      localStorage.setItem('usuario', JSON.stringify({ id: 1, nombreUsuario: 'admin-e2e', nombreCompleto: 'Admin E2E', rol: 'Administrador' }));
    });

    await page.route('**/permisos/mis-permisos', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          permisos: [
            'MovimientosInventario:Ver',
            'MovimientosInventario:Crear',
            'MovimientosInventario:Editar',
            'MovimientosInventario:CambiarEstado',
            'MovimientosInventario:Aprobar',
            'MovimientosInventario:Confirmar',
            'MovimientosInventario:Anular'
          ],
          esAdministrador: false
        }
      })
    }));
  });

  test('lista, abre detalle y solicita un borrador', async ({ page }) => {
    await page.route('**/transferencias-inventario?**', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { items: [transferencia()], totalCount: 1, page: 1, pageSize: 20, totalPages: 1 }
      })
    }));

    await page.route('**/transferencias-inventario/41', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, data: transferencia() })
    }));

    let solicitudRegistrada = false;
    await page.route('**/transferencias-inventario/41/solicitar', async route => {
      solicitudRegistrada = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, data: transferencia('Solicitada') })
      });
    });

    await page.goto('/inventario/transferencias');
    await expect(page.getByText('TR-000041')).toBeVisible();
    await expect(page.getByText('Tienda Centro')).toBeVisible();

    await page.getByRole('button', { name: 'Ver' }).click();
    await expect(page).toHaveURL(/\/inventario\/transferencias\/41$/);
    await expect(page.getByRole('heading', { name: 'TR-000041' })).toBeVisible();

    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Solicitar' }).click();

    await expect.poll(() => solicitudRegistrada).toBe(true);
    await expect(page.getByText('Solicitada', { exact: true })).toBeVisible();
  });
});
