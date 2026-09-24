# Solqaryn — separación segura de Desarrollo y Producción

## 1. Entornos oficiales

Los dos environments GitHub canónicos de SOLQARYN son:

```text
Desarrollo
Produccion
```

| Elemento | Produccion | Desarrollo |
|---|---|---|
| Git | `main`, congelada salvo autorización expresa | `Desarrollo`, rama ordinaria de trabajo |
| GitHub Environment | `Produccion` | `Desarrollo` |
| Vercel | proyecto técnico productivo vigente | proyecto técnico de Desarrollo vigente |
| Render | servicio técnico productivo vigente | servicio técnico de Desarrollo vigente |
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
- Desarrollo y Produccion comparten host, puerto, nodo y recursos físicos del mismo servicio Free; la frontera entre ambos es lógica por base, usuario, secretos y environment;
- no se cruzan credenciales, bases ni cadenas de conexión entre environments.

El aislamiento cruzado DEV -> PROD y PROD -> DEV fue validado con denegación MySQL real antes de conectar GitHub.

## 3. GitHub Actions canónico

Environment `Desarrollo`:

- variables: `SOLQARYN_DESARROLLO_DB_HOST`, `SOLQARYN_DESARROLLO_DB_PORT`, `SOLQARYN_DESARROLLO_DB_NAME`, `SOLQARYN_DESARROLLO_DB_USER`;
- secrets: `SOLQARYN_DESARROLLO_DB_PASSWORD`, `SOLQARYN_DESARROLLO_BACKUP_PASSPHRASE`, `SOLQARYN_AIVEN_TOKEN`.

Environment `Produccion`:

- variables: `SOLQARYN_PRODUCCION_DB_HOST`, `SOLQARYN_PRODUCCION_DB_PORT`, `SOLQARYN_PRODUCCION_DB_NAME`, `SOLQARYN_PRODUCCION_DB_USER`;
- secrets: `SOLQARYN_PRODUCCION_DB_PASSWORD`, `SOLQARYN_PRODUCCION_BACKUP_PASSPHRASE`, `SOLQARYN_AIVEN_TOKEN`.

Las connection strings completas no se almacenan como secretos duplicados; se construyen en memoria cuando un workflow autorizado las necesita.

## 4. Protección de environments

- `Desarrollo` debe aceptar únicamente la rama `Desarrollo`.
- `Produccion` debe aceptar únicamente `main`.
- No reutilizar un Environment para ambos ámbitos.
- No exponer secretos en logs, artifacts ni documentación.

## 5. Producción congelada

Configurar secretos/variables de `Produccion` no autoriza despliegues, migraciones, escrituras de datos ni cambios en `main`.

Cualquier acción productiva posterior requiere autorización expresa del propietario y validaciones causales aplicables.

## 6. Eliminación de legado

Un Environment, secret o variable antigua solo se retira después de demostrar que:

1. no existe consumidor vivo en workflows/código;
2. el reemplazo canónico está configurado;
3. las pruebas causales del reemplazo pasan;
4. no se elimina un dato o infraestructura productiva externa por confundirla con metadata de GitHub.

Los Environments GitHub antiguos con nombres de proveedor, preview, deployment o identidad retirada no son autoridad de configuración una vez que `Desarrollo` y `Produccion` están certificados.

## 7. Validación operativa

La configuración canónica de Desarrollo se valida con:

- prueba de token/alcance Aiven sin exponer secretos;
- certificado de proveedor;
- backup cifrado real;
- restore del mismo artifact en MySQL descartable;
- scope lock de SOLQARYN.

## 8. Acceso y operación

El trabajo ordinario continúa en `Desarrollo`. No crear ramas adicionales, no force-push y no auto-merge de `Desarrollo -> main`.

Las reglas colaborativas completas están en `AGENTS.md`; la memoria técnica está en `PROJECT_CONTEXT.md`.
