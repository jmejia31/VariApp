# TASKS — VariApp

Fuente operativa de pendientes. Mantenerla corta; los detalles extensos viven en el documento de fase correspondiente.

## Reglas

- No duplicar aquí todo el plan maestro.
- Cada agente actualiza solo tareas realmente afectadas.
- Una tarea cerrada debe quedar respaldada por commit/evidencia.
- No iniciar ERP-N1 hasta el cierre formal de ERP-N0.

## Estado inmediato

- [x] Crear memoria canónica: `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `TASKS.md`, `CHANGELOG_AI.md`.
- [x] Unificar reglas de colaboración y eliminar autorización de ramas temporales.
- [x] Definir matriz de acceso local/remoto del equipo.
- [x] Incorporar protocolo de optimización de rendimiento/tokens y recuperación tras reconexión.
- [x] ERP-N0 Punto 5: seed, preflight fail-closed, backfill histórico `MetodoPago`, postcheck y certificación MySQL 8.4.
- [ ] Javier/Codex/AntiG: mantener el checkout local sincronizado con `origin/Desarrollo` antes de continuar trabajo local.
- [ ] Continuar ERP-N0 desde el siguiente punto formal del plan rector, reutilizando `docs/ERP_N0_PUNTO_5_METODO_PAGO_BACKFILL.md` y el último commit de `Desarrollo` como handoff.
- [ ] Antes de marcar N0 cerrado, verificar que no queden consumidores legacy, migraciones transitorias pendientes, contratos antiguos o documentación contradictoria.

## Último baseline técnico documentado

Implementación N0.5 certificada: `0b0d18b6fe5cee2380175b0d6175b87274ad157e`.

Punto 5 cerrado: migración `20260812023600_N0_5_BackfillMetodoPagoHistorico`, preflight/postcheck dedicados y CI N0.5 verde. Consultar `docs/ERP_N0_PUNTO_5_METODO_PAGO_BACKFILL.md` para evidencia y reglas de continuidad.

La numeración del nombre de una migración no sustituye el número de punto del plan. Consultar el documento de fase/punto vigente.

## Regla de continuidad

Tras reconexión, compactación o cambio de agente:

1. leer `PROJECT_CONTEXT.md`;
2. leer este archivo;
3. revisar `git status --short --branch`;
4. revisar únicamente los últimos commits necesarios;
5. continuar el punto pendiente sin repetir el inventario arquitectónico.