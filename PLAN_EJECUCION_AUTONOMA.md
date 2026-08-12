# PLAN DE EJECUCIÓN AUTÓNOMA — VAEP v1

> VariApp Autonomous Execution Protocol. Fuente operativa humana: Google Sheets. Autoridad técnica y evidencia: GitHub `jmejia31/VariApp`, rama `Desarrollo`.

## 1. Identidad y fuentes

- `PROJECT_ID`: `VARIAPP`
- Repositorio: `jmejia31/VariApp`
- Rama única de trabajo: `Desarrollo`
- PR oficial: `#2`, siempre abierto y Draft hasta autorización de Javier Mejía.
- Tablero operativo: `VariApp — Cola de Ejecución Autónoma VAEP`
- Google Sheet: https://docs.google.com/spreadsheets/d/1RSgaF6q9wnvWT6cSO3bsxpesofompYUYUA7aohPMWTM/edit
- GitHub prevalece para código, commits, CI, arquitectura y evidencia verificable.

## 2. Objetivo

Permitir que ChatGPT consuma automáticamente los puntos autorizados sin requerir una instrucción manual para cada uno, preservando aislamiento de proyecto, trazabilidad, calidad, concurrencia segura y las restricciones de Producción.

## 3. Estados permitidos

`PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO`

Una tarea puede pasar de `EN_PROGRESO` o `VALIDANDO` a `BLOQUEADO` si existe un impedimento real. `CANCELADO` requiere instrucción explícita de Javier o evidencia inequívoca de que el punto dejó de aplicar.

Está prohibido marcar `LISTO` sin evidencia verificable.

## 4. Selección de tarea

En cada ejecución autónoma:

1. confirmar `PROJECT_ID=VARIAPP`, repositorio y rama;
2. leer `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md` y la última entrada relevante de `CHANGELOG_AI.md`;
3. leer `CONFIG` y `COLA` del Sheet VAEP;
4. descartar tareas de otro `PROJECT_ID`, repositorio o rama;
5. ordenar tareas `PENDIENTE` por `PRIORIDAD` ascendente y luego por orden de fila;
6. elegir la primera tarea cuyas dependencias estén todas en `LISTO`;
7. antes de escribir, volver a confirmar HEAD remoto y que ningún otro agente haya tomado la tarea;
8. registrar `EN_PROGRESO` y agente;
9. ejecutar solo el alcance de esa tarea y sus dependencias directas necesarias;
10. pasar a `VALIDANDO`, ejecutar validaciones proporcionales y registrar evidencia;
11. si todo cumple, publicar en `Desarrollo`, actualizar colaborativos aplicables y marcar `LISTO`.

## 5. Regla crítica de bloqueo y continuidad

Una tarea `BLOQUEADO` **no detiene toda la cola**.

Después de registrar el bloqueo, el agente debe buscar la siguiente tarea `PENDIENTE` elegible. Puede continuar únicamente cuando la nueva tarea **no dependa directa ni transitivamente** de ninguna tarea bloqueada pendiente de resolver.

Formalmente, una tarea `T` es elegible solo si:

- todas sus dependencias directas están `LISTO`; y
- ningún ancestro de `T` en el grafo de dependencias está `BLOQUEADO`.

Si `A` está bloqueada y `B -> A`, `C -> B`, entonces `B` y `C` no son elegibles. Una tarea `D` sin relación con `A` sí puede ejecutarse.

Una tarea bloqueada no se reintenta en bucle durante la misma ejecución. Se conserva causa, evidencia y siguiente acción requerida.

## 6. Concurrencia y lock lógico

- Una tarea con estado `EN_PROGRESO` o `VALIDANDO` y `AGENTE` asignado se considera tomada.
- Ningún segundo agente debe trabajar esa misma tarea.
- Justo antes de publicar, comprobar nuevamente HEAD de `Desarrollo`.
- Si otro agente avanzó la rama, integrar preservando sus cambios; nunca force-push.
- Si el conflicto no puede resolverse de forma dirigida y segura, marcar `BLOQUEADO` con evidencia y continuar solo con tareas independientes.

## 7. Evidencia obligatoria

Para marcar `LISTO` deben existir, según aplique:

- cambio publicado en `Desarrollo`;
- commit SHA;
- validaciones reales ejecutadas;
- `CHANGELOG_AI.md` actualizado;
- `TASKS.md` actualizado cuando cambie estado operativo;
- `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` o `ARCHITECTURE.md` solo si realmente cambió lo que documentan;
- fila de `COLA` actualizada;
- registro de transición en `BITACORA`.

No inventar pruebas, resultados, despliegues ni estados externos.

## 8. Política de seguridad

VAEP no autoriza:

- tocar `main`;
- fusionar PR #2;
- habilitar auto-merge;
- crear ramas adicionales;
- modificar Producción, secretos, variables, credenciales, bases, dominios, servicios o activos productivos;
- ejecutar migraciones productivas;
- saltarse validaciones funcionales mediante `[skip ci]`.

## 9. Alcance por ejecución

Objetivo por ejecución programada: completar una tarea elegible de extremo a extremo. Las tareas que resulten bloqueadas no cuentan como tarea completada; después del bloqueo se puede intentar la siguiente independiente dentro de la misma ejecución, evitando ciclos infinitos.

Si no existe ninguna tarea elegible, terminar sin realizar cambios funcionales. Si toda la cola está `LISTO`, registrar cierre únicamente cuando aporte valor y no generar commits vacíos.

## 10. Cómo agregar nuevas tareas

Javier puede agregar filas en la hoja `COLA` indicando como mínimo:

- `ID` único;
- `TAREA`;
- `ESTADO=PENDIENTE`;
- `PRIORIDAD`;
- `DEPENDENCIAS` por ID, separadas por coma cuando sean varias;
- `PROJECT_ID=VARIAPP`;
- `REPOSITORIO=jmejia31/VariApp`;
- `RAMA=Desarrollo`;
- criterios de aceptación y validaciones esperadas.

Si faltan criterios imprescindibles y no pueden deducirse con inspección dirigida, la tarea debe marcarse `BLOQUEADO`, no improvisarse.

## 11. Fuente de verdad y reconciliación

El Sheet define qué trabajo está solicitado y su estado operativo. GitHub demuestra qué trabajo existe realmente. Ante contradicción:

1. no sobrescribir evidencia válida;
2. comprobar commits/CI/archivos afectados;
3. reconciliar el Sheet con GitHub;
4. registrar la corrección en `BITACORA` y, si corresponde, `CHANGELOG_AI.md`.

Nunca marcar código como inexistente solo porque el Sheet está desactualizado, ni marcar `LISTO` solo porque el Sheet lo diga sin evidencia GitHub.
