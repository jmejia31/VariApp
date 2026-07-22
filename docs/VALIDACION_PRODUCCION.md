# Validación externa y productiva — VariStorehn Administrativo

Este documento separa la certificación automatizada ya aprobada de las validaciones que requieren servicios, dispositivos y datos reales.

## Condiciones previas confirmadas por el propietario

- [x] Respaldo automático de Aiven verificado.
- [x] Exportación SQL local creada y conservada fuera del repositorio.
- [x] Variables productivas de Render revisadas.
- [x] `AppSettings__BackendPublicUrl` configurada.
- [x] Cloudinary probado previamente con productos e imágenes.
- [x] La factura se clasifica como comprobante comercial interno, sin CAI ni RTN empresarial.

## Certificación automatizada de la rama

- [x] Migración EF Core generada desde el modelo real.
- [x] Método `Up()` revisado automáticamente para impedir operaciones destructivas.
- [x] Modelo EF y snapshot sin cambios pendientes.
- [x] Backend compilado en configuración Release.
- [x] Frontend compilado con configuración de producción.
- [x] **69 pruebas backend ejecutadas y 69 aprobadas**.
- [x] **9 pruebas end-to-end ejecutadas y 9 aprobadas**.
- [x] MySQL 8.4 temporal creado y destruido automáticamente.
- [x] Todas las migraciones aplicadas correctamente en la base temporal.
- [x] Administrador inicial vinculado al rol dinámico `Administrador`.
- [x] Roles dinámicos `Administrador` y `Vendedor` creados cuando están ausentes.
- [x] Matrices administradas conservadas entre reinicios.
- [x] Autorización backend validada con respuestas `200` y `403`.
- [x] Guardas frontend verificadas con Administrador, Vendedor y rol personalizado.
- [x] Perfil propio y política de contraseña verificados.
- [x] Venta, factura, PDF privado, enlace público, encabezados y revocación verificados.
- [x] Login validado a 320, 375, 390, 430, 768 y 1440 px sin desbordamiento horizontal.
- [x] SQL forward sin `DROP`, `TRUNCATE` ni `DELETE FROM`.
- [x] Evidencia registrada en `docs/FASE6_CERTIFICACION.md` y `docs/FASE7_CERTIFICACION_AISLADA.md`.

## Estado del Preview

- [x] Build local de producción Angular aprobado.
- [x] Backend Release aprobado.
- [ ] Preview de Vercel disponible.
  - Bloqueo actual: GitHub reportó `build-rate-limit` en Vercel.
- [ ] Preview de Render disponible para la rama.
- [ ] Base Preview separada de Aiven productivo.

## Validación de migración antes de Aiven

- [ ] Revisar `docs/migraciones/004_fase6_seguridad_facturacion_perfil.sql`.
- [ ] Confirmar que el script corresponda al servicio y base productivos correctos.
- [ ] Confirmar nuevamente la disponibilidad del respaldo local.
- [ ] Elegir una sola estrategia de aplicación:
  - ejecutar el SQL revisado manualmente; o
  - habilitar temporalmente `Database__ApplyMigrationsOnStartup=true`.
- [ ] No utilizar ambas estrategias en el mismo despliegue.
- [ ] Verificar la tabla `__EFMigrationsHistory` después de aplicar.
- [ ] Verificar conteos y registros críticos después de la migración.

## Validación funcional externa

### Administrador

- [x] Acceso administrativo validado automáticamente en entorno aislado.
- [x] Edición de usuarios protegida por permiso en backend y frontend.
- [x] Rol dinámico del administrador inicial validado.
- [ ] Repetir las acciones críticas con la cuenta administrativa real en Preview.
- [ ] Confirmar auditoría, configuración fiscal y finanzas corporativas en Preview.

### Vendedor y roles personalizados

- [x] Matriz Administrador/Vendedor/rol personalizado validada automáticamente.
- [x] Respuestas `403 Forbidden` validadas en endpoints prohibidos.
- [x] Guardas frontend y acceso permitido a Productos validados.
- [ ] Validar con cuentas reales de Preview.
- [ ] Confirmar visualmente que no aparecen botones no autorizados.
- [ ] Confirmar con datos reales que cada usuario solo ve sus ventas, facturas, finanzas y movimientos.

### Facturación y comunicaciones

- [x] PDF privado generado y validado como `application/pdf`.
- [x] PDF público generado mediante token temporal.
- [x] Encabezados `no-store`, `no-referrer` y `DENY` verificados.
- [x] Revocación del enlace verificada.
- [x] Correo inválido rechazado antes de contactar SMTP.
- [ ] PDF real incluye visualmente logo e identidad de VariStorehn.
- [ ] Imprimir abre el mismo PDF descargable en Preview.
- [ ] WhatsApp abre el enlace desde un teléfono real.
- [ ] Gmail SMTP entrega la factura a una cuenta externa.
- [ ] El PDF recibido por correo coincide con descarga y WhatsApp.
- [ ] Auditoría e historial productivos no contienen el token público completo.

### Compras, imágenes y perfil

- [x] Perfil, cambio de usuario y contraseña validados automáticamente.
- [x] Eliminación lógica conserva imágenes según pruebas backend.
- [ ] Una compra real acepta comprobante JPG, PNG, WebP o PDF.
- [ ] El comprobante puede visualizarse, descargarse y retirarse según permisos.
- [ ] La foto de perfil puede cargarse, reemplazarse y eliminarse en Cloudinary real.
- [ ] Cloudinary no presenta archivos duplicados u huérfanos inesperados.

### Responsividad y accesibilidad

- [x] Login automatizado a 320, 375, 390, 430, 768 y 1440 px.
- [x] Ausencia de desbordamiento horizontal validada en las vistas automatizadas.
- [x] Redirección de rutas prohibidas validada.
- [ ] Revisión visual en teléfono físico.
- [ ] Revisión visual en tablet física.
- [ ] Revisión visual en escritorio.
- [ ] Menú lateral, diálogos, tablas, formularios y factura revisados mediante teclado real.

## Criterio de aprobación productiva

El Pull Request solo podrá salir de borrador cuando:

1. Vercel permita generar el Preview o se utilice un entorno equivalente autorizado.
2. Exista un backend Preview separado de producción.
3. Gmail, WhatsApp, PDF y Cloudinary superen las pruebas reales.
4. La migración sea revisada y aplicada mediante una sola estrategia autorizada.
5. Se verifique la conservación de datos y `__EFMigrationsHistory`.
6. El propietario autorice expresamente la fusión a `main`.
