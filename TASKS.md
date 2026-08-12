# TASKS — VariApp

Fuente operativa de pendientes. Mantenerla corta; los detalles extensos viven en el documento de fase correspondiente.

## Reglas

- No duplicar aquí todo el plan maestro.
- Cada agente actualiza solo tareas realmente afectadas.
- Una tarea cerrada debe quedar respaldada por commit/evidencia.
- Todo changeset intencional debe quedar registrado en `CHANGELOG_AI.md`.
- No iniciar ERP-N1 hasta el cierre formal de ERP-N0.

## Gobierno y productividad

- [x] Crear memoria canónica: `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `TASKS.md`, `CHANGELOG_AI.md`.
- [x] Unificar reglas de colaboración y eliminar autorización de ramas temporales.
- [x] Definir matriz de acceso local/remoto del equipo.
- [x] Incorporar protocolo de optimización de rendimiento/tokens y recuperación tras reconexión.
- [x] Incorporar gate obligatorio de identidad `PROJECT_ID=VARIAPP` para cada conversación/sesión.
- [x] Incorporar aislamiento anti-contaminación entre proyectos.
- [x] Incorporar evidencia obligatoria de cada changeset mediante `CHANGELOG_AI.md`.
- [x] Incorporar guard local `pre-commit` para repo/rama/evidencia y validación de `origin` en `post-commit`.
- [x] Incorporar `scripts/iniciar-sesion-ia.ps1` como bootstrap read-only y de bajo consumo.
- [x] Definir uso limitado de `[skip ci]` para cambios exclusivamente administrativos/locales.
- [ ] Javier/Codex/AntiG: sincronizar el checkout local con `origin/Desarrollo` y ejecutar `scripts/configurar-colaboracion.ps1` para activar las guardas nuevas.
- [ ] Optimizar en un changeset separado los triggers redundantes de GitHub Actions, después de confirmar que no interfiera con la validación ERP-N0 activa.

## ERP-N0 — estado inmediato

- [x] Punto 5: seed, preflight fail-closed, backfill histórico `MetodoPago`, postcheck y certificación MySQL 8.4.
- [ ] Continuar ERP-N0 desde el siguiente punto formal del plan rector, reutilizando `docs/ERP_N0_PUNTO_5_METODO_PAGO_BACKFILL.md` y el último commit de `Desarrollo` como handoff.
- [ ] Antes de marcar N0 cerrado, verificar que no queden consumidores legacy, migraciones transitorias pendientes, contratos antiguos o documentación contradictoria.

## Último baseline técnico documentado

Punto 5 cerrado en `f878c4b30122dc2e594b2805cf2fca423bb5ca31`, preservando la implementación N0.5 previamente certificada en `0b0d18b6fe5cee2380175b0d6175b87274ad157e`.

Después del cierre del Punto 5, `Desarrollo` recibió `3dcc1eb8f304674c48758f7ce66c7dca404f6b00` con cambios frontend de escaparate VariStorehn. Este trabajo es concurrente e independiente del gobierno colaborativo y debe preservarse.

Punto 5: migración `20260812023600_N0_5_BackfillMetodoPagoHistorico`, preflight/postcheck dedicados y CI N0.5 verde. Consultar `docs/ERP_N0_PUNTO_5_METODO_PAGO_BACKFILL.md` para evidencia y reglas de continuidad.

La numeración del nombre de una migración no sustituye el número de punto del plan. Consultar el documento de fase/punto vigente.

## Regla de continuidad

Tras nueva conversación, reconexión, compactación o cambio de agente:

1. confirmar `PROJECT_ID=VARIAPP`, repositorio `jmejia31/VariApp` y rama `Desarrollo`;
2. leer `PROJECT_CONTEXT.md`;
3. leer este archivo;
4. leer la última entrada relevante de `CHANGELOG_AI.md`;
5. revisar únicamente los últimos commits necesarios;
6. continuar el punto pendiente sin repetir el inventario arquitectónico.