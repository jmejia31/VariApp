# Contribuir a VariApp

## Rama de trabajo

- `main`: referencia productiva congelada; no recibe commits directos desde este flujo.
- `Desarrollo`: **única rama de trabajo e integración autorizada**.
- No crear ramas temporales `feature/*`, `fix/*`, `chore/*` ni equivalentes sin autorización expresa de Javier Mejía.
- El PR oficial `Desarrollo -> main` debe permanecer abierto y en borrador; no fusionar ni habilitar auto-merge.

## Contexto antes de tocar código

Leer en este orden:

1. `AGENTS.md`.
2. `PROJECT_CONTEXT.md`.
3. `TASKS.md`.
4. `PROJECT_INDEX.md` si se necesita localizar el área.

No volver a indexar todo el repositorio ni releer archivos ya documentados si no cambiaron.

## Preparación local

Para Javier, Codex o AntiG/Antigravity:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

Si hay cambios locales ajenos, preservarlos y resolver el conflicto de forma explícita.

## Implementación

- Cambios pequeños y localizados.
- Inspeccionar el archivo objetivo y dependencias directas.
- Evitar refactors no solicitados.
- No tocar Producción.
- No subir secretos ni temporales.
- Actualizar memoria/arquitectura solo cuando el cambio realmente la invalide.

## Validación

Aplicar la validación proporcional definida en `AGENTS.md`.

Para cambios documentales no se exige build completo. Para cambios transversales, seguridad, persistencia, migraciones o cierre formal de fase, ejecutar la suite aplicable completa.

## Publicación

1. revisar diff;
2. comprobar que la rama es `Desarrollo`;
3. commit descriptivo;
4. push a `origin/Desarrollo`;
5. registrar cambio relevante en `CHANGELOG_AI.md` y pendiente en `TASKS.md` cuando aplique.

## Rendimiento

- No repetir `fetch/pull/status` sin necesidad.
- No releer archivos sin cambios.
- No escanear módulos no relacionados.
- Tras reconexión, recuperar estado con `PROJECT_CONTEXT.md` + `TASKS.md` + Git, no reiniciar el análisis.
- Si una tarea puede terminarse con menos archivos/comandos, preferir esa ruta sin sacrificar validación.