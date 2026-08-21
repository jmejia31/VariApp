# VAEP v3.20 — Sprint 40 — Cola condicional A/B/C/D

Ventana efectiva: `2026-08-20T23:02:00-06:00` → `2026-08-21T06:00:00-06:00` (`America/Tegucigalpa`).

Objetivo de cierre: **40 MICROTAREA padre reales en LISTO**. Cada Jules A/B/C/D mantiene una cola prearmada de hasta **40 padres candidatos**; esto NO significa 40 sesiones concurrentes. Cada cuenta conserva máximo un ownership autoritativo activo y, al liberar su lane, toma el siguiente parent/carril seguro después de revalidar COLA, dependencias, intentos, scope y ownership.

## Horizonte común de 40 padres candidatos

1. N2.5.D
2. N2.5.E
3. N2.5.F
4. N2.5.G
5. N2.5.H
6. N2.6.A
7. N2.6.B
8. N2.6.C
9. N2.6.D
10. N2.6.E
11. N2.6.F
12. N2.6.G
13. N2.6.H
14. N2.7.A
15. N2.7.B
16. N2.7.C
17. N2.7.D
18. N2.7.E
19. N2.7.F
20. N2.7.G
21. N2.7.H
22. N2.8.A
23. N2.8.B
24. N2.8.C
25. N2.8.D
26. N2.8.E
27. N2.8.F
28. N2.8.G
29. N2.8.H
30. N2.9.A
31. N2.9.B
32. N2.9.C
33. N2.9.D
34. N2.9.E
35. N2.9.F
36. N2.9.G
37. N2.9.H
38. N3.1.A
39. N3.1.B
40. N3.1.C

La lista es un horizonte de planificación, no una autorización para saltar dependencias. Si el estado fresco cambia, la automatización sustituye candidatos por los siguientes `MICROTAREA` reales de menor prioridad que sean técnicamente seguros, manteniendo profundidad objetivo 40 cuando sea posible.

## Reglas por Jules

Para cada candidato listo para ejecución, A/B/C/D reciben exclusivamente scopes no solapados o lanes de implementación/review/tests compatibles. Cada lane tiene `ATTEMPT=1` y como máximo una única corrección `ATTEMPT=2/R2`. El contador pertenece a la tarea lógica y no se reinicia por work-stealing. R3+ está prohibido.

Antes de `COMPLETED`, cada Jules debe ejecutar `SELF_REVIEW_PASS_1` y `SELF_REVIEW_PASS_2`. Si R2 todavía falla, `OWNER=CHATGPT_VAEP_VIBE` corrige/prueba/certifica y el Jules consume inmediatamente el siguiente candidato de su cola.

Los checkpoints :00/:15/:30/:45 deben revalidar y reponer estas colas, medir profundidad por Jules y evitar cualquier idle voluntario sin crear ownership duplicado.
