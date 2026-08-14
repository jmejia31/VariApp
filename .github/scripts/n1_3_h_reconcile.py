from pathlib import Path
import re

TASKS_SECTION = r'''## ✅ ERP-N1.3 — Ubicaciones internas de almacén — cierre certificado (2026-08-14)

- [x] N1.3.A Preflight y diseño — `docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md`; topología jerárquica aditiva definida sin stock, sin `SucursalId`/`EmpresaId` duplicados y con N1.4 como autoridad futura de existencias.
- [x] N1.3.B Dominio y contratos — `UbicacionAlmacen`, `TipoUbicacionAlmacen`, DTOs y guardas de contrato; backend Release/unitarias certificados.
- [x] N1.3.C Persistencia/migración — `20260814211647_N1_3_UbicacionAlmacenPersistencia`; FK a Almacén, jerarquía autorreferente compuesta del mismo Almacén, código activo único, constraints y triggers MySQL 8.4 para self-parent; snapshot sin drift e historial MySQL certificado.
- [x] N1.3.D Aplicación, servicios y API — repositorio/servicio/controller/DI, paginación y filtros, padre activo/mismo Almacén, prevención de ciclos y protección de descendientes; cierre funcional `4d2cc04b363df602f6de97b7f5ea876ea35a6196`; Backend Release/unitarias `31843085895` / `94903923345` SUCCESS.
- [x] N1.3.E Frontend/UX — listado responsive, filtros server-side, formulario jerárquico, selectores de Almacén/padre, rutas y menú principal RBAC; cierre `91f878ef3cbc56219b637e9b62c99bdd1109a9df`; Frontend producción `31846161956` / `94912936660` SUCCESS.
- [x] N1.3.F RBAC/auditoría/seguridad/observabilidad — módulo `UbicacionesAlmacen`, permisos por endpoint, auditoría de mutaciones y regresiones que congelan autorización/auditoría; baseline `4a6be38683f03fc2076f18a71115480c930ba79b`; backend `94913888850` SUCCESS.
- [x] N1.3.G QA, regresión y CI — run agregado `31846485117` SUCCESS: higiene `94913888918`, backend `94913888850`, frontend `94913888865`, Docker `94913888808` y MySQL 8.4/integración `94913888844`.
- [x] N1.3.H Documentación y certificación — fuente canónica `docs/ERP_N1_3_UBICACIONES_ALMACEN.md`, `TASKS.md`, `CHANGELOG_AI.md` y VAEP reconciliados preservando historial.

**Resultado:** ERP-N1.3 queda cerrado como topología interna de Almacenes. No introduce autoridad de cantidad ni stock. El siguiente foco es **N1.4.A — ExistenciaVariante — Preflight y diseño**, responsable de diseñar la autoridad de existencias por Almacén/Ubicación y la transición desde `ProductoVariante.Cantidad`.

'''

CHANGELOG_SECTION = r'''## [2026-08-14] — ERP-N1.3: Ubicaciones internas de almacén

### Alcance

Se completó ERP-N1.3 como maestro jerárquico de topología interna de Almacenes. `UbicacionAlmacen` modela pasillos, estantes, racks, secciones, bins y otras ubicaciones internas, sin introducir todavía existencias ni cantidades.

### Cambios principales

- Se añadió `UbicacionAlmacen` con Almacén obligatorio, padre opcional, tipo estable, estado operativo, soft-delete y auditoría.
- La persistencia impide padres de otro Almacén mediante FK autorreferente compuesta y protege código operativo único dentro del Almacén.
- MySQL 8.4 conserva la invariante anti-self-parent mediante triggers físicos porque un `CHECK` no puede referenciar el `Id AUTO_INCREMENT`.
- El servicio valida Almacén/padre operativos, previene ciclos indirectos y bloquea mover, desactivar o eliminar nodos cuando sus descendientes lo hacen inseguro.
- Se publicó API CRUD/consulta con paginación, filtros, activar/desactivar y RBAC `UbicacionesAlmacen`.
- El frontend incorpora listado responsive, filtros server-side, formulario jerárquico, selección de Almacén/padre y acceso de menú protegido por permiso.
- Se añadieron regresiones para los 9 contratos de autorización del controller y auditoría de Crear/Editar/Activar/Desactivar/EliminarLogico.

### Seguridad y consistencia

- No se duplican `SucursalId` ni `EmpresaId` en Ubicación; el contexto se deriva desde Almacén/Sucursal.
- N1.3 no contiene campos de stock ni cantidad. `ExistenciaVariante` queda reservado para ERP-N1.4.
- Se reutilizan autenticación/autorización global, Correlation ID, ProblemDetails, headers de seguridad y health/readiness existentes.

### Evidencia

- D backend: `4d2cc04b363df602f6de97b7f5ea876ea35a6196`; run `31843085895`, job `94903923345` SUCCESS.
- E frontend: `91f878ef3cbc56219b637e9b62c99bdd1109a9df`; run `31846161956`, job `94912936660` SUCCESS.
- F/G baseline: `4a6be38683f03fc2076f18a71115480c930ba79b`.
- QA agregado run `31846485117`: higiene, Backend Release/unitarias, frontend producción, Docker y MySQL 8.4/integración SUCCESS; job MySQL `94913888844` aplicó migraciones actuales, `Category=Integration`, snapshot/variantes/cargas y SQL forward.

### Documentación

- Preflight: `docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md`.
- Cierre canónico: `docs/ERP_N1_3_UBICACIONES_ALMACEN.md`.

### Siguiente foco

`N1.4.A — ExistenciaVariante — Preflight y diseño`.

'''


def insert_before(text: str, pattern: str, section: str, label: str) -> str:
    if 'ERP-N1.3' in text and label in text:
        return text
    match = re.search(pattern, text, flags=re.MULTILINE)
    if not match:
        raise SystemExit(f'No se encontró marcador de inserción para {label}')
    return text[:match.start()] + section + text[match.start():]


def main() -> None:
    tasks_path = Path('TASKS.md')
    changelog_path = Path('CHANGELOG_AI.md')
    tasks = tasks_path.read_text(encoding='utf-8')
    changelog = changelog_path.read_text(encoding='utf-8')

    tasks_new = insert_before(tasks, r'^## .*ERP-N1\.2', TASKS_SECTION, 'cierre certificado')
    changelog_new = insert_before(changelog, r'^## \[2026-08-14\] — ERP-N1\.2', CHANGELOG_SECTION, 'Ubicaciones internas de almacén')

    out = Path('out')
    out.mkdir(exist_ok=True)
    (out / 'TASKS.md').write_text(tasks_new, encoding='utf-8')
    (out / 'CHANGELOG_AI.md').write_text(changelog_new, encoding='utf-8')

    if len(tasks_new) <= len(tasks) or len(changelog_new) <= len(changelog):
        raise SystemExit('Reconciliación no incrementó ambos colaborativos')
    if 'N1.4.A — ExistenciaVariante' not in tasks_new or 'N1.4.A — ExistenciaVariante' not in changelog_new:
        raise SystemExit('Falta siguiente foco N1.4.A')

    print(f'TASKS: {len(tasks)} -> {len(tasks_new)} bytes')
    print(f'CHANGELOG: {len(changelog)} -> {len(changelog_new)} bytes')
    print('SUCCESS')


if __name__ == '__main__':
    main()
