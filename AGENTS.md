# Reglas de colaboración — VariApp

Este archivo es obligatorio para Javier Mejía, ChatGPT, Codex, Antigravity y cualquier otro agente que trabaje en el repositorio.

## Fuente única de verdad

- GitHub es la fuente única de verdad del proyecto.
- `main` es una referencia productiva de solo lectura: no se desarrolla, no se reescribe, no se fusiona, no se publica y no se modifica desde este flujo.
- `Desarrollo` es la única rama de trabajo compartido.
- Está prohibido crear ramas adicionales sin autorización expresa de Javier Mejía.
- Todo cambio intencional debe confirmarse y publicarse en `origin/Desarrollo`.
- El Pull Request `Desarrollo -> main` debe permanecer en borrador mientras existan validaciones pendientes.

## Entornos oficiales

Solo existen dos entornos lógicos autorizados:

```text
varistorehn_producción (Producción)
varistorehn_desarrollo
```

Los nombres técnicos ya existentes de servicios, proyectos, dominios, bases o claves pueden diferir. No deben renombrarse ni recrearse cuando el cambio pueda afectar Producción. Un nombre técnico distinto no constituye por sí solo un tercer entorno si está documentado como parte de uno de los dos entornos oficiales.

## Producción congelada

Durante todo el trabajo en `Desarrollo` queda prohibido modificar o eliminar:

- variables, secretos, credenciales, dominios, certificados, conexiones, bases, servicios y despliegues productivos;
- el usuario administrativo `avnadmin` de Aiven;
- claves `Raíz`, moderación o flujos de medios de Cloudinary;
- variables ya existentes de Producción o Desarrollo;
- activos, registros, respaldos o migraciones productivas.

Solo puede eliminarse un recurso cuando se demuestre simultáneamente que:

1. pertenece exclusivamente a Desarrollo;
2. duplica una función ya cubierta por `varistorehn_desarrollo`;
3. no tiene consumidores, dependencias, datos ni secretos necesarios;
4. su eliminación no afecta Producción;
5. Javier Mejía autoriza expresamente la eliminación.

Nunca se elimina un recurso únicamente por su nombre.

## Inicio obligatorio de cada sesión

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status
```

Si existen cambios locales ajenos, el agente debe preservarlos y explicar el conflicto; nunca debe descartarlos, sobrescribirlos ni usar `git reset --hard` sin autorización.

## Flujo obligatorio por cambio

1. Leer `AGENTS.md`, `CONTRIBUTING.md`, `docs/COLABORACION_IA.md` y `docs/ENTORNOS_DESARROLLO_PRODUCCION.md`.
2. Confirmar que la rama actual sea `Desarrollo`.
3. Revisar el estado y los últimos commits antes de editar.
4. Analizar alcance, dependencias y riesgos.
5. Hacer cambios pequeños, coherentes y trazables.
6. Ejecutar las verificaciones afectadas.
7. Actualizar la documentación cuando cambie comportamiento, configuración o arquitectura.
8. Crear un commit descriptivo indicando el agente.
9. Subir inmediatamente el commit a `origin/Desarrollo`.
10. Registrar el resultado en el Pull Request colaborativo o en el issue de coordinación.

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
npm run lint
npm run build:prod
```

Las pruebas E2E se ejecutan cuando el cambio afecta autenticación, permisos, navegación, facturación o flujos críticos.

## Reglas de seguridad y limpieza

- Nunca guardar contraseñas, tokens, cadenas productivas, claves SMTP o credenciales de Cloudinary.
- Nunca aplicar migraciones en Aiven ni desplegar a Producción sin autorización expresa.
- Nunca eliminar datos productivos.
- Desarrollo nunca puede usar la cadena o la base productiva.
- Desarrollo debe usar `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo`.
- Desarrollo no puede eliminar un activo Cloudinary cuyo `PublicId` no comience con `varistorehn_desarrollo/`.
- Un Preview de Desarrollo nunca puede apuntar al backend o base de datos productivos.
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
