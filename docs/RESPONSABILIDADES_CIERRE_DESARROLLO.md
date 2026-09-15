# Responsabilidades para cerrar la rama Desarrollo

## Regla invariable

Todo trabajo técnico se realiza en `Desarrollo`. `main`, Aiven productivo, Render productivo, Vercel productivo y los activos productivos de Cloudinary permanecen sin cambios hasta que Javier Mejía autorice expresamente un plan de publicación.

El identificador oficial del entorno de Desarrollo es `varistorehn_desarrollo`.

Nunca se deben copiar secretos en commits, Pull Requests, issues, capturas, chats o archivos del repositorio.

## 1. Trabajo que puede ejecutar cualquier agente con acceso al repositorio

ChatGPT, Codex, Antigravity o un desarrollador autorizado pueden realizar en `Desarrollo`:

- implementar y corregir backend, frontend, API y migraciones;
- agregar pruebas unitarias, de integración y Playwright;
- ejecutar compilaciones y pruebas con MySQL descartable en GitHub Actions;
- generar SQL forward revisable sin aplicarlo a Producción;
- validar tipado, calidad estática, Docker y configuración declarativa;
- revisar consultas, relaciones, permisos, eliminación lógica y auditoría;
- documentar cambios, riesgos y casos de prueba;
- actualizar el PR borrador y el issue colaborativo;
- corregir fallos encontrados por el CI;
- preparar instrucciones para los recursos externos.

Ningún agente debe fusionar a `main`, desplegar Producción ni aplicar migraciones productivas sin autorización expresa.

## 2. Trabajo que requiere acceso del propietario o un operador de infraestructura

### Aiven Desarrollo

Responsable: Javier Mejía u operador autorizado con acceso al panel de Aiven.

Recurso designado por el propietario:

- servicio visible: `variapp-mysql`;
- usuario de aplicación: `varistorehn_desarrollo`.

Validación obligatoria:

1. Confirmar un respaldo reciente del servicio.
2. Confirmar el nombre exacto de la base exclusiva para Desarrollo.
3. Confirmar que `varistorehn_desarrollo` solo tiene privilegios sobre esa base.
4. Confirmar que la aplicación de Desarrollo no utiliza `avnadmin`.
5. Confirmar que Producción utiliza otra base y no existe acceso cruzado.
6. Restringir las fuentes de acceso a las estrictamente necesarias.
7. Guardar la cadena únicamente como secreto de Render Desarrollo.
8. No compartir la cadena completa por GitHub ni chat.
9. Si la base contiene datos reales, limitar accesos y anonimizar datos personales antes de demostraciones.

No crear otro servicio o usuario de Desarrollo mientras el propietario mantenga este recurso como el oficial. `avnadmin` es un usuario administrativo predeterminado, no un tercer entorno, y no debe eliminarse.

Evidencia requerida:

- nombre de la base sin credenciales;
- captura con valores sensibles ocultos;
- confirmación de privilegios exclusivos;
- confirmación escrita de que Render Desarrollo usa `varistorehn_desarrollo`;
- confirmación de respaldo disponible.

### Cloudinary Desarrollo

Responsable: operador con acceso a Cloudinary.

Recurso designado por el propietario:

- clave de API etiquetada `varistorehn_desarrollo`;
- prefijo de activos `varistorehn_desarrollo/`.

Las claves de `Raíz`, moderación o flujos de medios no deben eliminarse automáticamente: pueden ser dependencias internas de Cloudinary y no representan por sí solas entornos duplicados.

Validación manual:

1. Confirmar que Render Desarrollo usa la clave etiquetada `varistorehn_desarrollo`.
2. Subir, reemplazar, descargar y eliminar una imagen de producto.
3. Subir y eliminar una foto de perfil.
4. Subir comprobantes JPG, PNG, WebP y PDF.
5. Confirmar que los `public_id` nuevos comienzan con `varistorehn_desarrollo/`.
6. Intentar eliminar una referencia productiva desde Desarrollo y confirmar que el backend la bloquea.
7. Revisar duplicados, huérfanos y consumo de almacenamiento o transformación.

Evidencia requerida:

- rutas o `public_id` parcialmente ocultos que muestren el prefijo;
- resultado de las operaciones sin mostrar API key ni API secret;
- confirmación de que ningún activo productivo fue eliminado.

### Render Desarrollo

Responsable: operador con acceso al dashboard de Render y a los secretos de Desarrollo.

Recurso designado por el propietario:

- entorno: `Desarrollo`;
- servicio: `variapp-api-desarrollo`;
- rama: `Desarrollo`.

Validación obligatoria:

1. Confirmar que el servicio sigue `render.yaml` de la rama `Desarrollo`.
2. Confirmar exclusivamente secretos no productivos:
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
3. Confirmar `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo`.
4. Confirmar que la conexión usa el usuario Aiven `varistorehn_desarrollo` y la base exclusiva de Desarrollo.
5. Mantener `Database__ApplyMigrationsOnStartup=false` en operación normal.
6. Comprobar que `/health` responde HTTP 200.

Evidencia requerida:

- nombre, rama y URL del servicio sin secretos;
- respuesta de `/health`;
- captura de variables con valores ocultos;
- confirmación de que no usa la conexión productiva.

### Aplicación controlada de migraciones en Aiven Desarrollo

Responsable: desarrollador u operador autorizado después de cerrar la separación de Aiven y Render Desarrollo.

1. Descargar o generar el SQL forward certificado desde GitHub Actions.
2. Revisar que el destino sea la base exclusiva de Desarrollo confirmada en la Fase 1.
3. Tomar o confirmar respaldo previo.
4. Aplicar las migraciones una sola vez mediante una estrategia acordada: EF Core CLI o SQL forward, nunca ambas.
5. Verificar `__EFMigrationsHistory`.
6. Comparar conteos de Productos, Categorías, Usuarios, Compras, Ventas y Facturas antes y después.
7. Verificar que los productos legados conservaron Marca y Modelo y recibieron sus nuevas relaciones.
8. Mantener migraciones automáticas deshabilitadas después de la operación.

Evidencia requerida:

- lista de `MigrationId` sin credenciales;
- conteos comparativos;
- resultado de una consulta de muestra de Marca/Modelo normalizados.

### Vercel Desarrollo

Responsable: operador con acceso al proyecto de Vercel.

Recurso designado por el propietario:

- proyecto: `variapp-desarrollo`;
- Production Branch: `Desarrollo`;
- dominio: `variapp-desarrollo.vercel.app`.

Validación obligatoria:

1. Confirmar Root Directory `frontend`.
2. Confirmar que `/api` llega a `variapp-api-desarrollo.onrender.com`.
3. Confirmar por separado que `varistorehn.vercel.app` sigue llegando a `variapp-api.onrender.com`.
4. Confirmar que no existe un tercer proyecto permanente de VariApp/VariStorehn.
5. No promover un deployment de Desarrollo al proyecto productivo.

Evidencia requerida:

- URL del deployment de Desarrollo;
- respuesta correcta de `/login` y `/api`;
- confirmación de ramas de producción de ambos proyectos.

## 3. Pruebas manuales que requieren servicios o dispositivos reales

Responsables: Javier Mejía y un integrante del equipo como testigo o revisor.

- enviar factura por Gmail/SMTP a una cuenta real y revisar bandeja de entrada y spam;
- abrir el enlace de WhatsApp desde un teléfono físico y confirmar texto, número y PDF o enlace;
- revisar visualmente el PDF en navegador, descarga e impresión;
- validar cámara, galería y carga de archivos desde Android o iPhone;
- recorrer teléfono, tablet y escritorio reales;
- probar red lenta, pérdida temporal de conexión y recuperación;
- mantener una venta y una compra abiertas durante más de 30 minutos con actividad real;
- dejar una sesión completamente inactiva durante 30 minutos y confirmar el cierre;
- revisar impresión y exportaciones con datos de Desarrollo.

Registrar para cada caso: fecha, ambiente, usuario o rol, pasos, resultado, captura sin secretos y defecto asociado si falla.

## 4. Orden obligatorio

1. Cerrar Fase 1: entornos y recursos.
2. Auditar configuraciones e integraciones en Fase 2.
3. Corregir UI en Fase 3.
4. Cerrar responsive en Fase 4.
5. Integrar imágenes en Fase 5.
6. Certificar facturación e impresión en Fase 6.
7. Reparar y certificar correo en Fase 7.
8. Ejecutar validación completa en Fase 8.
9. Entregar informe final en Fase 9.
10. Preparar respaldo, ventana y rollback productivos.
11. Solo después, Javier decide si autoriza el merge y el despliegue.

## 5. Criterio de cierre

`Desarrollo` puede considerarse candidata a Producción únicamente cuando:

- todos los checks de GitHub estén en verde;
- los recursos externos estén aislados y vinculados a `varistorehn_desarrollo`;
- la migración haya sido validada en Aiven Desarrollo;
- Gmail, WhatsApp, Cloudinary, PDF y dispositivos reales estén aprobados;
- no existan defectos críticos o altos abiertos;
- exista procedimiento de respaldo y reversión;
- Javier Mejía entregue autorización expresa.
