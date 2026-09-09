# Preparación J1–J6 — Fase 1

Estado: **PREPARED / NOT ACTIVE**.

Esta preparación es aditiva y no sustituye todavía a las lanes operativas `JULES_A/B/C/D`. El cutover ocurre únicamente en Fase 2 tras health read-only y canaries.

## Reglas

- Autoridad única: `docs/VAEP_AUTHORITY.md`.
- Repositorio: `jmejia31/VariApp`; rama: `Desarrollo`.
- `main`, Producción, secretos, deploy, branch/PR/push/merge Jules: fuera de alcance.
- J1–J6 entregan patch/artifact/evidencia; `COMPLETED != LISTO_REAL`.
- Máximo 2 attempts y 1 rework.
- No busywork, no duplicación semántica, un write-scope autoritativo por worker.
- En F1 todos los nuevos workers permanecen `enabled=false`.

## Roles preparados

| Worker | Preferencia | Fallback | Secret |
|---|---|---|---|
| J1 | CODE / Core | QA | `JULES_J1_API_KEY` |
| J2 | CODE / Backend / Data | CODE general | `JULES_J2_API_KEY` |
| J3 | CODE / Frontend | CODE general | `JULES_J3_API_KEY` |
| J4 | CODE / Infra / Integraciones | CODE general | `JULES_J4_API_KEY` |
| J5 | QA / Security / Regression | CODE | `JULES_J5_API_KEY` |
| J6 | Integration / Recovery | CODE / QA | `JULES_J6_API_KEY` |

## Gate de Fase 1

F1 se considera preparada cuando existe el registry canónico, los seis workflows manuales de readiness están presentes, el checker estático pasa y el sistema legacy A–D no ha sido desactivado.
