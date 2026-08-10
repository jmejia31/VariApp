# FASE M11 — Backups y restauración en Desarrollo

Fecha: 2026-08-10  
Rama exclusiva: `Desarrollo`  
Producción: **FUERA DE ALCANCE**  
Estado: **EN CERTIFICACIÓN AUTOMÁTICA**

## Objetivo

Implementar un procedimiento reproducible, cifrado, verificable y fail-closed para respaldar los activos recuperables de VariApp en Desarrollo y demostrar una restauración completa sobre un entorno MySQL descartable antes de considerar el backup utilizable.

## Activos incluidos

1. Base de datos MySQL completa mediante dump consistente.
2. Conteos exactos de filas por tabla para verificación post-restore.
3. Historial `__EFMigrationsHistory` y metadata de versión.
4. Configuración versionada segura mediante allowlist, nunca `.env`, certificados ni archivos locales de secretos.
5. Documentación versionada bajo `docs/`.
6. Referencias de imágenes `ProductoImagenes` (`Url`, `PublicId`, ámbito de producto/variante).
7. Referencias y metadata de `CompraDocumentos` (`Url`, `PublicId`, tipo, tamaño y recurso).
8. Metadata del backup, commit Git, versión MySQL, número de migraciones, número de tablas y política de retención.

Los binarios que residen en proveedores externos no se confunden con el dump SQL: sus referencias y metadata quedan inventariadas explícitamente.

## Seguridad

- `scripts/m11_backup_desarrollo.sh` acepta únicamente `desarrollo`, `development` o `ci`.
- Cualquier entorno/base que parezca Producción se rechaza antes de conectarse.
- Los secretos llegan únicamente por variables de entorno y no se escriben en metadata.
- El payload se conserva únicamente después de cifrarse con OpenPGP simétrico AES-256.
- El backup cifrado y archivos auxiliares quedan con permiso `0600`.
- Existe checksum SHA-256 externo del artefacto cifrado y manifest SHA-256 interno por archivo.
- La retención elimina exclusivamente artefactos que coincidan con el patrón autorizado de M11; no borra archivos ajenos.
- `.gitignore` bloquea artefactos de backup/restore locales.

## Restauración

`scripts/m11_restore_desarrollo.sh`:

- solo permite `ci`, `desarrollo-descartable` o `development-disposable`;
- exige `ALLOW_DESTRUCTIVE_RESTORE=YES_M11`;
- rechaza destinos que parezcan Producción;
- exige un nombre de base explícitamente descartable (`restore`, `drill`, `m11`, etc.);
- verifica checksum cifrado antes de descifrar;
- verifica todos los checksums internos antes de tocar MySQL;
- recrea exclusivamente la base descartable autorizada;
- restaura el dump;
- compara número de tablas, historial EF y conteos exactos de todas las tablas;
- extrae configuración/documentación/referencias a un directorio separado, sin sobrescribir el repositorio;
- genera un reporte JSON de restore.

## Gate M11

Workflow:

`.github/workflows/m11-backup-restore-desarrollo.yml`

La certificación automatizada ejecuta MySQL 8.4 con `sql_require_primary_key=ON`, aplica todo el historial EF, siembra registros sentinel y referencias de imagen/documento, genera un backup cifrado, prueba retención, restaura en otra base, compara todos los conteos, verifica los sentinels, arranca la API contra la base restaurada y prueba las protecciones fail-closed contra Producción/destinos no descartables.

Los números de run, resultados y artifact se incorporarán únicamente después de obtener evidencia verde real.
