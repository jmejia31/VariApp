# N8.21 — Backup real y restore aislado de Desarrollo

Esta carpeta contiene la certificación causal de N8.21 para `jmejia31/VariApp`, exclusivamente sobre la rama `Desarrollo`.

## Resultado certificado

- Backup real de la base de Desarrollo: **PASS**.
- Restore aislado del mismo artefacto cifrado: **PASS**.
- Verificación de checksums externos e internos: **PASS**.
- Conteos de todas las tablas: **PASS**.
- Topología validada: **132 tablas**, **104 migraciones EF**, **240 foreign keys** y **542 índices**.
- Smoke de backend contra el restore aislado: **PASS**.
- Base activa de Desarrollo modificada por el restore: **NO**.
- Producción tocada: **NO**.
- Secretos expuestos: **0**.
- Artefacto temporal con el backup completo retenido tras la validación: **NO**.
- Candidatos `REMOVE_SAFE_DB_AFTER_BACKUP` pendientes: **0**.

## Cadena causal

1. `N8.21.C` ejecutó el backup real y el restore aislado en el run `35111491587`.
2. `N8.21.D` ejecutó smoke real del backend contra el restore aislado en el run `35113349678`.
3. `N8.21.E` demostró que no existe superficie frontend de producto para esta operación.
4. `N8.21.F` detectó y corrigió same-run la retención innecesaria del artefacto completo de backup; el run de cleanup `35120379562` terminó `success` y el readback confirmó ausencia del artefacto `10452911215`.
5. `N8.21.G` reconcilió la cadena backup → restore → sanity, cerró formalmente `ARCH-02` sin borrado especulativo y preparó la reconciliación causal de N8.8/N8.9.

## Evidencia canónica

- `BACKUP_MANIFEST.md`
- `RESTORE_VERIFICATION.md`
- `CLEANUP.md`
- `N8.21.G_TEST_CI_RECONCILIATION.json`
- `N8.8_N8.9_RECONCILIATION.md`
- Receipts y REVIEW_FIRST bajo `vaep/evidence/`.

La evidencia histórica no se reescribe. Las conclusiones previas quedan supersedidas únicamente cuando existe evidencia causal más nueva y explícitamente referenciada.
