# Certificación ERP-N8.3 — Compatibilidad de navegadores

Fecha de cierre documental: 2026-09-15
Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Rama certificada: `Desarrollo`

## Alcance

N8.3 certifica la compatibilidad causal del storefront soportado en Chromium, Firefox y WebKit sin introducir cambios de runtime backend, persistencia, migraciones, autenticación, RBAC ni datos productivos.

## Cadena certificada

- N8.3.A PRE — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.A_LISTO_REAL_20260915T074220Z_SUP48.json`.
- N8.3.B DOMAIN — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.B_LISTO_REAL_20260915T074355Z_SUP48.json`.
- N8.3.C DB_MIG — `LISTO_REAL` N/A grounded: `vaep/evidence/receipts/N8.3.C_LISTO_REAL_20260915T074445Z_SUP48.json`.
- N8.3.D BACKEND_API — `LISTO_REAL` N/A grounded: `vaep/evidence/receipts/N8.3.D_LISTO_REAL_20260915T074535Z_SUP48.json`.
- N8.3.E FRONTEND_UX — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.E_LISTO_REAL_20260915T074915Z_SUP48.json`.
- N8.3.F SEC_AUDIT — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.F_LISTO_REAL_20260915T075410Z_SUP48.json`.
- N8.3.G TEST_CI — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.G_LISTO_REAL_20260915T075520Z_SUP48.json`.

## Functional candidate y gate causal

Functional candidate: `b833e44a976f92a426fff7f969a3f3f757234f20`.

Workflow causal: `N8.3 - Compatibilidad de navegadores`, run `34943377733`, job `104296994230`, terminal `success`.

Validaciones terminales:

- `npm ci`: PASS.
- frontend lint: PASS.
- build de producción frontend: PASS.
- instalación de Chromium, Firefox y WebKit: PASS.
- arranque Angular en `127.0.0.1`: PASS.
- regresión Playwright dirigida: PASS en Chromium, Firefox y WebKit.
- publicación de evidencia del job: PASS.

## Seguridad y observabilidad

El workflow usa `permissions: contents: read`, no consume secretos, levanta el servidor local exclusivamente en `127.0.0.1`, usa datos sintéticos para la configuración pública de empresa y no toca Producción. No existe delta de autenticación/RBAC/auditoría de producto atribuible a N8.3; el stage N8.3.F quedó certificado con P0=0, P1=0 y P2=0.

## Archivos causales

- `.github/workflows/n8-3-browser-compatibility.yml`
- `frontend/e2e/n83-browser-compatibility.spec.ts`
- `frontend/playwright.n83.config.ts`

El E2E verifica storefront visible, hero, header, superficie del carrito, ausencia de overflow horizontal y ausencia de errores de página en los tres motores soportados.

## Rollback

Si fuera necesario revertir exclusivamente N8.3, retirar de `Desarrollo` los tres artifacts causales anteriores y volver al baseline anterior al functional candidate. No se requiere rollback de esquema, datos, secretos ni infraestructura porque N8.3 no introduce delta en esas superficies.

## Guardas

- `main`: intacta.
- Producción: intacta.
- deploy: no ejecutado.
- secretos: no tocados.
- PR #2: no merge, no auto-merge.
- P0 abiertos atribuibles: 0.
- P1 abiertos atribuibles: 0.
- P2 abiertos atribuibles: 0.

N8.3.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación del control-plane; este documento por sí solo no sustituye ese cierre.
