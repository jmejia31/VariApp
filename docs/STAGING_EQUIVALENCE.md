# STAGING_EQUIVALENCE — N8.22

## Certificación vigente

**Estado:** `STAGING_EQUIVALENT_CERTIFIED`

**Rama evaluada:** `Desarrollo`

**Autoridad:** `docs/VAEP_AUTHORITY.md`

**Revalidación vigente:** `2026-09-16T19:27:58Z`

**Resultado final:** `MATERIAL_GAP=0`, `UNKNOWN=0`, `P0=0`, `P1=0`.

Esta certificación compara Desarrollo con metadata de Producción exclusivamente de forma read-only. No autoriza ni ejecuta cambios sobre `main`, Producción, deploys, secretos, DNS, certificados ni migraciones productivas.

## Frontera de seguridad

- Producción write: `0`.
- Deploy de Producción: `0`.
- Escritura en `main`: `0`.
- PR #2 write/merge: `0`.
- Valores de secretos leídos o persistidos: `0`.
- Datos de negocio productivos inspeccionados: `0`.

## Matriz de equivalencia final

| Superficie | Desarrollo | Producción / referencia read-only | Clasificación vigente |
|---|---|---|---|
| Backend Render | `variapp-api-desarrollo`, branch `Desarrollo`, Docker, plan free | `variapp-api`, branch `main`, Docker, plan free | `EQUIVALENT` para clase de runtime/plan; branch `EXPECTED_ENV_DIFFERENCE` |
| Región Render | Oregon | Virginia | `EXPECTED_ENV_DIFFERENCE` |
| Health check Render | `/health/ready` | `/health` | `EXPECTED_ENV_DIFFERENCE`; endpoints coherentes con las versiones desplegadas |
| Variables Render — nombres | Inventario sanitizado completo, sin valores | Inventario sanitizado completo, sin valores | nombres comunes equivalentes; diferencias justificadas como `EXPECTED_ENV_DIFFERENCE`; `UNKNOWN=0` |
| Frontend Vercel | `variapp-desarrollo` | `varistorehn`, `prj_djMCand2yYeY3AvaUWsjwHDDJDkM`, branch productiva `main` según evidencia de control-plane | identidad productiva confirmada; separación de proyecto/branch es `EXPECTED_ENV_DIFFERENCE` |
| Build frontend PROD | Angular, `npm run build`/`ng build` | root `frontend`, output `dist/inventoryapp-frontend/browser`, Node `24.x` | `EQUIVALENT` por contrato material |
| Custom env frontend | API resuelta por contrato versionado/rewrite, sin dependencia custom runtime detectada | versión productiva certificada usa contrato Angular estático | `N_A_JUSTIFIED_FOR_CURRENT_PRODUCTION_FRONTEND_CONTRACT` |
| Seguridad/observabilidad | JWT fail-closed, rate-limit, CORS, correlation y request observability revalidados | metadata no secreta/read-only | `PASS`; secretos expuestos `0` |

## Diferencias esperadas aceptadas

Las diferencias de branch, región, nombres DEV-only/PROD-only justificados, health-check version-aware y separación de proyecto/frontend son diferencias de entorno o versión. No representan una deuda material pendiente para la equivalencia de staging porque la cadena vigente demuestra que no queda `MATERIAL_GAP` ni `UNKNOWN` sin resolver.

No se exige igualdad textual de toda configuración entre Desarrollo y Producción. Se exige equivalencia material del contrato y clasificación explícita de toda diferencia conforme a `EQUIVALENT | EXPECTED_ENV_DIFFERENCE | MATERIAL_GAP | UNKNOWN`.

## REVIEW_FIRST y gate causal vigente

La revalidación ordenada por el propietario sobre la cola reabierta volvió a certificar secuencialmente `N8.22.E`, `N8.22.F` y `N8.22.G` bajo el estándar actual. El gate `N8.22.G` cerró con `P0=0`, `P1=0`, `P2=0`, `MATERIAL_GAP=0` y `UNKNOWN=0` usando readback fresco de Render/Vercel, evidencia de control-plane sanitizada y equivalencia exact-head sin delta funcional.

Los commits producidos durante esta revalidación son de evidencia/control; no introducen cambios de producto, runtime, schema ni configuración funcional. Por ello los tests/gates dirigidos causales conservan equivalencia y no se fabricó un rerun irrelevante.

## Evidencia causal vigente

Prerrequisitos anteriores al rango reabierto, preservados como certificación vigente:

- `vaep/evidence/receipts/N8.22.A_LISTO_REAL_20260916T162800Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.B_LISTO_REAL_20260916T162930Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.C_LISTO_REAL_20260916T163050Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.D_LISTO_REAL_20260916T163210Z_SUP00.json`

Revalidación actual del rango reabierto:

- `vaep/evidence/receipts/N8.22.E_LISTO_20260916T192028Z_SUP12.json`
- `docs/evidencias/staging-equivalence/N8.22.E_REVALIDATION_20260916T191934Z_SUP12.json`
- `vaep/evidence/reviews/N8.22.E_REVALIDATION_REVIEW_FIRST_20260916T192011Z_SUP12.json`
- `vaep/evidence/receipts/N8.22.F_LISTO_20260916T192420Z_SUP12.json`
- `docs/evidencias/staging-equivalence/N8.22.F_REVALIDATION_20260916T192343Z_SUP12.json`
- `vaep/evidence/reviews/N8.22.F_REVALIDATION_REVIEW_FIRST_20260916T192410Z_SUP12.json`
- `vaep/evidence/receipts/N8.22.G_LISTO_20260916T192646Z_SUP12.json`
- `docs/evidencias/staging-equivalence/N8.22.G_REVALIDATION_20260916T192607Z_SUP12.json`
- `vaep/evidence/reviews/N8.22.G_REVALIDATION_REVIEW_FIRST_20260916T192640Z_SUP12.json`

Evidencia de control-plane que resolvió las diferencias/unknowns sin exponer valores:

- `vaep/evidence/reconciliations/N8.22.G_OWNER_CONTROL_PLANE_EVIDENCE_20260916T172800Z.json`
- `vaep/evidence/reconciliations/N8.22.G_OWNER_RENDER_ENV_NAME_INVENTORY_20260916T182900Z.json`
- `vaep/evidence/reconciliations/N8.22.G_OWNER_EVIDENCE_RECONCILED_20260916T183900Z.json`

## Dictamen

`N8.22` vuelve a cumplir el contrato de `STAGING_EQUIVALENT`: todas las diferencias conocidas están clasificadas, no queda diferencia material sin resolver, no queda unknown pendiente y la certificación vigente se obtuvo sin modificar Producción ni exponer secretos. El cierre del parent requiere aún el receipt/readback vigente de `N8.22.H`; este documento es su artefacto certificador y no sustituye ese último paso de control-plane.
