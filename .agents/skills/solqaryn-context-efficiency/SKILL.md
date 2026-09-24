---
name: solqaryn-context-efficiency
description: "Eficiencia de contexto para SOLQARYN. Usar en sesiones largas, auditorias extensas, handoffs, prompts operativos o tareas con mucha documentacion para reducir duplicacion y cargar solo lo necesario dentro de solqaryn/VariApp. Nunca permite omitir seguridad, arquitectura, aislamiento, QA, datos, evidencia ni gates requeridos."
---

# SOLQARYN Context Efficiency

## Gate

Aplicar despues de `solqaryn-project-governance`.

## Secuencia

1. ejecutar gate de proyecto;
2. leer autoridad y contexto canonico una vez;
3. localizar el area con `PROJECT_INDEX.md`;
4. abrir objetivo y dependencias directas;
5. usar Git para identificar deltas;
6. cargar detalle adicional solo cuando una decision lo requiera;
7. resumir logs extensos separando hechos, errores y siguiente accion;
8. detener exploracion cuando el objetivo ya esta demostrado.

## Reglas

- no reindexar el repositorio sin necesidad;
- no duplicar contratos en varios documentos;
- separar instrucciones estables de evidencia temporal;
- preferir referencias a copiar bloques largos;
- conservar valores exactos cuando sean evidencia;
- nunca ahorrar contexto eliminando controles criticos.

## Handoff compacto

Incluir solo: identidad, objetivo, archivos/scope, hechos nuevos, validaciones, commit/HEAD y pendiente real.
