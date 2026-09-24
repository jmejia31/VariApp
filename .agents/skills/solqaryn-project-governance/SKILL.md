---
name: solqaryn-project-governance
description: "Gobierno tecnico exclusivo de SOLQARYN para cualquier tarea que afecte, analice, documente, pruebe, despliegue o administre solqaryn/VariApp. Usar exclusivamente dentro de SOLQARYN. Impone identidad canonica, rama Desarrollo, PROJECT_SCOPE_LOCK=STRICT, skills con prefijo solqaryn- y aislamiento fail-closed; prohibe consultar o reutilizar skills, documentos, chats, repositorios o contexto fuera de SOLQARYN salvo autorizacion explicita y versionada del propietario."
---

# SOLQARYN Project Governance

## Identidad canonica

- `PLATFORM=SOLQARYN`
- `PROJECT_ID=VARIAPP`
- `REPOSITORY=solqaryn/VariApp`
- `BRANCH=Desarrollo`
- `PROJECT_SCOPE_LOCK=STRICT`
- `EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`
- `SKILL_NAMESPACE=solqaryn-`
- Politica canonica: `docs/PROJECT_SCOPE_LOCK.md`
- Excepciones persistentes: `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`

No aplicar esta skill fuera de SOLQARYN.

## Skills permitidas

Toda skill propia del proyecto debe cumplir simultaneamente:

1. estar versionada bajo `.agents/skills/`;
2. tener directorio con prefijo `solqaryn-`;
3. tener frontmatter `name: solqaryn-...`;
4. tener nombre visible que comience por `SOLQARYN`;
5. declarar `PROJECT_SCOPE_LOCK=STRICT`;
6. declarar `REPOSITORY=solqaryn/VariApp`;
7. no encadenar, importar ni consultar skills fuera del namespace SOLQARYN.

Si una skill no cumple cualquiera de estas reglas, no usarla.

## Aislamiento obligatorio

Trabajar exclusivamente con fuentes identificadas como SOLQARYN:

- archivos versionados en `solqaryn/VariApp`;
- recursos que el propietario declare expresamente como parte de SOLQARYN;
- conectores o archivos identificados de forma inequívoca como recursos de SOLQARYN;
- skills propias con prefijo `solqaryn-`.

No buscar, listar, abrir, invocar, importar, resumir ni usar como autoridad fuentes ajenas al alcance SOLQARYN.

La disponibilidad tecnica de una fuente no constituye permiso.

## Excepciones

Aceptar una fuente externa solo cuando exista una autorizacion explicita del propietario para esa fuente y alcance, o una entrada `ACTIVE` en `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`.

No inferir permisos. Una excepcion no autoriza fuentes relacionadas.

## Fail-closed

Ante duda sobre el origen de una fuente:

1. no consultarla;
2. continuar solo con SOLQARYN;
3. registrar bloqueo si impide completar materialmente la tarea;
4. usar una excepcion unicamente cuando este autorizada.

Si aparece accidentalmente contexto ajeno al alcance, no usarlo para decidir, editar, recomendar, validar ni certificar SOLQARYN.

## Preflight obligatorio

Antes de cualquier cambio material:

1. confirmar `solqaryn/VariApp` y `Desarrollo`;
2. leer `AGENTS.md`;
3. leer `docs/PROJECT_SCOPE_LOCK.md`;
4. comprobar `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`;
5. usar exclusivamente skills `solqaryn-*`;
6. cuando exista checkout local, ejecutar `node scripts/verify-project-scope.mjs`.

## Cierre

Antes de declarar una tarea cerrada:

- confirmar que todas las skills usadas pertenecen al namespace `solqaryn-`;
- confirmar que las fuentes usadas pertenecen a SOLQARYN o estan autorizadas;
- confirmar que no se introdujeron referencias operativas fuera del alcance;
- ejecutar el scope validator cuando este disponible.
