# VAEP — Autoridad de versión operativa

Estado vigente para VariApp al 2026-08-20.

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
GLOBAL_CONTROL_PLANE=CONFIG.RUNNER_PROTOCOL_VERSION=VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION
JULES_PROTOCOL=CONFIG.JULES_PROTOCOL_VERSION=V3.19_CURRENT
```

## Precedencia

1. `CONFIG.RUNNER_PROTOCOL_VERSION` gobierna el runner/control-plane global de ChatGPT/VAEP.
2. `CONFIG.JULES_PROTOCOL_VERSION` gobierna creación de sesión, seguimiento automático, recovery, review y entrega de Jules A/B/C/D.
3. El manifest de despacho vigente define tarea, `primaryBaseHead`, `FILE_SCOPE_HINT` y criterios de aceptación.
4. `AGENTS.md` y `docs/VAEP_JULES.md` gobiernan ingeniería, seguridad y entrega.
5. HEAD/código/pruebas actuales resuelven la realidad técnica.

Cualquier referencia operativa a VAEP/Jules `v3.7`, `v3.13`, `v3.14`, `v3.16`, `v3.17` o `v3.18` es histórica y no puede desplazar `JULES_PROTOCOL=V3.19_CURRENT`.

`v3.19` no sustituye ni degrada el protocolo global v4.6: es el subprotocolo vigente de integración multi-Jules. ChatGPT/VAEP mantiene control-plane, reconciliación, publicación y certificación bajo la versión global indicada en CONFIG.

## Regla de seguimiento Jules v3.19

- A/B/C/D continúan autónomamente dentro de su tarea y scope asignado.
- Las dudas rutinarias se resuelven con este archivo, CONFIG, manifest, `AGENTS.md`, `docs/VAEP_JULES.md`, código y pruebas.
- Solo una decisión genuina de negocio/autorización humana puede dejar una sesión esperando.
- `COMPLETED` exige auto-review, observaciones/limitaciones/riesgos/recomendaciones, pruebas no ejecutadas y `ChangeSet/gitPatch` revisable con `baseCommitId`.
- No branch, PR, push, merge, deploy, main, Producción ni secretos.
- Un resultado Jules siempre entra en REVIEW-FIRST de ChatGPT/VAEP; nunca publica funcionalmente por sí solo.
- Un dispatch atómico puede contener un manifest exclusivo por cada worker A/B/C/D, siempre que el commit no contenga archivos distintos de esos manifests y cada worker conserve un scope de escritura exclusivo.
