# Certificación Vercel DEV — 2026-09-25

Estado: **PASS técnico / PENDIENTE ownership visual + retiro legacy personal**

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

## Pendiente manual mínimo

El conector Vercel actual no expone el email del owner/member principal del team y no ve el proyecto legacy de la cuenta personal. Para cerrar este punto completamente, el propietario debe:

1. mostrar `Vercel -> SOLQARYN team -> Settings/Members/General` donde se vea que la cuenta corporativa es `solqaryn.platform@outlook.com`;
2. después abrir la cuenta personal antigua y mostrar el proyecto `variapp-desarrollo` antes de eliminarlo.

No se debe tocar `varistorehn` productivo en esta fase DEV.

## Observación Cloudinary

El catálogo migrado todavía contiene referencias de medios con prefijos históricos como `desarrollo/` y `varistorehn_desarrollo/`. Esto pertenece al siguiente punto Cloudinary; no borrar esos activos hasta completar esa auditoría.
