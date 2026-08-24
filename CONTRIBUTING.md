# Contribuir a VariApp

## Gate de inicio — antes de tocar código

Cada conversación/sesión nueva debe demostrar que está en el proyecto correcto.

Con acceso local:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\iniciar-sesion-ia.ps1
```

Resultado esperado:

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Con acceso remoto, verificar los mismos datos mediante GitHub. Si no coinciden, detenerse: solo el contexto canónico de VariApp autoriza cambios aquí.

Después leer únicamente:

1. `AGENTS.md`;
2. `PROJECT_CONTEXT.md`;
3. `TASKS.md`;
4. última entrada relevante de `CHANGELOG_AI.md`;
5. `PROJECT_INDEX.md` si se necesita localizar el área.

No volver a indexar todo el repositorio ni releer archivos ya documentados si no cambiaron.

## Rama de trabajo

- `main`: referencia productiva congelada.
- `Desarrollo`: **única rama de trabajo e integración autorizada**.
- No crear ramas temporales sin autorización expresa de Javier Mejía.
- PR #2 `Desarrollo -> main`: abierto y borrador; no fusionar ni habilitar auto-merge.

## Preparación local

Después del gate, si el checkout está limpio y detrás de remoto:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

Si hay cambios locales ajenos, preservarlos y resolver el conflicto explícitamente.

## Implementación

- Cambios pequeños y localizados.
- Archivo objetivo + dependencias directas.
- Evitar refactors no solicitados.
- No tocar Producción.
- No subir secretos ni temporales.
- Usar únicamente contexto canónico y verificable de VariApp.
- Actualizar memoria/arquitectura solo cuando el cambio realmente la invalide.

## Evidencia obligatoria

Cada changeset debe:

1. incluir una entrada breve en `CHANGELOG_AI.md`;
2. actualizar `TASKS.md` si cambió el estado/pendiente;
3. actualizar contexto/índice/arquitectura solo cuando exista cambio real;
4. actualizar colaborativos si cambian reglas/accesos/gobierno;
5. reportar validaciones reales y SHA publicado.

El hook `pre-commit` local bloquea commits fuera de VariApp/`Desarrollo` y commits sin `CHANGELOG_AI.md`.

## Validación

Aplicar la validación proporcional de `AGENTS.md`.

Para cambios documentales/gobierno local no se exige build de aplicación. Para cambios transversales, seguridad, persistencia, migraciones o cierre formal de fase, ejecutar la suite aplicable completa.

Un commit puede usar `[skip ci]` únicamente bajo la regla estricta de cambios administrativos/locales definida en `AGENTS.md`.

## Publicación

1. revisar diff;
2. comprobar repo/rama/HEAD;
3. commit descriptivo;
4. push a `origin/Desarrollo`;
5. handoff compacto con evidencia.

## Rendimiento

- No repetir `fetch/pull/status` sin necesidad.
- No releer archivos sin cambios.
- No escanear módulos no relacionados.
- Tras reconexión, recuperar estado; no reiniciar análisis.
- Si una tarea puede terminarse con menos archivos/comandos, preferir esa ruta sin sacrificar validación.
