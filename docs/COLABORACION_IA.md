# Colaboración IA — VariApp

## Objetivo

Coordinar a Javier Mejía, Codex, AntiG/Antigravity y ChatGPT con mínima pérdida de contexto, mínimo trabajo redundante y máxima trazabilidad en `Desarrollo`.

## Equipo

### Javier Mejía

- propietario del proyecto;
- define prioridades y aceptación;
- autoriza merge, Producción, migraciones productivas y cambios de reglas.

### Codex

- implementa y prueba desde el proyecto local autorizado;
- debe trabajar sobre `Desarrollo`;
- debe reutilizar la memoria canónica y evitar reescaneos/relecturas innecesarias;
- tras reconexión debe continuar desde Git + `PROJECT_CONTEXT.md` + `TASKS.md`, no reiniciar el diagnóstico.

### AntiG / Antigravity

- implementa y prueba desde el proyecto local autorizado;
- sincroniza `Desarrollo` antes del trabajo y publica cambios trazables;
- aplica las mismas reglas de rendimiento y memoria canónica.

### ChatGPT

- arquitectura, auditoría, coordinación, revisión y cambios remotos cuando exista conexión GitHub autorizada;
- no tiene acceso al filesystem local de la PC por defecto;
- no debe afirmar cambios locales si solo actuó sobre GitHub.

## Acceso

### Local

Únicamente Javier Mejía, Codex y AntiG/Antigravity tienen acceso reconocido al proyecto local de la PC.

Cualquier ampliación debe ser indicada explícitamente por Javier y documentada aquí/`AGENTS.md`.

### GitHub remoto

Otros agentes, incluido ChatGPT, solo operan mediante conectores/conexiones GitHub autorizados y disponibles. El permiso remoto no implica acceso local.

## Memoria compartida

Fuentes canónicas:

- `PROJECT_CONTEXT.md` — contexto técnico.
- `PROJECT_INDEX.md` — mapa de carpetas.
- `ARCHITECTURE.md` — patrones y fronteras.
- `TASKS.md` — pendientes.
- `CHANGELOG_AI.md` — bitácora.

Todos los agentes deben actualizar estas fuentes en vez de volver a crear diagnósticos paralelos.

## Flujo eficiente de una tarea

1. leer `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md`;
2. localizar el módulo con `PROJECT_INDEX.md`;
3. revisar solo archivos objetivo y dependencias directas;
4. implementar el mínimo cambio correcto;
5. validar proporcionalmente;
6. publicar en `Desarrollo`;
7. actualizar `CHANGELOG_AI.md`/`TASKS.md` si corresponde.

## Optimización de tokens y tiempo

- No volver a recorrer todo el repositorio.
- No releer archivos ya documentados si no cambiaron.
- No repetir comandos ya confirmados por una reconexión.
- Usar `git diff`/historial para saber qué cambió desde el último contexto conocido.
- Usar búsquedas dirigidas por símbolo/ruta.
- Abrir únicamente el documento de fase/punto necesario.
- Finalizar cuando el objetivo y sus validaciones estén completos.
- Si falta una decisión de negocio real, preguntarla; no intentar resolverla escaneando módulos no relacionados.

## Recuperación tras reconexión/compactación

Una reconexión no reinicia la tarea. El agente recupera estado con:

```text
PROJECT_CONTEXT.md
TASKS.md
git status --short --branch
git log -3
```

Después continúa con los archivos de la tarea. Solo una modificación estructural importante habilita una nueva revisión arquitectónica amplia.

## Git y Producción

- rama única: `Desarrollo`;
- no crear ramas adicionales;
- `main` no se toca;
- PR `Desarrollo -> main` permanece en borrador;
- no auto-merge;
- Producción congelada;
- no secretos;
- no migraciones productivas sin autorización.

Las reglas completas están en `AGENTS.md` y `docs/ENTORNOS_DESARROLLO_PRODUCCION.md`.