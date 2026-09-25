# Solqaryn — separación segura de DEV y PROD

## 1. Entornos oficiales

Los dos environments GitHub canónicos de SOLQARYN son:

```text
DEV
PROD
```

| Elemento | PROD | DEV |
|---|---|---|
| Git | `main`, congelada salvo autorización expresa | `dev`, rama ordinaria de trabajo |
| GitHub Environment | `PROD` | `DEV` |
| Vercel | proyecto técnico productivo vigente | proyecto técnico DEV vigente |
| Render | servicio técnico productivo vigente | servicio técnico DEV vigente |
| Aiven project | `solqaryn` | `solqaryn` |
| Aiven service | `solqaryn-mysql` | `solqaryn-mysql` |
| Base MySQL | `solqaryn_prod` | `solqaryn_dev` |
| Usuario MySQL app | `solqaryn_prod_user` | `solqaryn_dev_user` |

VariStoreHN es un cliente de SOLQARYN; sus nombres de proyecto, dominio o activos no redefinen los environments canónicos de la plataforma.

## 2. Topología Aiven vigente

Aiven usa un único servicio MySQL Free, `solqaryn-mysql`, dentro del proyecto `solqaryn`.

Separación lógica obligatoria:

- `solqaryn_dev_user` tiene privilegios únicamente sobre `solqaryn_dev.*`;
- `solqaryn_prod_user` tiene privilegios únicamente sobre `solqaryn_prod.*`;
- `avnadmin` se conserva solo para administración y no se usa como credencial normal de aplicación;
- DEV y PROD comparten host, puerto, nodo y recursos físicos del mismo servicio Free; la frontera entre ambos es lógica por base, usuario, secretos y environment;
- no se cruzan credenciales, bases ni cadenas de conexión entre environments.

El aislamiento cruzado DEV -> PROD y PROD -> DEV fue validado con denegación MySQL real antes de conectar GitHub.

## 3. GitHub Actions canónico

Environment `DEV`:

- variables: `SOLQARYN_DEV_DB_HOST`, `SOLQARYN_DEV_DB_PORT`, `SOLQARYN_DEV_DB_NAME`, `SOLQARYN_DEV_DB_USER`;
- secrets: `SOLQARYN_DEV_DB_PASSWORD`, `SOLQARYN_DEV_BACKUP_PASSPHRASE`, `SOLQARYN_AIVEN_TOKEN`.

Environment `PROD`:

- variables: `SOLQARYN_PROD_DB_HOST`, `SOLQARYN_PROD_DB_PORT`, `SOLQARYN_PROD_DB_NAME`, `SOLQARYN_PROD_DB_USER`;
- secrets: `SOLQARYN_PROD_DB_PASSWORD`, `SOLQARYN_PROD_BACKUP_PASSPHRASE`, `SOLQARYN_AIVEN_TOKEN`.

Las connection strings completas no se almacenan como secretos duplicados; se construyen en memoria cuando un workflow autorizado las necesita.

## 4. Protección de environments

- `DEV` debe aceptar únicamente la rama `dev`.
- `PROD` debe aceptar únicamente `main`.
- No reutilizar un Environment para ambos ámbitos.
- No exponer secretos en logs, artifacts ni documentación.

## 5. PROD congelado

Configurar secretos/variables de `PROD` no autoriza despliegues, migraciones, escrituras de datos ni cambios en `main`.

Cualquier acción productiva posterior requiere autorización expresa del propietario y validaciones causales aplicables.

## 6. Eliminación de legado

Un Environment, secret o variable antigua solo se retira después de demostrar que:

1. no existe consumidor vivo en workflows/código;
2. el reemplazo canónico está configurado;
3. las pruebas causales del reemplazo pasan;
4. no se elimina un dato o infraestructura productiva externa por confundirla con metadata de GitHub.

Los Environments GitHub antiguos con nombres de proveedor, preview, deployment o identidad retirada no son autoridad de configuración una vez que `DEV` y `PROD` están certificados.

## 7. Validación operativa

### Certificación Aiven DEV — 2026-09-25

Estado: **CERRADO / PASS**.

Evidencia canónica: GitHub Actions run `36175275439` (`DEV - Certificación canónica Aiven`) y artifact `aiven-dev-certification-36175275439`.

- proyecto Aiven: `solqaryn`;
- servicio: `solqaryn-mysql`;
- servicio tipo MySQL y estado `RUNNING`;
- la organización que contiene el proyecto incluye `solqaryn.platform@outlook.com`;
- el endpoint configurado en DEV coincide con el servicio canónico;
- base efectiva: `solqaryn_dev`;
- usuario MySQL efectivo: `solqaryn_dev_user`;
- versión observada: MySQL `8.4.8`;
- esquema migrado: 137 tablas base / 107 migraciones EF;
- datos de catálogo presentes: 8 productos / 2 categorías;
- secretos impresos: NO;
- Producción tocada: NO.

Con esta evidencia, Aiven DEV se considera migrado/certificado en la cuenta canónica. El servicio/base legacy personal no debe eliminarse hasta verificar por separado que ningún recurso PROD legacy lo consume.

La configuración canónica de DEV se valida con:

- prueba de token/alcance Aiven sin exponer secretos;
- certificado de proveedor;
- backup cifrado real;
- restore del mismo artifact en MySQL descartable;
- scope lock de SOLQARYN.

### Certificación Render DEV — 2026-09-25

Estado: **CERRADO / PASS**.

Evidencia leída directamente del workspace y servicio Render:

- workspace: `SOLQARYN`;
- email del workspace: `solqaryn.platform@outlook.com`;
- servicio: `solqaryn-api-dev`;
- ownerId: workspace SOLQARYN;
- repositorio: `https://github.com/solqaryn/Solqaryn`;
- rama: `dev`;
- auto deploy: `checksPass`;
- runtime: Docker;
- Dockerfile: `./backend/Dockerfile`;
- health check: `/health/ready`;
- URL: `https://solqaryn-api-dev-fxx8.onrender.com`;
- estado: activo/no suspendido;
- maintenance: desactivado;
- logs de arranque actuales: conexión a `solqaryn_dev` en `solqaryn-mysql-solqaryn.h.aivencloud.com`;
- logs de health actuales: HTTP 200 repetido en `/health/ready`.

Conclusión: Render DEV canónico está bajo la cuenta/workspace SOLQARYN y consume el Aiven DEV nuevo, no el host/base legacy personal. Cualquier servicio Render DEV de la cuenta personal puede retirarse únicamente después de identificarlo en esa cuenta y comprobar que no tiene consumidores restantes.

### Certificación Vercel DEV — 2026-09-25

Estado del recurso nuevo: **CERRADO / PASS**.

- team: `SOLQARYN`;
- team ID: `team_owJ2SudSPWiEzeiDthSVV063`;
- proyectos visibles en el team: únicamente `solqaryn-dev`;
- project ID: `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`;
- dominio canónico: `solqaryn-dev.vercel.app`;
- deployments observados: `READY` y vinculados a `solqaryn/Solqaryn`, rama `dev`;
- `/`, `/login`, `/dashboard`, `/varistorehn` y `/varistorehn/productos`: HTTP 200;
- `/api/empresa-configuracion/publica`: HTTP 200 y devuelve VariStoreHN;
- `/api/tienda/categorias`: HTTP 200 con 2 categorías;
- `/api/tienda/productos?pagina=1&tamano=1`: HTTP 200 con catálogo migrado;
- runtime errors Vercel últimas 24h: ninguno.

Ownership visual confirmado por el propietario en el dashboard: la sesión que administra `vercel.com/solqaryn` corresponde a `solqarynplatform-5337` con correo `solqaryn.platform@outlook.com`, mientras el workspace/team activo es `SOLQARYN`.

El recurso nuevo Vercel DEV queda cerrado. El único pendiente de Vercel para retirar la dependencia personal es entrar a la cuenta antigua y eliminar exclusivamente el proyecto `proyecto Vercel DEV legacy retirado` después de una última inspección de dominios/variables. No tocar `varistorehn` PROD durante esta fase.

Evidencia visual de la cuenta personal legacy confirmó que el workspace `workspace Vercel personal legacy` contiene los proyectos `proyecto Vercel DEV legacy retirado` y `varistorehn`. En el cierre DEV, solo `proyecto Vercel DEV legacy retirado` entra en alcance de retiro; `varistorehn` permanece congelado para la futura fase PROD.

Cierre final: el propietario eliminó `proyecto Vercel DEV legacy retirado` y una captura posterior del workspace personal muestra únicamente `varistorehn`. Resultado: **VERCEL DEV LEGACY RETIRADO / CERRADO**. `varistorehn` permanece intacto para PROD.

La presencia de URLs Cloudinary históricas en datos migrados se trata en el punto Cloudinary y no invalida la certificación técnica de Vercel; sí impide cerrar DEV global hasta certificar medios.

### Auditoría Cloudinary DEV — ownership confirmado, credenciales/assets pendientes

Estado parcial: **OWNERSHIP PASS / CREDENCIALES Y ASSETS PENDIENTES**.

Evidencia visual del panel Cloudinary:

- perfil: `Solqaryn Platform`;
- email: `solqaryn.platform@outlook.com`;
- Product Environments: 1 (límite actual del plan);
- cloud name activo: `riyrzmob`;
- Product Environment ID: `7ab9e3e6de660a0b70eb4a5bacf331`;
- estado: `Active`;
- Media Library del nuevo cloud: solo carpeta/activos de ejemplo observados.

El código DEV exige `Cloudinary__EnvironmentPrefix=solqaryn_dev` y construye nuevas rutas bajo `solqaryn_dev/.../`. Sin embargo, el catálogo migrado conserva URLs históricas de otro cloud/prefijos legacy. Por tanto no se elimina ningún activo o credencial Cloudinary antigua todavía.

Control funcional completado: Render DEV usa el cloud `riyrzmob` con `Cloudinary__EnvironmentPrefix=solqaryn_dev`, el deploy posterior quedó `live`, y un upload real desde la aplicación creó un asset en `solqaryn_dev/inventoryapp/productos/empresas/1/`. La API pública de DEV ya devuelve esa URL nueva.

Inventario read-only canónico: GitHub Actions run `36182095589`, artifact `cloudinary-dev-legacy-inventory-36182095589`. Se detectaron 13 filas lógicas que todavía dependen del cloud legacy: 10 imágenes de producto, 1 documento de compra y 2 fotos de perfil. No hubo escrituras ni contacto con PROD. Cloudinary DEV no se considera cerrado globalmente hasta copiar esos assets al cloud `riyrzmob`, actualizar URL/PublicId en `solqaryn_dev` y obtener un inventario final con cero referencias legacy.

## 8. Gobierno de acceso GitHub

- Organización y repositorio canónicos: `solqaryn/Solqaryn`.
- Identidad corporativa primaria para la plataforma y sus proveedores: `solqaryn.platform@outlook.com`.
- `jmejia31` permanece deliberadamente como **Owner secundario/de recuperación** de la organización `solqaryn`. Su permiso efectivo `admin` sobre el repositorio es correcto mientras conserve ese rol.
- `morales35alex` permanece como colaborador externo con permiso `write`.
- No clasificar el rol Owner/Admin efectivo de `jmejia31` como legado a eliminar. La anotación histórica del 2026-09-23 que proponía retirarlo queda **supersedida** por esta decisión ratificada el 2026-09-25.
- El repositorio, workflows, environments y configuración canónica siguen perteneciendo a la organización `solqaryn`; ninguna operación debe volver a depender de un repositorio personal.

## 8. Acceso y operación

El trabajo ordinario continúa en `dev`. No crear ramas adicionales, no force-push y no auto-merge de `dev -> main`.

Las reglas colaborativas completas están en `AGENTS.md`; la memoria técnica está en `PROJECT_CONTEXT.md`.

### Reconciliación stock DEV — administración vs tienda pública

El 2026-09-25 se detectó un desfase de autoridad: el administrativo mostraba el bridge legacy `ProductoVariante.Cantidad`, mientras la tienda pública consume correctamente `ExistenciaVariante.StockDisponible`. El dataset migrado no había materializado seis existencias físicas de productos activos.

Controles ejecutados:

- diagnóstico read-only: run `36188235798`;
- backup cifrado + restore drill: run `36188392969`;
- reconciliación fail-closed: run `36190745792`;
- único almacén operativo activo usado de forma determinista: `UAT-ALM-001`;
- 6 existencias creadas;
- stock activo bridge: 52;
- stock físico activo: 52;
- variantes activas sin existencia: 0;
- diferencias bridge/físico: 0;
- API pública verificada: Cargador 26, Laptop 15, Funda para samsung 1, UAT Producto 001 10.

Dos variantes asociadas a productos eliminados conservan 5 unidades en campos legacy históricos y permanecen fuera del stock activo y del storefront. No se alteró Producción.


### Cloudflare DEV / DNS — 2026-09-25

Estado: **N/A COMO DEPENDENCIA RUNTIME / CERRADO PARA DEV**.

Readback de proveedores canónicos:

- Vercel project: `solqaryn-dev`;
- dominios DEV: `solqaryn-dev.vercel.app`, `solqaryn-dev-solqaryn.vercel.app`, `solqaryn-dev-git-dev-solqaryn.vercel.app`;
- no se observan dominios custom asociados al proyecto;
- Render API DEV: `https://solqaryn-api-dev-fxx8.onrender.com`;
- no hay configuración Cloudflare en el repositorio ni hostname `solqaryn.com` usado por el runtime DEV.

Conclusión: DEV opera directamente sobre hostnames administrados por Vercel y Render. No existe una dependencia DNS de Cloudflare que deba migrarse antes de retirar recursos personales DEV. La cuenta/zona Cloudflare sigue siendo relevante para dominios propios futuros y/o PROD, pero no es un blocker de cierre DEV y no se tocó en esta fase.
