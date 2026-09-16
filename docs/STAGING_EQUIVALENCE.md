# STAGING_EQUIVALENCE — N8.22

## Certificación

**Estado:** `STAGING_EQUIVALENT_CERTIFIED`

**Rama evaluada:** `Desarrollo`

**Autoridad:** `docs/VAEP_AUTHORITY.md`

**Resultado final:** `MATERIAL_GAP=0`, `UNKNOWN=0`, `P0=0`, `P1=0`.

Esta certificación compara Desarrollo con metadata de Producción exclusivamente de forma read-only y no autoriza ni ejecuta cambios sobre `main`, Producción, deploys, secretos, DNS, certificados o migraciones productivas.

## Frontera de seguridad

- Producción write: `0`.
- Deploy de Producción: `0`.
- Escritura en `main`: `0`.
- PR #2 write/merge: `0`.
- Valores de secretos leídos o persistidos: `0`.
- Datos de negocio productivos inspeccionados: `0`.

## Matriz de equivalencia final

| Superficie | Desarrollo | Producción / referencia read-only | Clasificación final |
|---|---|---|---|
| Backend Render | `variapp-api-desarrollo`, branch `Desarrollo`, Docker, plan free | `variapp-api`, branch `main`, Docker, plan free | `EQUIVALENT` para clase de runtime/plan; branch es `EXPECTED_ENV_DIFFERENCE` |
| Región Render | Oregon | Virginia | `EXPECTED_ENV_DIFFERENCE` |
| Health check Render | `/health/ready` | `/health` | `EXPECTED_ENV_DIFFERENCE`; endpoints coherentes con las versiones desplegadas y sin requisito de forzar paridad literal en Producción |
| Variables Render — nombres | Inventario sanitizado capturado, sin valores | Inventario sanitizado capturado, sin valores | `EQUIVALENT_NAME_SURFACE` para comunes; diferencias restantes clasificadas como `EXPECTED_ENV_DIFFERENCE`; `UNKNOWN=0` |
| Variables DEV-only | 15 nombres asociados a superficie de configuración más nueva en Desarrollo | No presentes en la versión productiva comparada | `EXPECTED_ENV_DIFFERENCE` |
| Variable PROD-only de origen CORS | No requerida con la misma forma en Desarrollo | Nombre productivo adicional de origen permitido | `EXPECTED_ENV_DIFFERENCE` |
| Frontend Vercel PROD | Proyecto DEV separado en el mismo equipo | `varistorehn`, proyecto `prj_djMCand2yYeY3AvaUWsjwHDDJDkM`, branch productiva `main` según evidencia de control-plane | Identidad productiva `CONFIRMED`; diferencia de proyecto/branch `EXPECTED_ENV_DIFFERENCE` |
| Custom env parity de frontend PROD | Desarrollo puede contener optimizaciones/configuración posterior | La versión productiva certificada usa entorno Angular estático para `apiUrl` | `N_A_JUSTIFIED_FOR_CURRENT_PRODUCTION_FRONTEND_CONTRACT` |
| DB/provider/storage metadata | Clasificada previamente por N8.22.C y reconciliada por la cadena N8.22.G | Sólo metadata permitida; sin filas productivas | Sin `MATERIAL_GAP` ni `UNKNOWN` pendientes en el gate final |
| Seguridad/observabilidad | Revalidada por N8.22.F + N8.22.G | Metadata no secreta/read-only | `PASS`, secretos expuestos `0` |

## Diferencias esperadas aceptadas

Las diferencias de branch, región, nombres DEV-only/PROD-only justificados, health-check version-aware y separación de proyecto/frontend son diferencias de entorno o versión. No representan una deuda material pendiente para la equivalencia de staging porque la cadena de evidencia final demuestra que no queda `MATERIAL_GAP` ni `UNKNOWN` sin resolver.

No se exige igualdad textual de toda configuración entre Desarrollo y Producción. Se exige equivalencia material del contrato y clasificación explícita de toda diferencia conforme a `EQUIVALENT | EXPECTED_ENV_DIFFERENCE | MATERIAL_GAP | UNKNOWN`.

## REVIEW_FIRST y gate causal

El cierre de `N8.22.G` fue revalidado con `P0=0`, `P1=0`, `P2=0`, `MATERIAL_GAP=0` y `UNKNOWN=0`. El bloqueo previo `RENDER_DEV_ENV_NAME_PARITY_INCOMPLETE` quedó resuelto mediante inventario sanitizado de nombres del servicio DEV, sin leer ni persistir valores secretos.

Entre la evidencia reconciliada y el candidate head usado para `N8.22.G`, el único cambio posterior fue documental de gobernanza en `docs/VAEP_AUTHORITY.md`; no hubo delta de producto, runtime, frontend, backend, schema o configuración funcional. Por ello los gates dirigidos previos conservan equivalencia causal y no se fabricó un rerun irrelevante.

## Evidencia causal

- `vaep/evidence/receipts/N8.22.A_LISTO_REAL_20260916T162800Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.B_LISTO_REAL_20260916T162930Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.C_LISTO_REAL_20260916T163050Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.D_LISTO_REAL_20260916T163210Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.E_LISTO_REAL_20260916T163330Z_SUP00.json`
- `vaep/evidence/receipts/N8.22.F_LISTO_REAL_20260916T164432Z_SUP36.json`
- `vaep/evidence/reconciliations/N8.22.G_OWNER_CONTROL_PLANE_EVIDENCE_20260916T172800Z.json`
- `vaep/evidence/reconciliations/N8.22.G_SUP48_FULL_BLOCK_REVALIDATION_20260916T181600Z.json`
- `vaep/evidence/reconciliations/N8.22.G_SUP12_REVALIDATION_20260916T182200Z.json`
- `vaep/evidence/reconciliations/N8.22.G_OWNER_RENDER_ENV_NAME_INVENTORY_20260916T182900Z.json`
- `vaep/evidence/reconciliations/N8.22.G_OWNER_EVIDENCE_RECONCILED_20260916T183900Z.json`
- `vaep/evidence/receipts/N8.22.G_LISTO_REAL_20260916T185302Z_SUP36.json`

## Dictamen

`N8.22` cumple el contrato de `STAGING_EQUIVALENT`: todas las diferencias conocidas están clasificadas, no queda diferencia material sin resolver, no queda unknown pendiente, y la certificación se obtuvo sin modificar Producción ni exponer secretos.
