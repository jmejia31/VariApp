# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Los SHA exactos se consultan en Git.

## Regla obligatoria

Todo changeset intencional debe incluir una entrada breve en este archivo. No es necesario modificar otros colaborativos si su contenido no cambió; evitar ruido documental.

## 2026-08-11 — VAEP v1: ejecución autónoma, Drive y dependencias

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo:** permitir ejecución autónoma de puntos autorizados sin necesitar una instrucción manual por cada tarea, manteniendo control de proyecto, dependencia, concurrencia y evidencia.

**Alcance:**

- creación de `PLAN_EJECUCION_AUTONOMA.md`;
- creación del Google Sheet `VariApp — Cola de Ejecución Autónoma VAEP` con pestañas `COLA`, `CONFIG` y `BITACORA`;
- estados estrictos `PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO|BLOQUEADO`;
- GitHub como autoridad técnica y Drive como tablero operativo;
- selección por prioridad + dependencias;
- lock lógico por estado/agente y revalidación de HEAD antes de publicar;
- regla de continuidad: una tarea bloqueada no detiene la cola, pero solo se puede pasar a tareas sin dependencia directa ni transitiva de la bloqueada;
- prohibición de reintentos en bucle de una tarea bloqueada;
- actualización de `AGENTS.md` y `TASKS.md` para hacer VAEP vinculante;
- preparación de ejecución recurrente de ChatGPT con un máximo operativo de una tarea completada por corrida, pudiendo saltar bloqueadas independientes.

**Validación real:** se verificó el Sheet creado y sus primeras filas; repositorio/rama se comprobaron antes de preparar el changeset. No se tocó `main`, Producción, secretos, bases ni recursos externos productivos.

**Riesgo controlado:** el repositorio tiene trabajo concurrente. Toda publicación VAEP debe ser fast-forward y preservar cambios de otros agentes; nunca force-push.

## 2026-08-11 — Gobierno colaborativo v2: identidad, aislamiento y evidencia

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** impedir confusión entre proyectos, reducir trabajo repetitivo y convertir trazabilidad y arranque de sesión en guardrails verificables.

**Alcance:** gate por `PROJECT_ID=VARIAPP`, aislamiento entre proyectos, lectura mínima de colaborativos, evidencia obligatoria en `CHANGELOG_AI.md`, `scripts/iniciar-sesion-ia.ps1`, `pre-commit`, hardening de `post-commit`, configuración colaborativa y política limitada `[skip ci]`.

**Validación real:** se preservaron avances concurrentes sin force-push y no se modificó código funcional, datos ni Producción.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Alcance:** creación de `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `TASKS.md`, `CHANGELOG_AI.md`; alineación colaborativa; `Desarrollo` como única rama; matriz de acceso y reglas de rendimiento/tokens.

**Baseline previo:** `0a60b9b6de7f7d14bbb40de5795cc3c390e57279`.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Cambios:** migración `20260812023600_N0_5_BackfillMetodoPagoHistorico`, seed idempotente, backfill exacto, preflight/postcheck, workflow N0.5 y acta documental.

**Validación real:** workflow dedicado N0.5 run `31558300465` success y CI general run `31558300370` en verde.

**Handoff:** enum y columnas legacy permanecen temporalmente hasta migrar consumidores posteriores previstos.

## 2026-08-11 — Catálogo público VARISTOREHN sin redirección a login

**Responsable:** Codex.

**Objetivo:** mostrar productos activos existentes mediante consulta pública segura sin sesión ni cambio de base de datos.

**Alcance:** proyección pública sin costos/auditoría, `GET /tienda/productos`, consumo desde `frontend/src/app/features/varistorehn` y personalización con identidad/tema públicos.

**Validaciones:** backend Release sin advertencias/errores; 2 pruebas `TiendaPublicaTests` aprobadas; frontend lint y build producción aprobados. Suite backend 273/291; 18 integraciones fallaron por credenciales MySQL locales, no por el cambio.

## Formato futuro

Cada entrada debe contener fecha, agente, objetivo, archivos/áreas, validaciones reales, riesgos/pendientes y commit cuando sea útil. No registrar secretos, credenciales ni datos sensibles.
