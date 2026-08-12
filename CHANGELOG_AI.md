# CHANGELOG_AI — VariApp

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance y handoff. Los SHA exactos se consultan en Git.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Alcance:**

- creación de `PROJECT_CONTEXT.md`;
- creación de `PROJECT_INDEX.md`;
- creación de `ARCHITECTURE.md`;
- creación de `TASKS.md`;
- creación de `CHANGELOG_AI.md`;
- alineación de `AGENTS.md`, `CONTRIBUTING.md`, `README.md` y documentación colaborativa;
- eliminación de la regla que permitía ramas temporales;
- definición de `Desarrollo` como única rama de trabajo;
- definición explícita de acceso local: Javier, Codex y AntiG/Antigravity;
- definición de ChatGPT/otros agentes como acceso remoto vía conector GitHub salvo autorización futura;
- incorporación de reglas de rendimiento/tokens;
- incorporación de protocolo de recuperación tras reconexión/compactación sin reescaneo global.

**Validación:** cambio exclusivamente documental; se verificó el estado remoto de `Desarrollo` y la documentación administrativa afectada. No se modificó código, datos, migraciones ni Producción.

**Baseline previo:** `0a60b9b6de7f7d14bbb40de5795cc3c390e57279`.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** cerrar la migración histórica hacia `MetodoPagoId` sin pérdida ni reinterpretación de datos legacy.

**Cambios:**

- migración forward-only `20260812023600_N0_5_BackfillMetodoPagoHistorico`;
- seed idempotente de `Efectivo`, `Transferencia`, `Tarjeta` y `Otro` por `Codigo` estable;
- backfill exacto de `Venta`, `FacturaPago` y `MovimientoFinanciero`;
- preflight `backend/scripts/preflight-erp-n0-5-metodo-pago.sql`;
- postcheck `backend/scripts/postdeploy-erp-n0-5-metodo-pago.sql`;
- workflow dedicado `.github/workflows/erp-n0-5-ci.yml`;
- corrección del postcheck para la limitación `Can't reopen table` de tablas temporales en MySQL 8.4;
- acta `docs/ERP_N0_PUNTO_5_METODO_PAGO_BACKFILL.md`;
- actualización de `TASKS.md` para continuidad del equipo.

**Validación real:** workflow dedicado N0.5 run `31558300465` en success, incluyendo prueba fail-closed, preflight válido, backfill 1:1, postcheck y snapshot EF. CI general `Desarrollo - Compilación y pruebas` run `31558300370` completó backend, frontend, higiene, Docker, migraciones actuales, integración MySQL, snapshot y SQL forward en verde.

**Handoff:** el enum y columnas legacy permanecen temporalmente; no retirarlos ni endurecer `MetodoPagoId` hasta migrar los consumidores posteriores previstos por la auditoría del Punto 3.

## Formato futuro

Cada entrada debe contener, de forma breve:

- fecha;
- agente;
- objetivo;
- archivos/áreas modificadas;
- validaciones reales;
- riesgos/pendientes;
- referencia al commit cuando sea útil.

No registrar secretos, credenciales ni datos sensibles.