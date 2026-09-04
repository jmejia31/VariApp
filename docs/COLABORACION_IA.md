# Colaboración IA — VariApp

## Objetivo

Coordinar a Javier Mejía, Codex, AntiG/Antigravity y ChatGPT con mínima pérdida de contexto, mínimo trabajo redundante, aislamiento entre proyectos y máxima trazabilidad en `Desarrollo`.

## Identidad de este proyecto

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Estas reglas pertenecen a VariApp y solo el contexto canónico de VariApp puede autorizar cambios aquí.

## Inicio de CADA conversación/sesión

Antes de escribir:

1. confirmar identidad del proyecto/repo/rama;
2. leer `AGENTS.md`;
3. leer `PROJECT_CONTEXT.md`;
4. leer `TASKS.md`;
5. leer la última entrada relevante de `CHANGELOG_AI.md`;
6. revisar únicamente commits nuevos desde el handoff conocido;
7. abrir solo archivos objetivo/dependencias directas.

Localmente, ejecutar `scripts/iniciar-sesion-ia.ps1`. Remotamente, realizar la verificación equivalente mediante GitHub.

Si hay discrepancia entre memoria y repositorio real, prevalece el repositorio y el agente no escribe hasta resolverla.

## Equipo

### Javier Mejía

- propietario del proyecto;
- define prioridades y aceptación;
- autoriza merge, Producción, migraciones productivas y cambios de reglas.

### Codex

- implementa y prueba desde el proyecto local autorizado;
- trabaja sobre `Desarrollo`;
- usa memoria canónica y evita reescaneos/relecturas innecesarias;
- tras reconexión continúa desde Git + contexto, no reinicia diagnóstico.

### AntiG / Antigravity

- implementa y prueba desde el proyecto local autorizado;
- sincroniza `Desarrollo` antes del trabajo y publica cambios trazables;
- aplica las mismas reglas de rendimiento y memoria canónica.

### ChatGPT

- arquitectura, auditoría, coordinación, revisión y cambios remotos cuando exista conexión GitHub autorizada;
- no tiene acceso al filesystem local de la PC por defecto;
- no afirma cambios locales si solo actuó sobre GitHub.

## Acceso

Acceso local reconocido: Javier Mejía, Codex y AntiG/Antigravity.

ChatGPT y otros agentes operan remotamente solo mediante conectores GitHub autorizados, salvo ampliación explícita documentada por Javier.

## Memoria compartida

- `PROJECT_CONTEXT.md` — contexto técnico e identidad.
- `PROJECT_INDEX.md` — mapa de carpetas.
- `ARCHITECTURE.md` — patrones y fronteras.
- `TASKS.md` — pendientes.
- `CHANGELOG_AI.md` — evidencia/handoff.

## Evidencia por cambio

Cada changeset debe actualizar `CHANGELOG_AI.md`. `TASKS.md` se actualiza si cambia el estado operativo. Contexto/arquitectura/índice y documentos colaborativos solo se modifican si su contenido realmente cambió.

Esto cumple trazabilidad sin generar ruido documental artificial.

## Flujo eficiente

1. gate de proyecto;
2. leer memoria canónica;
3. localizar módulo con `PROJECT_INDEX.md`;
4. revisar solo objetivo + dependencias directas;
5. implementar mínimo cambio correcto;
6. validar proporcionalmente;
7. registrar evidencia;
8. publicar en `Desarrollo`;
9. handoff con SHA/pendiente.

## Optimización de tokens y tiempo

- No volver a recorrer todo el repositorio.
- No releer archivos ya documentados si no cambiaron.
- No repetir comandos ya confirmados por reconexión.
- Usar Git para saber qué cambió.
- Usar búsquedas dirigidas por símbolo/ruta.
- Abrir únicamente documento de fase/punto necesario.
- Finalizar al completar objetivo + validaciones.
- Preguntar decisiones de negocio reales en lugar de escanear módulos no relacionados.

## Git, CI y Producción

- rama única `Desarrollo`;
- no ramas adicionales;
- `main` no se toca;
- PR #2 permanece borrador;
- no auto-merge;
- Producción congelada;
- no secretos;
- no migraciones productivas sin autorización;
- `[skip ci]` solo para cambios administrativos/locales permitidos por `AGENTS.md`.

Las reglas completas viven en `AGENTS.md`.


## AntiG automático — Reviewer/Fixer + Preflight + VAEP

AntiG/Antigravity queda especializado como `AUTOMATED_REVIEWER_FIXER`:

- recibe automáticamente handoffs terminales de Jules mediante el worker local;
- revisa artifact, patch, base SHA, attempt y scope;
- aplica/corrige únicamente dentro del scope autorizado;
- ejecuta build/lint/tests/E2E proporcionales cuando apliquen;
- devuelve `RETURN_TO_JULES` solo en ATTEMPT=1 cuando el defecto exige R2;
- devuelve `BLOCKED_QA_TAKEOVER` tras ATTEMPT=2 cuando el defecto no puede cerrarse de forma local/segura;
- entrega `READY_FOR_VAEP` con evidencia, nunca `LISTO_REAL`;
- puede mantener scripts/preflight/CI cuando ese sea el scope explícito, pero el reviewer de Jules no modifica gobierno/CI fuera del dispatch;
- nunca toca `main`, Producción, secretos, Vercel, dominios ni bases productivas.

Implementación canónica:

```text
.agents/agents/variapp-reviewer/agent.md
scripts/antig/antig-review-worker.ps1
scripts/antig/install-antig-automation.ps1
scripts/antig/antig-self-test.ps1
vaep/schemas/antig-review-result.schema.json
docs/ANTIGRAVITY_AUTOMATION.md
```

El worker es fail-closed: requiere checkout limpio y sincronizado, usa mutex local, procesa un handoff por ciclo, no publica si HEAD remoto se mueve y no usa permisos globales irrestrictos.
