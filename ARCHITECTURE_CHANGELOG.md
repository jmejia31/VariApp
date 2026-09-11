# ARCHITECTURE_CHANGELOG — VariApp

Registro conciso de cambios que obligan a actualizar `PROJECT_INDEX.md`, `PROJECT_CONTEXT.md` o `ARCHITECTURE.md`. No reconstruye historial anterior.

## Convención

Cada entrada debe indicar fecha, cambio observable, documentos/rutas afectados y verificación. Solo se agrega cuando cambian arquitectura, módulos, integraciones, rutas/API, datos o comandos documentados.

## 2026-08-24 — Guía operativa por dominio

- Cambio: `PROJECT_INDEX.md` amplió el mapa por capas a una matriz navegable por dominio y flujos transversales.
- Cobertura: frontend, API, Application, Domain, Infrastructure, DB/integraciones y migraciones ancla de los dominios principales.
- Alcance: se definieron límites explícitos para evitar reinspecciones globales ante cambios locales.
- Verificación: rutas y archivos citados contrastados con el checkout; guard documental y diff validados; sin cambios de código o configuración.

## 2026-08-24 — Contexto histórico ChatGPT/VAEP

- Cambio: se incorporó `docs/CONTEXTO_CHATGPT_VAEP.md` y se enlazó desde `PROJECT_INDEX.md`.
- Alcance: automatización VAEP, validación causal, cadena compras-recepciones-reservas-facturación, no duplicación y consulta selectiva.
- Límite: referencia histórica/operativa de VariApp; las fuentes canónicas y el HEAD actual prevalecen.
- Verificación: enlaces y guard documental local comprobados; sin cambios en código de producción.

## 2026-08-24 — Inicialización del mapa técnico persistente

- Cambio: se consolidó `PROJECT_INDEX.md` como mapa operativo y se agregó un índice de decisión para cambios frecuentes.
- Cobertura: backend .NET por capas, frontend Angular por features, puntos de entrada, API, persistencia, configuración, dependencias, comandos y pruebas.
- Evidencia: inspección estática selectiva de manifiestos, solución/proyectos, `Program.cs`, rutas Angular, controladores, `AppDbContext`, migraciones y directorios de pruebas.
- Verificación: rutas, archivos, scripts y ejecutables de comandos comprobados localmente; no se ejecutó la aplicación ni se modificó código de producción.
