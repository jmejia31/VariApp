# N8.18.A — PRE / plan de refactor y limpieza certificada

Estado de trabajo: `REVIEW_FIRST / PRE`.

Base causal: N8.15–N8.17 `LISTO_REAL`. Este PRE define orden, scopes, rollback y prohibiciones; no ejecuta todavía refactor destructivo ni DDL.

## Principios hard

1. Ordenar cambios por dependencia: contrato/gobierno → ownership/naming → backend → frontend → seguridad → tests → DOC_CERT.
2. Un changeset por intención material y scope no solapado; cada changeset debe poder revertirse sin tocar otros.
3. `UNKNOWN` prohíbe eliminar. `REMOVE_SAFE` exige traza completa de referencias, rutas/imports, DI/API, tests, runtime/config y persistencia/histórico.
4. DDL destructivo y limpieza física DB están prohibidos en N8.18; cualquier candidato queda encadenado a N8.21 tras backup+restore certificado.
5. No romper los 49 contratos N8.17 `SPEC_COMPLETE`; toda reorganización preserva backend authority, RBAC/tenant, idempotencia, auditoría y fail-closed.
6. No tocar `main`, Producción, deploy, secretos ni PR #2.

## Inputs certificados

- N8.15.H: 158 anchors arquitectónicos, todos conservados; `CONSOLIDATE=3`, `DEPRECATE=0`, `REMOVE_SAFE=0`.
- N8.16.H: 49 contract roots con identidad canónica y lifecycle gobernado.
- N8.17.H: 49/49 contract roots al menos `SPEC_COMPLETE`, sin universal `CERTIFIED` inflado.
- Arquitectura canónica: `DOMINIO/PADRE → SUBMÓDULO/HIJO → INTERFAZ/ACCIÓN`; menú refleja catálogo, no lo define.

## Clasificación de candidatos

| ID | Candidato | Clase PRE | Decisión N8.18.A | Etapa causal |
|---|---|---|---|---|
| `ARCH-01` | Taxonomía/ownership entre `catalogos-producto` y features específicas de catálogo | `CONSOLIDATE` | Mantener funcionalidad; unificar ownership/naming sin borrar rutas/contratos | B/E |
| `ARCH-02` | Dos roots históricos EF migrations | `KEEP` + candidato de consolidación documental solamente | No mover/borrar migraciones en N8.18; cualquier limpieza física queda `REMOVE_SAFE_DB_AFTER_BACKUP` y va a N8.21 | C/H → N8.21 |
| `ARCH-03` | Layout mixto de Application (`Bancos/`, `Services/`, `DTOs/`, `Interfaces/`) | `CONSOLIDATE` | Reubicar sólo cuando ownership por caso de uso esté demostrado y referencias/tests permanezcan verdes | B/D |
| `ARCH-04` | Alertas/confirmaciones repetidas por feature | `CONSOLIDATE` condicionado | Consolidar hacia primitive global sólo tras inventario dirigido; diálogos complejos conservan contrato propio | E |
| `ARCH-05` | Toast/snackbar disperso | `CONSOLIDATE` condicionado | Canalizar por servicio compartido sin perder semántica/error context | E |
| `ARCH-06` | Assets supuestamente dead/duplicados no demostrados | `UNKNOWN` | Prohibido borrar hasta completar trace estático+runtime+tests+config+DB/histórico | B–G |

`DEPRECATE=0` y `REMOVE_SAFE=0` al entrar en N8.18.A. Ningún asset cambia de clase por similitud nominal o ausencia del menú.

## Plan ordenado por changeset

### CS-01 — límites y ownership de dominio

**Scope:** documentación/catálogo de ownership y, sólo donde sea necesario, nombres/ubicación de código no destructivos.

- Mapear cada cambio a uno de los 9 bounded/domain areas N8.15 y a los 49 MATRIX_ID N8.17 afectados.
- Resolver `ARCH-01` y preparar `ARCH-03` sin mezclar backend/frontend en el mismo diff.
- Detectar cycles/aliases ambiguos y detener el changeset si la nueva frontera requiere duplicar regla crítica.

**Rollback:** `git revert <commit-CS-01>`; no schema change. Reejecutar gobierno de matrices y tests dirigidos del dominio afectado.

### CS-02 — integridad/modelo DB no destructivo

**Scope:** sólo constraints/índices/FKs/model mapping que sean aditivos, reversibles y respaldados por evidencia.

- `ARCH-02` permanece KEEP.
- No borrar tablas/columnas/migraciones; no ejecutar Down destructivo.
- Candidato destructivo, si aparece: registrar `REMOVE_SAFE_DB_AFTER_BACKUP` con evidencia y diferir a N8.21.

**Rollback:** revertir cambios de código/model mapping; para DDL aditivo usar forward-fix/reversión segura documentada. Nunca rollback destructivo sobre DB activa.

### CS-03 — backend por caso de uso

**Scope:** `backend/src/Application`, `Domain`, `Infrastructure`, `API` por dominio afectado.

- Consolidar servicios/endpoints sólo con duplicidad lógica demostrada.
- Mantener backend como autoridad de authz, tenant, invariantes, estados, totales y persistencia.
- Compatibilidad de API preservada o migrada explícitamente con tests.

**Rollback:** revert commit del dominio; restaurar wiring/DI previo; gate backend dirigido + contratos N8.17.

### CS-04 — frontend padre→hijo y primitives compartidas

**Scope:** `frontend/src/app/features`, `app.routes.ts`, shell/navigation y primitives compartidas.

- Menú/routes/features siguen el catálogo padre→hijo.
- Resolver `ARCH-04/05` sólo después de prueba de equivalencia de semántica.
- No usar guards/UI como autoridad backend.
- Retiro de componente sólo con `REMOVE_SAFE` demostrado.

**Rollback:** revert commit UI; lint/build/E2E dirigido; verificar rutas y permisos visibles sin alterar backend authority.

### CS-05 — hardening post-refactor

**Scope:** RBAC, tenant, auditoría, PII/secrets/logging, no-bypass.

- Revalidar fronteras modificadas por CS-01–04.
- Cualquier P0/P1 se corrige same-run antes de avanzar.

**Rollback:** revertir únicamente hardening si rompe contrato válido; nunca restaurar un bypass conocido.

### CS-06 — gates integrales y certificación

**Scope:** build/lint/unit/integration/E2E/security/responsive/accessibility, rutas/menú y documentación.

- P0=0/P1=0 obligatorio.
- Actualizar estados de matriz sólo según evidencia real: `IMPLEMENTATION_REVIEWED` o `CERTIFIED` cuando corresponda.
- Preservar historia y marcar documentación superseded sin borrarla.

**Rollback:** no cambios productivos adicionales; si un gate falla, volver al último changeset verde y corregir causalmente.

## Orden de ejecución y dependencias

`CS-01 -> CS-02 -> CS-03 -> CS-04 -> CS-05 -> CS-06`

- B DOMAIN consume CS-01.
- C DB_MIG consume CS-02 y no puede promover eliminación DB.
- D BACKEND_API consume CS-03.
- E FRONTEND_UX consume CS-04.
- F SEC_AUDIT consume CS-05.
- G TEST_CI consume CS-06 gates.
- H DOC_CERT publica estado real y encadena pendientes DB a N8.21.

No hay scopes simultáneos de escritura entre changesets. Cada etapa relee HEAD y contratos antes de aplicar su diff.

## Stop conditions

Detener el changeset concreto, sin bloquear trabajo independiente, si aparece cualquiera de:

- ownership ambiguo o dependencia circular no resuelta;
- contrato N8.17 afectado sin mapping explícito;
- necesidad de DDL destructivo antes de N8.21;
- asset candidato a eliminación que permanezca `UNKNOWN`;
- pérdida de authz/tenant/auditoría/fail-closed;
- P0/P1 no corregido;
- gate causal rojo por el diff.

## Fresh HEAD y baseline de ejecución

- Base de lease N8.18.A: `e05c4c557ebcb09091eba57291f05faa2b6e37b0`.
- El plan se publica como evidencia PRE; el HEAD candidato del propio plan se valida por gate documental antes del receipt.
- Cada changeset B–H debe registrar su propio fresh HEAD y no reutilizar este SHA como si siguiera vigente.

## REVIEW_FIRST candidato

- P0: `0`.
- P1: `0`.
- P2: `0`.
- Scopes solapados: `0`.
- Eliminaciones especulativas autorizadas: `0`.
- DDL destructivo autorizado: `0`.
- Candidatos clasificados: `6/6`.
- Rollback definido: `6/6 changesets`.

## Resultado candidato

N8.18.A cumple PRE cuando este plan pasa gate documental exact-head y se persisten REVIEW_FIRST + receipt/readback. Siguiente dependency-valid: `N8.18.B — DOMAIN`.
