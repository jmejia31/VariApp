# Certificación ERP-N1.9 — Series, lotes y vencimientos

## Baseline funcional

`4b5a5c9a8b495fcef62464bf50010ac69117fe48`

## Baseline documental inicial

`7704ed2450f6f63e71a71bd07a7af16f1520f920`

## Gates funcionales confirmados sobre 4b5a5c9a

- Desarrollo - Compilación y pruebas `#32086058893`: SUCCESS.
- Fase 8 `#32086058839`: SUCCESS.
- M10 `#32086058896`: SUCCESS.
- M13 `#32086058819`: SUCCESS.
- Aceptación funcional integral `#32086058832`: todavía en ejecución al abrir N1.9.H.

## Gates del HEAD documental

El commit documental inicial disparó un nuevo set causal:

- Desarrollo `#32088930036`.
- Aceptación integral `#32088930051`.
- Fase 8 `#32088930003`.
- M10 `#32088930030`.
- M13 `#32088930049`.

N1.9.H no debe marcarse cerrado hasta que estos gates aplicables terminen verdes y se reconcilien `CHANGELOG_AI.md`, `TASKS.md` y el tablero VAEP.

## Paquete documental

- `docs/ERP_N1_9_SERIES_LOTES_VENCIMIENTOS.md`
- `docs/ADR_N1_9_AUTORIDAD_TRAZABILIDAD.md`
- `docs/ERD_N1_9_TRAZABILIDAD.md`
- `docs/RUNBOOK_N1_9_TRAZABILIDAD.md`
- este documento de certificación

## DoD de cierre

- [x] Documento canónico N1.9.
- [x] ADR de autoridad/cutover.
- [x] ERD.
- [x] Runbook operativo/rollback.
- [x] Baseline funcional identificado.
- [x] Gates funcionales principales de QA identificados.
- [ ] Gates causales del HEAD documental verdes.
- [ ] `CHANGELOG_AI.md` reconciliado.
- [ ] `TASKS.md` reconciliado.
- [ ] COLA/CONFIG marcan N1.9.H `LISTO` y siguiente punto elegible.

## Protecciones

El cierre documental no autoriza cambios en `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura, ramas nuevas ni force-push.