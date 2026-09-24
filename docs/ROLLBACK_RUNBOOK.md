# ROLLBACK_RUNBOOK — VariApp Desarrollo

## Regla de autorización

Este documento define el procedimiento; no concede por sí mismo permiso para mutar servicios. Dentro de N8.23, las mutaciones reversibles DEV están autorizadas únicamente en los scopes expresamente habilitados por el propietario (`N8.23.D`, `N8.23.E`, `N8.23.G`, `N8.23.H`) y solo sobre:

- Render DEV `variapp-api-desarrollo`;
- Vercel DEV `variapp-desarrollo`.

Sigue prohibido tocar `main`, Producción, el proyecto Vercel `varistorehn`, Render `variapp-api`, datos/migraciones productivas, DNS, certificados, secretos, compras/upgrades, cambios de plan y PR #2.

## Secuencia obligatoria

Toda prueba material de rollback debe demostrar exactamente:

`CURRENT -> PREVIOUS_KNOWN_GOOD -> HEALTHY -> CURRENT -> HEALTHY`

No existe PASS si se omite una de las dos comprobaciones `HEALTHY` o si CURRENT no queda restaurado al final.

## Roles y single-writer

- `OPERADOR_DEV`: ejecuta la mutación autorizada y conserva lease exclusivo.
- `REVISOR`: ejecuta health/smoke y revisa evidencia sin iniciar una mutación paralela.
- `CLOSER`: certifica solo con REVIEW_FIRST, P0=0/P1=0 y readback.
- `OWNER`: autoriza cualquier excepción fuera del permiso vigente.

Antes de mutar, leer HEAD, deployment/service actual, previous known good, lease y estado de cola. Si existe writer físico vivo con progreso material fresco, no duplicar escritura.

## Preflight común

```bash
set -euo pipefail
test "$(git branch --show-current)" = "Desarrollo"
git remote get-url origin | grep -Eq '(^git@github.com:|^https://github.com/)solqaryn/VariApp(\.git)?$'
export FRONTEND_URL="https://variapp-desarrollo.vercel.app"
export BACKEND_URL="https://variapp-api-desarrollo.onrender.com"
curl --fail-with-body --silent --show-error --max-time 30 "$BACKEND_URL/health/ready"
curl --fail-with-body --silent --show-error --max-time 30 -L -o /dev/null "$FRONTEND_URL/"
```

Capturar antes de la mutación: CURRENT deployment id/SHA, PREVIOUS_KNOWN_GOOD id/SHA, timestamp UTC y health baseline. No copiar valores de environment variables.

## Vercel DEV

Proyecto permitido: `variapp-desarrollo`.

Ruta preferida: **Instant Rollback** hacia un deployment PREVIOUS_KNOWN_GOOD ya `READY`, sin build nuevo. Luego validar el dominio canónico:

```bash
set -euo pipefail
curl --fail-with-body --silent --show-error --max-time 30 -L \
  -o /dev/null -w 'frontend_status=%{http_code} total=%{time_total}\n' \
  "https://variapp-desarrollo.vercel.app/"
```

Para forward recovery, promover/restaurar el deployment CURRENT original ya `READY`; no crear un build sustituto si existe el deployment original. Repetir el smoke anterior y readback del alias canónico.

Si la herramienta autorizada no expone Instant Rollback/Promote, STOP: no simular el rollback con un commit, rebuild o cambio de alias manual no autorizado.

## Render DEV

Servicio permitido: `variapp-api-desarrollo`.

Usar la capacidad autorizada de rollback/redeploy del proveedor hacia PREVIOUS_KNOWN_GOOD y después recuperar CURRENT. En cada estado `HEALTHY` ejecutar:

```bash
set -euo pipefail
curl --fail-with-body --silent --show-error --max-time 30 \
  "https://variapp-api-desarrollo.onrender.com/health"
printf '\n'
curl --fail-with-body --silent --show-error --max-time 30 \
  "https://variapp-api-desarrollo.onrender.com/health/ready"
printf '\n'
```

`/health/ready` debe devolver `database=connected`. Revisar logs del deployment mediante la superficie autorizada y confirmar ausencia de restart loop/5xx material. No revelar variables o secretos durante la revisión.

Si la herramienta autorizada no expone la mutación necesaria, STOP y conservar el scope como BLOQUEADO; no construir un PASS alternativo.

## Timings obligatorios

Para cada transición registrar `start_utc`, `healthy_utc` y duración en segundos. La evidencia final debe contener al menos:

- tiempo CURRENT -> PREVIOUS_KNOWN_GOOD -> HEALTHY;
- tiempo PREVIOUS_KNOWN_GOOD -> CURRENT -> HEALTHY;
- ids/SHA de ambos deployments;
- health/smoke de ambos estados;
- readback de que CURRENT volvió a ser el destino canónico.

## STOP inmediato

- destino no es `variapp-api-desarrollo` o `variapp-desarrollo`;
- rama no es `Desarrollo`;
- previous known good no está demostrado/READY;
- se requiere build nuevo para fingir el rollback cuando existe uno previo utilizable;
- health/readiness falla;
- DB deja de conectar;
- aparecen P0/P1;
- se requiere tocar Producción, DNS, certificados, plan o secretos;
- existe writer concurrente en el mismo scope.

## Clasificación P0–P3

- **P0:** Producción tocada, pérdida/corrupción de datos, secreto expuesto, rollback ejecutado sobre destino equivocado.
- **P1:** rollback o forward recovery no completa, CURRENT no queda restaurado, readiness/health falla, previous known good no es verificable.
- **P2:** evidencia/timing incompleto sin pérdida de control material; corregir antes del cierre si afecta reproducibilidad.
- **P3:** mejora cosmética/documental.

## Condición de cierre

Solo `LISTO` cuando la secuencia completa fue materialmente ejecutada en un scope autorizado, REVIEW_FIRST termina P0=0/P1=0, gates causales pasan, CURRENT queda restaurado y healthy, existe exact-head o equivalencia demostrada, receipt persistido y write/readback del control-plane.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
```

Regla vinculante: este archivo solo puede interpretarse con contexto de SOLQARYN. Está prohibido consultar o usar skills, documentación, chats, repositorios, memorias o reglas fuera de SOLQARYN salvo autorización explícita del propietario para la fuente/alcance concreto o una entrada `ACTIVE` en la allowlist versionada. La disponibilidad técnica no equivale a permiso. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/VariApp`.


