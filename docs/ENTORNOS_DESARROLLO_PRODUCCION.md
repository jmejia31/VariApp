# VariApp — separación segura de Desarrollo y Producción

## 1. Regla principal

El identificador oficial y obligatorio del entorno de Desarrollo es:

```text
varistorehn_desarrollo
```

Los nombres públicos ya creados por el propietario se conservan porque cambiarlos podría alterar dominios, despliegues o integraciones. El identificador oficial se aplica a usuarios, claves, prefijos y documentación interna.

| Elemento | Producción | Desarrollo autorizado |
|---|---|---|
| Rama Git | `main` | `Desarrollo` |
| Pull Request | No se trabaja directamente | PR borrador `Desarrollo -> main` |
| Vercel | proyecto `varistorehn`, dominio `varistorehn.vercel.app` | proyecto `variapp-desarrollo`, dominio `variapp-desarrollo.vercel.app` |
| Render | servicio `variapp-api` | servicio `variapp-api-desarrollo` |
| Aiven MySQL | usuario y base productivos actuales | usuario de aplicación `varistorehn_desarrollo` y base exclusiva de Desarrollo pendiente de certificar por nombre |
| Cloudinary | clave y activos productivos | clave etiquetada `varistorehn_desarrollo` y prefijo `varistorehn_desarrollo/` |

`main`, Render productivo, Aiven productivo, Vercel productivo y los activos productivos de Cloudinary no se modifican durante este trabajo.

## 2. Evidencia recibida del propietario

Las capturas entregadas el 27 de julio de 2026 muestran:

- Aiven MySQL en ejecución con el usuario `varistorehn_desarrollo` creado junto al usuario administrativo predeterminado;
- una clave de Cloudinary etiquetada `varistorehn_desarrollo`, activa y con el secreto oculto;
- un entorno Render `Desarrollo` con el servicio `variapp-api-desarrollo` desplegado, separado del servicio `variapp-api` de Producción;
- dos proyectos Vercel visibles: `varistorehn` y `variapp-desarrollo`;
- el proyecto `variapp-desarrollo` siguiendo la rama `Desarrollo` y sirviendo `variapp-desarrollo.vercel.app`;
- la aplicación de Desarrollo operativa a través de Vercel y Render.

La evidencia no muestra el nombre de la base de datos Aiven ni los valores de las variables de Render. Por seguridad, esos valores no deben compartirse; solo se requiere una confirmación o captura con valores ocultos.

## 3. Protecciones implementadas en el repositorio

- `Desarrollo` es la única rama de implementación.
- El PR hacia `main` permanece en borrador.
- `render.yaml` define únicamente `variapp-api-desarrollo` sobre la rama `Desarrollo`.
- `frontend/vercel.json` dirige el host productivo al backend productivo y cualquier otro host al backend de Desarrollo.
- Las migraciones automáticas permanecen deshabilitadas con `Database__ApplyMigrationsOnStartup=false`.
- Los secretos se declaran como externos y no están versionados.
- Cloudinary usa `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo`.
- Desarrollo no puede eliminar un `PublicId` de Cloudinary que no comience con `varistorehn_desarrollo/`.
- GitHub Actions valida los nombres, dominios, rama, prefijo y configuración no destructiva.

## 4. Render

### Producción — no tocar

- Servicio: `variapp-api`.
- Rama esperada: `main`.
- No cambiar variables, credenciales, región, dominio, conexión, despliegue ni migraciones.

### Desarrollo — recurso autorizado existente

- Entorno visible: `Desarrollo`.
- Servicio: `variapp-api-desarrollo`.
- Rama: `Desarrollo`.
- Runtime: Docker.
- Dominio esperado: `variapp-api-desarrollo.onrender.com`.

Variables obligatorias de Desarrollo:

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
```

Comprobaciones pendientes para cerrar la fase:

1. Confirmar, sin mostrar la contraseña, que la conexión usa el usuario `varistorehn_desarrollo`.
2. Confirmar que la conexión apunta a una base exclusiva de Desarrollo y no a la base productiva.
3. Confirmar que las credenciales Cloudinary corresponden a la clave etiquetada `varistorehn_desarrollo`.
4. Aplicar el nuevo prefijo `varistorehn_desarrollo` mediante el despliegue normal después de checks verdes.
5. Mantener `Database__ApplyMigrationsOnStartup=false`.
6. Confirmar `/health` con HTTP 200.

## 5. Aiven MySQL

### Recurso autorizado

La captura muestra el servicio `variapp-mysql` y el usuario de aplicación `varistorehn_desarrollo`. No debe crearse otro usuario o servicio de Desarrollo si ese es el recurso que el propietario designó.

El usuario `avnadmin` es el usuario administrativo predeterminado de Aiven; no es un tercer entorno y no debe eliminarse. La aplicación de Desarrollo no debe conectarse como `avnadmin`.

Validaciones obligatorias:

1. Confirmar el nombre exacto de la base exclusiva de Desarrollo.
2. Confirmar que `varistorehn_desarrollo` solo tiene privilegios sobre esa base.
3. Confirmar que Render Desarrollo usa ese usuario y esa base.
4. Confirmar un respaldo reciente antes de aplicar migraciones.
5. Verificar que Producción utiliza otro usuario o, como mínimo, otra base sin privilegios cruzados.
6. Si la base contiene información copiada de Producción, limitar accesos y anonimizar datos personales antes de demostraciones.

No se debe eliminar el servicio `variapp-mysql` ni crear un duplicado hasta confirmar si aloja de forma controlada ambas bases. La independencia mínima exigida es: base separada, usuario separado y ausencia de privilegios cruzados. Un servicio Aiven separado sería una defensa adicional, no una razón para descartar el recurso creado por el propietario.

## 6. Cloudinary

### Recurso autorizado

- Clave etiquetada: `varistorehn_desarrollo`.
- Prefijo obligatorio: `varistorehn_desarrollo/`.

Las claves `Raíz`, `moderación` y las creadas por flujos internos no deben eliminarse automáticamente: son claves, no entornos, y podrían ser dependencias administradas por Cloudinary. Solo se eliminarán claves duplicadas después de identificar su consumidor y confirmar que no pertenecen a una función de la plataforma.

Nuevas cargas de Desarrollo:

```text
varistorehn_desarrollo/inventoryapp/productos
varistorehn_desarrollo/inventoryapp/compras
varistorehn_desarrollo/variapp/perfiles
```

Producción mantiene el prefijo vacío para conservar sus rutas actuales.

Validación externa:

1. Confirmar que Render usa la clave etiquetada `varistorehn_desarrollo`.
2. Subir, reemplazar, descargar y eliminar una imagen de producto.
3. Subir y eliminar una fotografía de perfil.
4. Subir comprobantes JPG, PNG, WebP y PDF.
5. Confirmar que todos los `public_id` nuevos comienzan con `varistorehn_desarrollo/`.
6. Intentar eliminar una referencia histórica sin ese prefijo y confirmar el bloqueo.

## 7. Vercel

### Producción — no tocar

- Proyecto: `varistorehn`.
- Rama esperada: `main`.
- Dominio: `varistorehn.vercel.app`.

### Desarrollo — recurso autorizado existente

- Proyecto: `variapp-desarrollo`.
- Production Branch: `Desarrollo`.
- Dominio: `variapp-desarrollo.vercel.app`.
- Root Directory esperada: `frontend`.

`frontend/vercel.json` conserva el enrutamiento:

- `varistorehn.vercel.app/api/*` -> `variapp-api.onrender.com`;
- cualquier otro host `/api/*` -> `variapp-api-desarrollo.onrender.com`.

Validaciones pendientes:

1. Confirmar Root Directory `frontend`.
2. Confirmar que no existe un tercer proyecto de VariApp/VariStorehn.
3. Abrir `/login` y comprobar una solicitud `/api`.
4. No promover un deployment de `Desarrollo` al proyecto productivo.

## 8. Qué se considera un entorno duplicado

Se considera duplicado un servicio, proyecto, base, usuario de aplicación o conjunto de credenciales creado para cumplir la misma función de Desarrollo y que no haya sido designado por el propietario.

No se considera automáticamente duplicado:

- `avnadmin`, porque es un usuario administrativo predeterminado de Aiven;
- claves internas de Cloudinary asociadas a moderación o flujos de medios;
- despliegues Preview históricos de Vercel, siempre que no sean proyectos o entornos permanentes activos;
- ejecuciones anteriores de Render que pertenezcan al mismo servicio autorizado.

Antes de eliminar cualquier recurso se debe identificar su consumidor, exportar la configuración necesaria y verificar que no afecta Producción.

## 9. Criterio de cierre de la Fase 1

La Fase 1 solo queda completa cuando:

- GitHub Actions aprueba la configuración con `varistorehn_desarrollo`;
- Render Desarrollo usa el usuario Aiven y la clave Cloudinary autorizados;
- la base de Desarrollo está identificada y aislada;
- los privilegios de Aiven no cruzan hacia Producción;
- `/health` responde 200;
- Vercel Desarrollo apunta al backend de Desarrollo;
- no existe ningún tercer entorno permanente confirmado;
- ningún recurso productivo fue modificado.

Hasta entonces no se inicia la auditoría general de la Fase 2.
