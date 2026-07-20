# Comandos de integración — Fase 1

## Backend
Desde la carpeta `backend/`:
```
dotnet build
```
No pude ejecutar esto en mi sandbox (bloqueo de red a nuget.org). Ejecútalo
tú y pégame el log si hay errores — los corrijo sobre evidencia real.

## Base de datos
Desde un cliente MySQL conectado a tu instancia de Aiven (requiere
credenciales, backup previo recomendado):
```
mysql -h <host> -u <usuario> -p <basededatos> < docs/migraciones/004_fase1_usuarios_bloqueo_eliminacion.sql
```
Modifica la base de datos: **sí**. Requiere variables de entorno: no
(usa las credenciales que ya tengas configuradas para conectarte). No
requiere privilegios especiales más allá de ALTER TABLE/CREATE INDEX.

## Frontend
Desde la carpeta `frontend/`:
```
npm install
npm run build
```
Ya verificado en mi sandbox con `npx ng build --configuration=development`
→ 0 errores. `npm run build` (producción) no lo pude verificar aquí porque
requiere `fonts.googleapis.com`, bloqueado en mi red — no es un problema de
código.

## Dependencias
Ninguna nueva en esta fase.

## Migraciones
Ver sección "Base de datos" arriba. Es SQL manual, no una migración de EF
Core generada con `dotnet ef migrations add` (sigo sin poder ejecutar esa
herramienta). Si prefieres una migración EF real, corre esto tú una vez
tengas `dotnet build` funcionando:
```
cd backend/src/API
dotnet ef migrations add Fase1UsuariosBloqueoEliminacion --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .
```

## Configuración
Ninguna variable de entorno nueva en la Fase 1.

**Desde la Fase 5/6 (WhatsApp/Correo), variables requeridas en producción:**
```
AppSettings__BackendPublicUrl=https://variapp-api.onrender.com
AppSettings__EnlacePublicoFacturaDiasValidez=7
Smtp__Host=smtp.tu-proveedor.com
Smtp__Port=587
Smtp__UsuarioSmtp=tu-correo@dominio.com
Smtp__PasswordSmtp=<contraseña o contraseña de aplicación, nunca en el repo>
Smtp__UsarSsl=true
Smtp__CorreoRemitente=tu-correo@dominio.com
Smtp__NombreRemitente=VariStorehn
Cors__AllowedOrigins__0=http://localhost:4200
Cors__AllowedOrigins__1=https://varistorehn.vercel.app
Swagger__Enabled=true
```
Configúralas en Render como variables de entorno del servicio backend
(no las pongas en `appsettings.json`, que sí está commiteado con
placeholders `CHANGE_ME`). El doble guion bajo `__` es la convención de
ASP.NET Core para mapear a configuración anidada (`Smtp:Host`, etc.) desde
variables de entorno planas.

Nota de robustez agregada el 19/07/2026: si `AppSettings__BackendPublicUrl`
falta o apunta a `localhost`, el backend ahora genera enlaces públicos de
factura usando el host real de la petición. Aun así, se recomienda mantener
la variable explícita con `https://variapp-api.onrender.com`.

## Ejecución
```
cd backend/src/API
dotnet run
```
```
cd frontend
npm start
```

## Integración (orden exacto)
1. Backup de la base de datos.
2. Aplicar `004_fase1_usuarios_bloqueo_eliminacion.sql`.
3. `dotnet build` en backend — corregir cualquier error real antes de continuar.
4. Desplegar backend.
5. `npm run build` en frontend.
6. Desplegar frontend.
7. Verificar login con un usuario normal y uno bloqueado (crear uno de prueba).
8. Verificar que no se puede bloquear/eliminar al único administrador.
# Comandos de integración — actualización 20/07/2026

## Backend

Desde `C:\VariApp`:

```powershell
$env:DOTNET_CLI_HOME='C:\VariApp\.dotnet_cli_home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet build backend\InventoryApp.sln --no-restore
dotnet test backend\InventoryApp.sln --no-build
```

No modifica base de datos.

## Frontend

Desde `C:\VariApp\frontend`:

```powershell
npm.cmd run build
```

No modifica base de datos.

## Migración local o producción

Desde `C:\VariApp`, con las variables de entorno apuntando a la base correcta
(local o Aiven):

```powershell
$env:DOTNET_CLI_HOME='C:\VariApp\.dotnet_cli_home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet ef database update -p backend\src\Infrastructure -s backend\src\API
```

Sí modifica base de datos. No elimina registros; aplica la migración EF Core
pendiente y agrega columnas/índices requeridos.

## Variables de entorno a confirmar en Render

```text
Jwt__ExpirationMinutes=30
Swagger__Enabled=true
Cors__AllowedOrigins__0=http://localhost:4200
Cors__AllowedOrigins__1=https://varistorehn.vercel.app
AppSettings__BackendPublicUrl=https://variapp-api.onrender.com
Smtp__Host=<smtp-real>
Smtp__Port=587
Smtp__UsuarioSmtp=<usuario-smtp>
Smtp__PasswordSmtp=<password-smtp>
Smtp__UsarSsl=true
Smtp__CorreoRemitente=<correo-remitente>
Smtp__NombreRemitente=VariStorehn
```

## Orden exacto de integración

1. Crear backup de Aiven.
2. Desplegar backend desde `main`.
3. Aplicar migración EF Core contra Aiven.
4. Verificar `/health` y login.
5. Desplegar frontend desde `main`.
6. Probar login, inactividad, venta con descuento/impuesto, factura PDF,
   WhatsApp, logo/configuración y correo si SMTP ya está configurado.
