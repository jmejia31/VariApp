# Contribuir a VariApp

## Ramas

- `main`: versión estable. No recibe commits directos.
- `Desarrollo`: integración compartida de Javier Mejía y los agentes de IA.
- Ramas temporales opcionales: `feature/<tema>`, `fix/<tema>` o `chore/<tema>`. Deben integrarse primero en `Desarrollo`.

Ningún cambio puede fusionarse a `main` sin autorización expresa de Javier Mejía.

## Preparación

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
```

En Windows puede ejecutarse una sola vez:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\configurar-colaboracion.ps1
```

Esto configura el hook compartido que intenta publicar automáticamente cada commit realizado en `Desarrollo`.

## Antes de publicar

```bash
git status
git diff --check
```

Backend:

```bash
cd backend
dotnet build InventoryApp.sln --configuration Release
dotnet test InventoryApp.sln --configuration Release
```

Frontend:

```bash
cd frontend
npm ci
npm run build:prod
```

## Pull Requests

Todo Pull Request hacia `main` debe:

- originarse desde `Desarrollo`;
- crearse como borrador;
- describir alcance, pruebas, migraciones, riesgos y pendientes;
- mantener CI aprobado;
- evitar secretos y artefactos temporales;
- permanecer sin merge hasta autorización expresa.

## Definición de terminado

Un cambio está terminado cuando compila, las pruebas aplicables pasan, la documentación está actualizada, no introduce archivos temporales ni secretos, y el commit está publicado en GitHub.
