# N5.8 — Exportaciones · Auditoría y preflight

Autoridad operativa: `docs/VAEP_AUTHORITY.md`  
Rama: `Desarrollo`  
Parent: `N5.8.A`  
Alcance del roadmap: soportar Excel (`xlsx`), PDF y CSV sin inventar una segunda autoridad de datos.

## Estado vivo inspeccionado

VariApp ya contiene dos primitivas maduras que deben reutilizarse en lugar de crear un subsistema paralelo:

1. `ReporteAdministrativoService.ExportarAsync` ya genera CSV y XLSX desde datos source-backed de usuarios/roles/auditoría. El endpoint autenticado de `ReportesAdministrativosController` delega en ese servicio y registra auditoría. El servicio limita hoy `formato` a `csv|xlsx`.
2. Infrastructure ya incluye `ClosedXML` para XLSX y `QuestPDF` para PDF. `QuestPdfFacturaPerfilesService` demuestra generación PDF real; no se autoriza convertir HTML/print en un sustituto de PDF.
3. `ArchivoDescargableDto(byte[] Contenido, string ContentType, string NombreArchivo)` ya es el contrato de archivo descargable usado por reportes/cargas masivas y debe preservarse salvo necesidad estrictamente demostrada.
4. El permiso sensible `Exportar` y los controles RBAC/auditoría existentes son autoridades reutilizables. N5.8 no debe ampliar acceso a datos ni saltarse el scope que ya protege el reporte origen.

## Gap material autorizado

Falta una frontera explícita y reutilizable para formatos de exportación que incluya `csv`, `xlsx` y `pdf`. El caso existente de reportes administrativos cubre CSV/XLSX, pero PDF está hoy especializado en facturación. N5.8 debe cerrar ese gap sin crear un motor BI, consultas ad-hoc ni nuevas métricas.

La primera superficie certificable y source-backed de N5.8 será la exportación de reportes administrativos existente: conservar sus mismos tipos/filtros/datos/RBAC/auditoría, añadir PDF real y factorizar sólo las primitivas de formato que sean necesarias para evitar duplicación. La frontera resultante podrá reutilizarse posteriormente por otros reportes, pero este parent no autoriza nuevas fuentes de datos ni nuevos datasets.

## Decisiones de diseño

- Formatos permitidos: `csv`, `xlsx`, `pdf`, catálogo cerrado y fail-closed para cualquier valor desconocido.
- CSV: UTF-8, escape determinista de delimitadores/comillas/saltos de línea y nombre de archivo seguro.
- XLSX: `ClosedXML`, hoja/encabezados deterministas; evitar fórmulas inyectables desde datos no confiables.
- PDF: `QuestPDF`, documento real generado en backend; diseño tabular legible y paginado, sin HTML remoto ni recursos externos arbitrarios.
- Content-Type y extensión deben corresponder exactamente al formato.
- El contenido exportado debe provenir de los mismos DTOs/queries autorizados que la vista/reporte origen; no SQL aportado por usuario, expresiones ni fórmulas financieras nuevas.
- La autorización se evalúa antes de generar bytes y la acción sigue siendo auditable.
- No se persisten archivos exportados ni jobs en N5.8 salvo que una etapa posterior demuestre una necesidad material. Por el estado vivo inspeccionado, N5.8.C puede ser `N/A_NO_NEW_PERSISTENCE` si B/D confirman que la generación sigue siendo request/response y usa la auditoría existente.

## Descomposición dependency-safe

- **B — Dominio y contratos:** definir el contrato cerrado de formato y las invariantes mínimas de una solicitud/resultado de exportación. Un solo write-scope coherente; no persistencia/API/UI.
- **C — Persistencia, migración y datos:** por defecto `N/A_NO_NEW_PERSISTENCE`; verificar que no exista necesidad de nueva entidad/migración y que auditoría existente cubra la acción. Sólo abrir migración si B descubre una necesidad real no inventada.
- **D — Aplicación, servicios y API:** reutilizar `ReporteAdministrativoService`, `ArchivoDescargableDto`, ClosedXML/QuestPDF; añadir PDF real y factor común mínimo; conservar filtros, RBAC, ProblemDetails y auditoría.
- **E — Frontend y UX:** ofrecer selección/descarga de CSV/XLSX/PDF en la superficie administrativa existente, con loading/error y nombre de archivo correcto; no nueva fuente de datos.
- **F — RBAC/auditoría/seguridad:** probar permiso Exportar/admin actual, aislamiento, CSV/Excel formula-injection cuando aplique, PDF sin recursos remotos y nombres/content-types seguros.
- **G — QA/regresión/CI:** backend/frontend/E2E exact-head y formatos válidos/corruptos según aplique.
- **H — Documentación/certificación:** rollup de contrato, formatos, seguridad, rollback y gates.

## Riesgos y mitigaciones

- **CSV/Excel injection:** valores que comiencen con caracteres de fórmula no deben convertirse accidentalmente en fórmulas ejecutables; preservar texto seguro.
- **PDF de gran volumen:** paginación y memoria deben permanecer acotadas; no cargar recursos remotos arbitrarios.
- **Desalineación MIME/extensión:** contrato cerrado y pruebas por formato.
- **Bypass RBAC/auditoría:** reutilizar controller/service actuales y negar formato/tipo desconocido antes de producir archivo.
- **Duplicación de librerías:** reutilizar `ClosedXML` y `QuestPDF` existentes; no añadir otra dependencia de Excel/PDF sin causa material.
- **Persistencia innecesaria:** no crear tabla/job/blob de exportaciones sólo para llenar N5.8.C.

## Rollback

Rollback de N5.8 debe ser un revert/forward-fix del contrato/generadores/endpoints/UI nuevos manteniendo intactas las exportaciones CSV/XLSX existentes. Como el preflight no requiere nueva persistencia, no debe existir DDL destructivo ni datos productivos que restaurar. Si C concluye que una migración es realmente necesaria, deberá documentar backup/preflight/postcheck/rollback antes de implementarla.

## Estrategia de pruebas

- Pruebas unitarias del catálogo cerrado de formatos y MIME/extensión.
- Pruebas dirigidas de CSV escaping/formula safety, XLSX legibilidad y PDF bytes/estructura no vacía.
- Backend build/tests exact-head.
- API: autorización, formato inválido, filtros existentes y auditoría.
- Frontend lint/build y flujo de descarga por los tres formatos.
- E2E sólo sobre la superficie existente si aporta cobertura material.
- Auditorías de dependencias/configuración cuando la delta toque Infrastructure/paquetes; no añadir paquetes si no es necesario.

## Aceptación N5.8.A

N5.8.A queda completo cuando esta frontera está registrada contra estado vivo, `N5.7.H` está `LISTO_REAL`, el siguiente scope B puede ejecutarse con un único writer sin solapamiento, N+1/N+2/N+3 permanecen dependency-gated, y no se ha creado filler, Producción, secretos, `main` ni merge de PR #2.
