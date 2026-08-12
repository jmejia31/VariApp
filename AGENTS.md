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
6. `ARCHITECTURE.md` únicamente para cambios estructurales/transversales;
7. `PLAN_EJECUCION_AUTONOMA.md` y tablero VAEP cuando la sesión sea autónoma.

Después se revisan solo los archivos objetivo y sus dependencias directas.

### Aislamiento absoluto entre proyectos

- **Una conversación/sesión activa pertenece a un solo proyecto mientras no exista una instrucción explícita de cambio.**
- Contexto, rutas, ramas, credenciales, bases, reglas, planes o conclusiones de otro proyecto se consideran no confiables para VariApp.
- Nunca ejecutar en VariApp una instrucción destinada a otro repositorio, ni aplicar reglas de VariApp sobre otro proyecto.
- Si el usuario cambia explícitamente de proyecto, se debe ejecutar nuevamente el gate de identidad del proyecto destino antes de cualquier escritura.
- Si existe contradicción entre memoria global y los archivos canónicos del repositorio actual, prevalece la evidencia del repositorio actual y se informa la discrepancia.

## 1. Fuente de contexto y orden de lectura

`PROJECT_CONTEXT.md` es la memoria técnica principal. No releer por defecto `README.md`, toda la carpeta `docs`, todos los planes ni todo el código. Abrir únicamente lo relacionado con la tarea.

La realidad actual se determina por:

```text
identidad + HEAD actual + PROJECT_CONTEXT.md + TASKS.md + CHANGELOG_AI.md + archivos afectados
```

No asumir que una conversación anterior refleja el HEAD vigente.

## 2. Equipo permanente

- **Javier Mejía:** propietario, prioridades, aceptación y autorizaciones finales.
- **Codex:** implementación/pruebas desde el proyecto local cuando opera en la PC autorizada.
- **AntiG / Antigravity:** implementación/pruebas desde el proyecto local cuando opera en la PC autorizada.
- **ChatGPT:** arquitectura, auditoría, coordinación, automatización VAEP y cambios remotos mediante conectores autorizados.

## 3. Matriz de acceso

### Acceso local

Solo se considera autorizado para Javier Mejía, Codex y AntiG/Antigravity. ChatGPT y cualquier otro agente se consideran **sin acceso al filesystem local** salvo autorización explícita posterior.

### Acceso GitHub

- Javier, Codex y AntiG pueden operar Git/GitHub desde el checkout autorizado.
- ChatGPT u otro agente remoto puede operar solo mediante una conexión GitHub autorizada y disponible.
- Tener conexión GitHub no equivale a tener acceso local.
- Ningún agente debe afirmar que sincronizó/modificó la PC si únicamente modificó GitHub.

### Acceso Drive para VAEP

- El Google Sheet VAEP es tablero operativo editable por Javier y consumible por ChatGPT cuando el conector esté disponible.
- Drive no sustituye GitHub como evidencia de implementación.
- Nunca ejecutar una fila cuyo `PROJECT_ID`, repositorio o rama no coincida exactamente con VariApp.

## 4. Git: reglas inviolables

- `Desarrollo` es la única rama de trabajo.
- `main` está congelada.
- No crear ramas adicionales sin autorización expresa de Javier.
- No fusionar PR #2.
- No habilitar auto-merge.
- PR #2 permanece Draft.
- Todo cambio autorizado se publica directamente en `origin/Desarrollo`.
- Preservar cambios ajenos; nunca `reset --hard`, force-push ni descartes destructivos sin autorización.

Inicio local normal después del gate:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

No repetir estos comandos compulsivamente; comprobar de nuevo antes de publicar cuando exista riesgo real de concurrencia.

## 5. Optimización de rendimiento y tokens — OBLIGATORIA

1. `PROJECT_CONTEXT.md` es la memoria base: no reconstruirla en cada solicitud.
2. No recorrer todo el repositorio salvo cambio estructural grande o petición expresa de Javier.
3. Analizar primero únicamente los archivos que se modificarán.
4. Expandir después solo a dependencias directas necesarias.
5. **No releer archivos ya documentados a menos que hayan cambiado.** Usar diff, SHA, historial o estado Git.
6. Usar búsquedas por símbolo/nombre/ruta antes que listados recursivos masivos.
7. Leer rangos relevantes de archivos grandes.
8. Elegir la alternativa de menor impacto suficiente.
9. No analizar módulos no relacionados una vez exista evidencia suficiente.
10. Agrupar comandos/validaciones cuando sea seguro.
11. No generar artefactos temporales sin necesidad técnica real y retiro controlado.
12. Terminar cuando se cumpla el objetivo y las validaciones aplicables.
13. Si una ambigüedad no puede resolverse mediante inspección dirigida, bloquear/pedir aclaración en vez de indexar todo el proyecto.
14. Para conocer cambios desde el último contexto, revisar primero nombres de archivos/commits y abrir contenido solo si afecta la tarea.

### Cambios administrativos y CI

`[skip ci]` se permite solo cuando absolutamente todos los cambios sean documentación o infraestructura local de colaboración y no se modifique aplicación, workflows, dependencias, migraciones, configuración de entorno o despliegue. Nunca usarlo para evitar validaciones funcionales, seguridad, persistencia o CI.

## 6. Protocolo tras reconexión o compactación

1. confirmar PROJECT_ID/repositorio/rama;
2. leer `PROJECT_CONTEXT.md` y `TASKS.md`;
3. revisar estado Git si existe acceso local;
4. revisar solo los últimos 1–3 commits si hubo movimiento remoto;
5. revisar el diff de la tarea;
6. si es ejecución autónoma, reconciliar tablero VAEP con GitHub;
7. continuar desde el último punto verificable.

Una reconexión no justifica repetir inventario arquitectónico ni releer documentación completa.

## 7. Cuándo renovar el mapa arquitectónico

Solo ante nueva capa/proyecto principal, módulo ERP mayor, cambio estructural de persistencia/framework, rediseño auth/RBAC o cambio transversal de tenancy/integración/despliegue/observabilidad. Entonces actualizar una sola vez `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE.md`.

## 8. Producción congelada

Entornos lógicos:

```text
varistorehn_producción
varistorehn_desarrollo
```

Está prohibido modificar/eliminar recursos productivos: `main`, variables, secretos, credenciales, dominios, certificados, bases, datos, servicios, despliegues, respaldos, migraciones, activos o configuraciones de Producción. No exponer secretos ni ejecutar migraciones productivas sin autorización expresa.

## 9. Evidencia obligatoria de CADA changeset

1. incluir `CHANGELOG_AI.md` con objetivo, agente, alcance y validaciones reales;
2. actualizar `TASKS.md` si cambió estado, bloqueo o pendiente;
3. actualizar contexto/índice/arquitectura solo si cambió lo que documentan;
4. actualizar documentos colaborativos cuando cambien reglas/accesos/gobierno;
5. commit descriptivo con agente cuando aplique;
6. publicar SHA/resultado en handoff;
7. no inventar pruebas ni estados externos.

La evidencia universal es `CHANGELOG_AI.md`; evitar modificaciones documentales artificiales.

## 10. Validación proporcional

- Documentación/gobierno: diff, rutas, nombres y guardas; no builds de aplicación sin motivo.
- Backend localizado: pruebas/build dirigidos; ampliar si es transversal, seguridad, datos, migraciones o cierre formal.
- Frontend localizado: lint/build/pruebas dirigidas; E2E para auth, permisos, navegación, facturación o flujo crítico.

La meta es reducir validación redundante, no reducir rigor.

## 11. Mejora continua controlada

Cada agente puede aplicar mejoras de bajo riesgo relacionadas con la tarea, registrar en `TASKS.md` las separadas/transversales, evitar refactors infinitos y medir por menos relecturas, menos comandos/CI redundantes, menor superficie y cero pérdida de trazabilidad.

## 12. Commits y handoff

Formato recomendado:

```text
<tipo>(<área>): <descripción> [agente]
```

Cada entrega indica proyecto confirmado, objetivo, área, validaciones, riesgos/pendientes y commit. Referenciar `PROJECT_CONTEXT.md` en vez de repetir arquitectura.

## 13. VAEP — Ejecución autónoma obligatoria

La especificación completa vive en `PLAN_EJECUCION_AUTONOMA.md`. Tablero operativo:

https://docs.google.com/spreadsheets/d/1RSgaF6q9wnvWT6cSO3bsxpesofompYUYUA7aohPMWTM/edit

### Máquina de estados

```text
PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO
                     \-> BLOQUEADO
```

`LISTO` requiere evidencia GitHub verificable.

### Selección

- elegir la tarea `PENDIENTE` de menor `PRIORIDAD` cuyas dependencias estén todas `LISTO`;
- confirmar PROJECT_ID/repositorio/rama antes de tomarla;
- considerar tomada cualquier tarea `EN_PROGRESO` o `VALIDANDO` con agente asignado;
- revalidar HEAD remoto antes de publicar.

### Regla de bloqueo solicitada por Javier

Una tarea `BLOQUEADO` no paraliza la cola. Después de registrar causa/evidencia, buscar otra tarea independiente.

**Solo se puede continuar con una nueva tarea si esta NO depende directa NI transitivamente de la tarea bloqueada.** Si un ancestro del grafo de dependencias está bloqueado, la tarea no es elegible.

Ejemplo: `A=BLOQUEADO`, `B depende A`, `C depende B`: ni B ni C se ejecutan. `D` sin dependencia de A/B/C sí puede continuar.

No reintentar una tarea bloqueada en bucle dentro de la misma ejecución.

### Actualización coordinada

Al cambiar el estado de una tarea autónoma:

- actualizar fila de `COLA`;
- registrar transición en `BITACORA`;
- actualizar `CHANGELOG_AI.md` en cada changeset;
- actualizar `TASKS.md` y demás colaborativos únicamente si su realidad cambió.

Si Sheet y GitHub discrepan, reconciliar usando GitHub como autoridad técnica antes de continuar.
