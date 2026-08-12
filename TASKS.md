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
- [ ] Javier/Codex/AntiG: sincronizar el checkout local con `origin/Desarrollo` después de este changeset documental.
- [ ] Continuar ERP-N0 desde el siguiente punto formal del plan rector, usando la documentación ERP-N0 específica y el último commit de `Desarrollo` como evidencia.
- [ ] Antes de marcar N0 cerrado, verificar que no queden consumidores legacy, migraciones transitorias pendientes, contratos antiguos o documentación contradictoria.

## Último baseline técnico documentado

`0a60b9b6de7f7d14bbb40de5795cc3c390e57279` — cierre documental de persistencia relacional base de `MetodoPago`.

La numeración del nombre de una migración no sustituye el número de punto del plan. Consultar el documento de fase/punto vigente.

## Regla de continuidad

Tras reconexión, compactación o cambio de agente:

1. leer `PROJECT_CONTEXT.md`;
2. leer este archivo;
3. revisar `git status --short --branch`;
4. revisar únicamente los últimos commits necesarios;
5. continuar el punto pendiente sin repetir el inventario arquitectónico.