# Colaboración IA — VariApp

## Objetivo

Coordinar a Javier Mejía, Codex y ChatGPT con mínima pérdida de contexto, mínimo trabajo redundante, aislamiento entre proyectos y máxima trazabilidad en `Desarrollo`.

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

### ChatGPT

- arquitectura, auditoría, coordinación, revisión y cambios remotos cuando exista conexión GitHub autorizada;
- no tiene acceso al filesystem local de la PC por defecto;
- no afirma cambios locales si solo actuó sobre GitHub.

## Acceso

Acceso local reconocido: Javier Mejía y Codex.

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


## AntiG / Antigravity — deshabilitado

AntiG/Antigravity está fuera del flujo operativo y no bloquea automatizaciones, handoffs, reviews, QA ni promociones.

Flujo vigente:

```text
Jules -> REVIEW-FIRST VAEP -> R2 único cuando corresponda / QA_TAKEOVER -> VAEP Controller -> LISTO_REAL
```

Los artefactos AntiG existentes permanecen únicamente como historial técnico inactivo. No se requiere `agy`, login Antigravity ni Scheduled Task para continuar VariApp.
