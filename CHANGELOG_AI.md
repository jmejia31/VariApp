# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-08-11 — N0.5.06 B: FacturaPago migra hacia MetodoPago relacional — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** `FacturaPago` dejó de usar el enum como autoridad operativa. `IFacturaRepository`/`FacturaRepository` resuelven métodos por código/nombre y cargan `MetodoPagoCatalogo` tanto en pagos como en la venta de origen. `FacturaService.RegistrarPagoAsync` resuelve el DTO temporal contra catálogo y persiste `MetodoPagoId`/navegación; el enum queda solo como proyección de compatibilidad derivada. Los DTOs de factura/pago leen el nombre desde el catálogo relacional.

**Evidencia funcional:** implementación hasta `d5e9a98c17848001fc64387c709a72ce0e379cd3`; fixtures relacionales ajustados en `e8ab2b733affea70ba47b3ea8a7ff450c6b7766f`; cierre resumido en `c53a99150520d25b3a91d4e8aee7d3c6003ccd97`.

**Validación real:** CI general run `31567189353` completó `success` en Backend Release/pruebas, migraciones MySQL, Docker, frontend e higiene. Workflow dedicado ERP-N0.5 run `31567189393` completó `success` en backend, esquema, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF.

**Control:** el Sheet estaba rezagado en `VALIDANDO` y fue reconciliado contra GitHub. El siguiente punto elegible de la cadena es `N0.5.06C`, retiro de autoridad legacy en `MovimientoFinanciero`.

## 2026-08-11 — N0.5.06 A3: lectura y propagación de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A3 `LISTO`. El commit funcional `c024cc7c96da45f6d2b21867950de3c4dce49fd4` eliminó el uso de `Venta.MetodoPago` como autoridad de lectura/propagación dentro de `VentaService`: `VentaDto.MetodoPago` se obtiene desde `MetodoPagoCatalogo.Nombre` y el `MovimientoFinanciero` automático creado al confirmar una venta recibe `MetodoPagoId`/`MetodoPagoCatalogo`; su enum legacy se deriva únicamente del catálogo cuando existe.

**Pruebas dirigidas:** `05687cffcf9d34b3fdd8efd9becf9d158b61f028` añadió cobertura para comprobar que el DTO usa el nombre del catálogo aunque el enum legacy difiera y que la confirmación propaga FK/navegación al movimiento financiero sin copiar el enum legacy de Venta.

**Validación real:** CI general run `31566541771`: job `Backend Release y pruebas` completó `success`, incluyendo restore, build Release y pruebas backend no-integración; `Frontend producción`, `Higiene del repositorio` y `Docker y aislamiento de entornos` también completaron `success`. El job MySQL seguía ejecutándose al cierre operativo de A3, por lo que no se atribuye un resultado aún no finalizado. El workflow dedicado ERP-N0.5 run `31566541808` fue generado sobre el mismo SHA y continuaba su certificación histórica.

**Control:** A3 no modifica `FacturaPago` ni el servicio financiero general. El siguiente punto de la cadena es B, que debe retirar la autoridad enum de `FacturaPago` y sus DTOs/flujos sin ampliar todavía reglas operativas de N0.5.07.

## 2026-08-11 — N0.5.06 A2: escrituras de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A2 `LISTO`. El commit funcional `32feca8840122c7eccd58246a6db7196730d8491` migró `VentaService.CreateAsync/UpdateAsync`: el texto temporal del DTO se resuelve contra el catálogo persistente mediante `IVentaRepository.GetMetodoPagoPorCodigoONombreAsync`, se establecen `MetodoPagoId` y `MetodoPagoCatalogo`, y el enum legacy queda únicamente como proyección de compatibilidad derivada. Un método inexistente o vacío produce `BusinessRuleException`; ya no existe fallback silencioso de método desconocido a `Efectivo`.

**Pruebas dirigidas:** `e00e20c614c8c66c34f726c82ef4922d48dc21d8` añadió `VentaMetodoPagoServiceTests` para creación con FK/navegación, rechazo de método inexistente y actualización hacia catálogo relacional.

**Validación real:** workflow `ERP-N0.5 - Certificación MetodoPago histórico` run `31566179324` completó finalmente `success`: restore/build/tests backend, esquema, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF quedaron verdes. CI general run `31566179269` fue generado para el mismo SHA; Docker e higiene estaban `success` durante el cierre operativo.

**Control:** A2 no cambia migraciones ni contratos HTTP; `CreateVentaDto/UpdateVentaDto.MetodoPago` sigue siendo adaptador string temporal. N0.5.06 no está cerrado: A3 migra lectura de `VentaDto` y propagación automática hacia `MovimientoFinanciero`.

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

**Alcance:** importación del Plan Maestro a Drive, tablero VAEP v2, ERP-N0→N9, gates, T0–T12, backlog futuro no-core, 778 microtareas, granularización adaptativa y bloqueo transitivo.

**Validación real:** fuente rectora y tablero verificados; sin cambios productivos.

## 2026-08-11 — VAEP v1: ejecución autónoma, Drive y dependencias

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

Estados estrictos, selección por prioridad/dependencias, lock lógico y bloqueo no global.

## 2026-08-11 — Gobierno colaborativo v2

Gate `PROJECT_ID=VARIAPP`, aislamiento entre proyectos, lectura mínima, evidencia obligatoria y hardening de publicación.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

Creación/alineación de memoria canónica y reglas de continuidad.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

Migración, seed idempotente, backfill, preflight/postcheck y workflow N0.5 certificados.

## 2026-08-11 — Catálogo público VARISTOREHN

**Responsable:** Codex. Consulta pública segura y personalización pública.

## Formato futuro

Cada entrada debe contener fecha, agente, objetivo, alcance, validaciones reales, riesgos/pendientes y commit cuando sea útil. No registrar secretos ni datos sensibles.
