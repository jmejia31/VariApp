# N8.16.G — TEST_CI / gobierno de matrices

Estado: `VALIDATING`.

## Checks automatizados

El validador canónico es `scripts/validate_matrix_governance.py` y el gate dedicado es `.github/workflows/matrix-governance.yml` (`Desarrollo - Gobierno de matrices`).

Debe rechazar, al menos:

- `MATRIX_ID` duplicado o fuera del patrón estable;
- IDs basados en row/index/posición visual;
- parent desconocido;
- columnas canónicas vacías en el catálogo;
- implementación root duplicada dentro del catálogo inicial;
- diferencia entre conteo declarado y filas reales;
- falta de campos obligatorios de identidad/ownership/data/backend/frontend/security/evidence en la plantilla;
- ausencia del invariante `MATERIAL_WITHOUT_ID` prohibido.

El gate corre sólo sobre `Desarrollo`, no despliega y no toca datos. El estado pasa a `LISTO_REAL` únicamente después de un run causal terminal `SUCCESS` y readback de su job.
