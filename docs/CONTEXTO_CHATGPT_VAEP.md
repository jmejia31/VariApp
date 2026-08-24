# Contexto de proyecto ChatGPT/VAEP

> Referencia histórica/operativa incorporada el 2026-08-24. No sustituye `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md`, `CHANGELOG_AI.md`, GitHub ni el estado actual del código.

## Alcance

Este contexto pertenece exclusivamente a **VariApp/VariStorehn** y resume su operación ChatGPT/VAEP.

## Resumen operativo heredado

- **Automatización VAEP:** coordina trabajo granular, dependencias, validación y evidencia. Sus versiones, estados y reglas vigentes son volátiles; antes de operar se consultan `AGENTS.md`, `PLAN_EJECUCION_AUTONOMA.md` y los artefactos VAEP actuales.
- **Ramas y validación causal:** el contexto histórico exige trabajar sobre la rama autorizada, preservar trabajo concurrente y atribuir cada validación al changeset que realmente evaluó. Un resultado anterior o de otro SHA no demuestra el estado del HEAD actual. La política vigente de ramas siempre proviene de `AGENTS.md`.
- **Cadena funcional prioritaria:** compras -> recepciones -> reservas de inventario -> facturación. Es una guía para localizar impacto, no una afirmación de que todos los pasos formen una única transacción o estén cerrados funcionalmente. Confirmar contratos actuales en controladores, servicios, persistencia, rutas Angular y pruebas del área.
- **No duplicar trabajo:** antes de crear código, migraciones, rutas o documentación, buscar implementación, tarea, commit y evidencia existentes. Extender o reconciliar la vía canónica; no abrir una segunda solución por desconocer el trabajo previo.
- **Consulta selectiva:** partir de `PROJECT_INDEX.md`, identificar el punto de entrada y seguir solo dependencias directas. Ampliar la revisión únicamente ante inconsistencia, cambio transversal o riesgo de datos/seguridad.

## Protocolo rápido de consulta

1. Ejecutar el guard indicado en `AGENTS.md` y confirmar repo, rama, HEAD y estado limpio/concurrente.
2. Leer `PROJECT_CONTEXT.md`, la tarea relevante y la última entrada aplicable de `CHANGELOG_AI.md`.
3. Usar el índice de decisión de `PROJECT_INDEX.md` para abrir el controlador/ruta, servicio, repositorio/modelo y prueba directamente afectados.
4. Para la cadena prioritaria, buscar por símbolos `Compra`, `RecepcionCompra`, `ReservaInventario` y `Factura`; no inferir integración solo por nombres próximos.
5. Verificar la implementación y evidencia del HEAD antes de declarar `PASS`, cierre o causalidad.

## Regla de vigencia

Si este documento contradice fuentes canónicas o código actual, prevalecen `AGENTS.md`, GitHub/HEAD y la evidencia verificable. Actualizar este resumen solo cuando cambie su utilidad operativa; registrar el cambio en `ARCHITECTURE_CHANGELOG.md` y `CHANGELOG_AI.md`.
