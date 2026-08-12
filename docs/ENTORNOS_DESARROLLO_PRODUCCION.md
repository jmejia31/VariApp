# VariApp — separación segura de Desarrollo y Producción

## 1. Entornos oficiales

Solo existen dos entornos lógicos autorizados:

```text
varistorehn_producción
varistorehn_desarrollo
```

Los nombres técnicos de servicios, dominios, bases, usuarios o claves pueden diferir; deben mapearse a uno de esos dos entornos y no renombrarse/recrearse si existe riesgo de afectar operación.

| Elemento | Producción | Desarrollo |
|---|---|---|
| Git | `main`, congelada | `Desarrollo`, única rama de trabajo |
| Vercel | proyecto técnico `varistorehn` | proyecto técnico `variapp-desarrollo` |
| Render | servicio técnico `variapp-api` | servicio técnico `variapp-api-desarrollo` |
| Aiven | datos/variables/administración productiva | usuario/base/variables de aplicación de Desarrollo |
| Cloudinary | claves/activos productivos | prefijo `varistorehn_desarrollo/` y credenciales de Desarrollo |

## 2. Producción congelada

Está prohibido modificar o eliminar desde el flujo de Desarrollo:

- `main`;
- variables, secretos y credenciales productivos;
- dominios, certificados, servicios y despliegues productivos;
- bases, usuarios, datos, respaldos o migraciones productivos;
- usuario administrativo `avnadmin` de Aiven;
- claves internas/productivas y activos productivos de Cloudinary;
- recursos externos basándose únicamente en su nombre.

No desplegar ni migrar Producción sin autorización expresa de Javier Mejía.

## 3. Regla de eliminación

Un recurso solo puede eliminarse si se demuestra que:

1. pertenece exclusivamente a Desarrollo;
2. duplica una función ya cubierta;
3. no tiene consumidores, dependencias, datos ni secretos necesarios;
4. no afecta Producción;
5. Javier autoriza expresamente su eliminación.

## 4. GitHub

- `Desarrollo` es la única rama de trabajo.
- No crear ramas adicionales.
- Cada cambio autorizado se publica en `origin/Desarrollo`.
- PR `Desarrollo -> main` permanece en borrador.
- No auto-merge.

## 5. Acceso local y remoto

Acceso reconocido al proyecto local de la PC:

- Javier Mejía;
- Codex;
- AntiG / Antigravity.

ChatGPT y otros agentes no tienen acceso local por defecto. Pueden operar GitHub solo mediante un conector autorizado. Una operación remota en GitHub no equivale a sincronizar la copia local.

Después de un cambio remoto, Javier/Codex/AntiG sincronizan localmente:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
```

## 6. Render

Producción conserva `variapp-api` sin cambios.

Desarrollo utiliza el servicio técnico `variapp-api-desarrollo`, rama `Desarrollo` y configuración/secretos exclusivos de Desarrollo.

No copiar valores reales al repositorio.

## 7. Aiven

- conservar el servicio existente y `avnadmin` como administrador;
- aplicación de Desarrollo debe usar su usuario/base designados, no `avnadmin`;
- no crear/eliminar servicio, base o usuario por iniciativa de un agente;
- no cruzar cadenas de conexión entre Producción y Desarrollo.

## 8. Cloudinary

- conservar claves y activos productivos;
- Desarrollo usa el prefijo obligatorio `varistorehn_desarrollo/`;
- Desarrollo no elimina `PublicId` fuera de ese prefijo;
- secretos Cloudinary nunca se versionan.

## 9. Vercel

- Producción conserva el proyecto `varistorehn` y su dominio/configuración sin cambios.
- Desarrollo utiliza `variapp-desarrollo`, con `frontend` como raíz técnica cuando corresponda y backend de Desarrollo.
- Un preview de Desarrollo nunca debe apuntar a API/base productiva.

## 10. Rendimiento de los agentes

Esta separación de entornos ya está documentada. Por tanto:

- no volver a auditar todos los proveedores externos en cada tarea;
- no releer este documento si no cambió y la tarea no afecta infraestructura;
- consultar únicamente la sección/recurso directamente afectado;
- una reconexión no justifica repetir la auditoría de entornos;
- cualquier cambio estructural de infraestructura sí debe actualizar `PROJECT_CONTEXT.md` y este documento.

## 11. Fuente de reglas

Las reglas colaborativas completas están en `AGENTS.md`. La memoria técnica está en `PROJECT_CONTEXT.md`. Los pendientes viven en `TASKS.md`.