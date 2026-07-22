# Validación de producción — VariStorehn Administrativo

Este documento separa la certificación automatizada de la validación manual que debe realizarse después de desplegar un Preview autorizado y, posteriormente, producción.

## Condiciones previas confirmadas por el propietario

- [x] Respaldo automático de Aiven verificado.
- [x] Exportación SQL local creada y conservada fuera del repositorio.
- [x] Variables productivas de Render revisadas.
- [x] `AppSettings__BackendPublicUrl` configurada.
- [x] Cloudinary probado con productos e imágenes.
- [x] La factura se clasifica como comprobante comercial interno, sin CAI ni RTN empresarial.

## Certificación automatizada de la rama

- [x] Migración EF Core generada desde el modelo real.
- [x] Método `Up()` revisado automáticamente para impedir operaciones destructivas.
- [x] Modelo EF y snapshot sin cambios pendientes.
- [x] Backend compilado en configuración Release.
- [x] Frontend compilado con configuración de producción.
- [x] **66 pruebas backend ejecutadas y 66 aprobadas**.
- [x] Migración guardada en `agent/mejoras-variapp`.
- [x] SQL forward generado desde la migración inmediatamente anterior.
- [x] SQL forward sin `DROP`, `TRUNCATE` ni `DELETE FROM`.
- [x] Evidencia registrada en `docs/FASE6_CERTIFICACION.md`.

## Validación de migración antes de Aiven

- [ ] Revisar `docs/migraciones/004_fase6_seguridad_facturacion_perfil.sql`.
- [ ] Confirmar que el script corresponda al servicio y base productivos correctos.
- [ ] Confirmar nuevamente la disponibilidad del respaldo local.
- [ ] Elegir una sola estrategia de aplicación:
  - ejecutar el SQL revisado manualmente; o
  - habilitar temporalmente `Database__ApplyMigrationsOnStartup=true`.
- [ ] No utilizar ambas estrategias en el mismo despliegue.
- [ ] Verificar la tabla `__EFMigrationsHistory` después de aplicar.

## Validación funcional después del Preview

### Administrador

- [ ] Puede acceder a todos los módulos autorizados.
- [ ] Puede editar usuarios.
- [ ] Puede asignar rol únicamente con `Usuarios:AsignarRol`.
- [ ] Puede restablecer contraseña únicamente con `Usuarios:RestablecerContrasena`.
- [ ] Puede activar, desactivar y eliminar lógicamente según permisos.
- [ ] Puede consultar auditoría, configuración fiscal y finanzas corporativas.

### Vendedor y roles personalizados

- [ ] Solo visualizan módulos autorizados.
- [ ] No visualizan botones de acciones no concedidas.
- [ ] Reciben `403 Forbidden` al intentar endpoints sin permiso.
- [ ] Cada usuario solo ve sus propias ventas, facturas, finanzas y movimientos.
- [ ] No acceden a auditoría ni a información de proveedores sin permiso.
- [ ] Un rol personalizado funciona por matriz, sin depender del nombre del rol.

### Facturación y comunicaciones

- [ ] PDF descargado incluye logo e identidad de VariStorehn.
- [ ] Subtotal, descuento, impuesto incluido, impuesto adicional y total cuadran.
- [ ] Imprimir abre el mismo PDF descargable.
- [ ] WhatsApp abre el mismo PDF mediante enlace temporal.
- [ ] El enlace funciona sin iniciar sesión y deja de funcionar al expirar o revocarse.
- [ ] Gmail SMTP entrega la factura a una cuenta externa.
- [ ] El PDF recibido por correo coincide con descarga y WhatsApp.
- [ ] Auditoría e historial registran los intentos sin guardar el token público completo.

### Compras, imágenes y perfil

- [ ] Una compra acepta comprobante JPG, PNG, WebP o PDF.
- [ ] El comprobante puede visualizarse, descargarse y retirarse según permisos.
- [ ] Los productos conservan sus imágenes al eliminarse lógicamente.
- [ ] El perfil permite cambiar nombre, usuario y contraseña.
- [ ] La foto de perfil puede cargarse, reemplazarse y eliminarse.
- [ ] Cloudinary no presenta archivos duplicados inesperados.

### Responsividad

- [ ] Login validado a 320, 375, 390 y 430 px.
- [ ] Módulos principales validados en teléfono, tablet y escritorio.
- [ ] No existen desbordamientos horizontales no controlados.
- [ ] Menú lateral, diálogos, tablas, formularios y factura son utilizables mediante teclado.

## Criterio de aprobación

El Pull Request solo podrá salir de borrador cuando:

1. La Fase 6 esté certificada completamente.
2. El Preview autorizado supere esta lista.
3. La migración haya sido revisada sin pérdida de datos.
4. El propietario autorice expresamente la fusión a `main`.
