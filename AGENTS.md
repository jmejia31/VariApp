# Reglas obligatorias de colaboración — VariApp

Este archivo es vinculante para Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y cualquier otro agente autorizado.

## 1. Fuente de contexto y orden de lectura

Lectura mínima al iniciar una tarea:

1. `AGENTS.md`.
2. `PROJECT_CONTEXT.md`.
3. `TASKS.md`.
4. `PROJECT_INDEX.md` solo para localizar el área.
5. `ARCHITECTURE.md` solo si la tarea es estructural/transversal.

No releer por defecto `README.md`, todos los documentos de `docs/`, todos los planes ni todo el código. Abrir únicamente lo relacionado con la tarea.

## 2. Equipo permanente

- **Javier Mejía:** propietario, prioridades, aceptación y autorizaciones finales.
- **Codex:** implementación/pruebas desde el proyecto local cuando opera en la PC autorizada.
- **AntiG / Antigravity:** implementación/pruebas desde el proyecto local cuando opera en la PC autorizada.
- **ChatGPT:** arquitectura, auditoría, coordinación y cambios remotos mediante el conector GitHub autorizado.

## 3. Matriz de acceso

### Acceso local a la PC/proyecto

Solo se considera autorizado para:

- Javier Mejía;
- Codex;
- AntiG / Antigravity.

ChatGPT y cualquier otro agente se consideran **sin acceso al filesystem local** salvo que Javier documente explícitamente lo contrario en el futuro.

### Acceso GitHub

- Javier, Codex y AntiG pueden operar Git/GitHub desde el checkout autorizado.
- ChatGPT u otro agente puede operar remotamente solo mediante una conexión GitHub autorizada y disponible.
- Tener conexión GitHub no equivale a tener acceso local.
- Ningún agente debe afirmar que sincronizó/modificó la PC si únicamente modificó GitHub.

## 4. Git: reglas inviolables

- `Desarrollo` es la única rama de trabajo.
- `main` está congelada y no recibe cambios desde este flujo.
- No crear ramas `feature/*`, `fix/*`, `chore/*` ni ninguna otra sin autorización expresa de Javier.
- No fusionar PR #2.
- No habilitar auto-merge.
- El PR `Desarrollo -> main` permanece en borrador.
- Todo cambio autorizado se publica directamente en `origin/Desarrollo`.
- Preservar cambios locales ajenos; nunca usar `reset --hard` o descartes destructivos sin autorización.

Inicio local normal para Javier/Codex/AntiG:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

No repetir `fetch/pull/status` compulsivamente durante la misma tarea. Volver a comprobar justo antes de publicar si existe riesgo de concurrencia.

## 5. Optimización de rendimiento y tokens — OBLIGATORIA

1. `PROJECT_CONTEXT.md` es la memoria base: no reconstruirla en cada solicitud.
2. No recorrer todo el repositorio otra vez salvo cambio estructural grande o petición expresa de Javier.
3. Analizar primero únicamente los archivos que se modificarán.
4. Expandir después solo a dependencias directas necesarias.
5. **No releer archivos ya documentados a menos que hayan cambiado.** Usar `git diff`, SHA, historial o estado Git para comprobarlo.
6. Usar búsquedas por símbolo/nombre/ruta (`rg`, búsqueda de código) antes que listados recursivos masivos.
7. Leer rangos relevantes de archivos grandes; no volcar archivos completos si solo se necesita una función/sección.
8. Si una tarea puede resolverse tocando menos archivos, elegir la alternativa de menor impacto.
9. No analizar módulos no relacionados una vez exista evidencia suficiente.
10. Agrupar comandos relacionados y validaciones cuando sea seguro, evitando ciclos repetitivos de lectura/estado.
11. No generar documentos temporales, scripts efímeros o workflows temporales salvo necesidad técnica real y retiro controlado.
12. Terminar la implementación cuando se cumpla el objetivo y las validaciones aplicables; no continuar explorando por curiosidad.
13. Si una ambigüedad no puede resolverse con una inspección dirigida, pedir aclaración en vez de indexar todo el proyecto.

### Protocolo tras reconexión o compactación de contexto

No reiniciar el trabajo desde cero. Ejecutar únicamente:

1. leer `PROJECT_CONTEXT.md` y `TASKS.md`;
2. `git status --short --branch`;
3. revisar los últimos 1–3 commits si hubo movimiento remoto;
4. revisar el diff de archivos de la tarea;
5. continuar desde el último punto verificable.

Una reconexión **no** justifica repetir el inventario arquitectónico, releer todos los documentos ni volver a ejecutar los mismos comandos ya confirmados.

## 6. Cuándo sí renovar el mapa arquitectónico

Solo cuando ocurra, entre otros:

- nueva capa/proyecto principal;
- módulo ERP mayor nuevo;
- cambio de persistencia/framework estructural;
- rediseño de autenticación/RBAC;
- cambio transversal de tenancy, integración, despliegue u observabilidad.

En ese caso se hace **una sola revisión arquitectónica**, se actualizan `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE.md`, y las sesiones siguientes reutilizan esa memoria.

## 7. Producción congelada

Solo existen dos entornos lógicos:

```text
varistorehn_producción
varistorehn_desarrollo
```

Durante el trabajo en `Desarrollo` está prohibido modificar o eliminar recursos productivos: `main`, variables, secretos, credenciales, dominios, certificados, bases, datos, servicios, despliegues, respaldos, migraciones, activos o configuraciones de Producción.

No exponer secretos. No usar `avnadmin` como usuario de aplicación de Desarrollo. No eliminar activos Cloudinary fuera del prefijo autorizado. No ejecutar migraciones productivas sin autorización expresa.

## 8. Alcance y edición

- No modificar archivos no relacionados.
- Evitar refactors globales para resolver tareas locales.
- Preservar compatibilidad durante migraciones legacy.
- No dejar código muerto, debug temporal, archivos huérfanos o secretos.
- Actualizar documentación canónica solo si cambió lo que documenta.

## 9. Validación proporcional

### Solo documentación

- revisar diff;
- comprobar enlaces/rutas/nombres mencionados cuando sea necesario;
- no ejecutar builds completos sin motivo.

### Cambio backend localizado

- ejecutar pruebas/compilación dirigidas al área cuando existan;
- ampliar a suite completa si el cambio es transversal, de seguridad, datos, migraciones o cierre formal de fase.

### Cambio frontend localizado

- lint/build/pruebas dirigidas aplicables;
- ampliar a E2E cuando cambien autenticación, permisos, navegación, facturación o flujo crítico.

La calidad es obligatoria; el objetivo es evitar validaciones globales redundantes cuando el cambio no las necesita.

## 10. Commits y handoff

Formato recomendado:

```text
<tipo>(<área>): <descripción> [agente]
```

Cada entrega debe indicar de forma compacta:

- objetivo;
- archivos/área;
- validaciones reales;
- riesgos/pendientes;
- commit publicado.

Registrar cambios relevantes en `CHANGELOG_AI.md` y pendientes en `TASKS.md`. No duplicar información extensa que ya vive en documentos de fase.