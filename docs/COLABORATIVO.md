# Espacio de Coordinación Colaborativa — VariApp

Este documento define el handoff entre Javier Mejía, Codex, AntiG/Antigravity y ChatGPT. El estado técnico vivo se consulta en Git y `TASKS.md`; no se congelan aquí cifras de tests/builds que puedan quedar obsoletas.

## 1. Fuentes canónicas

- `PROJECT_CONTEXT.md`: contexto técnico.
- `PROJECT_INDEX.md`: índice dirigido.
- `ARCHITECTURE.md`: arquitectura.
- `TASKS.md`: pendientes.
- `CHANGELOG_AI.md`: cambios del equipo.
- `AGENTS.md`: reglas obligatorias.

Si existe contradicción entre un texto histórico y estas fuentes, prevalecen `AGENTS.md` y la memoria canónica más reciente en `Desarrollo`.

## 2. Equipo y acceso

| Integrante | Proyecto local PC | GitHub | Rol principal |
|---|---:|---:|---|
| Javier Mejía | Sí | Sí | Propietario/decisión final |
| Codex | Sí, cuando opera en la PC autorizada | Sí | Implementación y pruebas |
| AntiG / Antigravity | Sí, cuando opera en la PC autorizada | Sí | Implementación y pruebas |
| ChatGPT | No | Sí, solo mediante conector autorizado | Arquitectura/revisión/coordinación/cambios remotos |
| Otros agentes | No por defecto | Solo si existe conector autorizado | Según asignación |

Nadie debe asumir acceso local que no esté expresamente documentado.

## 3. Rama y entornos

- `Desarrollo`: única rama de trabajo.
- `main`: congelada.
- PR oficial: `Desarrollo -> main`, abierto y borrador.
- No ramas temporales.
- No auto-merge.
- Producción no se modifica.

## 4. Handoff mínimo entre agentes

Cada entrega necesita únicamente:

```text
Agente:
Objetivo:
Archivos/área:
Validaciones reales:
Commit:
Pendiente/bloqueo:
```

No repetir el resumen completo de arquitectura en cada handoff; referenciar `PROJECT_CONTEXT.md`.

## 5. Protocolo FULL FLASH / bajo consumo

1. Leer memoria canónica una vez.
2. No releer un archivo si no cambió.
3. No reindexar repositorio por cada prompt.
4. Analizar archivo objetivo + dependencias directas.
5. Usar búsquedas dirigidas.
6. Ejecutar validación proporcional.
7. Hacer un changeset coherente, no decenas de microparches si no aportan valor.
8. Tras reconexión, recuperar estado y continuar; no repetir diagnóstico.
9. Detener la exploración cuando el objetivo esté resuelto.
10. Actualizar memoria solo cuando exista cambio real.

## 6. Historial

Las verificaciones históricas de sesiones anteriores permanecen disponibles en `git log`/versiones previas de este archivo. No deben presentarse como estado actual sin volver a ejecutarlas.

Los cambios nuevos se registran en `CHANGELOG_AI.md` y los pendientes en `TASKS.md`.