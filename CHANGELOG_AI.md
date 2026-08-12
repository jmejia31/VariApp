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