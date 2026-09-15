# Intervención obligatoria N8.15–N8.24

## Motivo
El flujo normal se pausa para eliminar deuda de especificación/arquitectura y revalidar cierres operativos sensibles antes de continuar. No se reescribe historia: los cierres previos permanecen como hechos históricos y son objeto de revalidación forense cuando corresponda.

La especificación ejecutable y los DoD detallados de esta intervención viven en:

`docs/matrices-evaluacion/00_GOBERNANZA/ESPECIFICACION_EJECUCION_N8_15_N8_24.md`

## Frontera y gate HARD

- Entrada causal: `N8.6.F`.
- Intervención obligatoria: `N8.15.A` → ... → `N8.24.H`.
- Reentrada: revalidar/promover desde `N8.6.G` según el resultado forense de N8.19.
- `N8.6.G` no debe volver a ser elegible materialmente antes del cierre de `N8.24.H`.
- `GATE-N8` debe incluir `N8.15.H`–`N8.24.H` como prerequisitos formales.
- Las diez automatizaciones permanecen pausadas hasta reactivación explícita del propietario; la pausa del propietario prevalece sobre liveness recovery.

## Secuencia HARD
### N8.15 — Baseline arquitectónico e inventario exhaustivo
Inventariar frontend, backend, BD, rutas, menú, componentes, servicios, endpoints, entidades, migraciones, roles/permisos, dialogs/modals, widgets, shared primitives y documentación. Entregar catálogo exacto y candidatos a deuda sin borrar nada todavía.

### N8.16 — Gobierno y catálogo de matrices
Cerrar taxonomía, IDs estables, plantilla, reglas de nueva interfaz, ownership, matriz→código→test→evidencia y catálogo exhaustivo. El conteo exacto sólo se fija después del inventario material.

### N8.17 — Matrices de TODAS las interfaces
Crear y completar una matriz por cada pantalla/ruta, diálogo de negocio, widget/formulario interactivo independiente, shell/navegación y primitive compartida que tenga contrato propio. Incluir campos, flujo, APIs, BD, RBAC, permisos, readonly/autofill, seguridad, responsive, accesibilidad y pruebas.

### N8.18 — Arquitectura limpia y limpieza certificada
Implementar la jerarquía padre→hijo en frontend/backend/docs y contratos de datos; consolidar alertas/modales reutilizables; retirar únicamente dead code/duplicados/stale docs demostrados. Regression completa; cero eliminación especulativa. DDL destructivo de BD queda bloqueado hasta certificar backup/restore en N8.21; los candidatos se cierran allí, no se olvidan.

### N8.19 — Auditoría forense desde N8.6.G
Ejecutar la FASE 0 del prompt maestro: N8.6.G/H, N8.7 A–H, N8.8 A–H y todo lo posterior que haya avanzado. Clasificar evidencia real; investigar timestamps/N/A; reabrir lo que corresponda sin fabricar PASS.

### N8.20 — Motor/proveedor real de BD DEV
Identificar runtime real, versión, proveedor, plan y capacidades backup/PITR/retention/restore sin exponer secretos.

### N8.21 — Backup real + restore aislado
Demostrar backup real, restore fuera de DB DEV activa, sanity de schema/data/app y cleanup; reconciliar N8.8/N8.9 sólo con evidencia. Consumir además cualquier candidato `REMOVE_SAFE_DB_AFTER_BACKUP` dejado por N8.18.

### N8.22 — STAGING_EQUIVALENT
Comparar PROD vs DEV exclusivamente mediante metadata no secreta/read-only conforme a la autorización; corregir gaps sólo en DEV; certificar equivalencia material o mantener gap explícito.

### N8.23 — Rollback DEV + runbooks ejecutables
Demostrar rollback reversible backend/frontend en DEV, estrategia DB; medir recuperación; crear/validar runbooks de migración, smoke y hypercare mediante dry-run seguro.

### N8.24 — GO_LIVE_GATE deliberado + cierre cero deuda
Implementar gobierno de N9.4 como gate humano deliberado, deduplicado y fail-closed. `GO_LIVE_AUTHORIZED=FALSE`. Cero write productivo. Su DOC_CERT ejecuta una revalidación global de cero deuda material en la intervención antes de habilitar la reentrada.

## Microtareas A–H por parent
A PRE/auditoría; B dominio/contratos; C persistencia/datos; D backend/API/operación; E frontend/UX o superficie operativa; F RBAC/seguridad/observabilidad; G QA/regresión/CI/prueba material; H documentación/certificación.

N/A sólo es válido con evidencia causal de que la capa no requiere delta.

## Arquitectura vs matrices

Orden obligatorio:

`inventario real -> arquitectura objetivo/gobierno -> matrices completas -> refactor/limpieza -> certificación`.

El contrato arquitectónico se define antes de las matrices; la reestructuración física se ejecuta después de las matrices para que código y limpieza se midan contra un objetivo verificable.

## Handoff
La cadena de intervención depende materialmente de N8.6.F. Al cerrar N8.24.H se revalida/promueve N8.6.G como checkpoint histórico-causal y se reconcilia todo avance posterior. El trabajo que ya ocurrió después de N8.6.F no se borra ni se da por inválido automáticamente: N8.19 decide causalmente qué se confirma, reabre o queda superseded.

## Restricciones
- Rama: `Desarrollo`.
- Main y Producción write: prohibidos.
- Sin secretos.
- Single writer/read-before-write/readback.
- P0=0 y P1=0 para LISTO_REAL.
- Evidencia > declaración.
- Las 10 automatizaciones permanecen detenidas hasta reactivación explícita del propietario.
