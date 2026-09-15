# Plantilla canónica — Matriz de evaluación UI

## Identidad
- MATRIX_ID:
- Módulo padre:
- Módulo hijo:
- Interfaz/contrato:
- Ruta(s):
- Componente(s):
- Tipo: `SCREEN | BUSINESS_DIALOG | EMBEDDED_INTERACTIVE | SHELL | SHARED_PRIMITIVE`
- Estado de matriz:

## Objetivo y flujo
- Propósito de negocio:
- Actor(es):
- Precondiciones:
- Entrada:
- Flujo feliz:
- Alternativas:
- Salida:
- Side effects:
- Idempotencia:

## Contratos de datos
| Campo | Fuente | Tipo | Requerido | Default | Autollenado | Calculado servidor | Visible | Editable | Permiso ver | Permiso editar | Sensible/máscara | Validación UX | Validación backend | Constraint BD | Auditoría |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

## APIs y persistencia
- Endpoints consumidos:
- DTOs/contratos:
- Servicio/caso de uso:
- Entidades/tablas:
- Relaciones/constraints/índices:
- Multiempresa/tenant isolation:
- Concurrencia/locking:

## Estados UX obligatorios
- loading:
- empty:
- error:
- forbidden:
- read-only:
- offline/retry si aplica:

## Acciones
| Acción | Visible con | Ejecutable con | Confirmación | Backend autoritativo | Resultado | Auditoría |
|---|---|---|---|---|---|---|

## Seguridad/RBAC
- Autenticación:
- Permisos:
- Fail-closed:
- Tamper tests:
- Exposición de datos:
- Cross-tenant tests:

## Responsive y accesibilidad
- Breakpoints relevantes:
- Mobile/tablet/desktop:
- Teclado/foco:
- Labels/ARIA:
- Contraste/overflow:

## Alertas/modales
- Primitive reutilizado:
- Severidad:
- Mensaje:
- Acciones:
- No crear modal de alerta específico de esta interfaz.

## Evidencia de certificación
- Unit:
- Integration/contract:
- E2E:
- Security:
- Responsive:
- Accessibility:
- CI:
- Evidencia/receipt:
- P0/P1:

## Dictamen
- Implementación coincide con matriz: `PASS | FAIL | NOT_TESTED`
- Estado final permitido: `CERTIFIED` sólo con evidencia material.
