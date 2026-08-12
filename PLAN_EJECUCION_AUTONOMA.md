# PLAN DE EJECUCIÓN AUTÓNOMA — VAEP v2

> VariApp Autonomous Execution Protocol. Fuente rectora: **Plan Maestro ERP V5 — VariApp**. Fuente operativa: Google Sheets. Autoridad técnica y evidencia: GitHub `jmejia31/VariApp`, rama `Desarrollo`.

## 1. Identidad y fuentes obligatorias

- `PROJECT_ID`: `VARIAPP`
- Repositorio: `jmejia31/VariApp`
- Rama única: `Desarrollo`
- PR oficial: `#2 Desarrollo -> main`, siempre abierto y Draft hasta autorización expresa de Javier Mejía.
- Plan rector en Drive: https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit
- Tablero VAEP v2: https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit
- GitHub prevalece para código, commits, CI, arquitectura y evidencia verificable.

El `.docx` original fue convertido a Google Docs para que el runner pueda consultar permanentemente la fuente rectora sin depender de una conversación concreta.

## 2. Cobertura integral del Plan Maestro ERP V5

VAEP v2 incorpora todo el roadmap ERP Core definido por el plan:

1. ERP-N0 — Saneamiento y retiro legacy.
2. ERP-N1 — Inventario empresarial.
3. ERP-N2 — Compras empresariales.
4. ERP-N3 — Ventas empresariales.
5. ERP-N4 — Tesorería, CxC, CxP y Contabilidad.
6. ERP-N5 — Reportería, BI y analítica.
7. ERP-N6 — Multiempresa y SaaS.
8. ERP-N7 — Integraciones empresariales.
9. ERP-N8 — Production Readiness.
10. ERP-N9 — Go-live y Hypercare.

Los tracks `T0`–`T12` son obligatorios y transversales. Las funcionalidades futuras no obligatorias para el Core —RRHH, CRM, MRP, activos fijos, proyectos, servicio técnico, logística avanzada y ecommerce futuro— están registradas en `PLAN_MAESTRO`, pero con `NO_AUTORIZADO`; no pueden autoejecutarse sin autorización explícita de Javier.

El tablero contiene:

- `DASHBOARD`: estado resumido.
- `COLA`: microtareas ejecutables y dependencias.
- `PLAN_MAESTRO`: puntos padre ERP-N0→N9, gates, T0–T12 y backlog futuro.
- `CONFIG`: invariantes operativas.
- `BITACORA`: transiciones y evidencia.
- `LEYENDA`: estados y significado.

Baseline VAEP v2: **778 microtareas** y **131 filas de plan/gobierno**, con el Plan Maestro completo representado.

## 3. Granularidad obligatoria

Ningún agente debe intentar resolver un punto ERP grande en un único changeset.

Salvo que un punto tenga una descomposición específica, cada punto funcional se divide en microtareas pequeñas:

1. `PRE`: auditoría/preflight, alcance, riesgos, dependencias, rollback y criterios.
2. `DOMAIN`: dominio, invariantes y contratos.
3. `DB_MIG`: persistencia, constraints, índices, migración/backfill/reconciliación/rollback cuando aplique.
4. `BACKEND_API`: aplicación, servicios, repositorios, DTOs y API.
5. `FRONTEND_UX`: UI/UX, formularios, tablas, responsive, accesibilidad y permisos UI.
6. `SEC_AUDIT`: RBAC, auditoría, seguridad y observabilidad.
7. `TEST_CI`: unit/integration/contract/E2E/security/migration/performance tests y CI aplicable.
8. `DOC_CERT`: documentación, evidencia, checkpoint, regresión y cierre del punto.

Si una microtarea sigue siendo demasiado grande, **debe subdividirse antes de editar**. El criterio operativo es un solo concern coherente y verificable por microtarea; no se permite convertir una fila en un refactor transversal gigante.

Si una etapa no aplica técnicamente al punto, se marca `LISTO` como `N/A` solo después de dejar evidencia suficiente; no se crea código artificial para “cumplir” una columna.

## 4. Máquina de estados

```text
PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO
                     \-> BLOQUEADO
```

`CANCELADO` requiere instrucción explícita de Javier o evidencia inequívoca de que el punto dejó de aplicar.

Está prohibido `PENDIENTE -> LISTO` sin ejecución/reconciliación y evidencia verificable.

## 5. Selección de tareas

En cada corrida:

1. confirmar `PROJECT_ID=VARIAPP`, repo y rama;
2. leer `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md`, última entrada relevante de `CHANGELOG_AI.md` y este archivo;
3. consultar `CONFIG`, `COLA` y `BITACORA`;
4. reconciliar estados con el HEAD real de GitHub;
5. ordenar `PENDIENTE` por `PRIORIDAD` ascendente y orden de fila;
6. elegir solamente tareas con todas sus dependencias directas/transitivas resueltas;
7. respetar lock lógico de `EN_PROGRESO`/`VALIDANDO` + agente;
8. marcar `EN_PROGRESO` antes de implementar;
9. marcar `VALIDANDO` antes de las comprobaciones finales;
10. publicar exclusivamente en `Desarrollo`, actualizar evidencia y marcar `LISTO` solo si cumple.

El runner puede completar **hasta 3 microtareas pequeñas por corrida**. Debe detenerse antes si el siguiente punto aumenta el riesgo, requiere una decisión humana o convertiría la corrida en un cambio demasiado grande.

## 6. Regla crítica de bloqueo y continuidad

Una tarea `BLOQUEADO` **no detiene toda la cola**.

Después de registrar causa y evidencia, el runner busca la siguiente `PENDIENTE` elegible. Puede continuar únicamente cuando esa tarea **no dependa directa ni transitivamente** de ninguna tarea bloqueada.

Ejemplo:

```text
A = BLOQUEADO
B depende de A  -> NO elegible
C depende de B  -> NO elegible
D independiente -> SÍ elegible
```

Una tarea bloqueada no se reintenta en bucle durante la misma corrida.

## 7. Gates de fase y orden estricto

VAEP crea `GATE-N0` ... `GATE-N9`.

El orden rector es:

```text
GATE-N0 -> ERP-N1 -> GATE-N1 -> ERP-N2 -> GATE-N2 -> ERP-N3 ->
GATE-N3 -> ERP-N4 -> GATE-N4 -> ERP-N5 -> GATE-N5 -> ERP-N6 ->
GATE-N6 -> ERP-N7 -> GATE-N7 -> ERP-N8 -> GATE-N8 -> ERP-N9 -> GATE-N9
```

Los gates aplican Definition of Done global: backend/frontend, migraciones, tests, E2E relevantes, seguridad, permisos, auditoría, backfill/reconciliación, rollback documentado, documentación, evidencia y cero P0/P1 abiertos. Una fase no se cierra solamente porque compile.

## 8. Estado inicial especializado de ERP-N0.5 — MetodoPago

El checklist aportado por Javier se cargó explícitamente en `COLA`:

| ID | Punto | Estado inicial |
|---|---|---|
| N0.5.01 | Análisis y diagnóstico inicial | LISTO |
| N0.5.02 | Diseño funcional de MetodoPago | LISTO |
| N0.5.03 | Auditoría legacy de MetodoPago | LISTO |
| N0.5.04 | Entidad y persistencia relacional MetodoPago | LISTO |
| N0.5.05 | Seed + preflight + backfill histórico | LISTO |
| N0.5.06 | Eliminar doble autoridad enum/string | PENDIENTE |
| N0.5.07 | Reglas operativas de métodos de pago | PENDIENTE |
| N0.5.08 | Backend/API/CRUD/DTOs | PENDIENTE |
| N0.5.09 | Frontend administrable/selectores dinámicos | PENDIENTE |
| N0.5.10 | RBAC + auditoría | PENDIENTE |
| N0.5.11 | Reportes, facturas y PDFs | PENDIENTE |
| N0.5.12 | Tests de regresión N0.5 | PENDIENTE |
| N0.5.13 | Workflow CI dedicado N0.5 | PENDIENTE / RECONCILIAR |
| N0.5.14 | Recertificación integral M13 | PENDIENTE |
| N0.5.15 | Documentación formal y cierre | PENDIENTE |

Para `N0.5.13`, GitHub ya contiene evidencia histórica de workflow/run N0.5. El runner debe **reconciliar antes de implementar** y cerrar la fila si el criterio actual ya está satisfecho; está prohibido duplicar workflows por confiar ciegamente en un checklist desactualizado.

## 9. Concurrencia y publicación

- Una tarea `EN_PROGRESO`/`VALIDANDO` con `AGENTE` se considera tomada.
- Antes de publicar, volver a confirmar HEAD remoto.
- Preservar commits de Codex, AntiG y otros agentes.
- Nunca force-push.
- Si el conflicto no puede resolverse de forma dirigida y segura: `BLOQUEADO` + evidencia, luego buscar tarea independiente.

## 10. Evidencia obligatoria

Cada changeset intencional debe actualizar `CHANGELOG_AI.md`. `TASKS.md` cambia cuando cambia estado/bloqueo/pendiente. Contexto/índice/arquitectura solo si cambia la realidad que documentan.

Para marcar `LISTO`, según aplique deben existir commit SHA, validaciones reales, fila actualizada en `COLA` y transición registrada en `BITACORA`. Nunca inventar pruebas, CI, despliegues ni estados externos.

## 11. Seguridad y Producción

VAEP no autoriza tocar `main`, fusionar PR #2, habilitar auto-merge, crear ramas nuevas, modificar Producción, secretos, variables, credenciales, bases, dominios, servicios, activos o ejecutar migraciones productivas.

ERP-N9.4 y cualquier operación productiva permanecerán bloqueadas hasta autorización expresa de Javier, aunque la fila exista en el Plan Maestro.

## 12. Qué debe hacer Javier

Para trabajo ya incluido en ERP V5: **nada**. No necesita escribir “continúa con el siguiente punto”.

Para añadir una mejora nueva fuera del Plan Maestro, puede agregarla a `COLA` o pedir a ChatGPT incorporarla. Debe indicar al menos objetivo/criterio; VAEP completará proyecto/repo/rama/dependencias cuando pueda inferirse con evidencia.

El archivo `PLAN_EJECUCION_AUTONOMA.md` es protocolo versionado; el Sheet es el tablero vivo. Javier no necesita editar este Markdown para que el runner continúe.
