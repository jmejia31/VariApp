# PROJECT_INDEX — VariApp

Índice operativo del repositorio. Su objetivo es llevar al equipo directamente al área correcta sin reindexar todo el proyecto.

## Lectura inicial mínima

1. `AGENTS.md` — reglas obligatorias.
2. `PROJECT_CONTEXT.md` — contexto técnico base.
3. `TASKS.md` — trabajo pendiente/vigente.
4. `PROJECT_INDEX.md` — localizar archivos.
5. `ARCHITECTURE.md` — solo cuando la tarea implique arquitectura, dependencias transversales o diseño.

No leer todos los documentos administrativos en cada tarea. Consultarlos solo cuando sean relevantes.

## Raíz

- `AGENTS.md`: reglas obligatorias para cualquier agente.
- `PROJECT_CONTEXT.md`: memoria técnica canónica.
- `PROJECT_INDEX.md`: este índice.
- `ARCHITECTURE.md`: arquitectura y patrones.
- `TASKS.md`: pendientes operativos.
- `CHANGELOG_AI.md`: bitácora de cambios del equipo.
- `README.md`: introducción, stack y operación básica.
- `CONTRIBUTING.md`: flujo Git y criterios de contribución.
- `implementation_plan.md`: plan de implementación histórico/específico cuando aplique.
- `render.yaml`: configuración versionada relacionada con Render; tratar con cautela por separación de entornos.

## Backend

`backend/InventoryApp.sln` — solución .NET.

### `backend/src/Domain`

Responsabilidad: entidades, enums y elementos puros de dominio. No debe depender de Infrastructure ni API.

### `backend/src/Application`

Responsabilidad: casos de uso, DTO, interfaces, servicios, validadores y contratos. Puede depender de Domain, no de detalles concretos de infraestructura.

### `backend/src/Infrastructure`

Responsabilidad: EF Core, `AppDbContext`, repositorios, configuraciones, migraciones, Cloudinary, SMTP, QuestPDF y demás adaptadores concretos.

### `backend/src/API`

Responsabilidad: controladores, middleware, filtros, configuración HTTP, autenticación/autorización, DI y arranque. `Program.cs` es el composition root.

### `backend/tests`

Responsabilidad: pruebas backend. Ejecutar pruebas dirigidas para cambios localizados y suite completa en cierres/cambios transversales.

## Frontend

### `frontend/src/app/core`

Autenticación, guards, interceptores, modelos y utilidades transversales.

### `frontend/src/app/features`

Pantallas/módulos funcionales: productos, variantes, catálogos, compras, ventas, facturas, inventario, finanzas, usuarios, roles, permisos, auditoría, etc.

### `frontend/src/app/services`

Clientes HTTP y servicios compartidos.

### `frontend/src/app/app.routes.ts`

Mapa principal de rutas y permisos. Revisarlo únicamente cuando una tarea afecte navegación, lazy loading o autorización por ruta.

### `frontend/e2e`

Pruebas Playwright para flujos críticos.

## Documentación

### `docs/`

Documentación funcional, técnica, certificaciones, auditorías ERP-N0 y scripts/guías complementarias.

Administración colaborativa central:

- `docs/COLABORACION_IA.md`
- `docs/COLABORATIVO.md`
- `docs/ENTORNOS_DESARROLLO_PRODUCCION.md`

Documentación ERP-N0: archivos `docs/ERP_N0_*.md` y documentos específicos por punto.

No cargar toda la carpeta `docs` por defecto. Abrir únicamente el documento asociado al punto en ejecución.

## Automatización y CI

- `.github/workflows/`: CI y automatizaciones versionadas.
- `.githooks/`: hooks compartidos locales.
- `scripts/`: scripts auxiliares de configuración/validación.

## Regla de navegación dirigida

Para una tarea normal:

1. identificar módulo;
2. abrir el archivo de entrada del módulo;
3. seguir únicamente imports/interfaces/repositorios/DTO directamente relacionados;
4. buscar símbolos concretos con `rg`/búsqueda de código;
5. detener expansión cuando ya exista evidencia suficiente para implementar.

No listar recursivamente todo `backend`, `frontend` o `docs` salvo cambio estructural justificado.