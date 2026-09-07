#!/usr/bin/env bash
set -Eeuo pipefail

umask 077

fail() {
  echo "[M11-RESTORE][ERROR] $*" >&2
  exit 1
}

log() {
  echo "[M11-RESTORE] $*"
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Falta el comando requerido: $1"
}

require_env() {
  local name="$1"
  [[ -n "${!name:-}" ]] || fail "Falta la variable requerida: $name"
}

for cmd in mysql tar sha256sum gpg python3 find; do
  require_cmd "$cmd"
done

for name in RESTORE_ENVIRONMENT TARGET_DB_HOST TARGET_DB_PORT TARGET_DB_NAME TARGET_DB_USER TARGET_DB_PASSWORD BACKUP_PASSPHRASE BACKUP_FILE; do
  require_env "$name"
done

[[ "${ALLOW_DESTRUCTIVE_RESTORE:-}" == "YES_M11" ]] || fail "Restore destructivo bloqueado. Define ALLOW_DESTRUCTIVE_RESTORE=YES_M11 solo para una base descartable."

RESTORE_ENV_NORMALIZED="$(printf '%s' "$RESTORE_ENVIRONMENT" | tr '[:upper:]' '[:lower:]')"
TARGET_DB_NORMALIZED="$(printf '%s' "$TARGET_DB_NAME" | tr '[:upper:]' '[:lower:]')"
TARGET_DB_SSL_MODE="${TARGET_DB_SSL_MODE:-PREFERRED}"
TARGET_DB_SSL_MODE="$(printf '%s' "$TARGET_DB_SSL_MODE" | tr '[:lower:]' '[:upper:]')"

case "$RESTORE_ENV_NORMALIZED" in
  ci|desarrollo-descartable|development-disposable) ;;
  *) fail "M11 solo permite restore en CI o Desarrollo descartable. Entorno recibido: $RESTORE_ENVIRONMENT" ;;
esac

case "$TARGET_DB_SSL_MODE" in
  DISABLED|PREFERRED|REQUIRED|VERIFY_CA|VERIFY_IDENTITY) ;;
  *) fail "TARGET_DB_SSL_MODE no soportado: $TARGET_DB_SSL_MODE" ;;
esac

if [[ "$RESTORE_ENV_NORMALIZED" == *prod* || "$RESTORE_ENV_NORMALIZED" == *produccion* || "$TARGET_DB_NORMALIZED" == *prod* || "$TARGET_DB_NORMALIZED" == *produccion* ]]; then
  fail "Protección fail-closed: el destino parece Producción. Operación abortada."
fi

if [[ ! "$TARGET_DB_NORMALIZED" =~ (restore|restaur|drill|discard|descart|m11) ]]; then
  fail "El nombre de la base destino debe identificar explícitamente un restore/drill descartable."
fi

[[ -f "$BACKUP_FILE" ]] || fail "No existe BACKUP_FILE: $BACKUP_FILE"
CHECKSUM_FILE="${BACKUP_CHECKSUM_FILE:-$BACKUP_FILE.sha256}"
[[ -f "$CHECKSUM_FILE" ]] || fail "No existe el checksum externo: $CHECKSUM_FILE"

WORKDIR="$(mktemp -d)"
RESTORE_FILES_DIR="${RESTORE_FILES_DIR:-$PWD/m11-restored-files}"
RESTORE_REPORT_PATH="${RESTORE_REPORT_PATH:-$PWD/m11-restore-report.json}"

cleanup() {
  rm -rf "$WORKDIR"
  unset MYSQL_PWD BACKUP_PASSPHRASE TARGET_DB_PASSWORD
}
trap cleanup EXIT

log "Verificando SHA-256 del backup cifrado antes de descifrar..."
(
  cd "$(dirname "$BACKUP_FILE")"
  expected_file="$(basename "$CHECKSUM_FILE")"
  if [[ "$(dirname "$CHECKSUM_FILE")" != "$(dirname "$BACKUP_FILE")" ]]; then
    cp "$CHECKSUM_FILE" "$WORKDIR/$expected_file"
    expected_file="$WORKDIR/$expected_file"
    sha256sum -c "$expected_file"
  else
    sha256sum -c "$expected_file"
  fi
)

ARCHIVE="$WORKDIR/backup.tar.gz"
log "Descifrando backup en directorio temporal protegido..."
printf '%s' "$BACKUP_PASSPHRASE" \
  | gpg --batch --yes --no-tty --pinentry-mode loopback --passphrase-fd 0 \
      --decrypt --output "$ARCHIVE" "$BACKUP_FILE"

[[ -s "$ARCHIVE" ]] || fail "El archivo descifrado quedó vacío."
tar -C "$WORKDIR" -xzf "$ARCHIVE"
PAYLOAD="$(find "$WORKDIR" -mindepth 1 -maxdepth 1 -type d -name 'variapp-*' | head -n 1)"
[[ -n "$PAYLOAD" && -d "$PAYLOAD" ]] || fail "No se encontró el payload M11 esperado."
[[ -f "$PAYLOAD/MANIFEST.sha256" ]] || fail "El backup no contiene MANIFEST.sha256."
[[ -f "$PAYLOAD/metadata.json" ]] || fail "El backup no contiene metadata.json."
[[ -s "$PAYLOAD/database/mysql.sql" ]] || fail "El backup no contiene un dump MySQL válido."
[[ -s "$PAYLOAD/database/integrity-row-counts.tsv" ]] || fail "El backup no contiene conteos de integridad."

log "Verificando checksums internos de todos los componentes..."
(
  cd "$PAYLOAD"
  sha256sum -c MANIFEST.sha256
)

SOURCE_DB_NAME="$(python3 - "$PAYLOAD/metadata.json" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    print(json.load(f)['databaseName'])
PY
)"
FORMAT_VERSION="$(python3 - "$PAYLOAD/metadata.json" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    print(json.load(f)['formatVersion'])
PY
)"
EXPECTED_TABLE_COUNT="$(python3 - "$PAYLOAD/metadata.json" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    print(json.load(f)['baseTableCount'])
PY
)"
EXPECTED_MIGRATION_COUNT="$(python3 - "$PAYLOAD/metadata.json" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    print(json.load(f)['efMigrationCount'])
PY
)"

[[ "$FORMAT_VERSION" == "M11.1" ]] || fail "Versión de backup no soportada: $FORMAT_VERSION"
[[ "$(printf '%s' "$SOURCE_DB_NAME" | tr '[:upper:]' '[:lower:]')" != "$TARGET_DB_NORMALIZED" ]] || fail "La base destino no puede ser la misma base origen."

export MYSQL_PWD="$TARGET_DB_PASSWORD"
MYSQL=(mysql --protocol=TCP -h "$TARGET_DB_HOST" -P "$TARGET_DB_PORT" -u "$TARGET_DB_USER" --default-character-set=utf8mb4 "--ssl-mode=$TARGET_DB_SSL_MODE")

log "Validando servidor MySQL destino usando SSL_MODE=$TARGET_DB_SSL_MODE..."
"${MYSQL[@]}" --batch --skip-column-names -e 'SELECT 1;' | grep -qx '1' || fail "No se pudo conectar al servidor destino."

[[ "$TARGET_DB_NAME" =~ ^[A-Za-z0-9_]+$ ]] || fail "TARGET_DB_NAME contiene caracteres no permitidos."

log "Recreando exclusivamente la base descartable '$TARGET_DB_NAME'..."
"${MYSQL[@]}" -e "DROP DATABASE IF EXISTS \`$TARGET_DB_NAME\`; CREATE DATABASE \`$TARGET_DB_NAME\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

log "Restaurando dump MySQL..."
"${MYSQL[@]}" "$TARGET_DB_NAME" < "$PAYLOAD/database/mysql.sql"
MYSQL_DB=("${MYSQL[@]}" "$TARGET_DB_NAME")

ACTUAL_TABLE_COUNT="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE';" | head -n 1)"
[[ "$ACTUAL_TABLE_COUNT" == "$EXPECTED_TABLE_COUNT" ]] || fail "Cantidad de tablas inconsistente: esperado=$EXPECTED_TABLE_COUNT actual=$ACTUAL_TABLE_COUNT"

ACTUAL_MIGRATION_COUNT="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT IF(EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='__EFMigrationsHistory'), (SELECT COUNT(*) FROM __EFMigrationsHistory), 0);" | head -n 1)"
[[ "$ACTUAL_MIGRATION_COUNT" == "$EXPECTED_MIGRATION_COUNT" ]] || fail "Historial EF inconsistente: esperado=$EXPECTED_MIGRATION_COUNT actual=$ACTUAL_MIGRATION_COUNT"

log "Comparando conteos exactos de todas las tablas..."
while IFS=$'\t' read -r table_name expected_count; do
  [[ -n "$table_name" ]] || continue
  [[ "$table_name" =~ ^[A-Za-z0-9_]+$ ]] || fail "Nombre de tabla inesperado en manifest: $table_name"
  actual_count="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM \`$table_name\`;" | head -n 1)"
  [[ "$actual_count" == "$expected_count" ]] || fail "Conteo inconsistente en $table_name: esperado=$expected_count actual=$actual_count"
done < "$PAYLOAD/database/integrity-row-counts.tsv"

log "Extrayendo configuración, documentos y referencias a un directorio seguro de restore..."
rm -rf "$RESTORE_FILES_DIR"
mkdir -p "$RESTORE_FILES_DIR"
chmod 700 "$RESTORE_FILES_DIR"
for dir in configuration repository-docs references; do
  if [[ -d "$PAYLOAD/$dir" ]]; then
    cp -a "$PAYLOAD/$dir" "$RESTORE_FILES_DIR/$dir"
  fi
done
cp -p "$PAYLOAD/metadata.json" "$PAYLOAD/ASSET_INVENTORY.md" "$RESTORE_FILES_DIR/"

python3 - "$RESTORE_REPORT_PATH" <<PY
import json, sys
from datetime import datetime, timezone
report = {
    "formatVersion": "M11.1",
    "restoredAtUtc": datetime.now(timezone.utc).isoformat(),
    "sourceDatabase": "${SOURCE_DB_NAME}",
    "targetDatabase": "${TARGET_DB_NAME}",
    "restoreEnvironment": "${RESTORE_ENVIRONMENT}",
    "targetDatabaseSslMode": "${TARGET_DB_SSL_MODE}",
    "baseTableCount": int("${ACTUAL_TABLE_COUNT}"),
    "efMigrationCount": int("${ACTUAL_MIGRATION_COUNT}"),
    "checksumsVerified": True,
    "allTableRowCountsVerified": True,
    "configurationExtracted": True,
    "repositoryDocsExtracted": True,
    "externalAssetReferencesExtracted": True,
    "productionTouched": False,
    "status": "SUCCESS"
}
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump(report, f, ensure_ascii=False, indent=2)
    f.write("\n")
PY
chmod 600 "$RESTORE_REPORT_PATH"

log "Restore M11 validado correctamente en '$TARGET_DB_NAME'."
printf 'M11_RESTORE_REPORT=%s\n' "$RESTORE_REPORT_PATH"
printf 'M11_RESTORED_FILES=%s\n' "$RESTORE_FILES_DIR"
