# GO_LIVE_SMOKE_RUNBOOK — VariApp Desarrollo

## Alcance

Smoke operativo exclusivamente para `Desarrollo`. Los destinos canónicos permitidos son:

- Frontend DEV: `https://variapp-desarrollo.vercel.app`
- Backend DEV: `https://variapp-api-desarrollo.onrender.com`
- Readiness backend: `https://variapp-api-desarrollo.onrender.com/health/ready`

Este runbook no autoriza escribir datos productivos, usar el proyecto Vercel `varistorehn`, usar el servicio Render `variapp-api`, tocar `main`, PR #2, DNS, certificados o secretos.

## Roles

- `OPERADOR_DEV`: ejecuta comandos de smoke y captura evidencia.
- `REVISOR`: compara respuestas, headers, estados y artefactos sin cambiar infraestructura.
- `CLOSER`: emite certificación únicamente con REVIEW_FIRST y P0=0/P1=0.

## Gate 0 — identidad

```bash
set -euo pipefail
test "$(git branch --show-current)" = "Desarrollo"
git remote get-url origin | grep -Eq '(^git@github.com:|^https://github.com/)jmejia31/VariApp(\.git)?$'
export FRONTEND_URL="https://variapp-desarrollo.vercel.app"
export BACKEND_URL="https://variapp-api-desarrollo.onrender.com"
printf '%s\n' "$FRONTEND_URL" "$BACKEND_URL" | grep -Ev 'varistorehn\.vercel\.app|https://variapp-api([./]|$)' >/dev/null
```

STOP si cualquiera de los destinos corresponde a Producción o si la rama/remoto no coinciden.

## Gate 1 — smoke HTTP sin autenticación ni escritura

```bash
set -euo pipefail
curl --fail-with-body --silent --show-error --max-time 30 \
  "$BACKEND_URL/health"
printf '\n'
curl --fail-with-body --silent --show-error --max-time 30 \
  "$BACKEND_URL/health/ready"
printf '\n'
curl --fail-with-body --silent --show-error --max-time 30 \
  -o /dev/null -D - "$FRONTEND_URL/"
```

Criterios:

- `/health` responde HTTP 2xx;
- `/health/ready` responde HTTP 2xx y confirma `database=connected`;
- frontend `/` responde HTTP 2xx/3xx final satisfactoriamente;
- no se acepta 401/403/404/429/5xx como PASS de estos endpoints públicos de salud/entrada.

## Gate 2 — headers mínimos del backend

```bash
set -euo pipefail
headers="$(mktemp)"
trap 'rm -f "$headers"' EXIT
curl --fail-with-body --silent --show-error --max-time 30 \
  -D "$headers" -o /dev/null "$BACKEND_URL/health"
grep -Eiq '^x-content-type-options:[[:space:]]*nosniff' "$headers"
grep -Eiq '^x-frame-options:[[:space:]]*DENY' "$headers"
grep -Eiq '^referrer-policy:[[:space:]]*no-referrer' "$headers"
```

Si falta un header obligatorio versionado en `Program.cs`, clasificar P1 cuando implique regresión de seguridad material; no maquillar como warning.

## Gate 3 — build reproducible del código que se pretende operar

Backend:

```bash
set -euo pipefail
cd backend
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln --configuration Release --no-restore
dotnet test InventoryApp.sln --configuration Release --no-build --filter "Category!=Integration"
cd ..
```

Frontend:

```bash
set -euo pipefail
cd frontend
npm ci
npm run lint
npm run build:prod
cd ..
```

Cualquier fallo bloquea el smoke. No se sustituye un build/test real con un HTTP 200 de una versión distinta.

## Gate 4 — Playwright remoto no destructivo

El `playwright.config.ts` acepta `PLAYWRIGHT_TEST_BASE_URL`. Ejecutar únicamente suites que no requieran credenciales ni escrituras destructivas. El smoke mínimo visual/responsive permitido es:

```bash
set -euo pipefail
cd frontend
export PLAYWRIGHT_TEST_BASE_URL="https://variapp-desarrollo.vercel.app"
npx playwright install --with-deps chromium
npm run test:responsive
cd ..
```

Si la suite requiere autenticación, tenant bootstrap o datos mutables que no estén autorizados para este scope, STOP y registrar esa suite como no ejecutable en este gate; no inventar usuario/token.

## Gate 5 — correlación con versión desplegada

Registrar al menos:

```bash
set -euo pipefail
git rev-parse HEAD
git log -1 --format='%H %cI %s'
```

La certificación debe añadir el deployment id/SHA obtenido del proveedor autorizado. Si HEAD contiene solamente evidencia posterior al runtime, la equivalencia debe demostrarse explícitamente por diff y readback del deployment actual; no asumirla.

## STOP / rollback

STOP inmediato cuando:

- un destino apunta a Producción;
- readiness de DB no es 2xx;
- build, lint o pruebas fallan;
- se requiere copiar/mostrar un secreto;
- el deployment id no puede correlacionarse con HEAD/equivalencia;
- aparece un P0/P1;
- existe un writer concurrente que está modificando el mismo deployment/scope.

Este runbook no hace rollback por sí solo. Ante fallo posterior a una mutación autorizada de N8.23, aplicar `docs/ROLLBACK_RUNBOOK.md` y demostrar `PREVIOUS_KNOWN_GOOD -> HEALTHY -> CURRENT -> HEALTHY` antes de cerrar.

## Clasificación P0–P3

- **P0:** Producción tocada, secreto expuesto, corrupción/pérdida de datos, control de acceso crítico roto.
- **P1:** health/readiness rojo, versión equivocada, rollback/forward recovery no disponible, build/test crítico rojo, error 5xx reproducible en flujo crítico.
- **P2:** degradación no crítica, warning de observabilidad o evidencia incompleta que no cambia seguridad/resultado material.
- **P3:** detalle cosmético o documental sin impacto funcional/operativo.

## Evidencia mínima para LISTO

HEAD/equivalencia, timestamps UTC, códigos HTTP, payload sanitizado de `/health/ready`, resultados build/test/lint, resultado Playwright aplicable, deployment id/SHA, REVIEW_FIRST, P0=0/P1=0, receipt y readback. Nunca guardar cookies, JWT, passwords, tokens ni valores de variables de entorno.
