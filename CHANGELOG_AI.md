# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.

## 2026-08-12 — VAEP v2.2 EXECUTION_TRUTH + CI sin push funcional de GITHUB_TOKEN — CONFIGURADO

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive + Programación.

**Problema detectado:** la corrida programada de las 13:02 terminó después de publicar la reparación EF, pero `RUNNER_MUTEX_STATE` quedó `RUNNING` con heartbeat 13:03, lo que podía confundirse con actividad real. Además, la reparación canónica de `N0.5.07B2` fue publicada por el workflow temporal `vaep-ef-snapshot-repair.yml` con `permissions: contents: write`; el HEAD `fc2ca060bbc7eefd84ead93ea370b292e3e200f2` quedó técnicamente actualizado, pero los workflows `pull_request` asociados aparecieron `action_required` y sin jobs, por lo que no constituyen evidencia de fallo funcional ni de CI ejecutado.

**Corrección de gobierno:** `PLAN_EJECUCION_AUTONOMA.md` evoluciona a VAEP v2.2 `EXECUTION_TRUTH`: el mutex deja de ser equivalente a actividad; se incorporan `RUNNER_ACTIVITY_STATE`, `RUNNER_LAST_REAL_ACTION_AT`, `STOP_REASON`, `RESUME_POINT` y un PRE-FINAL GATE obligatorio. Una respuesta final queda prohibida cuando la invocación todavía tiene capacidad, no hay CI activo y existe trabajo recuperable. Si la plataforma termina la invocación, debe declararse `IDLE_PLATFORM_LIMIT`/`WAITING_CI` según corresponda en vez de fingir ejecución continua.

**Corrección CI:** queda prohibido usar workflows temporales con `contents: write` para commitear/pushear cambios funcionales o migraciones mediante `GITHUB_TOKEN`. Actions podrá generar artefactos, pero la publicación final debe realizarla el Runner mediante el conector GitHub normal y fast-forward. `action_required` con jobs vacíos debe investigarse inmediatamente y no dejarse esperando hasta la siguiente hora.

**Continuidad B2:** `N0.5.07B2` continúa `VALIDANDO`; el snapshot/migración EF canónicos de Banco permanecen en `fc2ca060...`. El siguiente changeset operativo retira el workflow temporal escritor mediante el conector GitHub normal para provocar una sincronización ordinaria del PR y obtener CI real. No se toca `main`, Producción, merge/auto-merge de PR #2, force-push ni ramas nuevas.

## 2026-08-12 — VAEP v2.1 FINISH_FIRST: cerrar árbol foco antes de abrir hermanos

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive + Programación.

**Objetivo/alcance:** corregir la selección que permitía dejar `N0.5` parcialmente abierto mientras el runner avanzaba `N0.6`. Se alinea `PLAN_EJECUCION_AUTONOMA.md`, `CONFIG` y el prompt de `VariApp VAEP v2 Runner` para priorizar el punto padre más antiguo ya iniciado y terminar todos sus hijos/subhijos antes de abrir un hermano.

**Cambios de gobierno:** `MAX_MICROTAREAS_POR_CORRIDA=SIN_TOPE_FIJO`; `REGLA_BLOQUEO=NO_SALTAR_ARBOL_FOCO`; política `RUNNER_SELECTION_POLICY=FINISH_FIRST`; locks propios stale deben reconciliarse/recuperarse; padres deben reflejar estado de hijos; un bloqueo real conserva el foco y detiene la corrida en vez de saltarlo. `RUNNER_CURRENT_RECOVERY_TARGET=N0.5` congela nuevas aperturas de N0.6 hasta cerrar N0.5, preservando intacto todo lo ya certificado en N0.6.

**Evidencia:** protocolo versionado en commit `9efbfbe7d7d8a701d86b1aa60940321747c61783`; tablero CONFIG/BITACORA actualizado y Runner horario actualizado en sitio. Cambio exclusivamente documental/de gobierno, con `[skip ci]`; no modifica código funcional, main, Producción, PR #2, auto-merge ni ramas.

## 2026-08-12 — N0.6.D2B/D2: productores tipados cerrados — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** reconciliar y cerrar la cadena `N0.6.D2` después de certificar los tres productores documentales de `MovimientoInventario`, sin tocar `N0.5.07B/07B1`, contratos DTO/API ni persistencia adicional. Esta entrada supersede el estado operativo antiguo de D2A que quedó registrado abajo como `VALIDANDO`: la solución finalmente certificada fue el boundary `typed-first` del repositorio, no el intento incompleto de mapping EF.

**Resultado:** `D2A` quedó certificado mediante `6eadf19a27a0c7c90b0cec54262070f896209738` y CI `31587640123`; `D2B1` Compra mediante `e62b0667f4faace2d8d6520f753547b3e2624a1d` / pruebas `c76124980914edbea57ad7ff97eaa705171a2d58` / CI `31589093189`; `D2B2` Venta mediante `bac4d61b34813168b087fd7e9caf740a518c354a` / pruebas `06dea3390e0c40bef94e80f2e0ce30f482cac1f2` / CI `31589968458`; y `D2B3` ConsumoInsumo mediante `8648cc61f29a878d213ff2ddcce4e3731a81ff43` con correcciones de prueba hasta `ed570bb842ae4fbeb57b981bd596dfafbecf6072`.

**Validación real:** el CI general `31594243722` sobre `ed570bb842ae4fbeb57b981bd596dfafbecf6072` terminó `SUCCESS` completo en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. Los intentos previos `31593684786` y `31593975660` fallaron por defectos de prueba/build (`mapping EF` asumido y API `SqlQueryInterpolated` no disponible) y fueron corregidos sin modificar el servicio funcional; la verificación final usa ADO.NET contra MySQL real.

**Control:** `N0.6.D2B3`, `N0.6.D2B` y `N0.6.D2` quedan `LISTO`; `N0.6.D3` queda habilitada. `N0.5.07B/07B1` conserva su lock concurrente. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D2B1: productor Compra migra a origen tipado — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** migrar exclusivamente el productor de movimientos de inventario de Compras al boundary `typed-first` ya certificado en D2A, sin tocar Venta, ConsumoInsumo, EF, migraciones ni contratos HTTP. D2B se subdividió adaptativamente en D2B1 Compra, D2B2 Venta y D2B3 ConsumoInsumo para mantener un concern por changeset.

**Resultado:** `CompraService.ConfirmarAsync` y `AnularAsync` escriben mediante `IMovimientoInventarioRepository.AddConOrigenTipadoAsync` con `OrigenMovimientoInventario.DesdeCompra(compra.Id)`. La anulación usa `CausaMovimientoInventario.AnulacionCompra`, por lo que el repositorio deriva el snapshot legacy `CompraAnulada` sin recuperar autoridad desde `ReferenciaTipo/ReferenciaId`.

**Evidencia funcional:** `e62b0667f4faace2d8d6520f753547b3e2624a1d`. Pruebas dirigidas actualizadas en `c76124980914edbea57ad7ff97eaa705171a2d58`, comprobando confirmación y anulación con origen Compra tipado y ausencia de uso del `AddAsync` legacy en la confirmación.

**Validación real:** CI general `31589093189` terminó `SUCCESS` completo sobre `c76124980914edbea57ad7ff97eaa705171a2d58`: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene quedaron verdes; el job MySQL completó también verificación de variante legado, cargas y snapshot sin drift.

**Control:** `N0.6.D2B1` queda `LISTO`; habilita `N0.6.D2B2`. `N0.5.07B/07B1` conserva su lock concurrente y no fue intervenido. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D2A: mapear origen tipado de MovimientoInventario en dominio/EF — VALIDANDO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** incorporar al modelo de dominio y metadatos EF las columnas `CompraId`, `VentaId` y `ConsumoInsumoId` que C2/C3 ya crearon y certificaron físicamente. Esta microtarea no crea DDL nuevo ni modifica todavía los productores.

**Resultado:** `MovimientoInventario` expone las tres FKs nullable; `MovimientoInventarioConfiguration` las mapea hacia `Compra`, `Venta` y `ConsumoInsumo` con `DeleteBehavior.Restrict`, los nombres reales de constraints N0.6 y los índices existentes. Se añadió una prueba de metadatos EF para verificar propiedades y principales relacionales.

**Control:** estado `VALIDANDO` hasta CI real. Las columnas `ReferenciaTipo/ReferenciaId` permanecen como snapshot de compatibilidad y D2B sigue pendiente. No se tocó N0.5.07B/07B1, main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D1: repositorio y consultas de MovimientoInventario usan origen tipado — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** retirar `ReferenciaTipo/ReferenciaId` como autoridad decisoria en las consultas de inventario usadas por anulación de compras, manteniendo el fallback legacy únicamente para el provider InMemory de pruebas hasta que D2 migre productores.

**Resultado:** `MovimientoInventarioRepository` consulta `CompraId` en el provider relacional para localizar el movimiento original de compra y para determinar las claves de movimientos posteriores. La prueba de integración aislada fuerza desacuerdo entre el snapshot legacy y `CompraId` y demuestra que MySQL sigue la FK tipada.

**Evidencia funcional:** `2a2e093f66899b9c02c18026ecd3f270b6a730c1`. El primer CI general `31585321041` falló exclusivamente porque el fixture generaba un `NumeroCompra` más largo que la columna; el defecto de prueba quedó corregido en `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`, sin ocultar el fallo inicial.

**Validación real:** CI general `31585718867` terminó `SUCCESS` completo sobre `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. La integración dirigida `MovimientoInventarioOrigenTipadoIntegrationTests` quedó incluida en el job MySQL que finalizó en verde.

**Control:** `N0.6.D1` queda `LISTO`; habilita `N0.6.D2`. Los locks concurrentes `N0.5.07B/07B1` no se intervinieron. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.C3: postcheck, constraints e integridad histórica — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** cerrar la persistencia/migración N0.6 con postcheck y constraints de origen tipado, preservando el bridge transitorio desde `ReferenciaTipo/ReferenciaId` y sin retirar todavía las columnas legacy.

**Resultado:** después del primer intento de C3, el CI general detectó ocho integraciones incompatibles con una restricción demasiado amplia. La corrección final `01c1116e6db4e839b56176333251e3992fa09d77` acota la obligatoriedad del origen tipado a movimientos documentales mapeables (`Compra`, `Venta`, `ConsumoInsumo`) y permite que ajustes no documentales conserven temporalmente cero FKs tipadas. Se mantiene exclusividad, equivalencia con `ReferenciaId`, triggers transitorios de bridge y fail-closed frente a combinaciones inválidas.

**Evidencia funcional:** C3 evolucionó mediante `48ec0e9b9251e95522194e1580c0702a100e026c`, `e68184b2fccc9fd3e5e8c8950e261dce2d1c3e04` y corrección final `01c1116e6db4e839b56176333251e3992fa09d77`. El fallo inicial del CI general `31580565994` quedó corregido, no ocultado.

**Validación real:** CI general `31581993565` terminó `SUCCESS` en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. ERP-N0.6 `31581993553` terminó `SUCCESS`: preflight C1 fail-closed, historia representativa, aplicación C2/C3, integridad tipada, bridge legacy→FK, constraint permanente fail-closed y snapshot EF sin drift.

**Control:** `N0.6.C1/C2/C3` y el padre `N0.6.C` quedan `LISTO`. La siguiente tarea propia del punto es `N0.6.D`; no se inicia en esta corrida porque el usuario limitó la ejecución a una única tarea independiente. `N0.5.07B/07B1` mantiene lock concurrente y no fue intervenido. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.B: contrato de origen tipado de MovimientoInventario — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** introducir exclusivamente el contrato de dominio e invariante del origen tipado definido por el preflight N0.6.A, sin adelantar persistencia, configuración EF, backfill ni consumidores de N0.6.C/D.

**Resultado:** se añadieron `TipoOrigenMovimientoInventario` y el value object `OrigenMovimientoInventario`. El contrato representa `Compra`, `Venta` o `ConsumoInsumo`, expone el identificador tipado correspondiente y falla cerrado si no existe origen, existen varios orígenes o el identificador no es positivo. La operación concreta continúa separada en `TipoMovimientoInventario`/`CausaMovimientoInventario`; no se codifican anulaciones/reversiones en strings del origen.

**Pruebas dirigidas:** `OrigenMovimientoInventarioTests` cubre los tres orígenes admitidos, exclusividad del ID tipado, cero orígenes, múltiples orígenes e IDs no positivos.

**Evidencia funcional:** `5fe605cc93470a4f4b90f73185016b9e15bc622e`, publicado por fast-forward exclusivamente en `Desarrollo`.

**Validación real:** CI general run `31575657900`: `Backend Release y pruebas` terminó `SUCCESS`, incluyendo restore, build Release y pruebas backend no-integración; `Frontend producción`, `Higiene del repositorio` y `Docker y aislamiento de entornos` también terminaron `SUCCESS`. El job MySQL continuaba ejecutándose al cierre proporcional de B y no se usa como evidencia de cierre porque esta microtarea no modifica EF ni persistencia.

**Concurrencia/control:** `N0.5.07B/07B1` mantiene lock de otro runner y no fue intervenido. `N0.6.C` queda habilitada por dependencia; deberá añadir persistencia nullable, preflight/backfill/constraints/postcheck sin retirar aún las columnas legacy. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.A: preflight de referencias polimórficas críticas — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** auditoría dirigida del punto N0.6 sin cambios funcionales. Se confirmó que `MovimientoInventario` todavía usa `ReferenciaTipo + ReferenciaId` como autoridad de origen sin FK tipada y que esa pareja participa en la seguridad de anulación de compras. Los productores confirmados son compra/compra anulada, venta/venta anulada y consumo/reversión de insumos. El DTO/API de movimientos también expone el contrato legacy.

**Finanzas:** `MovimientoFinanciero` ya dispone de `CompraId`, `VentaId` y `FacturaId`; su configuración EF declara esas FKs como autoridad y conserva `ModuloOrigen/ReferenciaId` únicamente como snapshot de auditoría/correlación. N0.6 no debe deshacer esa migración ni eliminar snapshots antes de certificar históricos.

**Diseño de transición:** `N0.6.B` debe separar origen tipado de operación/reversión y preparar como mínimo `CompraId`, `VentaId` y `ConsumoInsumoId` en inventario. `N0.6.C` deberá añadir persistencia nullable de transición, preflight fail-closed sobre valores históricos, backfill determinista, constraints/postcheck y mantener columnas legacy hasta limpieza posterior segura en N0.8.

**Evidencia:** `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md` contiene alcance, archivos afectados, riesgos, rollback y matriz de validaciones. `TASKS.md` registra N0.6.A cerrado y la continuidad B→C→D–H.

**Validación real:** validación documental proporcional: inspección dirigida de entidades, EF, repositorio, productores Compra/Venta/ConsumoInsumo, DTO/servicio/API y finanzas; no se ejecutaron builds ni tests porque el changeset es exclusivamente documental/preflight y no modifica app, workflows, migraciones ni entorno. Publicación exclusivamente en `Desarrollo` con `[skip ci]` conforme a `AGENTS.md`.

**Concurrencia/control:** `N0.5.07B/07B1` estaba tomado por otro runner ChatGPT y no fue intervenido. `N0.6.A` es independiente de ese lock; el Plan Maestro declara N0.6 dependiente de N0.0. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.5.07A: elegibilidad Activo + preservación histórica — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** primer hijo de N0.5.07. Los resolvers de `VentaRepository`, `FacturaRepository` y `MovimientoFinancieroRepository` solo devuelven métodos `Activo && !Eliminado` para operaciones nuevas. El límite de persistencia financiera rechaza también una FK/navegación directa inactiva o eliminada. Las lecturas históricas no filtran la navegación, por lo que relaciones existentes continúan visibles tras desactivar el catálogo; las reversiones históricas conservan su relación original.

**Pruebas dirigidas:** `MetodoPagoElegibilidadRepositoryTests` cubre resolver activo/inactivo/eliminado en los tres consumidores, lectura histórica de un método inactivo y fail-closed de una nueva operación financiera con catálogo inactivo.

**Validación real:** CI general `31571200414` terminó `SUCCESS` en backend/pruebas, integración MySQL, Docker, frontend e higiene; ERP-N0.5 `31571200316` terminó `SUCCESS` completo. Evidencia funcional `11c958ead2a7a8cc5a3b1db4b502cbe63e8efba7`.

## 2026-08-11 — N0.5.06 C: MovimientoFinanciero migra a autoridad relacional — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** `MovimientoFinanciero.MetodoPago` legacy dejó de ser autoridad persistente/operativa sin adelantar N0.5.07. `IMovimientoFinancieroRepository` resuelve catálogo por código/nombre; todas las lecturas cargan `MetodoPagoCatalogo`; el límite de persistencia normaliza cualquier entrada legacy transitoria hacia `MetodoPagoId` y limpia el enum antes de guardar. La reversión pagada de compra copia exclusivamente FK/navegación relacional y falla cerrada si el original carece de relación. `FinanzasService` resuelve movimientos manuales contra catálogo y sus DTOs leen el nombre relacional.

**Pruebas dirigidas:** `FinanzasServiceTests` cubre persistencia/lectura relacional y fail-closed de método inexistente; `MovimientoFinancieroRepositoryTests` cubre normalización legacy→FK, carga de navegación y reversión sin propagación del enum.

**Evidencia funcional:** commit `0f14b9b9f5248a01cb6c98fa456cd306fe38ae19` publicado en `Desarrollo`. El temporal accidental `NOPE_DO_NOT_CREATE` fue eliminado de la punta efectiva mediante fast-forward, sin force-push.

**Validación real:** workflow dedicado `ERP-N0.5 - Certificación MetodoPago histórico`, run `31568099373`, terminó `success`: restauración/compilación/pruebas backend, esquema relacional, historia representativa, fail-closed, preflight, backfill, postcheck/preservación y snapshot EF quedaron verdes. El CI general run `31568099446` también terminó `success` en sus cinco jobs: Backend Release/pruebas, migraciones e integración MySQL, Docker, frontend e higiene.

**Control:** `N0.5.06C` y su padre `N0.5.06` quedan `LISTO`; con A1/A2/A3/B/C cerradas, la siguiente tarea de la cadena es `N0.5.07`, dependiente directamente de C.

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

**Validación real:** workflow `ERP-N0.5 - Certificación MetodoPago histórico` run `31566179324` completó finalmente `success`: restore/build/tests backend, esquema relacional, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF quedaron verdes. CI general run `31566179269` fue generado para el mismo SHA; Docker e higiene estaban `success` durante el cierre operativo.

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
## 2026-08-12 - N0.5.07B2 - snapshot EF canonico de Banco - VALIDANDO

**Responsable:** ChatGPT / VAEP v2 Runner.

**Correccion:** los CI 31622173253 y 31622173357 demostraron que la logica Banco/fail-closed y las pruebas pasaban, pero dotnet ef migrations has-pending-model-changes detecto drift porque la migracion inicial de Banco no actualizo el snapshot EF canonico. Se reemplaza esa migracion manual por una migracion generada con EF Core 8.0.8, su Designer y AppDbContextModelSnapshot, sin relajar ninguna validacion ni alterar el diseno normalizado.

**Control:** B2 permanece VALIDANDO hasta que los CI reales sobre el changeset canonico terminen en verde. No se toca main, Produccion, PR #2, auto-merge ni ramas nuevas.
