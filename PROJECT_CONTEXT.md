# PROJECT_CONTEXT — VariApp

> Fuente principal de contexto técnico de **VariApp exclusivamente**. Todo integrante debe leer este archivo antes de explorar código ya documentado.

## 0. Identidad inequívoca del proyecto

```text
PROJECT_ID: VARIAPP
Repositorio GitHub: jmejia31/VariApp
Repository ID: 1293033995
Rama única de trabajo: Desarrollo
PR oficial: #2 Desarrollo -> main
```

Esta identidad funciona como frontera anti-contaminación entre proyectos. Si la sesión actual no puede comprobar que está operando sobre `jmejia31/VariApp`, no debe modificar nada usando este contexto.

Nunca importar automáticamente rutas, ramas, planes, bases, reglas o decisiones de otro proyecto. Un cambio explícito de proyecto exige volver a verificar la identidad del repositorio destino y leer sus propios documentos canónicos.

## 1. Estado canónico

- Repositorio: `jmejia31/VariApp`.
- Rama única de trabajo: `Desarrollo`.
- `main`: referencia productiva congelada; no se modifica desde el flujo de desarrollo.
- PR oficial: `Desarrollo -> main`, debe permanecer abierto y en borrador hasta autorización expresa de Javier Mejía.
- Entornos lógicos autorizados: `varistorehn_producción` y `varistorehn_desarrollo`.
- Baseline arquitectónico revisado: `0a60b9b6de7f7d14bbb40de5795cc3c390e57279`.
- Fecha de consolidación arquitectónica: 2026-08-11.

El baseline arquitectónico no pretende ser el HEAD actual. Para estado operativo vivo consultar Git + `TASKS.md` + `CHANGELOG_AI.md`.

Este archivo reemplaza la necesidad de reanalizar el repositorio completo en cada sesión. Se actualiza cuando exista un cambio arquitectónico real, un módulo ERP mayor nuevo o una modificación transversal que invalide este mapa.

## 2. Bootstrap obligatorio de conversación/sesión

Cada conversación nueva, cambio de agente, reconexión o compactación comienza así:

1. verificar `PROJECT_ID=VARIAPP` y repositorio `jmejia31/VariApp`;
2. verificar rama `Desarrollo` y HEAD actual;
3. leer `AGENTS.md`;
4. leer este archivo;
5. leer `TASKS.md` y la última entrada relevante de `CHANGELOG_AI.md`;
6. revisar solo commits nuevos desde el handoff/baseline operativo conocido;
7. abrir únicamente archivos afectados que sean necesarios para la tarea.

Con acceso local, usar `scripts/iniciar-sesion-ia.ps1` como gate read-only antes de editar. Con acceso remoto, ejecutar la verificación equivalente mediante el conector GitHub disponible.

## 3. Propósito del sistema

VariApp administra la operación comercial de VariStorehn y evoluciona hacia un ERP empresarial: catálogo de productos, variantes, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, auditoría, reportes e integraciones.

La factura actual es un comprobante comercial interno mientras no exista habilitación fiscal SAR/CAI aplicable.

## 4. Stack vigente

### Frontend

- Angular 20.
- Componentes standalone.
- Signals.
- Angular Material.
- Rutas lazy-loaded.
- Guards de autenticación y permisos.
- Playwright para E2E.

### Backend

- ASP.NET Core 8 Web API.
- C# / .NET 8.
- Arquitectura por capas: `Domain`, `Application`, `Infrastructure`, `API`.
- FluentValidation.
- JWT Bearer y BCrypt.
- Rate limiting en autenticación.
- Health checks `/health` y `/health/ready`.

### Datos e infraestructura

- MySQL.
- Entity Framework Core 8 + Pomelo.
- Migraciones EF Core versionadas.
- Cloudinary para imágenes/documentos.
- QuestPDF para PDF.
- SMTP para correo.
- Vercel para frontend, Render para API, Aiven para MySQL y Cloudinary para medios.

## 5. Arquitectura funcional resumida

### Backend

`Domain` contiene entidades, enumeraciones y reglas de dominio básicas. `Application` contiene DTO, interfaces, servicios, validadores y casos de uso. `Infrastructure` implementa persistencia EF Core, repositorios, migraciones y adaptadores externos. `API` expone controladores, filtros, middleware, seguridad y composición de dependencias.

Flujo típico:

`HTTP -> Controller/API -> Application Service -> Repository/Infrastructure -> AppDbContext/MySQL`

Las integraciones externas se abstraen mediante servicios de infraestructura.

### Frontend

`frontend/src/app/core` concentra autenticación, guards, interceptores y modelos compartidos. `features` contiene módulos funcionales y pantallas. `services` contiene clientes HTTP y servicios compartidos. Las rutas cargan componentes bajo demanda y aplican `authGuard` + `permisoGuard` donde corresponde.

Flujo típico:

`Route -> Guard -> Standalone Component -> Angular Service -> API -> estado/UI`

## 6. Dominios/módulos actualmente presentes

El repositorio ya contiene funcionalidad para, entre otros:

- Dashboard.
- Productos y variantes.
- Catálogos de producto: color, talla, marca y modelo.
- Categorías.
- Clientes y tipos de cliente.
- Proveedores.
- Usuarios, roles y permisos.
- Auditoría.
- Compras y documentos de compra.
- Ventas.
- Facturación y pagos.
- Descuentos, impuestos y costos de envío.
- Inventario y movimientos.
- Finanzas y movimientos financieros.
- Cargas masivas.
- Reportería administrativa.
- Perfil, tema visual y configuración empresarial.

## 7. Seguridad y autorización

- Autenticación JWT.
- Autorización backend obligatoria; el frontend no sustituye controles del servidor.
- RBAC relacional en evolución ERP-N0 mediante `Usuario.RolId -> Rol -> RolPermiso -> Permiso`.
- El modelo debe evitar bypasses administrativos implícitos y privilegiar grants explícitos.
- Alcance de datos por usuario en operaciones donde aplica.
- Soft-delete en entidades que requieren preservación histórica.
- Auditoría transversal.
- Secretos únicamente en configuración segura de entornos; nunca en Git.

## 8. Persistencia y migraciones

- `AppDbContext` usa EF Core/MySQL.
- Las configuraciones de entidad se aplican desde el assembly de Infrastructure.
- Migraciones deben ser revisables, aditivas cuando sea posible y acompañadas de estrategia forward/reversión cuando el riesgo lo requiera.
- Producción no recibe migraciones sin autorización expresa de Javier Mejía y validaciones previas.
- ERP-N0 usa transiciones expand-and-contract para retirar legacy sin big-bang.

## 9. Roadmap rector vigente

El baseline histórico M0–M13 se conserva cerrado. La evolución ERP sigue, en orden estricto:

`ERP-N0 -> ERP-N1 -> ERP-N2 -> ERP-N3 -> ERP-N4 -> ERP-N5 -> ERP-N6 -> ERP-N7 -> ERP-N8 -> ERP-N9`

- N0: saneamiento y retiro legacy.
- N1: inventario empresarial.
- N2: compras empresariales.
- N3: ventas empresariales.
- N4: tesorería, CxC, CxP y contabilidad.
- N5: reportería y BI.
- N6: multiempresa/SaaS.
- N7: integraciones.
- N8: production readiness.
- N9: go-live/hypercare.

Transversales T0–T12: arquitectura, BD, migraciones, seguridad, auditoría, QA, API, frontend/UX, performance, observabilidad, DevOps, documentación y localización/fiscalidad.

No se inicia N1 hasta cerrar formalmente N0.

## 10. Estado técnico reciente relevante

El contexto arquitectónico documenta auditoría legacy y normalización de producto/variantes, RBAC relacional y `MetodoPago` dentro de ERP-N0.

El commit `0a60b9b6de7f7d14bbb40de5795cc3c390e57279` cerró documentalmente la persistencia relacional base de `MetodoPago`, manteniendo compatibilidad legacy de transición.

Después del primer changeset de gobierno colaborativo (`215d5feed3cdd4725b7c89a48bf8bad55874c6aa`), `Desarrollo` continuó con trabajo ERP-N0.5. El estado operativo exacto debe leerse en `TASKS.md`, `CHANGELOG_AI.md` y Git; no inferirlo del baseline arquitectónico.

## 11. Equipo y acceso

Equipo permanente:

- Javier Mejía — propietario y decisión final.
- Codex — implementación local/Git cuando opera en el equipo autorizado.
- AntiG / Antigravity — implementación local/Git cuando opera en el equipo autorizado.
- ChatGPT — arquitectura, revisión, coordinación y cambios remotos mediante el conector GitHub autorizado.

### Frontera de acceso

Acceso al filesystem/proyecto local de la PC: únicamente Javier Mejía, Codex y AntiG/Antigravity, salvo que Javier documente explícitamente una ampliación futura.

ChatGPT y cualquier otro agente se consideran **sin acceso local**. Pueden usar GitHub únicamente mediante una conexión/conector autorizado y comprobado. Nunca deben afirmar que modificaron archivos locales de la PC si solo modificaron GitHub.

## 12. Evidencia y continuidad

- Cada changeset intencional registra una entrada breve en `CHANGELOG_AI.md`.
- `TASKS.md` cambia cuando cambia el estado operativo, aparece un bloqueo o surge un pendiente relevante.
- `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE.md` solo se editan cuando cambia lo que documentan.
- Las reglas colaborativas se actualizan cuando cambian gobierno, accesos o protocolo.
- Git (`commit`, diff, workflow real) es la evidencia técnica; nunca inventar resultados.

Este criterio garantiza trazabilidad sin inflar artificialmente los documentos canónicos.

## 13. Regla de rendimiento y tokens

1. No volver a recorrer todo el repositorio para cada solicitud.
2. Usar este archivo como contexto base.
3. Leer solo archivos directamente afectados y dependencias directas.
4. No releer archivos ya documentados si Git demuestra que no cambiaron.
5. Detectar cambios con `git diff`, `git log` o búsquedas dirigidas.
6. Expandir el análisis únicamente cuando aparezca una dependencia real.
7. Si una tarea puede resolverse modificando menos archivos, elegir esa solución.
8. Priorizar cambios pequeños, localizados, reversibles y verificables.
9. No analizar módulos no relacionados una vez cumplido el objetivo.
10. Tras reconexión/compactación, recuperar estado y continuar; no reiniciar diagnóstico.
11. Solo renovar este contexto completo cuando cambie de forma importante la arquitectura o se agregue un módulo mayor.

## 14. Cuándo actualizar este archivo

Actualizar si ocurre alguno de estos eventos:

- nueva capa/proyecto principal;
- cambio de framework o versión mayor con impacto arquitectónico;
- cambio de proveedor de persistencia;
- rediseño de autenticación/RBAC;
- cambio transversal del modelo de datos;
- incorporación de un módulo ERP mayor;
- nueva integración estructural;
- cambio fuerte de despliegue/observabilidad.

Cambios CRUD, correcciones pequeñas, estilos, textos, validadores o endpoints localizados normalmente **no** justifican un reescaneo global.