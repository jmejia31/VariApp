import { defineConfig } from '@playwright/test';

const baseURL = process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://127.0.0.1:4200';
const tenantId = process.env['E2E_TENANT_ID'] ?? '1';
const hasTenantBootstrapCredentials = Boolean(
  process.env['PHASE7_ADMIN_USERNAME'] || process.env['PHASE7_ADMIN_PASSWORD']
);

export default defineConfig({
  testDir: './e2e',
  globalSetup: hasTenantBootstrapCredentials ? './e2e/global-setup.ts' : undefined,
  timeout: 45_000,
  expect: {
    timeout: 10_000
  },
  fullyParallel: false,
  forbidOnly: Boolean(process.env['CI']),
  retries: process.env['CI'] ? 1 : 0,
  workers: process.env['CI'] ? 1 : undefined,
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/phase7-e2e.xml' }],
    ['html', { outputFolder: 'playwright-report', open: 'never' }]
  ],
  use: {
    baseURL,
    extraHTTPHeaders: {
      'X-Empresa-Id': tenantId
    },
    storageState: {
      cookies: [],
      origins: [
        {
          origin: new URL(baseURL).origin,
          localStorage: [
            { name: 'inventoryapp_empresa_solicitada_id', value: tenantId }
          ]
        }
      ]
    },
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  }
});
