# ARCHITECTURE_CHANGELOG — VariApp

Registro conciso de cambios que obligan a actualizar `PROJECT_INDEX.md`, `PROJECT_CONTEXT.md` o `ARCHITECTURE.md`. No reconstruye historial anterior.

## Convención

Cada entrada debe indicar fecha, cambio observable, documentos/rutas afectados y verificación. Solo se agrega cuando cambian arquitectura, módulos, integraciones, rutas/API, datos o comandos documentados.

## 2026-08-24 — Inicialización del mapa técnico persistente

- Cambio: se consolidó `PROJECT_INDEX.md` como mapa operativo y se agregó un índice de decisión para cambios frecuentes.
- Cobertura: backend .NET por capas, frontend Angular por features, puntos de entrada, API, persistencia, configuración, dependencias, comandos y pruebas.
- Evidencia: inspección estática selectiva de manifiestos, solución/proyectos, `Program.cs`, rutas Angular, controladores, `AppDbContext`, migraciones y directorios de pruebas.
- Verificación: rutas, archivos, scripts y ejecutables de comandos comprobados localmente; no se ejecutó la aplicación ni se modificó código de producción.
