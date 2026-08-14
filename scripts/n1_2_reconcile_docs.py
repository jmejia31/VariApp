from pathlib import Path

TASKS_BLOCK = '''## ERP-N1.2 — Almacenes empresariales

- [x] N1.2.A Auditoría y preflight — no existía implementación legacy Almacén/Bodega/Ubicación; Almacén definido como hijo obligatorio de Sucursal, sin adelantar stock N1.4 ni multiempresa N6.
- [x] N1.2.B Dominio y contratos — `Almacen`, `TipoAlmacen` estable Tienda/Bodega/Transito/Devolucion/Cuarentena y DTOs; autoridad jerárquica única `SucursalId`; corrección concurrente final `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8` retiró `EmpresaId` duplicada y añadió guarda arquitectónica.
- [x] N1.2.C Persistencia/migración — tabla `Almacenes`, FK Restrict a `Sucursales`, código activo único, índices/checks, preflight/postcheck y rollback fail-closed; HEAD `bebafe3abb2ddc66448c805b107f8d1f8ee3f3e9`; CI `31834214669` con MySQL/integración/snapshot sin drift SUCCESS.
- [x] N1.2.D Aplicación/API/RBAC — repositorio, servicio, validadores, CRUD, filtros/paginación, jerarquía fail-closed, estado idempotente, soft-delete, `ModuloSistema.Almacenes=29` y permisos seedables; `5a97bf3844069a565e1aecf39e4b8001c10f386b`.
- [x] N1.2.E Frontend/UX — mantenimiento responsive, filtros server-side, selector Sucursal activa, catálogo de tipos API, rutas/menú RBAC y formulario sin EmpresaId/stock; `3a1b8004f2120c4be6459bb46fd120eff8704fe9`; M10 `31835928799` SUCCESS.
- [x] N1.2.F RBAC/auditoría/seguridad/observabilidad — auditoría `Entidad=Almacen`, correlation/health globales y métrica P50/P95 `/almacenes` sin término ni PII; `30c7e9ff1dedf69eb860916b92b1d5bee0941084`.
- [x] N1.2.G QA/regresión/CI — workflow dedicado `.github/workflows/n1-2-almacenes-ci.yml`; fallos reales de harness puerto y orden de rutas corregidos en `3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f` y `053152ae51de3617bf30a4e9987574c7879e3049`; run final `31837394309` SUCCESS, Playwright 8/8.
- [x] N1.2.H Documentación/certificación — fuente canónica `docs/ERP_N1_2_ALMACENES.md`; arquitectura, migración/rollback, API/RBAC, UX, observabilidad, QA y DoD documentados.

**ERP-N1.2 queda formalmente cerrado.** Siguiente foco autorizado por VAEP: `N1.3.A — Ubicaciones internas / auditoría y preflight`.

'''

CHANGELOG_BLOCK = '''## 2026-08-14 — ERP-N1.2 Almacenes empresariales — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** implementar y certificar `Almacen` como maestro hijo obligatorio de `Sucursal`, con tipos Tienda/Bodega/Transito/Devolucion/Cuarentena, persistencia MySQL, API, RBAC relacional, auditoría, observabilidad, frontend responsive/accesible y QA dedicado, sin adelantar ubicaciones N1.3, existencias por almacén N1.4 ni multiempresa N6.

**Resultado funcional:** `Almacen.SucursalId` queda como única jerarquía organizacional de N1.2; una introducción concurrente de `EmpresaId` duplicada fue detectada y corregida forward-only en `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8`. Persistencia final con FK Restrict a `Sucursales`, código activo único, checks/índices y rollback fail-closed. API `/almacenes` soporta CRUD, filtros/paginación, catálogo de tipos, activos y operaciones de estado idempotentes. Crear/mover/reactivar falla cerrado si la Sucursal no existe o está inactiva. RBAC `Almacenes=29`, auditoría `Entidad=Almacen` y métrica P50/P95 sin término/PII quedan integrados. Frontend ofrece lista server-side, selector Sucursal/tipo, rutas y menú protegidos, tabla/cards responsive y formulario sin stock ni EmpresaId.

**Trazabilidad:** B final `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8`; C `bebafe3abb2ddc66448c805b107f8d1f8ee3f3e9`; D `5a97bf3844069a565e1aecf39e4b8001c10f386b`; E `3a1b8004f2120c4be6459bb46fd120eff8704fe9`; F `30c7e9ff1dedf69eb860916b92b1d5bee0941084`; G base `f6f51bb6d0d5d1910e9561de30d934b30fa2d83e`, corrección harness `3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f` y corrección routing/final funcional `053152ae51de3617bf30a4e9987574c7879e3049`. Documento canónico publicado en `a507eee7e69a5bed15226855098c0c0a28e7962e`.

**QA real:** el primer certificado `31836552560` dejó 6 pruebas API verdes y detectó que el harness levantaba API en 5006 mientras Angular consumía 5005; se corrigió sin alterar la app. El segundo `31836970704` confirmó el login y detectó que `provideRoutes(ALMACENES_ROUTES)` registraba Almacenes después del wildcard `**`; se corrigió a `provideRouter([...ALMACENES_ROUTES, ...routes])`. El certificado final `31837394309`, job `94886619205`, terminó `SUCCESS`: build `-warnaserror`, 376 tests backend, API+migraciones MySQL 8.4+health, npm ci/lint/build, Angular y Playwright `8 passed / 0 failed / 0 skipped`.

**Documentación/control:** fuente canónica `docs/ERP_N1_2_ALMACENES.md`; TASKS, CHANGELOG y tablero VAEP se reconcilian en N1.2.H. `main`, Producción, PR #2 merge/auto-merge, secretos y force-push permanecen intactos. **ERP-N1.2 queda formalmente cerrado** y el siguiente foco FINISH_FIRST es `N1.3.A — Ubicaciones internas / auditoría y preflight`.

'''


def main() -> None:
    tasks_path = Path('TASKS.md')
    changelog_path = Path('CHANGELOG_AI.md')
    tasks = tasks_path.read_text(encoding='utf-8')
    changelog = changelog_path.read_text(encoding='utf-8')

    if '## ERP-N1.2 — Almacenes empresariales' not in tasks:
        marker = '## Fuentes VAEP v2'
        if marker not in tasks:
            raise SystemExit('Marcador de TASKS no encontrado')
        tasks = tasks.replace(marker, TASKS_BLOCK + marker, 1)

    if 'ERP-N1.2 Almacenes empresariales — CIERRE FORMAL' not in changelog:
        marker = '## 2026-08-14 — ERP-N1.1 Sucursales empresariales — CIERRE FORMAL'
        if marker not in changelog:
            raise SystemExit('Marcador de CHANGELOG no encontrado')
        changelog = changelog.replace(marker, CHANGELOG_BLOCK + marker, 1)

    out = Path('/tmp/n12-docs')
    out.mkdir(parents=True, exist_ok=True)
    (out / 'TASKS.md').write_text(tasks, encoding='utf-8')
    (out / 'CHANGELOG_AI.md').write_text(changelog, encoding='utf-8')


if __name__ == '__main__':
    main()
