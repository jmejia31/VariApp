# Contexto ChatGPT / VAEP — VariApp

## Autoridad operativa

La única autoridad de reglas de automatización es:

```text
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
```

ChatGPT/VAEP y Jules A/B/C/D deben leer ese MAESTRO. No deben inferir reglas vigentes desde prompts anteriores, Issues, CHANGELOG, BITACORA, artifacts ni etiquetas numéricas históricas.

## Fuentes de estado

- CONFIG/COLA/PLAN_MAESTRO/BITACORA/EJECUCION_MANUAL: estado operativo fresco.
- GitHub `Desarrollo`, PR #2, Actions, Issues, artifacts, sesiones, código y tests: evidencia técnica.
- Plan Maestro ERP V5: roadmap/DoD funcional.

## Trabajo

ChatGPT/VAEP ejecuta REVIEW_FIRST, QA, integración, correcciones, CI, certificación y rollup. Jules A/B/C/D implementan scopes exclusivos y entregan patch/artifact. AntiG queda `RESERVED_INACTIVE`: scheduler deshabilitado, handoff processing deshabilitado y sin autoridad LISTO_REAL; solo puede reincorporarse con autorización explícita futura. Codex no participa salvo orden explícita futura del usuario.

## Cambio de reglas

Una modificación de política se hace sobre `docs/VAEP_AUTHORITY.md` y los consumidores siguen leyendo la misma ruta. No se crea una autoridad nueva ni una copia numerada.
