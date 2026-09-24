# PROJECT_SCOPE_LOCK — SOLQARYN

```text
PLATFORM=SOLQARYN
PROJECT_ID=VARIAPP
REPOSITORY=solqaryn/VariApp
BRANCH=Desarrollo
PROJECT_SCOPE_LOCK=STRICT
SKILL_NAMESPACE=solqaryn-
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
```

## 1. Regla absoluta de alcance

Todo agente, automatización, chat, sesión, script o proceso que trabaje sobre SOLQARYN debe operar exclusivamente con fuentes identificadas como pertenecientes a SOLQARYN.

No se permite consultar, listar, leer, invocar, importar, resumir ni utilizar como autoridad una fuente que no pertenezca inequívocamente a SOLQARYN.

La disponibilidad técnica de una fuente no constituye autorización.

## 2. Skills de SOLQARYN

Toda skill propia del proyecto debe cumplir simultáneamente:

- vivir bajo `.agents/skills/`;
- usar un directorio cuyo nombre empiece por `solqaryn-`;
- declarar un `name:` que empiece por `solqaryn-`;
- usar un nombre visible que empiece por `SOLQARYN`;
- declarar `PROJECT_SCOPE_LOCK=STRICT`;
- declarar `REPOSITORY=solqaryn/VariApp`;
- no depender de skills fuera del namespace `solqaryn-`.

La skill rectora actual es:

`.agents/skills/solqaryn-project-governance/SKILL.md`

Si una skill no cumple estas condiciones, queda fuera del proyecto y no puede utilizarse.

## 3. Chats y sesiones

Cada chat o sesión nueva relacionada con SOLQARYN nace con este bloqueo activo.

Antes de utilizar una skill, se debe comprobar que pertenece al namespace `solqaryn-`.

No se debe descubrir ni seleccionar una skill por semejanza, conveniencia o disponibilidad. La identidad SOLQARYN es requisito previo.

## 4. Fuentes permitidas

Sin autorización adicional solo pueden utilizarse:

1. archivos versionados en `solqaryn/VariApp`;
2. skills `solqaryn-*` versionadas dentro del proyecto;
3. información entregada por el propietario y declarada expresamente como parte de SOLQARYN;
4. recursos conectados identificados inequívocamente como recursos de SOLQARYN.

## 5. Excepciones

Una fuente fuera del alcance solo puede utilizarse cuando:

1. el propietario autoriza expresamente esa fuente y alcance en la conversación actual; o
2. existe una entrada `ACTIVE` en `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`.

No se infieren permisos. Una excepción no autoriza ninguna otra fuente.

## 6. Fail-closed

Ante cualquier duda:

1. no consultar la fuente;
2. continuar únicamente con SOLQARYN;
3. si la fuente es materialmente necesaria, exigir autorización;
4. no utilizar contenido cargado accidentalmente para decidir, editar, recomendar, validar ni certificar.

## 7. Gate obligatorio

Antes de editar o publicar:

```bash
node scripts/verify-project-scope.mjs
```

El gate debe rechazar cualquier skill sin prefijo SOLQARYN, cualquier identidad de proyecto distinta de la canónica, cualquier repositorio distinto del autorizado o cualquier referencia operativa de skill fuera del namespace SOLQARYN.

## 8. Precedencia

Esta política es vinculante para toda superficie operativa de SOLQARYN. Ningún documento histórico, evidencia previa ni contexto de sesión puede anularla.
