# VariApp — separación segura de Desarrollo y Producción

## 1. Entornos oficiales

Solo existen dos entornos lógicos autorizados:

```text
varistorehn_producción (Producción)
varistorehn_desarrollo
```

Los nombres técnicos existentes de servicios, proyectos, dominios, bases, usuarios o claves pueden diferir. Deben mapearse a uno de los dos entornos oficiales y no deben renombrarse ni recrearse cuando exista riesgo de afectar funcionamiento.

| Elemento | varistorehn_producción (Producción) | varistorehn_desarrollo |
|---|---|---|
| Git | rama `main`, referencia de solo lectura | rama única `Desarrollo` |
| Vercel | proyecto técnico `varistorehn`, dominio `varistorehn.vercel.app` | proyecto técnico `variapp-desarrollo`, dominio `variapp-desarrollo.vercel.app` |
| Render | servicio técnico productivo `variapp-api` | servicio técnico `variapp-api-desarrollo` |
| Aiven | datos, variables y usuario administrativo existentes | usuario de aplicación `varistorehn_desarrollo` y variables de Desarrollo existentes |
| Cloudinary | claves, activos y variables productivas existentes | clave etiquetada `varistorehn_desarrollo` y prefijo `varistorehn_desarrollo/` |

## 2. Regla absoluta sobre Producción

Producción no se toca. Está prohibido modificar o eliminar:

- rama `main`;
- variables, secretos y credenciales productivos;
- dominios, certificados, servicios y despliegues productivos;
- bases, usuarios, datos, respaldos o migraciones productivos;
- usuario administrativo `avnadmin` de Aiven;
- claves `Raíz`, moderación y flujos de medios de Cloudinary;
- activos Cloudinary productivos;
- variables ya existentes de Producción y Desarrollo.

No se realizará ni siquiera un cambio de nombre visible sobre Producción durante estas fases.

## 3. Regla de eliminación

Solo puede eliminarse un recurso cuando se compruebe que:

1. pertenece exclusivamente a Desarrollo;
2. duplica una función ya cubierta por `varistorehn_desarrollo`;
3. no tiene consumidores, dependencias, datos, secretos ni referencias activas;
4. su eliminación no afecta Producción;
5. Javier Mejía autoriza expresamente la eliminación.

Nunca se elimina un recurso por coincidencia de nombre. `avnadmin`, las claves internas de Cloudinary, previews históricos y ejecuciones anteriores no son automáticamente terceros entornos.

Con la evidencia disponible no se identificó un tercer entorno permanente que pudiera eliminarse de forma segura. Por esa razón no se eliminó ningún recurso externo.

## 4. GitHub

- `main`: referencia productiva, no se modifica.
- `Desarrollo`: única rama de trabajo.
- No se crean ramas adicionales.
- Cada cambio se confirma y publica en `origin/Desarrollo`.
- El PR `Desarrollo -> main` permanece en borrador.
- No hay auto-merge.

## 5. Render

### Producción

Se conserva sin cambios el servicio técnico existente `variapp-api`, sus variables, dominio, región, conexión y despliegue.

### Desarrollo

Recurso autorizado:

- entorno lógico: `varistorehn_desarrollo`;
- entorno visible actual: Desarrollo;
- servicio técnico existente: `variapp-api-desarrollo`;
- rama: `Desarrollo`;
- runtime: Docker;
- dominio: `variapp-api-desarrollo.onrender.com`.

Variables de Desarrollo que se mantienen:

```text
ConnectionStrings__DefaultConnection
Cloudinary__CloudName
Cloudinary__ApiKey
Cloudinary__ApiSecret
Cloudinary__EnvironmentPrefix=varistorehn_desarrollo
Smtp__Host
Smtp__UsuarioSmtp
Smtp__PasswordSmtp
Smtp__CorreoRemitente
SeedAdmin__Username
SeedAdmin__Password
Database__ApplyMigrationsOnStartup=false
```

Los valores no deben compartirse ni copiarse al repositorio. Su auditoría corresponde a la Fase 2.

## 6. Aiven

- El servicio existente se conserva.
- `avnadmin` se conserva como usuario administrativo.
- El usuario de aplicación de Desarrollo es `varistorehn_desarrollo`.
- Las variables y credenciales actuales de Producción y Desarrollo se mantienen.
- La aplicación de Desarrollo no debe usar `avnadmin`.
- La aplicación de Desarrollo debe usar su base y credenciales designadas.
- No se crea otro servicio, base o usuario por iniciativa de un agente.

La revisión de bases, privilegios y conexiones cruzadas corresponde a la Fase 2 y se hará sin modificar Producción.

## 7. Cloudinary

- Las claves productivas y las claves internas se conservan.
- La clave etiquetada `varistorehn_desarrollo` es la referencia autorizada para Desarrollo.
- El prefijo obligatorio es `varistorehn_desarrollo/`.
- Desarrollo no puede eliminar un `PublicId` que no comience con ese prefijo.

Nuevas cargas de Desarrollo:

```text
varistorehn_desarrollo/inventoryapp/productos
varistorehn_desarrollo/inventoryapp/compras
varistorehn_desarrollo/variapp/perfiles
```

Producción mantiene sus rutas y variables actuales.

## 8. Vercel

### Producción

Se conservan sin cambios el proyecto técnico `varistorehn`, su dominio, variables, rama productiva y despliegues.

### Desarrollo

Recurso autorizado:

- entorno lógico: `varistorehn_desarrollo`;
- proyecto técnico existente: `variapp-desarrollo`;
- Production Branch: `Desarrollo`;
- dominio: `variapp-desarrollo.vercel.app`;
- Root Directory: `frontend`.

El proxy mantiene:

```text
varistorehn.vercel.app/api/* -> variapp-api.onrender.com
variapp-desarrollo.vercel.app/api/* -> variapp-api-desarrollo.onrender.com
```

Los Preview generados dentro del proyecto productivo no se modificarán desde este trabajo porque hacerlo cambiaría la configuración de Producción. Se auditarán en la Fase 2 y solo se cambiarán con autorización expresa.

## 9. Cierre de la Fase 1

La Fase 1 queda cerrada porque:

- se definieron exactamente dos entornos lógicos;
- los recursos aportados por el propietario quedaron asignados a esos entornos;
- Producción quedó formalmente congelada;
- `Desarrollo` quedó como única rama de trabajo;
- el prefijo oficial `varistorehn_desarrollo` está versionado y validado por CI;
- se conservaron variables y recursos existentes;
- no se ejecutó ninguna eliminación insegura;
- no se identificó un tercer entorno permanente confirmado;
- la compilación y la aceptación automatizada de la estandarización quedaron aprobadas.

La Fase 2 puede iniciar como auditoría de solo lectura y correcciones exclusivamente en `Desarrollo`.
