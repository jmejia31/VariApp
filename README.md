# InventoryApp - Sistema de Gestion de Inventario

Aplicacion web ligera para administrar productos, categorias, usuarios, compras, stock, valores de inventario y fotos de productos. No es un ERP; esta enfocada en inventario simple con acceso protegido por login.

## Stack

- Frontend: Angular 20, Standalone Components, Signals, Angular Material
- Backend: ASP.NET Core 8 Web API
- Arquitectura backend: Clean Architecture por capas `Domain`, `Application`, `Infrastructure`, `API`
- Base de datos: MySQL + Entity Framework Core + migraciones
- Autenticacion: JWT + BCrypt
- Imagenes: Cloudinary

## Estado local actual

El proyecto ya fue restaurado, compilado y probado localmente.

- Backend build: OK
- Backend tests: OK, 35 pruebas
- Frontend build: OK
- MySQL local de desarrollo: `127.0.0.1:3307`
- API local: `http://localhost:5005`
- Frontend local: `http://localhost:4200`

Usuario inicial:

- Usuario: `admin`
- Password: `Admin123!`

Cambia esta contrasena antes de produccion.

## Arranque local

### 1. MySQL local

El proyecto usa el MySQL de Laragon en puerto `3307`, separado del servicio Windows `MySQL80`.

```powershell
powershell -ExecutionPolicy Bypass -File backend\scripts\start-variapp-mysql.ps1
```

Este script prepara:

- Base de datos `inventoryapp`
- Usuario MySQL `VariApp`
- Permisos sobre la base
- Connection string en `dotnet user-secrets`

Para detenerlo:

```powershell
powershell -ExecutionPolicy Bypass -File backend\scripts\stop-variapp-mysql.ps1
```

### 2. Backend

```powershell
cd backend\src\API
$env:ASPNETCORE_URLS="http://localhost:5005"
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --no-build
```

Swagger queda disponible en:

```text
http://localhost:5005/swagger
```

### 3. Frontend

```powershell
cd frontend
npm start
```

La app queda disponible en:

```text
http://localhost:4200
```

## Comandos de verificacion

Backend:

```powershell
cd backend
dotnet build
dotnet test
```

Frontend:

```powershell
cd frontend
npm install
npm run build
```

Migraciones:

```powershell
cd backend
dotnet ef database update -p src\Infrastructure -s src\API
```

## Secretos locales

Los valores reales no deben guardarse en `appsettings.json` ni en el README.

Este proyecto usa `dotnet user-secrets` para:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Secret`
- `Cloudinary:CloudName`
- `Cloudinary:ApiKey`
- `Cloudinary:ApiSecret`

Archivos locales ignorados:

- `.dotnet_cli_home/`
- `.mysql-data/`

## Despliegue recomendado

Para acceso desde cualquier computadora:

- Backend: Render o Railway
- Frontend: Vercel o Netlify
- Base de datos: Railway MySQL o Aiven
- Imagenes: Cloudinary
- Codigo fuente: GitHub

Flujo recomendado:

1. Subir el proyecto a GitHub.
2. Crear base MySQL cloud.
3. Configurar variables de entorno del backend.
4. Desplegar API en Render/Railway.
5. Configurar `environment.prod.ts` con la URL real de la API.
6. Desplegar frontend en Vercel/Netlify.
7. Agregar el dominio final del frontend a `Cors:AllowedOrigins`.

## Estructura

```text
backend/
  src/
    Domain/          Entidades
    Application/     DTOs, interfaces, servicios, validadores
    Infrastructure/  EF Core, repositorios, JWT, Cloudinary
    API/             Controladores, middleware, configuracion
  tests/
    InventoryApp.Tests/

frontend/
  src/app/
    core/            auth, guards, interceptors, models
    features/        login, dashboard, productos, categorias, compras, usuarios
    services/        clientes HTTP hacia API
```
