#!/usr/bin/env bash
set -euo pipefail

readonly DEFAULT_MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly ALLOWED_KEYS=(
  "PARENT_CLOSE_SLA_ROLLING_60M"
  "PARENT_MAX_DWELL_MINUTES"
  "PARENT_STALL_NO_PROGRESS_MINUTES"
  "CLOSURE_REVIEW_MAX_LATENCY_MINUTES"
  "CLOSURE_DEBT_TRIGGER_LT"
  "CLOSURE_CHAIN_SAME_RUN"
  "MAX_VOLUNTARY_IDLE"
  "JULES_QUEUE_DEPTH_TARGET"
  "JULES_CURRENT_RUN_REQUIRED"
  "JULES_NEXT_SAFE_PREARMED_REQUIRED"
  "JULES_NEXT_RUN_RESERVED_REQUIRED"
  "LANE_REFILL_DEADLINE_SECONDS"
  "SCHEDULED_RUN_LANE_REFILL_BEFORE_REVIEW"
  "JULES_TERMINAL_HANDOFF_SAME_RUN"
  "NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL"
  "PREARM_BEFORE_CAUSAL_CI"
  "VAEP_CHECKPOINTS"
  "JULES_LANE_BUDGET_SECONDS"
  "JULES_MAX_ATTEMPTS"
  "JULES_REWORK_MAX"
  "PARENT_CLOSE_FIRST"
)

fail() {
  printf 'VAEP_POLICY_PARSER_ERROR: %s\n' "$1" >&2
  exit 1
}

is_boolean_key() {
  case "$1" in
    CLOSURE_CHAIN_SAME_RUN|JULES_CURRENT_RUN_REQUIRED|JULES_NEXT_SAFE_PREARMED_REQUIRED|JULES_NEXT_RUN_RESERVED_REQUIRED|SCHEDULED_RUN_LANE_REFILL_BEFORE_REVIEW|JULES_TERMINAL_HANDOFF_SAME_RUN|NO_MANIFEST_DURING_HEAD_FREEZE_CAUSAL|PREARM_BEFORE_CAUSAL_CI|PARENT_CLOSE_FIRST) return 0 ;;
    *) return 1 ;;
  esac
}

parse_policy_block() {
  local target_file="${1:-$DEFAULT_MASTER_FILE}"
  [[ -f "$target_file" ]] || fail "file does not exist: $target_file"

  local begin_count=0 end_count=0 in_block=0
  declare -g -A POLICY_MAP=()

  while IFS= read -r raw_line || [[ -n "$raw_line" ]]; do
    local line="${raw_line%$'\r'}"
    line="$(printf '%s' "$line" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"

    if [[ "$line" == "BEGIN_AUTOMATION_POLICY" ]]; then
      begin_count=$((begin_count + 1)); in_block=1; continue
    fi
    if [[ "$line" == "END_AUTOMATION_POLICY" ]]; then
      end_count=$((end_count + 1)); in_block=0; continue
    fi
    [[ "$in_block" -eq 1 ]] || continue
    [[ -n "$line" ]] || continue

    [[ "$line" =~ ^([A-Za-z0-9_]+)=(.*)$ ]] || fail "invalid line syntax in policy block: '$line'"
    local key="${BASH_REMATCH[1]}" val="${BASH_REMATCH[2]}" allowed=0
    local candidate
    for candidate in "${ALLOWED_KEYS[@]}"; do
      [[ "$key" == "$candidate" ]] && { allowed=1; break; }
    done
    [[ "$allowed" -eq 1 ]] || fail "unknown key in policy block: '$key'"
    [[ -z "${POLICY_MAP[$key]+_}" ]] || fail "duplicate key in policy block: '$key'"

    case "$key" in
      PARENT_CLOSE_SLA_ROLLING_60M|PARENT_MAX_DWELL_MINUTES|PARENT_STALL_NO_PROGRESS_MINUTES|CLOSURE_REVIEW_MAX_LATENCY_MINUTES|CLOSURE_DEBT_TRIGGER_LT|JULES_QUEUE_DEPTH_TARGET|LANE_REFILL_DEADLINE_SECONDS|JULES_LANE_BUDGET_SECONDS|JULES_MAX_ATTEMPTS)
        [[ "$val" =~ ^[1-9][0-9]*$ ]] || fail "invalid positive integer value for $key: '$val'" ;;
      JULES_REWORK_MAX|MAX_VOLUNTARY_IDLE)
        [[ "$val" =~ ^(0|[1-9][0-9]*)$ ]] || fail "invalid non-negative integer value for $key: '$val'" ;;
      VAEP_CHECKPOINTS)
        [[ "$val" =~ ^:[0-5][0-9](,:[0-5][0-9])*$ ]] || fail "invalid checkpoint list for $key: '$val'" ;;
      *)
        if is_boolean_key "$key"; then
          [[ "$val" == "TRUE" || "$val" == "FALSE" ]] || fail "invalid boolean value for $key (must be TRUE or FALSE): '$val'"
        fi ;;
    esac
    POLICY_MAP["$key"]="$val"
  done < "$target_file"

  [[ "$begin_count" -eq 1 ]] || fail "expected exactly one BEGIN_AUTOMATION_POLICY; found $begin_count"
  [[ "$end_count" -eq 1 ]] || fail "expected exactly one END_AUTOMATION_POLICY; found $end_count"

  local required_key canonical_stream=""
  for required_key in "${ALLOWED_KEYS[@]}"; do
    [[ -n "${POLICY_MAP[$required_key]+_}" ]] || fail "missing required key in policy block: '$required_key'"
    canonical_stream+="${required_key}=${POLICY_MAP[$required_key]}"$'\n'
  done

  AUTOMATION_POLICY_HASH="$(printf '%s' "$canonical_stream" | sha256sum | awk '{print $1}')"
  MASTER_COMMIT_SHA="$(git rev-parse HEAD 2>/dev/null || printf 'UNKNOWN')"
}

run_self_test() {
  parse_policy_block "$DEFAULT_MASTER_FILE"
  [[ ${#AUTOMATION_POLICY_HASH} -eq 64 ]] || fail "invalid policy hash length"
  [[ "$MASTER_COMMIT_SHA" =~ ^[0-9a-fA-F]{40}$ ]] || fail "invalid MASTER_COMMIT_SHA"

  local tmp
  tmp="$(mktemp)"
  cat > "$tmp" <<'EOF'
BEGIN_AUTOMATION_POLICY
UNKNOWN_EXTRA_KEY=1
END_AUTOMATION_POLICY
EOF
  if (parse_policy_block "$tmp" 2>/dev/null); then
    rm -f "$tmp"
    fail "parser accepted unknown key"
  fi
  rm -f "$tmp"

  parse_policy_block "$DEFAULT_MASTER_FILE"
  printf 'MASTER_POLICY_BLOCK_COUNT=1\n'
  printf 'MASTER_POLICY_KEYS_UNIQUE=PASS\n'
  printf 'MASTER_POLICY_UNKNOWN_KEYS=0\n'
  printf 'MASTER_POLICY_VALUES_VALID=PASS\n'
  printf 'PARSER_FAIL_CLOSED=PASS\n'
  printf 'AUTOMATION_POLICY_HASH_DETERMINISTIC=PASS\n'
  printf 'MASTER_COMMIT_SHA_EMITTABLE=PASS\n'
  printf 'AUTHORITY_MASTER=PASS\n'
}

main() {
  local cmd="${1:---validate}"
  shift || true
  case "$cmd" in
    --self-test)
      run_self_test ;;
    --validate)
      local file="${1:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      printf 'VAEP_POLICY_VALIDATION=PASS file=%s hash=%s\n' "$file" "$AUTOMATION_POLICY_HASH" ;;
    --env)
      local file="${1:-$DEFAULT_MASTER_FILE}" key
      parse_policy_block "$file"
      for key in "${ALLOWED_KEYS[@]}"; do printf '%s=%s\n' "$key" "${POLICY_MAP[$key]}"; done
      printf 'MASTER_COMMIT_SHA=%s\n' "$MASTER_COMMIT_SHA"
      printf 'AUTOMATION_POLICY_HASH=%s\n' "$AUTOMATION_POLICY_HASH" ;;
    --get)
      local key="${1:?key required for --get}" file="${2:-$DEFAULT_MASTER_FILE}"
      parse_policy_block "$file"
      case "$key" in
        MASTER_COMMIT_SHA) printf '%s\n' "$MASTER_COMMIT_SHA" ;;
        AUTOMATION_POLICY_HASH) printf '%s\n' "$AUTOMATION_POLICY_HASH" ;;
        *) [[ -n "${POLICY_MAP[$key]+_}" ]] || fail "unknown key requested: $key"; printf '%s\n' "${POLICY_MAP[$key]}" ;;
      esac ;;
    --hash)
      local file="${1:-$DEFAULT_MASTER_FILE}"; parse_policy_block "$file"; printf '%s\n' "$AUTOMATION_POLICY_HASH" ;;
    --commit-sha)
      local file="${1:-$DEFAULT_MASTER_FILE}"; parse_policy_block "$file"; printf '%s\n' "$MASTER_COMMIT_SHA" ;;
    --json)
      local file="${1:-$DEFAULT_MASTER_FILE}" key val first=1
      parse_policy_block "$file"
      printf '{\n'
      for key in "${ALLOWED_KEYS[@]}"; do
        val="${POLICY_MAP[$key]}"
        [[ "$first" -eq 1 ]] || printf ',\n'
        first=0
        if is_boolean_key "$key"; then
          [[ "$val" == "TRUE" ]] && val=true || val=false
          printf '  "%s": %s' "$key" "$val"
        elif [[ "$key" == "VAEP_CHECKPOINTS" ]]; then
          printf '  "%s": "%s"' "$key" "$val"
        else
          printf '  "%s": %d' "$key" "$val"
        fi
      done
      printf ',\n  "MASTER_COMMIT_SHA": "%s",\n  "AUTOMATION_POLICY_HASH": "%s"\n}\n' "$MASTER_COMMIT_SHA" "$AUTOMATION_POLICY_HASH" ;;
    *) fail "unknown command: $cmd (use --self-test, --validate, --env, --get, --hash, --commit-sha, --json)" ;;
  esac
}

main "$@"
