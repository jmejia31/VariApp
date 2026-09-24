# PLAN DE EJECUCIÓN AUTÓNOMA — CONSUMIDOR DEL MAESTRO

> Fuente rectora funcional: Plan Maestro ERP V5. Fuente operativa: Google Sheets. Evidencia técnica: GitHub `solqaryn/Solqaryn`, rama `Desarrollo`. Reglas operativas: `docs/VAEP_AUTHORITY.md`.

## Identidad

- PROJECT_ID: `SOLQARYN`
- Repositorio: `solqaryn/Solqaryn`
- Rama: `Desarrollo`
- PR #2: OPEN + DRAFT
- AUTOMATION_AUTHORITY: `MASTER`
- MASTER_FILE: `docs/VAEP_AUTHORITY.md`
- Equipo de control/QA: `ChatGPT/VAEP + Chat B (ChatGPT Business)` con alcance full-access dentro de `Desarrollo`, sin quinta lane Jules.
- Plan rector: https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit
- Tablero: https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

## Ejecución

1. leer el MAESTRO;
2. revalidar CONFIG/COLA/PLAN_MAESTRO/BITACORA/EJECUCION_MANUAL;
3. revalidar HEAD, PR #2, CI, Issues/artifacts/sesiones;
4. seleccionar CURRENT_PARENT/CURRENT_WORK real;
5. ejecutar trabajo material seguro conforme al MAESTRO;
6. REVIEW_FIRST de terminales;
7. integrar/corregir/probar/certificar;
8. persistir estado y siguiente acción;
9. continuar mientras exista trabajo autorizado y seguro.

## Fuente única

Este documento no crea reglas alternativas. Cuando cambie una regla de automatización, se modifica `docs/VAEP_AUTHORITY.md`; no se crea otra edición numerada. Git conserva el historial.

- MAESTRO = reglas.
- CONFIG/COLA/PLAN_MAESTRO/BITACORA/EJECUCION_MANUAL = estado/control fresco.
- GitHub HEAD/CI/código/pruebas = evidencia técnica.
- Historial = evidencia, nunca autoridad.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Regla vinculante: este archivo solo puede interpretarse con contexto de SOLQARYN. Está prohibido consultar o usar skills, documentación, chats, repositorios, memorias o reglas fuera de SOLQARYN salvo autorización explícita del propietario para la fuente/alcance concreto o una entrada `ACTIVE` en la allowlist versionada. La disponibilidad técnica no equivale a permiso. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/Solqaryn`. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.


