---
name: solqaryn-skill-standard
description: "Estandar exclusivo de SOLQARYN para estructura, naming, metadata, progressive disclosure, referencias, scripts, assets y validacion de skills locales. Usar al revisar o definir la forma de cualquier skill bajo .agents/skills/ en solqaryn/VariApp. Requiere namespace solqaryn-, PROJECT_SCOPE_LOCK=STRICT y subordinacion a solqaryn-project-governance."
---

# SOLQARYN Skill Standard

## Gate

- `PLATFORM=SOLQARYN`
- `PROJECT_ID=VARIAPP`
- `REPOSITORY=solqaryn/VariApp`
- `PROJECT_SCOPE_LOCK=STRICT`
- `SKILL_NAMESPACE=solqaryn-`
- `PARENT_GOVERNANCE=solqaryn-project-governance`

No aplicar este estandar fuera de SOLQARYN.

## Estructura obligatoria

Toda skill local debe:

1. vivir en `.agents/skills/solqaryn-<funcion>/`;
2. incluir `SKILL.md` con frontmatter `name` y `description`;
3. usar `name: solqaryn-<funcion>`;
4. incluir `agents/openai.yaml` con `display_name` iniciado por `SOLQARYN`;
5. mantener `SKILL.md` compacto y mover detalle estable a `references/` cuando aporte valor;
6. usar `scripts/` solo para validacion o automatizacion determinista realmente necesaria;
7. usar `assets/` solo para recursos de salida propios de SOLQARYN;
8. declarar el scope del proyecto y no depender de skills fuera del namespace SOLQARYN.

## Progressive disclosure

- Frontmatter: decidir cuando activar la skill.
- `SKILL.md`: workflow y restricciones esenciales.
- `references/`: detalle que solo se carga cuando se necesita.
- `scripts/`: controles repetibles; deben ser deterministas y revisables.
- `assets/`: recursos de salida; no autoridad de proyecto.

## Validacion

Antes de cerrar una skill:

- verificar namespace y display name;
- verificar que no introduzca otro repositorio, otra identidad de proyecto o una URI externa de skill;
- verificar que respete arquitectura, seguridad y rama definidas por `solqaryn-project-governance`;
- ejecutar `node scripts/verify-project-scope.mjs` cuando exista checkout local;
- no declarar PASS solo por formato: comprobar que las instrucciones son ejecutables y coherentes.
