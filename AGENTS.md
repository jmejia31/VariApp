# Reglas obligatorias de colaboración — VariApp

Este archivo es vinculante para Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y cualquier agente autorizado.

## 0. Gate obligatorio de identidad

Antes de analizar, editar, ejecutar o publicar:

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Con acceso local, Javier/Codex/AntiG ejecutan:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\iniciar-sesion-ia.ps1
```

Con acceso remoto, ChatGPT confirma repositorio, `Desarrollo`, HEAD actual, `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md` y commits relevantes desde el último handoff.

Lectura mínima por sesión: `AGENTS.md` -> `PROJECT_CONTEXT.md` -> `TASKS.md` -> última entrada relevante de `CHANGELOG_AI.md`. `PROJECT_INDEX.md` solo para localizar; `ARCHITECTURE.md` solo para cambios estructurales. En ejecución VAEP también leer `PLAN_EJECUCION_AUTONOMA.md`, `CONFIG/COLA/BITACORA` y la fuente rectora.

Una sesión pertenece a un solo proyecto. Contexto, rutas, ramas, credenciales, bases, reglas o planes de otro proyecto son no confiables para VariApp. Si memoria y repo discrepan, prevalece la evidencia actual del repositorio.

## 1. Fuentes canónicas

- Contexto técnico: `PROJECT_CONTEXT.md`.
- Pendientes resumidos: `TASKS.md`.
- Evidencia colaborativa: `CHANGELOG_AI.md`.
- Protocolo autónomo: `PLAN_EJECUCION_AUTONOMA.md`.
- Plan rector: `Plan Maestro ERP V5 — VariApp`.
- Plan rector en Drive: https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit
- Tablero VAEP v2: https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

La realidad actual se determina por identidad + HEAD + contexto + tareas + changelog + archivos afectados. No reconstruir el proyecto desde cero.

## 2. Equipo y acceso

- Javier: propietario, prioridades, aceptación y autorizaciones finales.
- Codex: implementación/pruebas desde checkout local autorizado.
- AntiG/Antigravity: implementación/pruebas desde checkout local autorizado.
- ChatGPT: arquitectura, auditoría, coordinación, VAEP y cambios remotos mediante conectores autorizados.

Solo Javier/Codex/AntiG se consideran con acceso local. ChatGPT no debe afirmar que modificó/sincronizó la PC por tener acceso GitHub/Drive.

Drive es tablero operativo; GitHub es autoridad técnica y de evidencia. Nunca ejecutar filas cuyo `PROJECT_ID`, repo o rama no coincidan exactamente.

## 3. Git — reglas inviolables

- `Desarrollo` es la única rama de trabajo.
- `main` está congelada.
- No crear ramas adicionales sin autorización explícita.
- No fusionar PR #2 ni habilitar auto-merge.
- PR #2 permanece Draft.
- Publicar cambios autorizados directamente en `origin/Desarrollo`.
- Preservar trabajo ajeno; nunca force-push, `reset --hard` ni descartes destructivos sin autorización.

Con acceso local, después del gate:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

No repetir estos comandos compulsivamente; reconfirmar antes de publicar cuando exista riesgo real de concurrencia.

## 4. Producción congelada

Entornos lógicos:

```text
varistorehn_producción
varistorehn_desarrollo
```

Está prohibido modificar/eliminar recursos productivos: `main`, variables, secretos, credenciales, dominios, certificados, bases, datos, servicios, despliegues, respaldos, migraciones, activos o configuraciones. No ejecutar migraciones productivas sin autorización expresa.

## 5. Rendimiento y consumo — obligatorio

1. Reutilizar `PROJECT_CONTEXT.md`; no rehacer inventarios en cada prompt.
2. No recorrer todo el repo salvo cambio estructural real o petición expresa.
3. Revisar primero solo archivos objetivo y dependencias directas.
4. **No releer archivos ya documentados a menos que hayan cambiado.** Verificar por diff/SHA/historial.
5. Buscar por símbolo/ruta antes de listados recursivos.
6. Leer rangos relevantes de archivos grandes.
7. Elegir la solución suficiente de menor impacto.
8. No explorar módulos no relacionados tras obtener evidencia suficiente.
9. Agrupar comandos/validaciones cuando sea seguro.
10. No crear scripts/artefactos temporales sin necesidad técnica real.
11. Terminar al cumplir objetivo y validaciones.
12. Si una ambigüedad no se resuelve con inspección dirigida, bloquear/pedir aclaración; no indexar todo el proyecto.
13. Tras movimiento remoto, revisar primero nombres de commits/archivos y abrir solo lo afectado.

`[skip ci]` solo puede usarse cuando todos los cambios sean documentación o infraestructura local de colaboración y no se modifique app, workflows, dependencias, migraciones, entorno o despliegue. Nunca usarlo para esconder validaciones funcionales.

## 6. Reconexión/compactación

Confirmar identidad, leer contexto/tareas, revisar solo 1–3 commits si hubo movimiento, revisar diff de tarea y continuar desde el último punto verificable. En VAEP, reconciliar Sheet con GitHub. Una reconexión no justifica repetir arquitectura completa.

Renovar mapa arquitectónico solo ante nueva capa/módulo ERP mayor, cambio estructural de persistencia/framework, rediseño auth/RBAC o cambio transversal de tenancy/integración/deploy/observabilidad.

## 7. Evidencia de cada changeset

Todo changeset intencional:

1. actualiza `CHANGELOG_AI.md`;
2. actualiza `TASKS.md` si cambió estado/bloqueo/pendiente;
3. actualiza contexto/índice/arquitectura solo si cambió lo que documentan;
4. actualiza gobierno solo si cambiaron reglas/accesos;
5. usa commit descriptivo con agente cuando aplique;
6. entrega SHA y validaciones reales;
7. nunca inventa pruebas, CI o estados externos.

## 8. Validación proporcional

- Docs/gobierno: diff y consistencia; sin builds inútiles.
- Backend localizado: build/tests dirigidos; ampliar si seguridad/datos/migraciones/cierre.
- Frontend localizado: lint/build/tests dirigidos; E2E en auth/permisos/navegación/facturación/flujos críticos.
- Gates ERP: DoD global, migraciones, seguridad, permisos, auditoría, QA, documentación y regresión aplicable.

## 9. VAEP v2 — ejecución autónoma

La especificación completa vive en `PLAN_EJECUCION_AUTONOMA.md`.

VAEP v2 representa el Plan Maestro ERP V5 completo en `PLAN_MAESTRO` y mantiene microtareas ejecutables en `COLA`. Los tracks T0–T12 son obligatorios. Las funciones futuras no-core están registradas, pero `NO_AUTORIZADO` y no son autoejecutables.

Máquina de estados:

```text
PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO
                     \-> BLOQUEADO
```

Selección:

- menor `PRIORIDAD` elegible;
- todas las dependencias directas/transitivas deben estar resueltas;
- `EN_PROGRESO`/`VALIDANDO` + agente actúa como lock;
- revalidar HEAD antes de publicar.

### Granularidad

Cada punto se divide en microtareas `PRE`, `DOMAIN`, `DB_MIG`, `BACKEND_API`, `FRONTEND_UX`, `SEC_AUDIT`, `TEST_CI`, `DOC_CERT`, salvo descomposición especializada. Si una microtarea todavía es grande, subdividirla antes de editar.

El runner puede completar hasta **3 microtareas pequeñas por corrida**, pero debe detenerse antes si aumenta el riesgo o el alcance deja de ser pequeño/coherente.

### Bloqueos

Una tarea `BLOQUEADO` no paraliza toda la cola. Después de registrar causa/evidencia, continuar únicamente con otra que **NO dependa directa NI transitivamente** de la bloqueada. No reintentar el mismo bloqueo en bucle.

### Fases

`GATE-N0` ... `GATE-N9` hacen cumplir el orden N0→N1→N2→N3→N4→N5→N6→N7→N8→N9. Ninguna fase se cierra solo porque compile.

### Coordinación

Cada transición actualiza `COLA`/`BITACORA`. Cada changeset actualiza `CHANGELOG_AI.md`. Ante contradicción entre Sheet y GitHub, reconciliar usando GitHub como autoridad técnica antes de continuar.

## 10. Mejora continua

Aplicar mejoras de bajo riesgo relacionadas con la tarea; registrar mejoras transversales separadas en `TASKS.md`; evitar refactors infinitos. Medir por menos relecturas, menos comandos/CI redundantes, menor superficie y cero pérdida de trazabilidad.

## 11. Commits y handoff

Formato recomendado:

```text
<tipo>(<área>): <descripción> [agente]
```

Cada entrega indica proyecto, objetivo, área, validaciones, riesgos/pendientes y SHA. Referenciar contexto canónico en vez de repetir arquitectura completa.
