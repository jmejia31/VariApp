# Responsabilidades para cerrar la rama Desarrollo

## Regla invariable

Todo trabajo técnico se realiza en `Desarrollo`. `main`, Aiven productivo, Render productivo, Vercel productivo y los activos productivos de Cloudinary permanecen sin cambios hasta que Javier Mejía autorice expresamente un plan de publicación.

Nunca se deben copiar secretos en commits, Pull Requests, issues, capturas, chats o archivos del repositorio.

## 1. Trabajo que puede ejecutar cualquier agente con acceso al repositorio

ChatGPT, Codex, Antigravity o un desarrollador autorizado pueden realizar en `Desarrollo`:

- implementar y corregir backend, frontend, API y migraciones;
- agregar pruebas unitarias, de integración y Playwright;
- ejecutar compilaciones y pruebas con MySQL descartable en GitHub Actions;
- generar SQL forward revisable sin aplicarlo a producción;
- validar tipado, calidad estática, Docker y configuración declarativa;
- revisar consultas, relaciones, permisos, eliminación lógica y auditoría;
- documentar cambios, riesgos y casos de prueba;
- actualizar el PR borrador y el issue colaborativo;
- corregir fallos encontrados por el CI;
- preparar instrucciones para los recursos externos.

Ningún agente debe fusionar a `main`, desplegar producción ni aplicar migraciones productivas sin autorización expresa.

## 2. Trabajo que requiere acceso del propietario o un operador de infraestructura

### Aiven Desarrollo

Responsable: Javier Mejía u operador autorizado con acceso al panel de Aiven.

1. Confirmar un respaldo reciente del servicio productivo.
2. Crear un fork o servicio MySQL independiente llamado `variapp-mysql-desarrollo`.
3. Crear o confirmar la base `inventoryapp_desarrollo`.
4. Crear el usuario de aplicación `variapp_desarrollo` con privilegios limitados a esa base.
5. Restringir las fuentes de acceso a las estrictamente necesarias.
6. Guardar la cadena únicamente como secreto de Render Desarrollo.
7. No compartir la cadena completa por GitHub ni chat.
8. Si el fork contiene datos reales, limitar accesos y anonimizar datos personales antes de demostraciones.

Evidencia requerida:

- nombre del servicio y base;
- captura sin secretos de la sección general;
- confirmación escrita de que no es el servicio productivo;
- confirmación de respaldo disponible.

### Cloudinary Desarrollo

Responsable: operador con acceso a Cloudinary.

Opción recomendada: crear un product environment o cuenta independiente para Desarrollo.

Opción temporal soportada: usar credenciales autorizadas con `Cloudinary__EnvironmentPrefix=desarrollo`.

Validación manual:

1. Subir, reemplazar, descargar y eliminar una imagen de producto.
2. Subir y eliminar una foto de perfil.
3. Subir comprobantes JPG, PNG, WebP y PDF.
4. Confirmar que los `public_id` nuevos comienzan con `desarrollo/`.
5. Intentar eliminar una referencia productiva desde Desarrollo y confirmar que el backend lo bloquea.
6. Revisar duplicados, huérfanos y consumo de almacenamiento/transformación.

Evidencia requerida:

- rutas o `public_id` parcialmente ocultos que muestren el prefijo;
- resultado de las operaciones sin mostrar API secret;
- confirmación de que ningún activo productivo fue eliminado.

### Render Desarrollo

Responsable: operador con acceso al dashboard de Render y a los secretos de Desarrollo.

1. Crear un Blueprint desde `jmejia31/VariApp`.
2. Usar `render.yaml` de la rama `Desarrollo`.
3. Confirmar el servicio `variapp-api-desarrollo` y la rama `Desarrollo`.
4. Cargar exclusivamente secretos no productivos:
   - `ConnectionStrings__DefaultConnection`
   - `Cloudinary__CloudName`
   - `Cloudinary__ApiKey`
   - `Cloudinary__ApiSecret`
   - `Smtp__Host`
   - `Smtp__UsuarioSmtp`
   - `Smtp__PasswordSmtp`
   - `Smtp__CorreoRemitente`
   - `SeedAdmin__Username`
   - `SeedAdmin__Password`
5. Confirmar que la conexión apunta a Aiven Desarrollo.
6. Mantener `Database__ApplyMigrationsOnStartup=false` en operación normal.
7. Comprobar que `/health` responde HTTP 200.

Evidencia requerida:

- nombre, rama y URL del servicio sin secretos;
- respuesta de `/health`;
- confirmación de que no usa la conexión productiva.

### Aplicación controlada de migraciones en Aiven Desarrollo

Responsable: desarrollador u operador autorizado después de crear Aiven y Render Desarrollo.

1. Descargar o generar el SQL forward certificado desde GitHub Actions.
2. Revisar que el destino sea `inventoryapp_desarrollo`.
3. Tomar o confirmar respaldo previo.
4. Aplicar las migraciones una sola vez mediante una estrategia acordada: EF Core CLI o SQL forward, nunca ambas.
5. Verificar `__EFMigrationsHistory`.
6. Comparar conteos de Productos, Categorías, Usuarios, Compras, Ventas y Facturas antes/después.
7. Verificar que los productos legados conservaron Marca y Modelo y recibieron sus nuevas relaciones.
8. Mantener migraciones automáticas deshabilitadas después de la operación.

Evidencia requerida:

- lista de `MigrationId` sin credenciales;
- conteos comparativos;
- resultado de una consulta de muestra de Marca/Modelo normalizados.

### Vercel Desarrollo

Responsable: operador con acceso al proyecto de Vercel.

1. Crear un segundo proyecto llamado `variapp-desarrollo`.
2. Vincular `jmejia31/VariApp`.
3. Configurar Root Directory `frontend`.
4. Configurar Production Branch `Desarrollo`.
5. Usar un dominio de desarrollo, por ejemplo `variapp-desarrollo.vercel.app`.
6. Confirmar que `/api` llega a `variapp-api-desarrollo.onrender.com`.
7. Confirmar por separado que `varistorehn.vercel.app` sigue llegando a `variapp-api.onrender.com`.
8. No promover un deployment de Desarrollo al proyecto productivo.

Evidencia requerida:

- URL del deployment de Desarrollo;
- respuesta correcta de `/login` y `/api`;
- confirmación de ramas de producción de ambos proyectos.

## 3. Pruebas manuales que requieren servicios o dispositivos reales

Responsables: Javier Mejía y un integrante del equipo como testigo/revisor.

- enviar factura por Gmail/SMTP a una cuenta real y revisar bandeja de entrada y spam;
- abrir el enlace de WhatsApp desde un teléfono físico y confirmar texto, número y PDF/enlace;
- revisar visualmente el PDF en navegador, descarga e impresión;
- validar cámara/galería y carga de archivos desde Android o iPhone;
- recorrer teléfono, tablet y escritorio reales;
- probar red lenta, pérdida temporal de conexión y recuperación;
- mantener una venta y una compra abiertas durante más de 30 minutos con actividad real;
- dejar una sesión completamente inactiva durante 30 minutos y confirmar el cierre;
- revisar impresión y exportaciones con datos de Desarrollo.

Registrar para cada caso: fecha, ambiente, usuario/rol, pasos, resultado, captura sin secretos y defecto asociado si falla.

## 4. Orden obligatorio

1. CI y aceptación aislada en verde.
2. Aiven Desarrollo.
3. Cloudinary Desarrollo.
4. Render Desarrollo.
5. Migraciones únicamente en Aiven Desarrollo.
6. Vercel Desarrollo.
7. Pruebas integradas y manuales.
8. Correcciones nuevamente en `Desarrollo`.
9. Repetir hasta obtener aprobación completa.
10. Preparar respaldo, ventana y rollback productivos.
11. Solo después, Javier decide si autoriza el merge y el despliegue.

## 5. Criterio de cierre

`Desarrollo` puede considerarse candidata a producción únicamente cuando:

- todos los checks de GitHub estén en verde;
- los recursos externos estén separados de producción;
- la migración haya sido validada en Aiven Desarrollo;
- Gmail, WhatsApp, Cloudinary, PDF y dispositivos reales estén aprobados;
- no existan defectos críticos o altos abiertos;
- exista procedimiento de respaldo y reversión;
- Javier Mejía entregue autorización expresa.
