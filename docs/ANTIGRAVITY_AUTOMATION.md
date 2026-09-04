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

El worker exige repo jmejia31/VariApp, rama Desarrollo, working tree limpio, HEAD==origin/Desarrollo, artifact causal único descargado desde el workflow run registrado por Jules, identidad causal dispatch/result/patch, task/attempt/base/scope válidos, salida AntiG conforme al schema, P0=0 y P1=0 para READY_FOR_VAEP, cero scope leak, rutas protegidas fuera de alcance, git diff --check y remoto sin cambios antes de publicar.

AntiG headless no recibe permiso para commit/push/merge/rebase/reset/checkout/switch. Cada revisión ocurre en un Git worktree temporal y aislado creado desde el exact-head inicial; el checkout primario no se usa como superficie de edición ni se restaura globalmente. El wrapper publica únicamente los paths exactos autorizados mediante staging explícito; git add --all está prohibido. Un push non-fast-forward falla cerrado. No existe force-push ni rebase automático.

El instalador agrega únicamente permisos finos necesarios para lectura/aplicación de patch y validaciones. Si la configuración global contiene ask=command(*), la activación se bloquea en vez de degradar la seguridad. Nunca se usa bypass global de permisos. La escritura de settings, watermark y Scheduled Task se trata como una instalación transaccional: si la fase mutante falla, el instalador restaura settings/estado y elimina la tarea creada.

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

## Hardening P1 de activación

Antes de habilitar la tarea local, el worker aplica estas garantías adicionales:

- aislamiento por worktree temporal para preservar cualquier trabajo concurrente del checkout primario;
- staging por lista exacta de archivos autorizados, nunca `git add --all`;
- rutas protegidas para gobierno, workflows, agente AntiG, schemas, secretos y superficies productivas;
- validación causal de Issue -> workflow run -> artifact único -> dispatch -> result -> gitpatch -> changes.patch;
- compatibilidad explícita con manifests Jules v3.25 y validación estricta cuando llega el contrato estructurado v1.0;
- base Jules obligatoriamente ancestro de Desarrollo y `git apply --check` antes de entregar el patch al reviewer;
- handoffs inválidos se guardan en `.git/vaep-antig/quarantine/`, avanzan el watermark y no bloquean Issues posteriores;
- fallos transitorios de red/CLI/remoto no se cuarentenan automáticamente y permanecen fail-closed para reintento seguro;
- self-test funcional cubre contrato, rutas protegidas e aislamiento real de worktree/concurrencia.

## Cierre P1 adicional — contrato causal y transacciones

El hardening posterior añade comparación byte-equivalente (con normalización exclusiva CRLF/LF) entre `gitpatch.json.unidiffPatch` y `changes.patch`, además de la verificación de `baseCommitId`, identidad y ancestry. El worker detecta cambios staged, unstaged y untracked, y exige igualdad exacta de paths declarados, autorizados y staged.

Los errores de handoff estructural se clasifican como `QUARANTINE` y avanzan el watermark; los fallos de transporte GitHub/CLI/red permanecen retryables y no avanzan el watermark. Tras confirmar `push`, se persiste `COMMENT_PENDING` con el `evidenceHead` confirmado antes de intentar el comentario, evitando republicaciones.

El instalador captura el XML de una tarea previa. Si la tarea no existía, el rollback elimina la tarea nueva; si existía, restaura su XML original. El self-test de transacción ejercita ambos planes sin crear una tarea real. Las rutas `frontend/vercel.json` y `frontend/scripts/vercel-ignore-build.mjs` están protegidas explícitamente.
