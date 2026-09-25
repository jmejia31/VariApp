# Certificación Vercel DEV — 2026-09-25

Estado: **PASS / CERRADO para el recurso nuevo; pendiente únicamente retiro legacy personal**

## Team y proyecto nuevo

- Team: `SOLQARYN`
- Team ID: `team_owJ2SudSPWiEzeiDthSVV063`
- Proyecto: `solqaryn-dev`
- Project ID: `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`
- Único proyecto visible en el team nuevo: sí
- Dominio: `solqaryn-dev.vercel.app`
- Fuente de deploy: GitHub org `solqaryn`, repo `Solqaryn`, branch `dev`
- Estado de deployments recientes: `READY`
- Runtime errors últimas 24h: 0

## Validación de rutas

- `/`: HTTP 200, shell SOLQARYN
- `/login`: HTTP 200, shell SOLQARYN
- `/dashboard`: HTTP 200
- `/varistorehn`: HTTP 200
- `/varistorehn/productos`: HTTP 200
- `/api/empresa-configuracion/publica`: HTTP 200; nombre comercial VariStoreHN
- `/api/tienda/categorias`: HTTP 200; 2 categorías observadas
- `/api/tienda/productos?pagina=1&tamano=1`: HTTP 200; producto migrado observado

## Ownership confirmado visualmente

Capturas del propietario en `vercel.com/solqaryn` muestran:

- workspace/team activo: `SOLQARYN`;
- cuenta de sesión: `solqarynplatform-5337`;
- correo de la sesión: `solqaryn.platform@outlook.com`;
- único proyecto visible en ese workspace: `solqaryn-dev`.

Con esto el ownership operativo del Vercel DEV nuevo queda confirmado.

## Pendiente de retiro legacy

El conector Vercel actual no tiene acceso al team/cuenta personal antigua. El siguiente paso manual es abrir esa cuenta y revisar el proyecto `variapp-desarrollo`; si no contiene dominios/variables/recursos que deban conservarse, se elimina. No se debe tocar `varistorehn` productivo en esta fase DEV.

Evidencia visual recibida el 2026-09-25: el workspace personal `VariApp` muestra dos proyectos, `variapp-desarrollo` y `varistorehn`. Se confirma así el inventario legacy previo a eliminación. Solo `variapp-desarrollo` pertenece al alcance DEV.

## Observación Cloudinary

El catálogo migrado todavía contiene referencias de medios con prefijos históricos como `desarrollo/` y `varistorehn_desarrollo/`. Esto pertenece al siguiente punto Cloudinary; no borrar esos activos hasta completar esa auditoría.
