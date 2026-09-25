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

La configuración canónica de DEV se valida con:

- prueba de token/alcance Aiven sin exponer secretos;
- certificado de proveedor;
- backup cifrado real;
- restore del mismo artifact en MySQL descartable;
- scope lock de SOLQARYN.

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
