# Reglas obligatorias de colaboración — VariApp

Este archivo es vinculante para Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y cualquier otro agente autorizado.

## 0. Gate obligatorio de identidad al iniciar CADA conversación o sesión

Antes de analizar, editar, ejecutar, crear commits o proponer rutas concretas, el agente debe comprobar **qué proyecto tiene realmente delante**. La memoria de conversaciones anteriores nunca sustituye esta comprobación.

### Si el agente tiene acceso local

Javier, Codex y AntiG/Antigravity deben ejecutar primero:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\iniciar-sesion-ia.ps1
```

El gate debe confirmar como mínimo:

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Si el repositorio, remote `origin`, rama o identidad no coinciden, **DETENERSE y no escribir nada** hasta resolver la discrepancia.

### Si el agente trabaja solo por GitHub remoto

ChatGPT u otro agente remoto debe comprobar antes de escribir:

1. repositorio exacto `jmejia31/VariApp`;
2. rama objetivo exacta `Desarrollo`;
3. HEAD remoto actual;
4. existencia de `AGENTS.md`, `PROJECT_CONTEXT.md` y `TASKS.md`;
5. últimos commits relevantes desde el handoff conocido.

### Lectura mínima de cada conversación nueva

1. `AGENTS.md`;
2. `PROJECT_CONTEXT.md`;
3. `TASKS.md`;
4. última entrada relevante de `CHANGELOG_AI.md`;
5. `PROJECT_INDEX.md` únicamente para localizar el área;
6. `ARCHITECTURE.md` únicamente para cambios estructurales/transversales.

Después se revisan solo los archivos objetivo y sus dependencias directas.

### Aislamiento absoluto entre proyectos

- **Una conversación/sesión activa pertenece a un solo proyecto mientras no exista una instrucción explícita de cambio.**
- Contexto, rutas, ramas, credenciales, bases, reglas, planes o conclusiones de otro proyecto se consideran no confiables para VariApp.
- Nunca ejecutar en VariApp una instrucción destinada a otro repositorio, ni aplicar reglas de VariApp sobre otro proyecto.
- Si el usuario cambia explícitamente de proyecto, se debe ejecutar nuevamente el gate de identidad del proyecto destino antes de cualquier escritura.
- Si existe contradicción entre memoria global y los archivos canónicos del repositorio actual, prevalece la evidencia del repositorio actual y se informa la discrepancia.

## 1. Fuente de contexto y orden de lectura

`PROJECT_CONTEXT.md` es la memoria técnica principal. No releer por defecto `README.md`, toda la carpeta `docs`, todos los planes ni todo el código. Abrir únicamente lo relacionado con la tarea.

La realidad actual se determina por la combinación de:

```text
identidad del repositorio + HEAD actual + PROJECT_CONTEXT.md + TASKS.md + CHANGELOG_AI.md + archivos afectados
```

No asumir que una conversación anterior refleja el HEAD vigente.

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

ChatGPT y cualquier otro agente se consideran **sin acceso al filesystem local** salvo que Javier documente explícitamente lo contrario.

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

Inicio local normal después de superar el gate:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

No repetir `fetch/pull/status` compulsivamente durante la misma tarea. Comprobar de nuevo antes de publicar cuando exista riesgo real de concurrencia.

## 5. Optimización de rendimiento y tokens — OBLIGATORIA

1. `PROJECT_CONTEXT.md` es la memoria base: no reconstruirla en cada solicitud.
2. No recorrer todo el repositorio salvo cambio estructural grande o petición expresa de Javier.
3. Analizar primero únicamente los archivos que se modificarán.
4. Expandir después solo a dependencias directas necesarias.
5. **No releer archivos ya documentados a menos que hayan cambiado.** Usar `git diff`, SHA, historial o estado Git para comprobarlo.
6. Usar búsquedas por símbolo/nombre/ruta antes que listados recursivos masivos.
7. Leer rangos relevantes de archivos grandes; no volcar archivos completos si solo se necesita una función/sección.
8. Si una tarea puede resolverse tocando menos archivos, elegir la alternativa de menor impacto.
9. No analizar módulos no relacionados una vez exista evidencia suficiente.
10. Agrupar comandos relacionados y validaciones cuando sea seguro.
11. No generar documentos temporales, scripts efímeros o workflows temporales salvo necesidad técnica real y retiro controlado.
12. Terminar cuando se cumpla el objetivo y las validaciones aplicables; no seguir explorando por curiosidad.
13. Si una ambigüedad no puede resolverse con inspección dirigida, pedir aclaración en vez de indexar todo el proyecto.
14. Para conocer cambios desde el último contexto, revisar primero nombres de archivos/commits; abrir contenido solo cuando afecte la tarea o invalide la memoria canónica.

### Cambios administrativos y consumo de CI

Un commit puede incluir `[skip ci]` **solo** cuando todos los cambios sean documentación o infraestructura local de colaboración (`*.md`, `.githooks/*`, `scripts/iniciar-sesion-ia.ps1`, `scripts/configurar-colaboracion.ps1`) y no se haya modificado aplicación, workflow GitHub Actions, dependencia, migración, configuración de entorno o artefacto de despliegue.

Nunca usar `[skip ci]` para ocultar o evitar validaciones de código funcional, seguridad, persistencia, migraciones o CI.

## 6. Protocolo tras reconexión o compactación

No reiniciar el trabajo desde cero. Ejecutar únicamente:

1. confirmar PROJECT_ID/repositorio/rama;
2. leer `PROJECT_CONTEXT.md` y `TASKS.md`;
3. `git status --short --branch`;
4. revisar los últimos 1–3 commits si hubo movimiento remoto;
5. revisar el diff de archivos de la tarea;
6. continuar desde el último punto verificable.

Una reconexión **no** justifica repetir el inventario arquitectónico, releer todos los documentos ni volver a ejecutar comandos ya confirmados.

## 7. Cuándo sí renovar el mapa arquitectónico

Solo cuando ocurra, entre otros:

- nueva capa/proyecto principal;
- módulo ERP mayor nuevo;
- cambio de persistencia/framework estructural;
- rediseño de autenticación/RBAC;
- cambio transversal de tenancy, integración, despliegue u observabilidad.

En ese caso se hace **una sola revisión arquitectónica**, se actualizan `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE.md`, y las sesiones siguientes reutilizan esa memoria.

## 8. Producción congelada

Solo existen dos entornos lógicos:

```text
varistorehn_producción
varistorehn_desarrollo
```

Durante el trabajo en `Desarrollo` está prohibido modificar o eliminar recursos productivos: `main`, variables, secretos, credenciales, dominios, certificados, bases, datos, servicios, despliegues, respaldos, migraciones, activos o configuraciones de Producción.

No exponer secretos. No usar `avnadmin` como usuario de aplicación de Desarrollo. No eliminar activos Cloudinary fuera del prefijo autorizado. No ejecutar migraciones productivas sin autorización expresa.

## 9. Evidencia obligatoria de CADA changeset

Todo cambio intencional debe dejar trazabilidad suficiente para que el siguiente agente continúe sin reconstruir la historia.

Obligatorio:

1. incluir `CHANGELOG_AI.md` en el changeset con objetivo, agente, alcance y validaciones reales;
2. actualizar `TASKS.md` si cambió el estado de una tarea, apareció un bloqueo o surgió un pendiente relevante;
3. actualizar `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` o `ARCHITECTURE.md` **solo si cambió lo que documentan**;
4. actualizar `AGENTS.md`, `CONTRIBUTING.md` y documentos colaborativos cuando cambien reglas, accesos o gobierno;
5. usar commit descriptivo con identificación de agente cuando aplique;
6. publicar el SHA/resultado en el handoff final;
7. no inventar pruebas, estados externos ni validaciones no ejecutadas.

No modificar todos los colaborativos solo para cambiar una fecha: eso genera ruido y contradice la optimización. La evidencia mínima universal es `CHANGELOG_AI.md`; el resto se actualiza por cambio real de contenido.

El hook local `.githooks/pre-commit` aplica un guardrail: valida repositorio/rama y exige que `CHANGELOG_AI.md` forme parte del commit local.

## 10. Validación proporcional

### Solo documentación/gobierno local

- revisar diff;
- comprobar rutas/nombres/guardas mencionados;
- no ejecutar builds de aplicación sin motivo.

### Cambio backend localizado

- ejecutar pruebas/compilación dirigidas al área cuando existan;
- ampliar a suite completa si el cambio es transversal, de seguridad, datos, migraciones o cierre formal de fase.

### Cambio frontend localizado

- lint/build/pruebas dirigidas aplicables;
- ampliar a E2E cuando cambien autenticación, permisos, navegación, facturación o flujo crítico.

La calidad es obligatoria; el objetivo es eliminar validaciones globales redundantes, no reducir rigor.

## 11. Mejora continua controlada

El objetivo es aproximarse continuamente a mayor fluidez, control, calidad y precisión. Cada agente puede identificar mejoras adicionales, pero debe:

- aplicar inmediatamente las de bajo riesgo y alcance claro relacionadas con la tarea;
- registrar en `TASKS.md` las mejoras útiles que requieran una intervención separada o tengan riesgo transversal;
- no convertir una corrección local en un refactor infinito;
- priorizar automatización, guardrails y eliminación de trabajo repetitivo;
- medir la mejora por evidencia: menos relecturas, menos comandos redundantes, menos CI innecesario, menor superficie de cambio y cero pérdida de trazabilidad.

## 12. Commits y handoff

Formato recomendado:

```text
<tipo>(<área>): <descripción> [agente]
```

Cada entrega debe indicar de forma compacta:

- proyecto confirmado;
- objetivo;
- archivos/área;
- validaciones reales;
- riesgos/pendientes;
- commit publicado.

No repetir el resumen completo de arquitectura: referenciar `PROJECT_CONTEXT.md`.