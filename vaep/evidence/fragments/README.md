# VAEP evidence fragments

Cada worker escribe únicamente un fragmento inmutable por `taskId/dispatchId`,
validado contra `vaep/schemas/evidence-fragment.schema.json`. Los workers no
editan `CHANGELOG_AI.md` ni `TASKS.md`. El closure governor ejecuta el
agregador con revisión humana; el agregador nunca cambia estados a
`LISTO_REAL` automáticamente.

Un fragmento mínimo contiene `taskId`, `parentId`, `worker`, `dispatchId`,
`baseHead`, `resultHead`, estado, evidencia, tests, workflows, artifacts,
`p0`, `p1`, timestamp, blockers, attempt, file scope y notas.
