# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-08-11 — N0.5.06 A2: escrituras de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A2 `LISTO`. El commit funcional `32feca8840122c7eccd58246a6db7196730d8491` migró `VentaService.CreateAsync/UpdateAsync`: el texto temporal del DTO se resuelve contra el catálogo persistente mediante `IVentaRepository.GetMetodoPagoPorCodigoONombreAsync`, se establecen `MetodoPagoId` y `MetodoPagoCatalogo`, y el enum legacy queda únicamente como proyección de compatibilidad derivada. Un método inexistente o vacío produce `BusinessRuleException`; ya no existe fallback silencioso de método desconocido a `Efectivo`.

**Pruebas dirigidas:** `e00e20c614c8c66c34f726c82ef4922d48dc21d8` añadió `VentaMetodoPagoServiceTests` para creación con FK/navegación, rechazo de método inexistente y actualización hacia catálogo relacional.

**Validación real:** workflow `ERP-N0.5 - Certificación MetodoPago histórico` run `31566179324`: etapa `Restaurar, compilar y probar backend` completada `success`, incluyendo las pruebas nuevas; también completaron correctamente creación de esquema, historia representativa, fail-closed y preflight mientras el resto de la certificación histórica continuaba. CI general run `31566179269` fue generado para el mismo SHA; Docker e higiene ya estaban `success` al cierre operativo de A2. No se atribuye todavía resultado final a pasos que seguían en ejecución.

**Control:** A2 no cambia migraciones ni contratos HTTP; `CreateVentaDto/UpdateVentaDto.MetodoPago` sigue siendo adaptador string temporal. N0.5.06 no está cerrado: A3 debe migrar lectura de `VentaDto` y propagación automática hacia `MovimientoFinanciero` para que tampoco tomen decisiones desde `Venta.MetodoPago` legacy.

## 2026-08-11 — Cierre N0.5.06 A1: repositorio Venta preparado para MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A1 `LISTO`. El commit funcional `d987cb669de6dfbd00b8691a46e27f566e32138c` añadió resolución de `MetodoPago` por código/nombre en `IVentaRepository`/`VentaRepository`, carga `MetodoPagoCatalogo` en lecturas operativas y carga explícita de la navegación en `FOR UPDATE`.

**Validación real:** en el CI general run `31563809556`, el job `Backend Release y pruebas` completó `success`, incluyendo restore, build Release y pruebas backend no-integración; frontend, higiene y Docker también completaron `success`. El workflow dedicado `ERP-N0.5 - Certificación MetodoPago histórico`, run `31563809580`, completó su job `metodo-pago-historico` en `success`: backend, esquema relacional, historia representativa, fail-closed, preflight, backfill histórico, postcheck/preservación 1:1 y snapshot EF quedaron verdes.

**Continuidad:** N0.5.06 no está cerrado. El siguiente punto elegible de esta cadena es A2: migrar escrituras de `VentaService` hacia `MetodoPagoId`/catálogo. A3, FacturaPago y MovimientoFinanciero continúan después según dependencias VAEP.

## 2026-08-11 — N0.5.06 A1: preparar Venta para autoridad relacional de MetodoPago

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** iniciar la eliminación de la doble autoridad de métodos de pago con un changeset pequeño y coherente, preparando el repositorio de Venta para que las siguientes microtareas puedan resolver y leer el catálogo relacional sin depender del enum legacy.

**Granularización VAEP:** el punto original N0.5.06 cruzaba Venta, FacturaPago y MovimientoFinanciero y resultó demasiado amplio. Se subdividió en A1 repositorio/carga relacional de Venta, A2 escrituras de Venta, A3 lecturas/propagación de Venta, B FacturaPago y C MovimientoFinanciero. N0.5.07 depende del cierre de C.

**Alcance funcional de A1:**

- `IVentaRepository` expone resolución de `MetodoPago` por código/nombre;
- `VentaRepository` carga `MetodoPagoCatalogo` en consultas operativas normales;
- la lectura transaccional `FOR UPDATE` carga explícitamente la navegación `MetodoPagoCatalogo`;
- se añade resolución dirigida contra el catálogo persistente excluyendo registros eliminados;
- no se cambia todavía el DTO/API, las reglas `Activo/RequiereReferencia/...`, ni se retiran físicamente columnas legacy.

**Validación previa real:** se verificaron de forma dirigida `Venta`, `FacturaPago`, `MovimientoFinanciero`, sus configuraciones EF, `VentaService`, `FacturaService`, `IVentaRepository` y `VentaRepository`. La revisión confirmó que `VentaService` todavía usa el enum como autoridad en creación/edición y que N0.5.06 debía dividirse antes de modificarlo. El build/CI se ejecutará sobre el commit funcional publicado; no se declara éxito de CI en esta entrada antes de que GitHub lo reporte.

**Riesgo/control:** A1 es infraestructura preparatoria y no declara N0.5.06 cerrado. Las escrituras y lecturas de `VentaService` siguen pendientes en A2/A3; GitHub/CI determinarán si A1 puede marcarse `LISTO`.

## 2026-08-11 — VAEP-001: reducir ejecuciones CI redundantes en certificaciones ERP-N0

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** evitar que los workflows históricos de certificación ERP-N0.2, N0.3, N0.4 y N0.5 consuman CI ante cambios exclusivamente frontend/documentales/no relacionados, sin reducir cobertura cuando cambien backend, tests, scripts propios o el workflow correspondiente.

**Alcance:** se añadieron filtros `paths` al evento `push` de `.github/workflows/erp-n0-2-ci.yml`, `erp-n0-3-ci.yml`, `erp-n0-4-ci.yml` y `erp-n0-5-ci.yml`, alineándolos con sus filtros de `pull_request`. `workflow_dispatch` permanece intacto y el CI general `desarrollo-ci.yml` no se reduce.

**Validación real:** el commit funcional `d2466a3047e7cd2001f1cf998faa08c4ae229c1b` fue publicado por fast-forward sobre `Desarrollo`. GitHub aceptó los cuatro YAML y generó ejecuciones `push`/`pull_request` para los workflows modificados; por ejemplo ERP-N0.2 run `31562526962` y ERP-N0.5 run `31562526984` fueron creados sobre el mismo SHA. El diff confirma que el cambio funcional se limita a los filtros `paths` de `push`; N0.1 ya estaba filtrado. Los jobs de certificación seguían ejecutándose al momento del cierre documental, por lo que no se atribuye un resultado funcional de esas suites que todavía no había concluido.

**Resultado:** `VAEP-001` queda `LISTO` porque el objetivo de trigger fue implementado y aceptado por GitHub. Los futuros pushes exclusivamente frontend/documentales/no relacionados dejan de disparar estas cuatro certificaciones históricas; cambios en backend, tests, scripts propios y los propios workflows siguen cubiertos.

## 2026-08-11 — VAEP v2: Plan Maestro ERP V5 completo + cola granular

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive.

**Objetivo:** convertir el Plan Maestro ERP V5 en una ejecución autónoma integral, granular y auditable, evitando changesets gigantes y permitiendo continuidad cuando exista un bloqueo independiente.

**Alcance:**

- importación del `Plan Maestro ERP V5 — VariApp` original a Google Docs como fuente rectora permanente;
- creación del Google Sheet `VariApp — VAEP v2 — Plan Maestro ERP V5 + Cola Granular`;
- representación completa ERP-N0→N9, gates N0→N9, tracks T0–T12 y backlog futuro no-core;
- generación de 778 microtareas ejecutables y 131 filas de plan/gobierno;
- granularización estándar `PRE`, `DOMAIN`, `DB_MIG`, `BACKEND_API`, `FRONTEND_UX`, `SEC_AUDIT`, `TEST_CI`, `DOC_CERT`;
- regla adaptativa: si una microtarea sigue siendo grande, subdividir antes de editar;
- máximo operativo de hasta 3 microtareas pequeñas por corrida;
- gates estrictos para impedir avanzar N0→N1→...→N9 sin cierre certificado;
- funciones futuras no-core registradas como `NO_AUTORIZADO` y no autoejecutables;
- checklist especializado N0.5.01–N0.5.15 cargado con N0.5.01–05 `LISTO` y N0.5.06–15 `PENDIENTE` iniciales;
- N0.5.13 marcado para reconciliación antes de duplicar workflow/CI ya existente en GitHub;
- mantenimiento de la regla solicitada: una tarea `BLOQUEADO` solo puede saltarse hacia otra sin dependencia directa ni transitiva de ella;
- actualización de `AGENTS.md`, `PROJECT_CONTEXT.md`, `TASKS.md` y `PLAN_EJECUCION_AUTONOMA.md` para VAEP v2.

**Validación real:** el `.docx` rector fue convertido correctamente a Google Docs; el workbook VAEP v2 fue generado y verificado con 778 tareas, 5 `LISTO`, 773 `PENDIENTE`, sin errores de fórmula detectados; después se importó a Google Sheets y se leyó nuevamente `COLA!A1:U12`, confirmando estructura, estados, dependencias y URL de fuente. Antes del changeset GitHub se verificó `Desarrollo` en `6bb272548b4f13011931310db526c6c4e6826142`. No se modificó aplicación, migraciones, datos ni Producción.

**Riesgo/control:** las filas futuras son plan operativo, no evidencia de implementación. GitHub prevalece ante cualquier discrepancia. La granularización no autoriza scope creep ni operaciones productivas.

## 2026-08-11 — VAEP v1: ejecución autónoma, Drive y dependencias

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo:** permitir ejecución autónoma de puntos autorizados sin instrucción manual por tarea.

**Alcance:** `PLAN_EJECUCION_AUTONOMA.md`, Google Sheet inicial, estados estrictos, selección por prioridad/dependencias, lock lógico y bloqueo no global.

**Validación:** Sheet inicial verificado; repositorio/rama comprobados; sin tocar main/Producción.

## 2026-08-11 — Gobierno colaborativo v2

**Responsable:** ChatGPT.

Gate `PROJECT_ID=VARIAPP`, aislamiento entre proyectos, lectura mínima, evidencia obligatoria, scripts/hook locales, hardening de publicación y política limitada `[skip ci]`.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

Creación/alineación de `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `TASKS.md`, `CHANGELOG_AI.md` y reglas de continuidad.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

Migración `20260812023600_N0_5_BackfillMetodoPagoHistorico`, seed idempotente, backfill, preflight/postcheck, workflow N0.5 y acta documental. Workflow N0.5 run `31558300465` success y CI general `31558300370` verde. Enum/columnas legacy permanecen temporalmente hasta migrar consumidores posteriores.

## 2026-08-11 — Catálogo público VARISTOREHN

**Responsable:** Codex.

Consulta pública segura `GET /tienda/productos`, consumo frontend y personalización pública. Backend/lint/build dirigidos aprobados; integraciones restantes afectadas por credenciales MySQL locales según evidencia del changeset.

## Formato futuro

Cada entrada debe contener fecha, agente, objetivo, alcance, validaciones reales, riesgos/pendientes y commit cuando sea útil. No registrar secretos ni datos sensibles.
