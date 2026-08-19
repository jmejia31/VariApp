# Certificación ERP-N2.2 — OrdenCompra

## Estado

`VALIDANDO_CI_DOCUMENTAL`.

Este documento consolida la evidencia de cierre de N2.2. El baseline funcional anterior al paquete documental es `b4d477e2de25077c459d02b479968c93c93bc910`.

## Matriz A-H

| Punto | Estado | Evidencia principal |
| --- | --- | --- |
| N2.2.A Preflight | LISTO | `73ef31c49f08c8bff9732978ffc86dbe74e0a116` + `ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md` |
| N2.2.B Dominio/contratos | LISTO | `88047cde42929c1b2dcd8faf77da1c6543a2f2a9`, fix `f17983ef49bb8f5032e6fb328564f36c02f103b9` |
| N2.2.C Persistencia/migración | LISTO | `adff03723b4336b570328179e468e8470e611b95`; M12 `#32184108722` SUCCESS MySQL 8.4 |
| N2.2.D Aplicación/API | LISTO | hasta `a5340f991b0f93438ac184afeac41cc9ed82a756`; Development/Recovery verdes |
| N2.2.E Frontend/UX | LISTO | E.1 `26a7eada...`; E.2 `9ede060d...`; E.3 `f9000061...` con cinco gates verdes |
| N2.2.F RBAC/auditoría/seguridad | LISTO | hasta `1eb26cf60a3d4e1e37f9c89b60929f432de3c1ac`; snapshot EF reconciliado |
| N2.2.G QA/regresión/CI | LISTO | G.1 `23fa5ac6...`; G.2/G.3 `b4d477e2...`; Development/Acceptance/Fase8/M10/M13 verdes |
| N2.2.H Documentación/certificación | VALIDANDO | documento canónico + runbook + ADR + contrato HTTP + este certificado; pendiente reconciliación final de CI/documentos colaborativos |

## Evidencia funcional final

Baseline: `b4d477e2de25077c459d02b479968c93c93bc910`.

- Development `#32218997006` — SUCCESS.
- Acceptance `#32218996971` — SUCCESS.
- Fase 8 `#32218996994` — SUCCESS.
- M10 `#32218996973` — SUCCESS.
- M13 `#32218996978` — SUCCESS.
- Migración N2.2.C / MySQL 8.4: M12 `#32184108722` — SUCCESS.

## Controles certificados

### Dominio

- agregado independiente de `Compra` y `SolicitudCompra`;
- lifecycle `Borrador -> PendienteAprobacion -> Aprobada` + `Cancelada`;
- documento no editable después de Borrador;
- moneda/proveedor/líneas/totales validados;
- transiciones inválidas fail-closed;
- idempotencia durable e inmutable.

### Datos

- tablas dedicadas OrdenCompra/cabecera-detalles;
- número único;
- FKs restrictivas y cascade sólo cabecera→detalle;
- guards pre/post de migración;
- `Down` bloqueado cuando existen documentos;
- MySQL 8.4 e integration tests certificados.

### API y seguridad

- `[Authorize]` global;
- permisos `Compras:Ver/Crear/Editar/Confirmar/Aprobar/Anular` según operación;
- `Idempotency-Key` obligatorio al crear;
- 401/403/404/ProblemDetails cubiertos;
- auditoría y correlation-id mediante infraestructura común.

### UX

- listado, detalle, creación/edición y lifecycle;
- loading/error/vacío;
- paginación server-side y umbral operativo;
- limpieza de filas stale ante error;
- permisos runtime y E2E comprador/aprobador/cancelación;
- accesibilidad/responsive validados por M10/Fase8.

### Frontera empresarial

Aprobar una OrdenCompra no materializa:

- recepción;
- `ExistenciaVariante`;
- Kardex;
- costeo;
- movimiento financiero/CxP;
- factura de proveedor.

La recepción empieza en N2.3.

## Documentos canónicos

- `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md`
- `docs/ERP_N2_2_ORDEN_COMPRA.md`
- `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`
- `docs/ADR_N2_2_ORDEN_COMPRA_AUTORIDAD_DOCUMENTAL.md`
- `docs/OPENAPI_N2_2_ORDEN_COMPRA.md`
- `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`

## Criterio de cierre H

Marcar `N2.2.H = LISTO` sólo cuando:

1. el HEAD documental conserve gates causales suficientes en verde;
2. `TASKS.md` refleje N2.2 A-H;
3. `CHANGELOG_AI.md` registre intención, trazabilidad, validación y handoff;
4. COLA/BITACORA/CONFIG queden reconciliados;
5. PR #2 permanezca Draft, sin merge, y `main` no cambie;
6. scope Jules permanezca excluido.

## Handoff

Con H certificado, el siguiente punto FINISH_FIRST es `N2.3.A — Recepción de mercancía — Auditoría y preflight`.

N2.3 debe diseñar recepción total/parcial/múltiple y será la autoridad del incremento de stock por mercancía realmente recibida.