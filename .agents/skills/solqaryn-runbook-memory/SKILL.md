---
name: solqaryn-runbook-memory
description: "Curacion de runbooks y conocimiento operativo recurrente de SOLQARYN. Usar cuando una leccion confirmada se repite, un procedimiento operativo necesita estabilizarse o un troubleshooting validado debe quedar reutilizable dentro de solqaryn/VariApp. No sustituye autoridades canonicas, evidencia, estado vivo ni decisiones de arquitectura."
---

# SOLQARYN Runbook Memory

## Gate

Aplicar despues de `solqaryn-project-governance`.

## Que guardar

- procedimientos repetibles confirmados;
- diagnosticos recurrentes con causa demostrada;
- secuencias de recovery seguras;
- checks operativos que evitan errores repetidos;
- convenciones practicas ya verificadas.

## Que no guardar como autoridad

- estado temporal de una tarea;
- opiniones no verificadas;
- resultados de una sola ejecucion sin valor recurrente;
- secretos o credenciales;
- reglas que ya tienen una autoridad canonica;
- snapshots que puedan quedar stale.

## Curacion

- deduplicar antes de agregar;
- indicar precondiciones, pasos, validacion y rollback cuando aplique;
- enlazar la autoridad canonica en vez de copiarla;
- retirar o marcar obsoleto un runbook cuando cambie la realidad;
- nunca usar memoria operativa para contradecir HEAD, CI o la autoridad vigente.
