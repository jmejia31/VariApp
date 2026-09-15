# Contexto ChatGPT / VAEP — VariApp

## Autoridad operativa

La única autoridad de reglas de automatización es:

```text
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
```

ChatGPT/VAEP, Chat B (ChatGPT Business), Jules J1/J2/J3/J4/J5/J6, Codex cuando sea autorizado y cualquier colaborador futuro deben leer ese MAESTRO. No deben inferir reglas vigentes desde prompts anteriores, Issues, CHANGELOG, BITACORA, artifacts ni etiquetas numéricas históricas.

## Fuentes de estado

- CONFIG/COLA/PLAN_MAESTRO/BITACORA/EJECUCION_MANUAL: estado operativo fresco y reconciliable.
- GitHub `Desarrollo`, PR #2, Actions, Issues, artifacts, sesiones, código y tests: evidencia técnica.
- Plan Maestro ERP V5: roadmap/DoD funcional.
- `PROJECT_CONTEXT.md`: contexto transversal compartido.
- `docs/VAEP_HANDOFF_CURRENT.md`: handoff compartido de cambios recientes y puntos de inspección; informativo, no autoridad ni estado machine-readable.

## Trabajo

ChatGPT/VAEP y Chat B ejecutan REVIEW_FIRST, QA, integración, correcciones, CI, certificación, rollup y failover dentro de `Desarrollo`. Jules J1/J2/J3/J4/J5/J6 implementan scopes exclusivos y entregan patch/artifact. Chat B no es una lane Jules y no puede omitir ningún gate del MAESTRO.

AntiG queda `RESERVED_INACTIVE`: scheduler deshabilitado, handoff processing deshabilitado y sin autoridad LISTO_REAL. Codex no participa salvo orden explícita futura del usuario, pero debe leer el handoff antes de reincorporarse para no repetir trabajo ni usar estado stale.

Las tareas externas canónicas de controller/watchdog son cinco y, a fecha 2026-09-09, fueron recreadas y verificadas habilitadas en `America/Tegucigalpa`: `:00`, `:12`, `:24`, `:36`, `:48`. No crear duplicados sin consultar primero el sistema de Tareas.

## Continuidad compartida

Antes de retomar trabajo después de una desconexión o cambio de agente:

1. Leer `docs/VAEP_HANDOFF_CURRENT.md`.
2. Reconsultar HEAD vivo de `Desarrollo`.
3. Verificar `CURRENT_PARENT`, `lastClosedParent`, `closureReceipts`, `throughputPlan` y lanes en `vaep/control/jules-autorefill-catalog.json`.
4. Verificar `vaep/control/dispatch-admission.json`.
5. Reconciliar contra COLA/PLAN_MAESTRO y revisar los gates causales exactos del recibo de cierre.

Si las superficies difieren, corregir estado stale antes de implementar. No fabricar PASS/LISTO ni busywork para ocultar una divergencia.

Estado base compartido de esta resincronización: `N5.1.H` cerrado `LISTO_REAL`; `N5.2.A` promovido y abastecido con seis scopes materiales J1–J6; admission `OPEN`. Tratarlo como handoff, no como sustituto de la lectura viva.

## Cambio de reglas

Una modificación de política se hace sobre `docs/VAEP_AUTHORITY.md` y los consumidores siguen leyendo la misma ruta. No se crea una autoridad nueva ni una copia numerada.
