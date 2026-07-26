# VariApp — Separación segura de Desarrollo y Producción

## 1. Regla principal

| Elemento | Producción | Desarrollo |
|---|---|---|
| Rama Git | `main` | `Desarrollo` |
| Pull Request | No se trabaja directamente | PR borrador `Desarrollo -> main` |
| Vercel | `varistorehn.vercel.app` | `variapp-desarrollo.vercel.app` |
| Render | `variapp-api.onrender.com` | `variapp-api-desarrollo.onrender.com` |
| Aiven MySQL | Servicio y base productivos actuales | Servicio independiente o fork de desarrollo |
| Cloudinary | Credenciales y activos productivos | Product environment/cuenta separada o prefijo `desarrollo/` |

`main`, Render productivo, Aiven productivo y Cloudinary productivo no se modifican hasta completar todas las validaciones en Desarrollo y recibir autorización expresa de Javier Mejía.

## 2. Protecciones ya implementadas en el repositorio

- La rama oficial de trabajo es `Desarrollo`.
- El PR hacia `main` permanece en borrador.
- El CI compila backend, ejecuta pruebas, compila frontend y revisa temporales.
- `backend/Dockerfile` permite construir la API de forma reproducible en Render.
- `render.yaml` define únicamente el servicio de desarrollo `variapp-api-desarrollo`.
- `frontend/vercel.json` dirige:
  - el host productivo `varistorehn.vercel.app` a `variapp-api.onrender.com`;
  - cualquier Preview u otro host a `variapp-api-desarrollo.onrender.com`.
- Las migraciones están deshabilitadas por defecto con `Database__ApplyMigrationsOnStartup=false`.
- Cloudinary admite `Cloudinary__EnvironmentPrefix`:
  - producción: vacío, conserva las rutas actuales;
  - desarrollo: `desarrollo`, crea activos bajo `desarrollo/...`.
- Los secretos no están versionados.

## 3. Render

### Producción — conservar sin cambios

Servicio actual:

```text
variapp-api.onrender.com
```

Debe continuar vinculado a `main`. No debe cambiar de rama, base de datos, secretos ni estrategia de migración durante las pruebas de Desarrollo.

### Desarrollo — pendiente de crear en la cuenta

El archivo `render.yaml` está preparado para crear:

```text
variapp-api-desarrollo.onrender.com
```

Pasos en Render:

1. Crear un Blueprint desde el repositorio `jmejia31/VariApp`.
2. Seleccionar el archivo `render.yaml` de la rama `Desarrollo`.
3. Confirmar que el servicio se llame `variapp-api-desarrollo` y use la rama `Desarrollo`.
4. Completar únicamente los secretos marcados como `sync: false`.
5. Usar exclusivamente la conexión de Aiven Desarrollo.
6. Mantener `Database__ApplyMigrationsOnStartup=false` en el primer despliegue.
7. Confirmar que `/health` responde `200`.
8. Aplicar las migraciones una sola vez contra la base de Desarrollo.
9. Volver a confirmar que las migraciones automáticas queden deshabilitadas.

Variables obligatorias de Desarrollo:

```text
ConnectionStrings__DefaultConnection
Cloudinary__CloudName
Cloudinary__ApiKey
Cloudinary__ApiSecret
Smtp__Host
Smtp__UsuarioSmtp
Smtp__PasswordSmtp
Smtp__CorreoRemitente
SeedAdmin__Username
SeedAdmin__Password
```

No copiar la cadena de conexión productiva en el servicio de Desarrollo.

## 4. Aiven MySQL

### Opción recomendada

Crear un fork independiente del servicio productivo desde un respaldo reciente. El fork es independiente y permite probar migraciones sin cargar ni modificar el servicio original.

Nombre sugerido:

```text
variapp-mysql-desarrollo
```

Configuración mínima:

1. Crear el fork o un servicio MySQL independiente.
2. Crear una base llamada `inventoryapp_desarrollo`.
3. Crear un usuario exclusivo, por ejemplo `variapp_desarrollo`.
4. No utilizar `avnadmin` en la aplicación salvo durante una operación administrativa controlada.
5. Configurar reglas de acceso únicamente para los orígenes necesarios.
6. Guardar la cadena de conexión solo en Render Desarrollo.
7. Verificar que exista al menos un respaldo antes de probar migraciones.
8. Aplicar todas las migraciones en orden.
9. Verificar `__EFMigrationsHistory`.
10. Ejecutar pruebas funcionales y comparar conteos críticos.

Si el fork contiene información real, limitar su acceso y anonimizar datos personales antes de compartirlo con colaboradores o usarlo para demostraciones.

## 5. Cloudinary

### Alternativa más segura

Usar un product environment independiente para Desarrollo. En planes que no permiten varios product environments, utilizar una segunda cuenta gratuita exclusivamente para Desarrollo.

### Alternativa temporal ya soportada por el código

Usar las mismas credenciales con:

```text
Cloudinary__EnvironmentPrefix=desarrollo
```

Esto almacena las nuevas cargas de Desarrollo en carpetas separadas, por ejemplo:

```text
desarrollo/inventoryapp/productos
desarrollo/inventoryapp/compras
desarrollo/variapp/perfiles
```

Producción debe mantener el prefijo vacío para conservar sus URLs y public IDs actuales.

Validaciones pendientes:

- cargar, reemplazar, descargar y eliminar una imagen de producto;
- cargar y eliminar una fotografía de perfil;
- cargar comprobantes JPG, PNG, WebP y PDF;
- confirmar que Desarrollo no elimina activos productivos;
- revisar activos huérfanos y duplicados;
- confirmar límites de almacenamiento, transformación y ancho de banda.

## 6. Vercel

### Proyecto productivo

- Proyecto actual: VariStorehn/VariApp productivo.
- Production Branch: `main`.
- Dominio: `varistorehn.vercel.app`.
- No cambiar el dominio ni la rama productiva.

### Proyecto de desarrollo

Crear un segundo proyecto con estos valores:

```text
Nombre: variapp-desarrollo
Repositorio: jmejia31/VariApp
Root Directory: frontend
Production Branch: Desarrollo
Dominio sugerido: variapp-desarrollo.vercel.app
```

El `vercel.json` ya garantiza que el dominio productivo utilice el backend productivo y que los demás hosts utilicen el backend de Desarrollo.

Después de crear el proyecto:

1. Confirmar build de Angular.
2. Abrir `/login`.
3. Confirmar que `/api` responde desde `variapp-api-desarrollo.onrender.com`.
4. Confirmar que el frontend productivo sigue respondiendo desde `variapp-api.onrender.com`.
5. No promover un deployment de `Desarrollo` al proyecto productivo.

Si se agrega un dominio productivo nuevo, debe añadirse como condición productiva en `frontend/vercel.json` antes de utilizarlo.

## 7. Orden correcto de activación

1. Crear Aiven Desarrollo.
2. Crear credenciales o separación de Cloudinary Desarrollo.
3. Crear Render Desarrollo y cargar secretos.
4. Aplicar migraciones únicamente en Aiven Desarrollo.
5. Crear Vercel Desarrollo.
6. Ejecutar pruebas automáticas y manuales.
7. Corregir defectos en `Desarrollo`.
8. Repetir pruebas hasta obtener aprobación completa.
9. Revisar el script de migración productiva y respaldo.
10. Solo con autorización expresa, planificar el merge y despliegue productivo.

## 8. Validación obligatoria antes de producción

- Administrador, Vendedor y rol personalizado.
- Aislamiento de ventas, facturas, finanzas y movimientos por usuario.
- CRUD y eliminación lógica.
- Productos, categorías, clientes y proveedores.
- Compras y comprobantes.
- Ventas, impuestos, descuentos y cálculos.
- PDF visual, descarga e impresión.
- WhatsApp desde teléfono físico.
- Entrega real por Gmail/SMTP.
- Cloudinary real.
- Teléfono, tablet y escritorio.
- Auditoría y ausencia de secretos/tokens completos.
- Migración, conteos y `__EFMigrationsHistory`.

## 9. Responsabilidad compartida

ChatGPT, Codex, Antigravity y Javier deben trabajar desde `Desarrollo`, actualizar GitHub después de cada commit intencional y documentar resultados en el PR o en el issue de coordinación.

Ningún agente puede:

- hacer merge a `main`;
- cambiar servicios productivos;
- aplicar migraciones en Aiven productivo;
- reutilizar una base productiva para Desarrollo;
- borrar activos productivos;
- desplegar a producción;

sin autorización expresa de Javier Mejía.
