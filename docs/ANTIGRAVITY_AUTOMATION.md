# Antigravity Automation — VariApp

## Objetivo

Integrar AntiG/Antigravity como reviewer/fixer automático entre los handoffs terminales Jules y la certificación VAEP, sin convertirlo en una segunda autoridad de cierre.

Flujo: Jules terminal COMPLETED + artifact -> Scheduled Task local -> scripts/antig/antig-review-worker.ps1 -> agy headless con variapp-reviewer -> review + aplicación segura + correcciones menores/medias + pruebas -> READY_FOR_VAEP | RETURN_TO_JULES | BLOCKED_QA_TAKEOVER -> VAEP/controller -> LISTO_REAL solo por autoridad separada.

## Componentes

- .agents/agents/variapp-reviewer/agent.md: Custom Agent especializado.
- scripts/antig/antig-review-worker.ps1: consumidor automático de resultados Jules.
- scripts/antig/install-antig-automation.ps1: activación local única.
- scripts/antig/antig-self-test.ps1: validación estática/CI.
- vaep/schemas/antig-review-result.schema.json: contrato estructurado de decisión.

## Trigger real

Los cuatro workers Jules ya crean Issues terminales con prefijos [VAEP-JULES], [VAEP-JULES-B], [VAEP-JULES-C] y [VAEP-JULES-D].

El Scheduled Task VariApp-AntiG-Reviewer ejecuta el worker cada minuto. El worker usa un mutex local y procesa como máximo un handoff nuevo por ciclo. El watermark se guarda bajo .git/vaep-antig/, por lo que no ensucia el repositorio y la instalación inicial no reprocesa Issues históricos.

## Seguridad

El worker exige repo jmejia31/VariApp, rama Desarrollo, working tree limpio, HEAD==origin/Desarrollo, artifact causal descargado desde el workflow run registrado por Jules, task/dispatch/attempt/scope válidos, salida AntiG conforme al schema, P0=0 y P1=0 para READY_FOR_VAEP, cero scope leak, git diff --check y remoto sin cambios antes de publicar.

AntiG headless no recibe permiso para commit/push/merge/rebase/reset/checkout/switch. El wrapper controla publicación y un push non-fast-forward falla cerrado. No existe force-push ni rebase automático.

El instalador agrega únicamente permisos finos necesarios para lectura/aplicación de patch y validaciones. Si la configuración global contiene ask=command(*), la activación se bloquea en vez de degradar la seguridad. Nunca se usa bypass global de permisos.

## Correcciones

- Error menor/medio e in-scope: AntiG corrige, revalida y prepara READY_FOR_VAEP.
- Error estructural en ATTEMPT=1: RETURN_TO_JULES para el único R2 permitido.
- Error estructural en ATTEMPT=2: BLOCKED_QA_TAKEOVER; R3 está prohibido.
- Stale head, evidencia inválida, scope leak, P0/P1 o dependencia material: fail-closed.

## Activación local única

Desde la PC autorizada y con el checkout limpio/sincronizado en Desarrollo:

powershell -ExecutionPolicy Bypass -File scripts\antig\install-antig-automation.ps1

Precondiciones: git, gh autenticado y Antigravity CLI agy instalado/autenticado. El instalador verifica que variapp-reviewer sea descubierto, ejecuta un probe headless, configura permisos finos, crea watermark y registra el Scheduled Task cada minuto.

Para retirar solo el Scheduled Task:

powershell -ExecutionPolicy Bypass -File scripts\antig\install-antig-automation.ps1 -Remove

## Autoridad

READY_FOR_VAEP es un candidato revisado. AntiG no escribe LISTO_REAL, no modifica COLA/BITACORA como autoridad de cierre y no toca main, Producción, secretos, Vercel ni bases productivas.
