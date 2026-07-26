# Espacio colaborativo: Javier + ChatGPT + Codex + Antigravity

## Objetivo

Coordinar todo el desarrollo de VariApp mediante GitHub, evitando cambios aislados, pérdida de contexto, archivos temporales y modificaciones directas sobre producción.

## Estructura oficial

- `main`: código estable y productivo.
- `Desarrollo`: integración compartida y única rama secundaria oficial.
- Pull Request colaborativo: muestra en tiempo real la diferencia entre `Desarrollo` y `main`.
- Issue de coordinación: registra decisiones, responsables, bloqueos y próximos pasos.
- GitHub Actions: valida compilación y pruebas en cada actualización de `Desarrollo`.

## Responsabilidades

### Javier Mejía

- Define prioridades y aprueba cambios funcionales.
- Autoriza expresamente migraciones, despliegues y merge a `main`.
- Resuelve decisiones de negocio y aceptación visual.

### ChatGPT

- Audita repositorio, arquitectura, Pull Requests y documentación.
- Coordina cambios mediante GitHub y deja evidencia verificable.
- No realiza merge ni despliegues productivos sin autorización.

### Codex

- Implementa, refactoriza, prueba y corrige código desde un checkout actualizado.
- Debe leer `AGENTS.md` antes de actuar.
- Debe publicar cada commit en `Desarrollo` y reportar sus verificaciones.

### Antigravity

- Implementa o revisa cambios desde el mismo flujo Git.
- Debe sincronizar `Desarrollo` antes de editar y publicar sus commits al finalizar.
- No debe conservar cambios relevantes únicamente en su entorno local.

## Ciclo de una tarea

1. La tarea se registra en el issue colaborativo o en el Pull Request.
2. El agente sincroniza `Desarrollo`.
3. Implementa un alcance pequeño y verificable.
4. Ejecuta compilación y pruebas aplicables.
5. Crea commit con identificación del agente.
6. El hook local publica el commit automáticamente cuando está configurado.
7. GitHub Actions certifica la actualización.
8. El agente registra resultado, riesgos y pendientes.
9. Javier revisa y decide el siguiente paso.

## Actualización automática

El repositorio incluye `.githooks/post-commit`. Después de ejecutar `scripts/configurar-colaboracion.ps1`, cada commit creado mientras la rama activa sea `Desarrollo` intentará ejecutar:

```bash
git push origin Desarrollo
```

La automatización no crea commits por sí sola: primero debe existir un commit intencional y revisable. Si no hay conexión o autenticación, el commit se conserva localmente y el hook muestra la instrucción para reintentar.

## Reglas de exclusión

No se aceptan en GitHub:

- credenciales o secretos;
- `node_modules`, `bin`, `obj`, `dist` o `.angular`;
- reportes de Playwright o TestResults;
- registros, cachés, respaldos o archivos temporales;
- código muerto, pruebas deshabilitadas o archivos sin referencia;
- migraciones ejecutadas en producción sin autorización.

## Estado actual al crear este espacio

- `main` permanece sin modificaciones.
- `Desarrollo` nació desde el commit certificado `b863bb2d8fe23177da040c60bdbb5fe2288f022e`.
- La rama anterior `agent/mejoras-variapp` queda como referencia histórica hasta que pueda eliminarse de forma segura.
- No se ha realizado ningún merge a `main`.
