# Reglas de colaboración — VariApp

Este archivo es obligatorio para Javier Mejía, ChatGPT, Codex, Antigravity y cualquier otro agente que trabaje en el repositorio.

## Fuente única de verdad

- GitHub es la fuente única de verdad del proyecto.
- La rama estable y productiva es `main`.
- La rama única de trabajo compartido es `Desarrollo`.
- Está prohibido desarrollar directamente sobre `main`.
- Está prohibido fusionar a `main` sin autorización expresa de Javier Mejía.
- Todo Pull Request hacia `main` debe permanecer en borrador mientras existan validaciones pendientes.

## Inicio obligatorio de cada sesión

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status
```

Si existen cambios locales ajenos, el agente debe preservarlos y explicar el conflicto; nunca debe descartarlos, sobrescribirlos ni usar `git reset --hard` sin autorización.

## Flujo obligatorio por cambio

1. Leer `AGENTS.md`, `CONTRIBUTING.md` y `docs/COLABORACION_IA.md`.
2. Confirmar que la rama actual sea `Desarrollo`.
3. Revisar el estado y los últimos commits antes de editar.
4. Hacer cambios pequeños, coherentes y trazables.
5. Ejecutar las verificaciones afectadas.
6. Actualizar la documentación cuando cambie comportamiento, configuración o arquitectura.
7. Crear un commit descriptivo indicando el agente.
8. Subir inmediatamente el commit a `origin/Desarrollo`.
9. Registrar el resultado en el Pull Request colaborativo o en el issue de coordinación.

## Formato de commits

```text
<tipo>(<área>): <descripción> [agente]
```

Ejemplos:

```text
fix(auth): corregir validación de permisos [Codex]
feat(productos): agregar filtro por categoría [Antigravity]
docs(colaboración): actualizar protocolo [ChatGPT]
chore(repo): limpiar temporales [Javier]
```

Tipos permitidos: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`, `security`.

## Verificación mínima

Backend:

```bash
cd backend
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln --configuration Release
dotnet test InventoryApp.sln --configuration Release
```

Frontend:

```bash
cd frontend
npm ci
npm run build:prod
```

Las pruebas E2E se ejecutan cuando el cambio afecta autenticación, permisos, navegación, facturación o flujos críticos.

## Reglas de seguridad y limpieza

- Nunca guardar contraseñas, tokens, cadenas productivas, claves SMTP o credenciales de Cloudinary.
- Nunca aplicar migraciones en Aiven ni desplegar a producción sin autorización expresa.
- Nunca eliminar datos productivos.
- No versionar `node_modules`, `bin`, `obj`, `dist`, `.angular`, reportes de pruebas, registros, temporales ni respaldos.
- No modificar archivos no relacionados con la tarea.
- No dejar código comentado, pruebas deshabilitadas, marcadores temporales ni archivos sin uso.
- Cualquier cambio de base de datos debe incluir migración, revisión de `Up()`, SQL forward y estrategia de reversión.

## Comunicación entre agentes

Cada entrega debe indicar:

- agente responsable;
- objetivo del cambio;
- archivos modificados;
- pruebas ejecutadas y resultado;
- riesgos o pendientes;
- commit publicado.

Un agente no debe asumir que otro agente todavía conserva contexto local. Toda decisión relevante debe quedar registrada en GitHub.
