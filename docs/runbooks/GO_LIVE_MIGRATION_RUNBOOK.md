# GO_LIVE_MIGRATION_RUNBOOK — VariApp Desarrollo

## Alcance obligatorio

Este runbook se ejecuta solamente contra `Desarrollo` y contra una base MySQL efímera o explícitamente identificada como `varistorehn_desarrollo`. Nunca autoriza acciones sobre `main`, `varistorehn`, `varistorehn_producción`, `avnadmin` ni datos productivos.

La ruta preferida para un ensayo reproducible es una instancia MySQL 8.4 efímera local/CI. La configuración versionada de Render usa MySQL ServerVersion `8.4.3` y `Database__ApplyMigrationsOnStartup=true`; por eso cualquier cambio de despliegue real debe detenerse si no existe evidencia previa de backup/restore y de migración en entorno equivalente.

## Roles y separación de funciones

- `OPERADOR_DEV`: ejecuta preflight y ensayo en base efímera; no posee credenciales productivas.
- `REVISOR`: valida diff, migraciones, logs y resultados; no modifica la base durante revisión.
- `OWNER`: única persona que puede autorizar excepciones que excedan Desarrollo.
- `CLOSER`: certifica evidencia solo cuando P0=0 y P1=0.

Una misma sesión automatizada puede actuar como `OPERADOR_DEV` y `CLOSER` únicamente en Desarrollo cuando conserva trazabilidad completa y no existe otro writer vivo.

## Gate 0 — identidad y fail-closed

Ejecutar desde la raíz del repositorio:

```bash
set -euo pipefail
test "$(git branch --show-current)" = "Desarrollo"
git remote get-url origin | grep -Eq '(^git@github.com:|^https://github.com/)jmejia31/VariApp(\.git)?$'
git status --short
```

STOP inmediato si la rama no es `Desarrollo`, el remoto no es `jmejia31/VariApp`, existe un proceso de migración concurrente sobre el mismo scope o la cadena de conexión apunta a Producción.

## Gate 1 — compilación y pruebas antes de datos

Los comandos son los mismos usados por `.github/workflows/desarrollo-ci.yml`:

```bash
set -euo pipefail
cd backend
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln --configuration Release --no-restore
dotnet test InventoryApp.sln --configuration Release --no-build --filter "Category!=Integration" --logger "trx;LogFileName=tests.trx"
cd ..
```

Cualquier fallo es STOP. No se migra para intentar ocultar un build/test rojo.

## Gate 2 — ensayo real de migraciones en MySQL 8.4 efímero

Este gate usa credenciales aleatorias de vida de proceso; no imprime ni versiona secretos.

```bash
set -euo pipefail
export N823_MYSQL_CONTAINER="variapp-n823-migration"
export N823_MYSQL_PORT="33306"
export N823_MYSQL_ROOT_PASSWORD="$(openssl rand -hex 24)"
export ASPNETCORE_ENVIRONMENT="Development"
export Database__ServerVersion="8.4.3"
export Database__ApplyMigrationsOnStartup="false"
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=${N823_MYSQL_PORT};Database=inventoryapp_n823;User=root;Password=${N823_MYSQL_ROOT_PASSWORD};SslMode=None;AllowPublicKeyRetrieval=True;"
export Jwt__Secret="$(openssl rand -hex 32)"
export Jwt__Issuer="VariApp.N823.Runbook"
export Jwt__Audience="VariApp.N823.Runbook.Frontend"
export Cors__AllowedOrigins__0="https://variapp-desarrollo.vercel.app"

docker rm -f "$N823_MYSQL_CONTAINER" >/dev/null 2>&1 || true
docker run --rm -d \
  --name "$N823_MYSQL_CONTAINER" \
  -e MYSQL_ROOT_PASSWORD="$N823_MYSQL_ROOT_PASSWORD" \
  -e MYSQL_DATABASE=inventoryapp_n823 \
  -p "${N823_MYSQL_PORT}:3306" \
  mysql:8.4 >/dev/null

for i in $(seq 1 60); do
  if docker exec "$N823_MYSQL_CONTAINER" mysqladmin ping -uroot -p"$N823_MYSQL_ROOT_PASSWORD" --silent >/dev/null 2>&1; then
    break
  fi
  sleep 2
  test "$i" -lt 60
 done

cd backend
dotnet tool install --global dotnet-ef --version 8.0.8 >/dev/null 2>&1 || dotnet tool update --global dotnet-ef --version 8.0.8 >/dev/null
dotnet ef migrations list \
  --project src/Infrastructure/InventoryApp.Infrastructure.csproj \
  --startup-project src/API/InventoryApp.API.csproj \
  --context AppDbContext

time dotnet ef database update \
  --project src/Infrastructure/InventoryApp.Infrastructure.csproj \
  --startup-project src/API/InventoryApp.API.csproj \
  --context AppDbContext
cd ..

docker exec "$N823_MYSQL_CONTAINER" mysql -uroot -p"$N823_MYSQL_ROOT_PASSWORD" inventoryapp_n823 \
  --batch --skip-column-names \
  -e "SELECT COUNT(*) FROM __EFMigrationsHistory;"
```

El contador debe ser numérico y mayor que cero. Guardar duración del `database update`, salida de `migrations list`, SHA de Git y estado final; nunca guardar el password generado.

## Gate 3 — segunda pasada e idempotencia

```bash
set -euo pipefail
cd backend
time dotnet ef database update \
  --project src/Infrastructure/InventoryApp.Infrastructure.csproj \
  --startup-project src/API/InventoryApp.API.csproj \
  --context AppDbContext
cd ..
```

Debe completar sin aplicar cambios inesperados. Si una segunda pasada modifica esquema/datos fuera de la semántica esperada, clasificar al menos P1 y detener.

## Gate 4 — rollback técnico del ensayo

El rollback seguro de este runbook no intenta `database update` hacia atrás sobre datos persistentes. Destruye únicamente la base efímera del ensayo:

```bash
set -euo pipefail
docker rm -f "$N823_MYSQL_CONTAINER"
unset N823_MYSQL_ROOT_PASSWORD ConnectionStrings__DefaultConnection Jwt__Secret
```

En un DEV persistente, la recuperación se realiza desde el backup validado correspondiente y requiere un gate separado. No ejecutar migraciones destructivas inversas por intuición.

## STOP / rollback obligatorio

Detener y no promover si ocurre cualquiera de estos eventos:

- rama/remoto/entorno no coinciden con Desarrollo;
- cadena de conexión contiene indicios de Producción o `avnadmin`;
- build o pruebas no pasan;
- MySQL no está listo;
- `dotnet ef migrations list` falla;
- primera o segunda pasada de `database update` falla;
- aparece pérdida de datos, SQL destructivo no previsto o divergencia de historial;
- existe writer concurrente sobre el mismo scope;
- se necesita revelar, copiar o persistir un secreto para continuar.

## Clasificación P0–P3

- **P0:** toque real de Producción, pérdida/corrupción de datos, secreto expuesto, migración irreversible ejecutada sobre destino equivocado. STOP total y escalación al propietario.
- **P1:** migración falla, historial diverge, segunda pasada no es idempotente, rollback/restore no demostrable, permisos excesivos necesarios. No cerrar.
- **P2:** evidencia incompleta, tiempos no capturados o advertencia operacional sin impacto material inmediato. Corregir antes de certificación cuando afecte reproducibilidad.
- **P3:** mejora documental/cosmética sin impacto de seguridad, datos o repetibilidad.

## Evidencia mínima para LISTO

Registrar: HEAD exacto o equivalencia demostrada; versión MySQL; lista de migraciones; tiempo de primera y segunda pasada; conteo de `__EFMigrationsHistory`; pruebas backend; decisión REVIEW_FIRST; P0=0/P1=0; receipt y readback. Ninguna salida debe contener contraseñas, tokens o connection strings completas.
