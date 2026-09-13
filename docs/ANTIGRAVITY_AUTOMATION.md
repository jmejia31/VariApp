# Antigravity Automation — VariApp

> [!WARNING]
> **ESTADO ACTUAL: RESERVED_INACTIVE**
>
> La única autoridad operativa es `docs/VAEP_AUTHORITY.md`. Este documento describe una capacidad técnica preservada, no un worker operativo.

```text
ANTIG_STATUS=RESERVED_INACTIVE
ANTIG_OPERATIONAL_NOW=FALSE
ANTIG_SCHEDULER=DISABLED
ANTIG_HANDOFF_PROCESSING=DISABLED
ANTIG_AUTHORITY=MASTER
ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE
ANTIG_FUTURE_REINCORPORATION=EXPLICIT_AUTHORIZATION_REQUIRED
```

## Estado operativo

AntiG no participa actualmente en el flujo Jules -> REVIEW_FIRST -> QA/CI -> certificación VAEP. No consume Issues, artifacts ni handoffs; no ejecuta reviewer externo; no aplica patches; no publica cambios; no crea comentarios de review y no puede certificar `LISTO_REAL`.

El worker `scripts/antig/antig-review-worker.ps1` está preservado como shim fail-closed: una invocación normal, `-Once` o de polling devuelve `ANTIG_NO_ACTION=RESERVED_INACTIVE` sin usar red ni procesar handoffs.

El instalador `scripts/antig/install-antig-automation.ps1` no contiene una ruta de creación de scheduler. Su modo normal falla cerrado. Solo conserva `-SelfTest` y `-Remove` para verificar el estado reservado o retirar, cuando sea posible, una tarea local heredada.

## Componentes preservados

- `.agents/agents/variapp-reviewer/agent.md`
- `docs/ANTIGRAVITY_AUTOMATION.md`
- `scripts/antig/antig-review-worker.ps1`
- `scripts/antig/antig-self-test.ps1`
- `scripts/antig/install-antig-automation.ps1`
- `vaep/schemas/antig-review-result.schema.json`

El schema se conserva como contrato técnico dormido. Su presencia no habilita runtime ni autoridad de cierre.

## Flujo vigente sin AntiG

```text
Jules A/B/C/D
  -> terminal patch/artifact
  -> ChatGPT/VAEP REVIEW_FIRST
  -> R2 único cuando corresponda o QA_TAKEOVER
  -> CI/gates aplicables
  -> VAEP Controller
  -> LISTO_REAL
```

AntiG no es requisito, gate ni dependencia para este flujo.

## Seguridad

Mientras `ANTIG_STATUS=RESERVED_INACTIVE`:

- scheduler AntiG: deshabilitado;
- procesamiento de handoffs: deshabilitado;
- publicación Git: deshabilitada;
- ejecución de reviewer externo: deshabilitada;
- promoción/certificación: deshabilitada;
- `LISTO_REAL`: exclusivamente VAEP Controller;
- `main`, Producción, Vercel, secretos, dominios, certificados y BD productiva siguen fuera de alcance.

Git conserva la implementación histórica previa para auditoría y posible recuperación futura; esa historia nunca constituye autorización de ejecución.

## Reincorporación futura

AntiG solo puede volver al flujo cuando Javier lo autorice explícitamente. Esa autorización futura debe materializarse en un changeset posterior que, como mínimo:

1. modifique el mismo `docs/VAEP_AUTHORITY.md`;
2. cambie explícitamente el estado `RESERVED_INACTIVE`;
3. reintroduzca runtime/scheduler únicamente bajo MASTER;
4. valide seguridad, causalidad, REVIEW_FIRST y ausencia de autoridad `LISTO_REAL`;
5. ejecute CI causal terminal antes de habilitar procesamiento real.

Hasta que esas condiciones ocurran, no existe una ruta de activación operativa vigente.
