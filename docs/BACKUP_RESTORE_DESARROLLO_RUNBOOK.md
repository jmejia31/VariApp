# Backup y restauración de Desarrollo — SOLQARYN

```text
PLATFORM=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=Desarrollo
GITHUB_ENVIRONMENT=Desarrollo
PRODUCTION_IN_SCOPE=FALSE
```

## Propósito

Mantener una capacidad permanente de backup cifrado y restore descartable para Desarrollo. Esta capacidad no pertenece a una fase numerada: es infraestructura operativa reutilizable y debe sobrevivir a cambios de roadmap.

## Variables GitHub Actions canónicas

Repositorio:
- `SOLQARYN_DESARROLLO_BACKUP_SCHEDULE_ENABLED`

Environment `Desarrollo`:
- `SOLQARYN_DESARROLLO_DB_HOST`
- `SOLQARYN_DESARROLLO_DB_PORT`
- `SOLQARYN_DESARROLLO_DB_NAME`
- `SOLQARYN_DESARROLLO_DB_USER`

## Secrets GitHub Actions canónicos

Todos viven en el environment `Desarrollo`:
- `SOLQARYN_DESARROLLO_DB_PASSWORD`
- `SOLQARYN_DESARROLLO_BACKUP_PASSPHRASE`
- `SOLQARYN_AIVEN_TOKEN`
- `SOLQARYN_DESARROLLO_DB_CONNECTION`

Los valores nunca se documentan ni se copian al repositorio.

## Environment canónico

Todo workflow que requiera credenciales de Desarrollo usa únicamente el environment GitHub `Desarrollo`.

No crear environments con nombres de proveedor, servicio, milestone o fase salvo que exista una frontera de seguridad real distinta.

## Rotación de Aiven

Cambiar de cuenta Aiven obliga a reemplazar el valor de `SOLQARYN_AIVEN_TOKEN`.

Los datos de conexión MySQL solo cambian si cambia el servicio, endpoint, usuario, contraseña o base de datos. Rotar el token API de Aiven no rota automáticamente las credenciales MySQL.

## Orden de migración segura

1. Crear/actualizar los nombres canónicos sin borrar todavía los valores antiguos.
2. Ejecutar provider proof y backup/restore drill en `Desarrollo`.
3. Confirmar PASS y restore descartable exitoso.
4. Eliminar secrets/variables/environments antiguos solo después de la validación.
5. No tocar Producción ni `main` sin autorización nueva.
