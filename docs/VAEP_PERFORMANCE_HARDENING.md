# VAEP/Jules performance hardening

## Estado y límites

Este diseño conserva los gates lógicos A-H, `PARENT_CLOSE_FIRST` y la
autoridad del Closure Governor. `LISTO_REAL` nunca es escrito por un workflow
individual ni por el sincronizador de evidencia. El modo predeterminado es
`VAEP_EXECUTION_MODE=legacy`; `bundled` requiere pilotaje y revisión del
controller.

## Admission y stale heads

Los manifests nuevos usan `vaep/schemas/jules-dispatch.schema.json` y se
validan con `scripts/vaep/dispatch-preflight.mjs`. Un manifest inválido,
dependencia no satisfecha, ownership en conflicto, scope protegido, base no
existente/no ancestral o duplicado se rechaza antes de iniciar ATTEMPT. Una
base stale que sigue siendo ancestro y no tiene cambios que solapen el scope
es `REFRESHABLE`, sin consumir ATTEMPT; un stale con conflicto material es
`FAIL_CLOSED`.

## Evidencia y consolidación

Cada worker escribe solo `vaep/evidence/fragments/<task>/<dispatch>.json`,
según `evidence-fragment.schema.json`. `aggregate-evidence.mjs` valida,
ordena, detecta duplicados y colisiones y soporta `--check`, `--dry-run` y
`--apply`. La aplicación escribe temporalmente y renombra en la misma carpeta;
un digest repetido es idempotente. El resultado es evidencia para revisión,
no una promoción automática.

## Bundles y fallback

El planificador conserva A-H y ofrece tres unidades operativas: `CORE` (A-D),
`UI_RBAC` (E-F) y `E2E_CERT` (G-H). Un gate fallido retiene los posteriores
dentro del bundle. `legacy` mantiene ejecución gate-by-gate y es el rollback
operativo seguro.

## GitHub → BITÁCORA y reconciliación

`sync-bitacora.mjs` acepta un payload estructurado, usa URL/token/HMAC por
variables de entorno, timeout de 10 s, tres intentos e idempotency key
`taskId+commitSha+workflow/runId`. Sin configuración devuelve `SKIPPED` sin
fallar la CI funcional y nunca imprime secretos. `reconcile-status.mjs` solo
produce `ELIGIBLE_FOR_CONTROLLER_REVIEW` cuando exact-head, gates, P0/P1,
dependencias, documentación y ownership son válidos; `autoPromote=false`.

## Métricas y piloto

`metrics.mjs` agrega eventos observables de dispatches, sesiones, attempts,
rechazos, refreshes, conflictos, CI y bundles. `vaep/metrics/baseline.json`
declara honestamente que aún no existe una medición histórica comparable.
El workflow `VAEP engine lightweight checks` ejecuta schema, syntax y
self-tests solo para paths VAEP. El piloto posterior debe ser autorizado por
VAEP, conservar A-H visibles y volver a `legacy` ante cualquier fallo causal.

No se modifica producción, Vercel, secretos reales, `main` ni workflows
históricos N0.2-N0.5.
