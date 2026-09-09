# Contexto ChatGPT / VAEP — VariApp

## Autoridad operativa

La única autoridad de reglas de automatización es:

```text
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
```

ChatGPT/VAEP, Chat B (ChatGPT Business) y Jules J1/J2/J3/J4/J5/J6 deben leer ese MAESTRO. No deben inferir reglas vigentes desde prompts anteriores, Issues, CHANGELOG, BITACORA, artifacts ni etiquetas numéricas históricas.

## Fuentes de estado

- CONFIG/COLA/PLAN_MAESTRO/BITACORA/EJECUCION_MANUAL: estado operativo fresco.
- GitHub `Desarrollo`, PR #2, Actions, Issues, artifacts, sesiones, código y tests: evidencia técnica.
- Plan Maestro ERP V5: roadmap/DoD funcional.
- `docs/VAEP_HANDOFF_CURRENT.md`: handoff compartido de cambios recientes y puntos de inspección; informativo, no autoridad ni estado machine-readable.

## Trabajo

ChatGPT/VAEP y Chat B ejecutan REVIEW_FIRST, QA, integración, correcciones, CI, certificación, rollup y failover dentro de `Desarrollo`. Jules J1/J2/J3/J4/J5/J6 implementan scopes exclusivos y entregan patch/artifact. Chat B no es una lane Jules y no puede omitir ningún gate del MAESTRO. AntiG queda `RESERVED_INACTIVE`: scheduler deshabilitado, handoff processing deshabilitado y sin autoridad LISTO_REAL. Codex no participa salvo orden explícita futura del usuario, pero debe leer el handoff antes de reincorporarse para no repetir trabajo ni usar estado stale.

## Continuidad compartida

Antes de retomar trabajo después de una desconexión o cambio de agente: leer `docs/VAEP_HANDOFF_CURRENT.md`, verificar el `CURRENT_PARENT` real en `vaep/control/jules-autorefill-catalog.json` y reconciliarlo contra COLA/PLAN_MAESTRO. Si difieren, corregir estado stale antes de implementar.

## Cambio de reglas

Una modificación de política se hace sobre `docs/VAEP_AUTHORITY.md` y los consumidores siguen leyendo la misma ruta. No se crea una autoridad nueva ni una copia numerada.
