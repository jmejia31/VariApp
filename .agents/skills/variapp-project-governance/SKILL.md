---
name: variapp-project-governance
description: "Gobierno tecnico exclusivo de VariApp para cualquier tarea que afecte, analice, documente, pruebe, despliegue o administre el proyecto solqaryn/VariApp. Usar solamente dentro de VariApp. Impone identidad canonica, rama Desarrollo, PROJECT_SCOPE_LOCK=STRICT y aislamiento fail-closed; prohibe consultar o reutilizar skills, documentos, chats, repositorios o contexto de otros proyectos salvo autorizacion explicita del propietario o una excepcion activa versionada en la allowlist del proyecto."
---

# Gobierno exclusivo de VariApp

## Identidad canonica

- `PROJECT_ID=VARIAPP`
- `REPOSITORY=solqaryn/VariApp`
- `BRANCH=Desarrollo`
- `PROJECT_SCOPE_LOCK=STRICT`
- `EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`
- Politica canonica: `docs/PROJECT_SCOPE_LOCK.md`
- Excepciones persistentes: `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`

No aplicar esta skill a ningun otro proyecto.

## Aislamiento obligatorio

Trabajar exclusivamente con contexto perteneciente a VariApp. Se permite usar:

1. archivos versionados en `solqaryn/VariApp`;
2. material que el propietario entregue y declare expresamente como parte de VariApp;
3. conectores, archivos o recursos externos identificados expresamente como recursos de VariApp;
4. capacidades genericas de la plataforma necesarias para ejecutar la tarea, siempre que no importen reglas, arquitectura, decisiones ni contexto de otro proyecto.

Prohibido consultar, listar, leer, invocar, importar, resumir o usar como autoridad:

- skills especificas de otro proyecto;
- documentacion, ADRs, runbooks, repositorios, archivos, chats o memorias de otro proyecto;
- reglas de arquitectura o gobierno cuyo `PROJECT_ID` o repositorio no sea VariApp;
- contexto externo elegido por similitud, conveniencia o disponibilidad.

La mera disponibilidad de una skill o documento no constituye autorizacion.

## Excepciones

Aceptar contexto externo solo cuando ocurra una de estas condiciones:

1. el propietario lo autoriza de forma explicita en la conversacion actual, indicando la fuente o el alcance; o
2. existe una entrada `ACTIVE` en `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md` que identifica de forma exacta la fuente, proposito, alcance y autorizacion.

No inferir permisos anteriores. Un permiso persistente solo existe si esta versionado en la allowlist. Una excepcion sirve unicamente para el alcance indicado y no habilita otras fuentes relacionadas.

## Fail-closed

Ante duda sobre si una fuente pertenece a VariApp:

1. no consultarla;
2. continuar con las fuentes internas disponibles;
3. registrar el bloqueo si impide completar la tarea;
4. solicitar autorizacion solo cuando esa fuente sea materialmente necesaria.

Si aparece o se carga accidentalmente contexto de otro proyecto, no usarlo para decidir, editar, recomendar ni validar VariApp.

## Preflight obligatorio

Antes de cualquier cambio material:

1. confirmar `solqaryn/VariApp` y `Desarrollo`;
2. leer `AGENTS.md`;
3. leer `docs/PROJECT_SCOPE_LOCK.md`;
4. leer `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md` solo para comprobar excepciones activas;
5. leer los archivos canonicos de VariApp necesarios para la tarea, sin salir del alcance;
6. cuando exista checkout local, ejecutar `node scripts/verify-project-scope.mjs` antes de editar y antes de publicar.

Si falla el scope gate, detener la escritura hasta corregir el alcance.

## Reglas de skills

- Esta es la skill de gobierno de proyecto de VariApp.
- No encadenar skills especificas de otro proyecto.
- No importar referencias desde rutas de skills ajenas a VariApp.
- Una skill generica de plataforma puede utilizarse solo para una capacidad tecnica transversal requerida por el entorno o la tarea; nunca puede sustituir la autoridad de VariApp ni aportar contexto de otro proyecto.
- Si una skill externa contradice este aislamiento, ignorar su contexto de proyecto y mantener el scope de VariApp.

## Cierre

Antes de declarar una tarea cerrada:

- confirmar que las fuentes usadas pertenecen a VariApp o estan autorizadas en la allowlist;
- confirmar que no se introdujeron referencias operativas a otro proyecto;
- ejecutar el scope validator cuando este disponible;
- registrar cualquier excepcion realmente utilizada.
