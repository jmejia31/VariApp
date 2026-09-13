# Espacio de Coordinación Colaborativa — VariApp

Este documento define el handoff entre Javier Mejía, Codex, AntiG/Antigravity y ChatGPT. El estado técnico vivo se consulta en Git, `TASKS.md` y `CHANGELOG_AI.md`; no se congelan aquí cifras de tests/builds que puedan quedar obsoletas.

## 1. Identidad y gate de sesión

```text
PROJECT_ID=VARIAPP
REPOSITORY=jmejia31/VariApp
BRANCH=Desarrollo
```

Toda conversación/sesión nueva comienza confirmando esos tres valores. Si alguno no coincide, no se modifica nada con estas reglas.

Con acceso local: `scripts/iniciar-sesion-ia.ps1`.

Con acceso remoto: comprobación equivalente mediante GitHub.

## 2. Fuentes canónicas

- `PROJECT_CONTEXT.md`: contexto técnico/identidad.
- `PROJECT_INDEX.md`: índice dirigido.
- `ARCHITECTURE.md`: arquitectura.
- `TASKS.md`: pendientes.
- `CHANGELOG_AI.md`: evidencia de cambios.
- `AGENTS.md`: reglas obligatorias.

Si existe contradicción entre texto histórico/memoria y estas fuentes, prevalecen la evidencia Git y las fuentes canónicas más recientes de `Desarrollo`.

## 3. Equipo y acceso

| Integrante | Proyecto local PC | GitHub | Rol principal |
|---|---:|---:|---|
| Javier Mejía | Sí | Sí | Propietario/decisión final |
| Codex | Sí, cuando opera en PC autorizada | Sí | Implementación/pruebas |
| AntiG / Antigravity | Sí, cuando opera en PC autorizada | Sí | Implementación/pruebas |
| ChatGPT | No | Sí, con conector autorizado | Arquitectura/revisión/coordinación/cambios remotos |
| Otros agentes | No por defecto | Solo con conector autorizado | Según asignación |

Nadie debe asumir acceso local que no esté documentado.

## 4. Aislamiento entre proyectos

- Esta coordinación aplica exclusivamente a VariApp.
- Usar únicamente rutas, ramas, planes y decisiones verificadas de VariApp.
- Una conversación no cambia de proyecto por inferencia.
- Un cambio explícito de proyecto obliga a ejecutar el gate del proyecto destino antes de escribir.

## 5. Rama y entornos

- `Desarrollo`: única rama de trabajo.
- `main`: congelada.
- PR #2: abierto y borrador.
- No ramas temporales.
- No auto-merge.
- Producción no se modifica.

## 6. Evidencia y handoff mínimo

Cada changeset registra `CHANGELOG_AI.md`. Si cambia el estado operativo, también `TASKS.md`.

Handoff:

```text
Proyecto confirmado:
Agente:
Objetivo:
Archivos/área:
Validaciones reales:
Commit:
Pendiente/bloqueo:
```

No repetir arquitectura completa; referenciar `PROJECT_CONTEXT.md`.

## 7. Protocolo FULL FLASH / bajo consumo

1. Gate de identidad.
2. Leer memoria canónica una vez.
3. No releer archivo si no cambió.
4. No reindexar repositorio por prompt.
5. Analizar objetivo + dependencias directas.
6. Usar búsquedas dirigidas.
7. Validación proporcional.
8. Changeset coherente, evitando microparches sin valor.
9. Tras reconexión recuperar estado; no repetir diagnóstico.
10. Detener exploración al resolver objetivo.
11. Actualizar documentos solo cuando cambie su contenido real.

## 8. Guardrails locales

- `.githooks/pre-commit`: bloquea repo/rama incorrectos y exige `CHANGELOG_AI.md`.
- `.githooks/post-commit`: auto-push solo si `origin` es VariApp y la rama es `Desarrollo`.
- `scripts/iniciar-sesion-ia.ps1`: diagnóstico corto de sesión.

## 9. Mejora continua

Las mejoras de bajo riesgo y directamente relacionadas pueden aplicarse. Las transversales se registran en `TASKS.md` para ejecución controlada. El objetivo es reducir latencia, tokens, trabajo repetido y errores sin sacrificar validación ni trazabilidad.
