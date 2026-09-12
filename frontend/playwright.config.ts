import { execFileSync } from 'node:child_process';
import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { defineConfig } from '@playwright/test';

const baseURL = process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://127.0.0.1:4200';
const tenantId = process.env['E2E_TENANT_ID'] ?? '1';
const hasTenantBootstrapContext = Boolean(
  process.env['E2E_TENANT_ID'] &&
  process.env['PHASE7_ADMIN_USERNAME'] &&
  process.env['PHASE7_ADMIN_PASSWORD']
);

function parseConnectionString(value: string): Record<string, string> {
  return Object.fromEntries(
    value
      .split(';')
      .map(segment => segment.trim())
      .filter(Boolean)
      .map(segment => {
        const separator = segment.indexOf('=');
        return separator < 0
          ? [segment.toLowerCase(), '']
          : [segment.slice(0, separator).trim().toLowerCase(), segment.slice(separator + 1).trim()];
      })
  );
}

function sqlLiteral(value: string): string {
  return value.replaceAll("'", "''");
}

/**
 * Los workflows CI levantan bases MySQL descartables y crean SeedAdmin antes de
 * ejecutar Playwright. Desde N6.6, los permisos son estrictamente tenant-aware:
 * un usuario administrador sin UsuarioEmpresa ya no puede usar endpoints RBAC.
 *
 * Centralizamos aquí el bootstrap exclusivo de CI para que las suites históricas
 * no dependan de que cada workflow replique SQL ad-hoc. No cambia autorización
 * productiva: sólo materializa la membresía del SeedAdmin dentro de la BD efímera
 * que el propio workflow suministra mediante ConnectionStrings__DefaultConnection.
 */
function ensureSeedAdminTenantMembershipForCi(): void {
  if (!process.env['CI']) return;

  const connectionString = process.env['ConnectionStrings__DefaultConnection'];
  const username = process.env['SeedAdmin__Username'] ?? process.env['PHASE7_ADMIN_USERNAME'];
  if (!connectionString || !username) return;

  const requestedTenantId = Number.parseInt(tenantId, 10);
  if (!Number.isInteger(requestedTenantId) || requestedTenantId <= 0) {
    throw new Error(`E2E_TENANT_ID inválido: ${tenantId}`);
  }

  const connection = parseConnectionString(connectionString);
  const host = connection['server'] ?? connection['host'];
  const port = connection['port'] ?? '3306';
  const database = connection['database'];
  const user = connection['user'] ?? connection['user id'] ?? connection['uid'];
  const password = connection['password'] ?? connection['pwd'] ?? '';

  if (!host || !database || !user) {
    throw new Error('No se pudo resolver host, database y user para el bootstrap tenant E2E.');
  }

  const safeUsername = sqlLiteral(username);
  const sql = `
    SET @tenant_id = ${requestedTenantId};
    INSERT INTO Empresas (Id, Nombre, Activa, FechaCreacion, FechaActualizacion)
    SELECT @tenant_id, CONCAT('CI Tenant ', @tenant_id), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()
    WHERE NOT EXISTS (SELECT 1 FROM Empresas WHERE Id = @tenant_id);

    UPDATE Empresas
    SET Activa = 1, FechaActualizacion = UTC_TIMESTAMP()
    WHERE Id = @tenant_id;

    UPDATE UsuarioEmpresas ue
    INNER JOIN Usuarios u ON u.Id = ue.UsuarioId
    SET ue.RolId = u.RolId,
        ue.Activa = 1,
        ue.FechaActualizacion = UTC_TIMESTAMP()
    WHERE u.NombreUsuario = '${safeUsername}'
      AND ue.EmpresaId = @tenant_id;

    INSERT INTO UsuarioEmpresas (UsuarioId, EmpresaId, RolId, Activa, FechaCreacion, FechaActualizacion)
    SELECT u.Id, @tenant_id, u.RolId, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()
    FROM Usuarios u
    WHERE u.NombreUsuario = '${safeUsername}'
      AND NOT EXISTS (
        SELECT 1
        FROM UsuarioEmpresas ue
        WHERE ue.UsuarioId = u.Id
          AND ue.EmpresaId = @tenant_id
      );
  `;

  execFileSync(
    'mysql',
    [
      '--protocol=tcp',
      '-h', host,
      '-P', port,
      '-u', user,
      database,
      '--batch',
      '--skip-column-names',
      '-e', sql
    ],
    {
      stdio: 'inherit',
      env: { ...process.env, MYSQL_PWD: password }
    }
  );
}

function enableTenantAwareFixtureForCi(): void {
  if (!process.env['CI']) return;

  const e2eDir = resolve(process.cwd(), 'e2e');
  for (const entry of readdirSync(e2eDir, { withFileTypes: true })) {
    if (!entry.isFile() || !entry.name.endsWith('.spec.ts')) continue;

    const path = resolve(e2eDir, entry.name);
    const source = readFileSync(path, 'utf8');
    const tenantAwareSource = source.replace(
      /from\s+(['"])@playwright\/test\1/g,
      "from './tenant-aware-test'"
    );

    if (tenantAwareSource !== source) {
      writeFileSync(path, tenantAwareSource, 'utf8');
    }
  }
}

ensureSeedAdminTenantMembershipForCi();
enableTenantAwareFixtureForCi();

export default defineConfig({
  testDir: './e2e',
  globalSetup: hasTenantBootstrapContext ? './e2e/global-setup.ts' : undefined,
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
