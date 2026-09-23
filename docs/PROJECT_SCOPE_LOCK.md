# PROJECT_SCOPE_LOCK — VariApp

```text
PROJECT_ID=VARIAPP
REPOSITORY=solqaryn/VariApp
BRANCH=Desarrollo
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SKILL=.agents/skills/variapp-project-governance/SKILL.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
```

## 1. Regla de aislamiento

Todo agente, automatización, chat, sesión, script o proceso que trabaje sobre VariApp debe operar exclusivamente con contexto perteneciente a este proyecto.

Está prohibido consultar, listar, leer, invocar, importar, resumir o utilizar como autoridad cualquier skill, documento, chat, repositorio, archivo, memoria, ADR, runbook, regla de arquitectura o contexto perteneciente a otro proyecto.

La mera disponibilidad técnica de una fuente no constituye autorización.

## 2. Fuentes permitidas

Sin autorización adicional pueden utilizarse únicamente:

1. archivos versionados en `solqaryn/VariApp`;
2. información que el propietario entregue y declare expresamente como parte de VariApp;
3. recursos conectados identificados expresamente como recursos de VariApp;
4. la skill propia `.agents/skills/variapp-project-governance/SKILL.md`;
5. capacidades genéricas de plataforma necesarias para ejecutar la tarea, siempre que no aporten ni importen reglas, arquitectura, decisiones, datos o contexto de otro proyecto.

Una capacidad genérica de plataforma no se convierte en autoridad de VariApp y no autoriza contexto cruzado.

## 3. Fuentes prohibidas por defecto

`EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`.

No se puede salir del alcance de VariApp para consultar:

- skills específicas de otros proyectos;
- documentación de otros proyectos;
- repositorios ajenos como fuente de gobierno o arquitectura;
- chats, memorias o decisiones de otros proyectos;
- archivos de Drive u otros conectores que no estén identificados como VariApp;
- reglas escogidas por similitud, conveniencia o disponibilidad.

Si una fuente parece relacionada pero no está inequívocamente identificada como VariApp, se considera externa y queda bloqueada.

## 4. Excepciones autorizadas

Solo existen dos vías válidas:

1. autorización explícita del propietario en la conversación actual, con fuente o alcance suficientemente identificado; o
2. una entrada `ACTIVE` versionada en `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`.

No se debe inferir un permiso anterior a partir de memoria, costumbre, accesibilidad, nombres parecidos ni uso en otro proyecto.

Para que un permiso anterior sea reutilizable debe estar registrado en la allowlist con fuente exacta, propósito, alcance, autorización y vigencia.

Una excepción nunca habilita automáticamente fuentes relacionadas.

## 5. Fail-closed

Ante duda:

1. no consultar la fuente externa;
2. continuar con las fuentes internas de VariApp;
3. declarar el bloqueo únicamente si impide completar materialmente la tarea;
4. solicitar autorización solo cuando sea realmente necesaria.

Si contexto de otro proyecto aparece accidentalmente en la sesión, no puede usarse para decidir, editar, recomendar, validar ni certificar VariApp.

## 6. Skills

La skill de gobierno de este proyecto es:

`.agents/skills/variapp-project-governance/SKILL.md`

Toda skill versionada bajo `.agents/skills/` debe declarar:

- `PROJECT_ID=VARIAPP`;
- `REPOSITORY=solqaryn/VariApp`;
- `PROJECT_SCOPE_LOCK=STRICT`.

No se permite que una skill de VariApp encadene, importe o delegue gobierno a una skill específica de otro proyecto.

## 7. Gate obligatorio

Antes de editar o publicar:

```bash
node scripts/verify-project-scope.mjs
```

El gate debe fallar si los archivos canónicos pierden el marcador de aislamiento, si una skill del repositorio no declara el scope correcto o si se introduce una referencia explícita prohibida en la superficie de gobierno.

## 8. Precedencia interna

Dentro del proyecto, esta política debe estar referenciada por `AGENTS.md`, `PROJECT_CONTEXT.md`, `docs/VAEP_AUTHORITY.md` y los demás archivos operativos.

Ningún documento histórico ni evidencia antigua puede anular este bloqueo.
