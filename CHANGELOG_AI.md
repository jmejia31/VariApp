# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-08-11 — VAEP v2: Plan Maestro ERP V5 completo + cola granular

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive.

**Objetivo:** convertir el Plan Maestro ERP V5 en una ejecución autónoma integral, granular y auditable, evitando changesets gigantes y permitiendo continuidad cuando exista un bloqueo independiente.

**Alcance:**

- importación del `Plan Maestro ERP V5 — VariApp` original a Google Docs como fuente rectora permanente;
- creación del Google Sheet `VariApp — VAEP v2 — Plan Maestro ERP V5 + Cola Granular`;
- representación completa ERP-N0→N9, gates N0→N9, tracks T0–T12 y backlog futuro no-core;
- generación de 778 microtareas ejecutables y 131 filas de plan/gobierno;
- granularización estándar `PRE`, `DOMAIN`, `DB_MIG`, `BACKEND_API`, `FRONTEND_UX`, `SEC_AUDIT`, `TEST_CI`, `DOC_CERT`;
- regla adaptativa: si una microtarea sigue siendo grande, subdividir antes de editar;
- máximo operativo de hasta 3 microtareas pequeñas por corrida;
- gates estrictos para impedir avanzar N0→N1→...→N9 sin cierre certificado;
- funciones futuras no-core registradas como `NO_AUTORIZADO` y no autoejecutables;
- checklist especializado N0.5.01–N0.5.15 cargado con N0.5.01–05 `LISTO` y N0.5.06–15 `PENDIENTE` iniciales;
- N0.5.13 marcado para reconciliación antes de duplicar workflow/CI ya existente en GitHub;
- mantenimiento de la regla solicitada: una tarea `BLOQUEADO` solo puede saltarse hacia otra sin dependencia directa ni transitiva de ella;
- actualización de `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md` y `PLAN_EJECUCION_AUTONOMA.md` para VAEP v2.

**Validación real:** el `.docx` rector fue convertido correctamente a Google Docs; el workbook VAEP v2 fue generado y verificado con 778 tareas, 5 `LISTO`, 773 `PENDIENTE`, sin errores de fórmula detectados; después se importó a Google Sheets y se leyó nuevamente `COLA!A1:U12`, confirmando estructura, estados, dependencias y URL de fuente. Antes del changeset GitHub se verificó `Desarrollo` en `6bb272548b4f13011931310db526c6c4e6826142`. No se modificó aplicación, migraciones, datos ni Producción.

**Riesgo/control:** las filas futuras son plan operativo, no evidencia de implementación. GitHub prevalece ante cualquier discrepancia. La granularización no autoriza scope creep ni operaciones productivas.

## 2026-08-11 — VAEP v1: ejecución autónoma, Drive y dependencias

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo:** permitir ejecución autónoma de puntos autorizados sin instrucción manual por tarea.

**Alcance:** `PLAN_EJECUCION_AUTONOMA.md`, Google Sheet inicial, estados estrictos, selección por prioridad/dependencias, lock lógico y bloqueo no global.

**Validación:** Sheet inicial verificado; repositorio/rama comprobados; sin tocar main/Producción.

## 2026-08-11 — Gobierno colaborativo v2

**Responsable:** ChatGPT.

Gate `PROJECT_ID=VARIAPP`, aislamiento entre proyectos, lectura mínima, evidencia obligatoria, scripts/hook locales, hardening de publicación y política limitada `[skip ci]`.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

Creación/alineación de `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `TASKS.md`, `CHANGELOG_AI.md` y reglas de continuidad.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

Migración `20260812023600_N0_5_BackfillMetodoPagoHistorico`, seed idempotente, backfill, preflight/postcheck, workflow N0.5 y acta documental. Workflow N0.5 run `31558300465` success y CI general `31558300370` verde. Enum/columnas legacy permanecen temporalmente hasta migrar consumidores posteriores.

## 2026-08-11 — Catálogo público VARISTOREHN

**Responsable:** Codex.

Consulta pública segura `GET /tienda/productos`, consumo frontend y personalización pública. Backend/lint/build dirigidos aprobados; integraciones restantes afectadas por credenciales MySQL locales según evidencia del changeset.

## Formato futuro

Cada entrada debe contener fecha, agente, objetivo, alcance, validaciones reales, riesgos/pendientes y commit cuando sea útil. No registrar secretos ni datos sensibles.
