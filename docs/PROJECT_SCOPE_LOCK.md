# PROJECT_SCOPE_LOCK — SOLQARYN

```text
PLATFORM=SOLQARYN
PROJECT_ID=VARIAPP
REPOSITORY=solqaryn/VariApp
BRANCH=Desarrollo
PROJECT_SCOPE_LOCK=STRICT
LOCAL_SKILL_COUNT=1
LOCAL_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
```

## 1. Regla absoluta de alcance

Todo agente, automatizacion, chat, sesion, script o proceso que trabaje sobre SOLQARYN debe operar con autoridad y contexto de SOLQARYN.

La disponibilidad tecnica de una fuente no constituye autorizacion.

## 2. Unica skill local

La unica skill local permitida es:

`.agents/skills/solqaryn-project-governance/SKILL.md`

No se permiten otras copias locales de skills externas bajo `.agents/skills/`.

## 3. Referencias externas de skills autorizadas

Existen nueve referencias externas autorizadas por el propietario. Su inventario, origen, pin y ruta oficial viven en:

`docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`

Su autorizacion persistente vive en:

`docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`

Estas referencias se consultan directamente en el origen original registrado. No se consultan copias, mirrors o reutilizaciones alojadas en otros proyectos.

La consulta externa autoriza leer instrucciones en el pin fijado. No autoriza instalar, ejecutar o copiar dependencias/scripts/binarios sin el analisis y autorizacion aplicables.

## 4. Chats y agentes

Cada chat o sesion nueva relacionada con SOLQARYN debe:

1. aplicar primero `solqaryn-project-governance`;
2. confirmar repo/rama;
3. usar fuentes internas de SOLQARYN para gobierno, arquitectura y estado;
4. si una tarea requiere una referencia externa de skill, resolverla exclusivamente mediante el registro;
5. no descubrir fuentes externas adicionales por semejanza o conveniencia;
6. aplicar fail-closed si origen, pin o ruta no pueden verificarse.

## 5. Fuentes permitidas

Sin autorizacion adicional pueden utilizarse:

1. archivos versionados en `solqaryn/VariApp`;
2. la unica skill local de SOLQARYN;
3. las nueve referencias externas ACTIVE en la allowlist, solo dentro de su alcance;
4. informacion entregada por el propietario y declarada expresamente como parte de SOLQARYN;
5. recursos conectados identificados inequívocamente como recursos de SOLQARYN.

## 6. Precedencia

Las referencias externas aportan metodologia o criterios especializados, pero nunca sustituyen:

- `AGENTS.md`;
- `docs/VAEP_AUTHORITY.md`;
- `PROJECT_CONTEXT.md`;
- `ARCHITECTURE.md`;
- `docs/PROJECT_SCOPE_LOCK.md`;
- estado vivo de Git/CI/proveedores autorizado.

## 7. Gate obligatorio

Antes de editar o publicar:

```bash
node scripts/verify-project-scope.mjs
```

El gate debe fallar si:

- existe mas de una skill local;
- la skill local no es la rectora autorizada;
- faltan el registro o la allowlist;
- la identidad canonica se contradice;
- se introduce una URI de skill externa en la gobernanza en lugar de resolverla por registro;
- una regla intenta convertir una referencia externa en autoridad de proyecto.

## 8. Excepciones adicionales

Fuera de las nueve referencias ACTIVE, cualquier fuente externa permanece en `DENY` salvo autorizacion explicita del propietario y actualizacion versionada de la allowlist cuando deba ser persistente.

## 9. Precedencia final

Ningun documento historico, evidencia previa, chat ni fuente externa puede anular este bloqueo.
