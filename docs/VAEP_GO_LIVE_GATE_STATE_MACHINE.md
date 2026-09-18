# VAEP · GO_LIVE_GATE · State machine canónica

## Propósito

Definir la semántica inequívoca del gate de go-live sin convertir una decisión humana planificada en un bloqueo técnico y sin conceder autoridad productiva implícita.

## Fuente de verdad

- `GO_LIVE_AUTHORIZED` es la única bandera de autorización humana/productiva.
- Su valor por defecto y durante la intervención N8.24 es `false`.
- Nunca se infiere autorización a partir de readiness, pruebas verdes, despliegues DEV/UAT, receipts, fechas, notificaciones ni ausencia de defectos.
- `READY_FOR_GO_LIVE` es un estado técnico derivado. No concede autoridad para escribir o desplegar en Producción.
- `WAITING_OWNER_GO_LIVE_AUTHORIZATION` representa un gate técnicamente listo que espera una decisión explícita del propietario. No es `BLOQUEADO` y no debe reportarse como fallo técnico.

## Estados

### NOT_READY

Uno o más prerequisitos técnicos del gate todavía no están certificados. No existe autorización productiva.

### READY_FOR_GO_LIVE

Todos los prerequisitos técnicos aplicables están certificados y el readiness es calculable y auditable. `GO_LIVE_AUTHORIZED=false`. No permite ninguna escritura productiva.

### WAITING_OWNER_GO_LIVE_AUTHORIZATION

El gate está técnicamente `READY_FOR_GO_LIVE`, se emitió como máximo una notificación deduplicada para la versión vigente del gate y se espera una decisión explícita del propietario. `GO_LIVE_AUTHORIZED=false`. Este estado es una espera humana planificada, no un bloqueo técnico.

### TECHNICAL_BLOCKED

Existe un prerequisito técnico causal que falló o no puede resolverse internamente de forma segura. Sólo este tipo de condición puede justificar `BLOQUEADO`, con evidencia concreta del fallo y siguiente acción.

### AUTHORIZED_FOR_GO_LIVE

Estado futuro únicamente. Requiere una autorización explícita, auditable y vigente del propietario que cambie `GO_LIVE_AUTHORIZED=true`. Aun así, la ejecución productiva pertenece a un flujo futuro separado y explícitamente autorizado; este estado por sí solo no ejecuta cambios.

## Transiciones

1. `NOT_READY -> READY_FOR_GO_LIVE` cuando todos los prerequisitos técnicos causales aplicables pasan y el resultado queda auditable.
2. `READY_FOR_GO_LIVE -> WAITING_OWNER_GO_LIVE_AUTHORIZATION` cuando se registra la versión del gate y la notificación deduplicada correspondiente, manteniendo `GO_LIVE_AUTHORIZED=false`.
3. `WAITING_OWNER_GO_LIVE_AUTHORIZATION -> WAITING_OWNER_GO_LIVE_AUTHORIZATION` mientras no exista decisión explícita del propietario. No hay error, retry técnico ni `BLOQUEADO` por esta espera.
4. `WAITING_OWNER_GO_LIVE_AUTHORIZATION -> AUTHORIZED_FOR_GO_LIVE` sólo mediante autorización futura explícita, auditada y vigente del propietario.
5. Desde cualquier estado técnico, un fallo causal real de un prerequisito puede llevar a `TECHNICAL_BLOCKED`; al resolver y recertificar el prerequisito vuelve a evaluarse readiness desde `NOT_READY`.
6. Si cambia la versión del gate o un prerequisito material, readiness y deduplicación se recalculan; nunca se reutiliza una autorización o readiness obsoletos como verdad vigente.

## Invariantes fail-closed

- `GO_LIVE_AUTHORIZED=false` implica `production_write=false`.
- Readiness técnico nunca equivale a autorización.
- Una decisión humana pendiente nunca se representa como bloqueo técnico.
- No existe autorización client-side ni por UI.
- No existe autoautorización por CI, scheduler, receipt o automatización.
- Ninguna Task canónica puede alterar Producción durante N8.24 bajo este contrato.
- La notificación `READY_FOR_GO_LIVE` debe deduplicarse por versión estable del gate.
- Toda autorización futura debe registrar actor, timestamp, versión/evidencia y readback.

## Auditoría mínima

Para cada evaluación del gate se conservan: versión estable del gate, estado técnico, `GO_LIVE_AUTHORIZED`, prerequisitos y evidencia, timestamp, actor/automatización, dedupe key de notificación y resultado de readback.

## Estado durante N8.24

`GO_LIVE_AUTHORIZED=false` y `production_touched=false`. El objetivo de N8.24 es cerrar la semántica, cálculo, persistencia, seguridad, pruebas y certificación del gate sin ejecutar el go-live productivo.
