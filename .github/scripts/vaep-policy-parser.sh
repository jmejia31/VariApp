#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# VAEP Master Automation Policy Parser
#
# Sole authority: docs/VAEP_AUTHORITY.md (MASTER)
# Extracts and validates the canonical machine-readable block:
#   BEGIN_AUTOMATION_POLICY ... END_AUTOMATION_POLICY
#
# Rules:
# - Fail-closed: missing key => FAIL, duplicate key => FAIL,
#   unknown key => FAIL, invalid value => FAIL.
# - No eval, no source, no Markdown interpretation.
# - Computes deterministic SHA-256 hash over normalized block lines.
# - Emits MASTER_COMMIT_SHA and AUTOMATION_POLICY_HASH.
# ==============================================================================

readonly DEFAULT_MASTER_FILE="docs/VAEP_AUTHORITY.md"

readonly ALLOWED_KEYS=(
  "PARENT_CLOSE_SLA_ROLLING_60M"
  "PARENT_MAX_DWELL_MINUTES"
  "JULES_LANE_BUDGET_SECONDS"
  "JULES_MAX_ATTEMPTS"
  "JULES_REWORK_MAX"
  "PARENT_CLOSE_FIRST"
)

fail() {
  printf 'VAEP_POLICY_PARSER_ERROR: %s\n' "$1" >&2
  exit 1
}

# Parse policy file into internal state
parse_policy_block() {
  local target_file="${1:-$DEFAULT_MASTER_FILE}"

  if [[ ! -f "$target_file" ]]; then
    fail "file does not exist: $target_file"
  fi

  local begin_count end_count in_block
  begin_count=0
  end_count=0
  in_block=0

  # Arrays to hold extracted lines and keys
  EXTRACTED_LINES=()
  EXTRACTED_KEYS=()
  declare -g -A POLICY_MAP=()

  # Read file line by line, stripping carriage returns
  while IFS= read -r raw_line || [[ -n "$raw_line" ]]; do
    local line="${raw_line%$'\r'}"
    # Strip leading and trailing whitespace
    line="$(echo "$line" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"

    if [[ "$line" == "BEGIN_AUTOMATION_POLICY" ]]; then
      begin_count=$((begin_count + 1))
      in_block=1
      continue
    fi

    if [[ "$line" == "END_AUTOMATION_POLICY" ]]; then
      end_count=$((end_count + 1))
      in_block=0
      continue
    fi

    if [[ "$in_block" -eq 1 ]]; then
      # Ignore empty lines inside block
      [[ -z "$line" ]] && continue

      if [[ ! "$line" =~ ^([A-Za-z0-9_]+)=(.*)$ ]]; then
        fail "invalid line syntax in policy block: '$line'"
      fi

      local key="${BASH_REMATCH[1]}"
      local val="${BASH_REMATCH[2]}"

      # Check for unknown key
      local is_allowed=0
      for allowed in "${ALLOWED_KEYS[@]}"; do
        if [[ "$key" == "$allowed" ]]; then
          is_allowed=1
          break
        fi
      done
      if [[ "$is_allowed" -eq 0 ]]; then
        fail "unknown key in policy block: '$key'"
      fi

      # Check for duplicate key
      if [[ -n "${POLICY_MAP[$key]+_}" ]]; then
        fail "duplicate key in policy block: '$key'"
      fi

      # Validate values strictly
      case "$key" in
        PARENT_CLOSE_SLA_ROLLING_60M|PARENT_MAX_DWELL_MINUTES|JULES_LANE_BUDGET_SECONDS|JULES_MAX_ATTEMPTS)
          if [[ ! "$val" =~ ^[1-9][0-9]*$ ]]; then
            fail "invalid positive integer value for $key: '$val'"
          fi
          ;;
        JULES_REWORK_MAX)
          if [[ ! "$val" =~ ^(0|[1-9][0-9]*)$ ]]; then
            fail "invalid non-negative integer value for $key: '$val'"
          fi
          ;;
        PARENT_CLOSE_FIRST)
          if [[ "$val" != "TRUE" && "$val" != "FALSE" ]]; then
            fail "invalid boolean value for $key (must be TRUE or FALSE): '$val'"
          fi
          ;;
      esac

      POLICY_MAP["$key"]="$val"
      EXTRACTED_KEYS+=("$key")
      EXTRACTED_LINES+=("$key=$val")
    fi
  done < "$target_file"

  # Enforce exactly one block
  if [[ "$begin_count" -ne 1 ]]; then
    fail "expected exactly one BEGIN_AUTOMATION_POLICY; found $begin_count"
  fi
  if [[ "$end_count" -ne 1 ]]; then
    fail "expected exactly one END_AUTOMATION_POLICY; found $end_count"
  fi

  # Check that all allowed keys are present
  for required_key in "${ALLOWED_KEYS[@]}"; do
    if [[ -z "${POLICY_MAP[$required_key]+_}" ]]; then
      fail "missing required key in policy block: '$required_key'"
    fi
  done

  # Calculate deterministic SHA-256 over normalized canonical lines
  # Normalized canonical representation: allowed keys in defined order
  local canonical_stream=""
  for k in "${ALLOWED_KEYS[@]}"; do
    canonical_stream+="${k}=${POLICY_MAP[$k]}"$'\n'
  done

  AUTOMATION_POLICY_HASH="$(printf '%s' "$canonical_stream" | sha256sum | awk '{print $1}')"
  MASTER_COMMIT_SHA="$(git rev-parse HEAD 2>/dev/null || echo "UNKNOWN")"
}

run_self_test() {
  local tmp_dir
  tmp_dir="$(mktemp -d 2>/dev/null || mktemp -d -t 'vaep-self-test')"
  trap 'rm -rf "${tmp_dir:-}"' EXIT

  printf 'Running VAEP Policy Parser Self-Tests...\n'

  # Test 1: Real file validation
  if ! parse_policy_block "$DEFAULT_MASTER_FILE"; then
    printf 'FAIL: Real file validation failed\n' >&2
    exit 1
  fi
  printf 'MASTER_POLICY_BLOCK_COUNT=1\n'
  printf 'MASTER_POLICY_KEYS_UNIQUE=PASS\n'
  printf 'MASTER_POLICY_UNKNOWN_KEYS=0\n'
  printf 'MASTER_POLICY_VALUES_VALID=PASS\n'

  # Test 2: Missing key test (fail-closed)
  local missing_file="$tmp_dir/missing.md"
  cat <<'EOF' > "$missing_file"
BEGIN_AUTOMATION_POLICY
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_MAX_DWELL_MINUTES=20
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
EOF
  if (parse_policy_block "$missing_file" 2>/dev/null); then
    printf 'FAIL: parser accepted missing key\n' >&2
    exit 1
  fi
  printf 'PARSER_MISSING_KEY_TEST=PASS\n'

  # Test 3: Duplicate key test (fail-closed)
  local dup_file="$tmp_dir/dup.md"
  cat <<'EOF' > "$dup_file"
BEGIN_AUTOMATION_POLICY
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_MAX_DWELL_MINUTES=20
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
JULES_MAX_ATTEMPTS=2
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
EOF
  if (parse_policy_block "$dup_file" 2>/dev/null); then
    printf 'FAIL: parser accepted duplicate key\n' >&2
    exit 1
  fi
  printf 'PARSER_DUPLICATE_KEY_TEST=PASS\n'

  # Test 4: Unknown key test (fail-closed)
  local unknown_file="$tmp_dir/unknown.md"
  cat <<'EOF' > "$unknown_file"
BEGIN_AUTOMATION_POLICY
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_MAX_DWELL_MINUTES=20
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
PARENT_CLOSE_FIRST=TRUE
UNKNOWN_EXTRA_KEY=99
END_AUTOMATION_POLICY
EOF
  if (parse_policy_block "$unknown_file" 2>/dev/null); then
    printf 'FAIL: parser accepted unknown key\n' >&2
    exit 1
  fi
  printf 'PARSER_UNKNOWN_KEY_TEST=PASS\n'

  # Test 5: Invalid value test (fail-closed)
  local invalid_file="$tmp_dir/invalid.md"
  cat <<'EOF' > "$invalid_file"
BEGIN_AUTOMATION_POLICY
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_MAX_DWELL_MINUTES=20
JULES_LANE_BUDGET_SECONDS=abc
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
EOF
  if (parse_policy_block "$invalid_file" 2>/dev/null); then
    printf 'FAIL: parser accepted non-numeric value\n' >&2
    exit 1
  fi
  printf 'PARSER_INVALID_VALUE_TEST=PASS\n'

  # Test 6: Duplicate block test (fail-closed)
  local dup_block_file="$tmp_dir/dup_block.md"
  cat <<'EOF' > "$dup_block_file"
BEGIN_AUTOMATION_POLICY
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_MAX_DWELL_MINUTES=20
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
BEGIN_AUTOMATION_POLICY
PARENT_CLOSE_SLA_ROLLING_60M=3
PARENT_MAX_DWELL_MINUTES=20
JULES_LANE_BUDGET_SECONDS=1080
JULES_MAX_ATTEMPTS=2
JULES_REWORK_MAX=1
PARENT_CLOSE_FIRST=TRUE
END_AUTOMATION_POLICY
EOF
  if (parse_policy_block "$dup_block_file" 2>/dev/null); then
    printf 'FAIL: parser accepted multiple blocks\n' >&2
    exit 1
  fi
  printf 'PARSER_FAIL_CLOSED=PASS\n'

  # Test 7: Hash determinism test
  parse_policy_block "$DEFAULT_MASTER_FILE"
  local h1="$AUTOMATION_POLICY_HASH"
  parse_policy_block "$DEFAULT_MASTER_FILE"
  local h2="$AUTOMATION_POLICY_HASH"
  if [[ "$h1" != "$h2" || ${#h1} -ne 64 ]]; then
    printf 'FAIL: policy hash is non-deterministic or invalid length: %s vs %s\n' "$h1" "$h2" >&2
    exit 1
  fi
  printf 'AUTOMATION_POLICY_HASH_DETERMINISTIC=PASS\n'

  # Test 8: Commit SHA emittable
  if [[ -z "$MASTER_COMMIT_SHA" ]]; then
    printf 'FAIL: commit sha was empty\n' >&2
    exit 1
  fi
  printf 'MASTER_COMMIT_SHA_EMITTABLE=PASS\n'
  printf 'AUTHORITY_MASTER=PASS\n'
  return 0
}

main() {
  local cmd="${1:---validate}"
  shift || true

  case "$cmd" in
    --self-test)
      run_self_test
      ;;
    --validate)
      local file="${1:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      printf 'VAEP_POLICY_VALIDATION=PASS file=%s hash=%s\n' "$file" "$AUTOMATION_POLICY_HASH"
      ;;
    --env)
      local file="${1:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      for k in "${ALLOWED_KEYS[@]}"; do
        printf '%s=%s\n' "$k" "${POLICY_MAP[$k]}"
      done
      printf 'MASTER_COMMIT_SHA=%s\n' "$MASTER_COMMIT_SHA"
      printf 'AUTOMATION_POLICY_HASH=%s\n' "$AUTOMATION_POLICY_HASH"
      ;;
    --get)
      local key="${1:?key required for --get}"
      local file="${2:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      case "$key" in
        MASTER_COMMIT_SHA) printf '%s\n' "$MASTER_COMMIT_SHA" ;;
        AUTOMATION_POLICY_HASH) printf '%s\n' "$AUTOMATION_POLICY_HASH" ;;
        *)
          if [[ -n "${POLICY_MAP[$key]+_}" ]]; then
            printf '%s\n' "${POLICY_MAP[$key]}"
          else
            fail "unknown key requested: $key"
          fi
          ;;
      esac
      ;;
    --hash)
      local file="${1:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      printf '%s\n' "$AUTOMATION_POLICY_HASH"
      ;;
    --commit-sha)
      local file="${1:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      printf '%s\n' "$MASTER_COMMIT_SHA"
      ;;
    --json)
      local file="${1:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      printf '{\n'
      for k in "${ALLOWED_KEYS[@]}"; do
        local val="${POLICY_MAP[$k]}"
        if [[ "$k" == "PARENT_CLOSE_FIRST" ]]; then
          local bool_val="true"
          [[ "$val" == "FALSE" ]] && bool_val="false"
          printf '  "%s": %s,\n' "$k" "$bool_val"
        else
          printf '  "%s": %d,\n' "$k" "$val"
        fi
      done
      printf '  "MASTER_COMMIT_SHA": "%s",\n' "$MASTER_COMMIT_SHA"
      printf '  "AUTOMATION_POLICY_HASH": "%s"\n' "$AUTOMATION_POLICY_HASH"
      printf '}\n'
      ;;
    *)
      fail "unknown command: $cmd (use --self-test, --validate, --env, --get, --hash, --commit-sha, --json)"
      ;;
  esac
}

main "$@"
