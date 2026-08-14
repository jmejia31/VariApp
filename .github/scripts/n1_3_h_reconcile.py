from pathlib import Path
import re

TASKS_SECTION = '''## ✅ ERP-N1.3 — Ubicaciones internas de almacén — cierre certificado (2026-08-14)

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

CHANGELOG_SECTION = '''## 2026-08-14 — ERP-N1.3 Ubicaciones internas de almacén — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** completar `UbicacionAlmacen` como topología física jerárquica interna de cada Almacén para pasillos, estantes, racks, secciones, bins y otras ubicaciones, sin introducir todavía existencias, cantidades ni semántica WMS avanzada.

**Resultado funcional:** `UbicacionAlmacen.AlmacenId` es la única relación organizacional persistida; `SucursalId` y `EmpresaId` se derivan transitivamente. Padre opcional restringido al mismo Almacén, prevención de ciclos directos/indirectos, protección de descendientes al mover/desactivar/eliminar, código operativo único por Almacén, soft-delete y estados idempotentes. MySQL 8.4 conserva la invariante anti-self-parent mediante triggers porque un CHECK no puede referenciar el `Id AUTO_INCREMENT`. API `/ubicaciones-almacen` soporta búsqueda, Almacén, padre/raíz, tipo, estado, paginación, CRUD y operaciones de estado. Frontend incorpora listado responsive, filtros server-side, formulario jerárquico, selectores de Almacén/padre, rutas y menú protegidos por RBAC.

**RBAC/auditoría/seguridad:** módulo `UbicacionesAlmacen`, permisos `Ver/Crear/Editar/Activar/Desactivar/EliminarLogico`, auditoría de mutaciones con referencia de entidad y pruebas que congelan los 9 contratos de autorización. Se reutilizan Correlation ID, ProblemDetails, headers de seguridad y health/readiness globales. N1.3 no contiene campos de stock; `ExistenciaVariante` queda reservado para ERP-N1.4.

**Trazabilidad:** D backend `4d2cc04b363df602f6de97b7f5ea876ea35a6196`, run `31843085895`, job `94903923345` SUCCESS; E frontend `91f878ef3cbc56219b637e9b62c99bdd1109a9df`, run `31846161956`, job `94912936660` SUCCESS; F/G baseline `4a6be38683f03fc2076f18a71115480c930ba79b`.

**QA real:** run agregado `31846485117` SUCCESS: higiene `94913888918`, Backend Release/unitarias `94913888850`, frontend producción `94913888865`, Docker `94913888808` y MySQL 8.4/integración `94913888844`; el job MySQL aplicó migraciones actuales, ejecutó `Category=Integration`, verificó snapshot/variantes/cargas y generó SQL forward sin regresiones.

**Documentación/control:** preflight `docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md`; cierre canónico `docs/ERP_N1_3_UBICACIONES_ALMACEN.md`; TASKS, CHANGELOG y tablero VAEP reconciliados preservando historial. `main`, Producción, merge/auto-merge del PR #2, secretos y force-push permanecen intactos. **ERP-N1.3 queda formalmente cerrado** y el siguiente foco FINISH_FIRST es `N1.4.A — ExistenciaVariante — Preflight y diseño`.

'''


def insertar(texto: str, patron: str, seccion: str, etiqueta: str) -> str:
    if 'ERP-N1.3' in texto and etiqueta in texto:
        return texto
    match = re.search(patron, texto, flags=re.MULTILINE)
    if not match:
        raise SystemExit(f'No se encontró marcador de inserción para {etiqueta}')
    return texto[:match.start()] + seccion + texto[match.start():]


def main() -> None:
    tasks_path = Path('TASKS.md')
    changelog_path = Path('CHANGELOG_AI.md')
    tasks = tasks_path.read_text(encoding='utf-8')
    changelog = changelog_path.read_text(encoding='utf-8')

    tasks_new = insertar(tasks, r'^## .*ERP-N1\.2', TASKS_SECTION, 'cierre certificado')
    changelog_new = insertar(changelog, r'^## 2026-08-14 — ERP-N1\.2', CHANGELOG_SECTION, 'Ubicaciones internas de almacén')

    out = Path('out')
    out.mkdir(exist_ok=True)
    (out / 'TASKS.md').write_text(tasks_new, encoding='utf-8')
    (out / 'CHANGELOG_AI.md').write_text(changelog_new, encoding='utf-8')

    if len(tasks_new) <= len(tasks) or len(changelog_new) <= len(changelog):
        raise SystemExit('Reconciliación no incrementó ambos colaborativos')
    if 'N1.4.A — ExistenciaVariante' not in tasks_new or 'N1.4.A — ExistenciaVariante' not in changelog_new:
        raise SystemExit('Falta siguiente foco N1.4.A')
    if 'ERP-N1.2' not in tasks_new or 'ERP-N1.1' not in changelog_new:
        raise SystemExit('La reconciliación no preservó historial previo')

    print(f'TASKS: {len(tasks)} -> {len(tasks_new)} bytes')
    print(f'CHANGELOG: {len(changelog)} -> {len(changelog_new)} bytes')
    print('SUCCESS')


if __name__ == '__main__':
    main()
