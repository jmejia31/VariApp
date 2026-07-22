# VariApp — Administración de inventario y operación de VariStorehn

VariApp es una aplicación web para administrar la operación comercial de VariStorehn: productos, inventario, compras, ventas, clientes, proveedores, facturas, finanzas, usuarios, roles, permisos, descuentos, impuestos y auditoría.

La factura generada actualmente se considera **comprobante comercial interno**. No se presenta como comprobante fiscal autorizado por el SAR mientras VariStorehn no disponga de CAI, rango autorizado y demás requisitos tributarios aplicables.

## Arquitectura y stack

- Frontend: Angular 20, componentes standalone, Signals y Angular Material.
- Backend: ASP.NET Core 8 Web API.
- Arquitectura backend: capas `Domain`, `Application`, `Infrastructure` y `API`.
- Persistencia: MySQL mediante Entity Framework Core 8 y Pomelo.
- Autenticación: JWT y BCrypt.
- Archivos e imágenes: Cloudinary.
- PDF: QuestPDF.
- Correo: SMTP autenticado.
- Producción: Vercel, Render, Aiven y Cloudinary.

## Funcionalidades principales

- CRUD de productos con múltiples imágenes, imagen principal, stock y eliminación lógica.
- Categorías, clientes y proveedores con acciones independientes para ver, crear, editar, activar, desactivar y eliminar lógicamente.
- Compras y ventas con alcance por `UsuarioId`, estados, confirmación, anulación y trazabilidad.
- Comprobantes de proveedor en JPG, PNG, WebP o PDF asociados a compras.
- Descuentos e impuestos administrables desde la interfaz.
- Impuestos incluidos en el precio o adicionados al subtotal.
- Factura única para descarga, impresión, WhatsApp y correo.
- Enlaces públicos de factura con token aleatorio, hash SHA-256, expiración, revocación y límite de accesos.
- Dashboard, finanzas y movimientos aislados por usuario para perfiles no administrativos.
- Usuarios, roles y permisos por acción.
- Perfil propio con cambio de nombre, usuario, contraseña y fotografía.
- Auditoría transversal reservada al administrador.
- Interfaz adaptativa para escritorio, tablet y teléfono.

## Seguridad y alcance de datos

El administrador conserva acceso global. Los demás usuarios reciben únicamente la información permitida por su matriz de permisos y, en los módulos transaccionales, los registros asociados a su `UsuarioId`.

Los permisos se validan en dos capas:

- Backend: filtros y reglas de negocio protegen cada endpoint.
- Frontend: rutas, módulos, botones y acciones se muestran según el permiso exacto.

La interfaz no sustituye la seguridad del backend. Una solicitud manual sin autorización debe recibir `403 Forbidden` o ser rechazada por la regla de negocio correspondiente.

## Preparación local

### Requisitos

- .NET SDK 8.
- Node.js 20 o compatible con Angular 20.
- npm.
- MySQL 8.
- Credenciales de Cloudinary solo cuando se prueben cargas reales.

### Backend

```powershell
cd backend
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln
```

Para ejecutar la API desde `backend/src/API`:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

Swagger se habilita en desarrollo. La URL exacta depende de `launchSettings.json` o de `ASPNETCORE_URLS`.

### Frontend

```powershell
cd frontend
npm ci
npm start
```

Build de producción:

```powershell
npm run build:prod
```

## Configuración y secretos

No se deben guardar valores reales en GitHub, `appsettings.json`, capturas o documentación.

Claves principales del backend:

```text
ConnectionStrings__DefaultConnection
Database__ServerVersion
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Cloudinary__CloudName
Cloudinary__ApiKey
Cloudinary__ApiSecret
AppSettings__BackendPublicUrl
AppSettings__LogoPublicUrl
AppSettings__EnlacePublicoFacturaHorasValidez
AppSettings__EnlacePublicoFacturaMaximoAccesos
Smtp__Host
Smtp__Port
Smtp__UsuarioSmtp
Smtp__PasswordSmtp
Smtp__UsarSsl
Smtp__CorreoRemitente
Smtp__NombreRemitente
Smtp__TimeoutSeconds
SeedAdmin__Username
SeedAdmin__Password
Database__ApplyMigrationsOnStartup
```

`SeedAdmin` se utiliza únicamente para crear la cuenta inicial cuando todavía no existe. Un despliegue posterior no debe restablecer su contraseña, rol o estado.

## Base de datos y migraciones

Las migraciones se revisan antes de aplicarlas. En producción no se ejecuta una migración sin:

1. Respaldo automático de Aiven verificado.
2. Exportación SQL local disponible.
3. Build y pruebas aprobadas.
4. Revisión del método `Up()` y del SQL forward.
5. Autorización del propietario.

Comprobar cambios pendientes del modelo:

```powershell
cd backend
dotnet ef migrations has-pending-model-changes `
  --project src/Infrastructure/InventoryApp.Infrastructure.csproj `
  --startup-project src/API/InventoryApp.API.csproj `
  --context AppDbContext
```

Aplicar migraciones en un entorno autorizado:

```powershell
cd backend
dotnet ef database update `
  --project src/Infrastructure/InventoryApp.Infrastructure.csproj `
  --startup-project src/API/InventoryApp.API.csproj `
  --context AppDbContext
```

El script forward revisable de la Fase 6 se genera en:

```text
docs/migraciones/004_fase6_seguridad_facturacion_perfil.sql
```

No debe ejecutarse manualmente y además habilitar `Database__ApplyMigrationsOnStartup=true` durante el mismo despliegue, porque se intentaría aplicar el mismo cambio por dos rutas distintas.

## Compilación y pruebas

Backend:

```powershell
cd backend
dotnet build InventoryApp.sln --configuration Release
dotnet test InventoryApp.sln --configuration Release
```

Frontend:

```powershell
cd frontend
npm ci
npm run build:prod
```

El workflow `.github/workflows/ci.yml` certifica de forma controlada:

- generación o detección de la migración EF;
- ausencia de operaciones destructivas en `Up()`;
- alineación del modelo y el snapshot;
- generación y revisión básica del SQL forward;
- build Release del backend;
- pruebas backend;
- build de producción de Angular.

El workflow no aplica migraciones a Aiven ni despliega Render o Vercel.

## Estructura del repositorio

```text
backend/
  src/
    Domain/          Entidades y enumeraciones
    Application/     DTO, interfaces, servicios, reglas y validadores
    Infrastructure/  EF Core, repositorios, migraciones, Cloudinary, SMTP y PDF
    API/             Controladores, filtros, middleware y configuración
  tests/
    InventoryApp.Tests/

frontend/
  src/app/
    core/            autenticación, guards, interceptores y modelos
    features/        módulos funcionales
    services/        clientes HTTP

docs/
  migraciones/       scripts SQL revisables
  PLAN_CIERRE_VARIAPP.md
  VALIDACION_PRODUCCION.md
```

## Flujo de publicación

1. Trabajar en una rama distinta de `main`.
2. Mantener el Pull Request en borrador mientras haya fases pendientes.
3. Ejecutar CI controlado.
4. Revisar migraciones y pruebas.
5. Crear un Preview autorizado.
6. Validar perfiles, cálculos, PDF, correo, WhatsApp y Cloudinary.
7. Fusionar a `main` únicamente con autorización expresa del propietario.
