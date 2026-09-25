# GO_LIVE_SMOKE_RUNBOOK — Solqaryn Desarrollo

## Alcance

Smoke operativo exclusivamente para `Desarrollo`. Los destinos canónicos permitidos son:

- Frontend DEV: `https://solqaryn-desarrollo.vercel.app`
- Backend DEV: `https://solqaryn-api-dev-fxx8.onrender.com`
- Readiness backend: `https://solqaryn-api-dev-fxx8.onrender.com/health/ready`

Este runbook no autoriza escribir datos productivos, usar el proyecto Vercel `varistorehn`, usar el servicio Render `solqaryn-api`, tocar `main`, PR #2, DNS, certificados o secretos.

## Roles

- `OPERADOR_DEV`: ejecuta comandos de smoke y captura evidencia.
- `REVISOR`: compara respuestas, headers, estados y artefactos sin cambiar infraestructura.
- `CLOSER`: emite certificación únicamente con REVIEW_FIRST y P0=0/P1=0.

## Gate 0 — identidad

```bash
set -euo pipefail
test "$(git branch --show-current)" = "Desarrollo"
git remote get-url origin | grep -Eq '(^git@github.com:|^https://github.com/)solqaryn/Solqaryn(\.git)?$'
export FRONTEND_URL="https://solqaryn-desarrollo.vercel.app"
export BACKEND_URL="https://solqaryn-api-dev-fxx8.onrender.com"
test "$FRONTEND_URL" = "https://solqaryn-desarrollo.vercel.app"
test "$BACKEND_URL" = "https://solqaryn-api-dev-fxx8.onrender.com"
```

STOP si cualquiera de los destinos corresponde a Producción o si la rama/remoto no coinciden. No sustituir estas constantes por un dominio recibido sin validación explícita.

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

## Gate 4 — browser smoke público y no destructivo

No usar suites que inicien sesión con credenciales por defecto durante un smoke remoto. El browser smoke permitido abre únicamente la tienda pública real de Desarrollo y no envía formularios ni mutaciones:

```bash
set -euo pipefail
cd frontend
npm ci
npx playwright install --with-deps chromium
node --input-type=module <<'NODE'
import { chromium } from '@playwright/test';
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
const response = await page.goto('https://solqaryn-desarrollo.vercel.app/varistorehn', { waitUntil: 'domcontentloaded', timeout: 45000 });
if (!response || response.status() >= 400) throw new Error(`HTTP ${response?.status() ?? 'sin respuesta'}`);
await page.locator('body').waitFor({ state: 'visible' });
const title = await page.title();
if (!title.trim()) throw new Error('Documento sin title');
console.log(JSON.stringify({ url: page.url(), status: response.status(), title }));
await browser.close();
NODE
cd ..
```

Si el browser smoke requiere autenticación o datos mutables para demostrar un criterio adicional, ese flujo debe ejecutarse en un gate separado con identidad no productiva autorizada. No inventar usuario, password, cookie ni token.

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

HEAD/equivalencia, timestamps UTC, códigos HTTP, payload sanitizado de `/health/ready`, resultados build/test/lint, resultado del browser smoke público, deployment id/SHA, REVIEW_FIRST, P0=0/P1=0, receipt y readback. Nunca guardar cookies, JWT, passwords, tokens ni valores de variables de entorno.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Este runbook solo puede ejecutarse con contexto de SOLQARYN. No consultar ni utilizar skills, documentación, chats, repositorios, memorias o reglas de otro proyecto salvo autorización explícita del propietario o allowlist `ACTIVE`. Ante duda, fail-closed. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.

