---
name: solqaryn-motion
description: "Motion y microinteracciones para SOLQARYN. Usar cuando una tarea Angular requiera animaciones, transiciones, feedback de estado, apertura/cierre, cambios de layout o revision de movimiento. Solo autoriza motion con proposito claro, accesible, performante y compatible con el stack vigente de solqaryn/VariApp."
---

# SOLQARYN Motion

## Gate

Aplicar despues de `solqaryn-project-governance`.

## Principios

1. definir primero por que debe existir movimiento;
2. no animar si el cambio se entiende mejor de forma inmediata;
3. usar la herramienta mas simple compatible con Angular y CSS vigente;
4. priorizar `transform` y `opacity`;
5. evitar animaciones largas, decorativas o que bloqueen tareas;
6. respetar foco, teclado y `prefers-reduced-motion`;
7. no introducir una libreria nueva solo para una microinteraccion;
8. no degradar rendimiento ni estabilidad del layout.

## Revision

Comprobar entrada, salida, interrupciones, estados rapidos, navegacion por teclado, reduced motion, layout shift y comportamiento en movil.
