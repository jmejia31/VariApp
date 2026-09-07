#!/usr/bin/env bash
set -euo pipefail

readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly AUTOREFILL=".github/scripts/vaep-jules-autorefill.sh"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly BRANCH="Desarrollo"
readonly WORKERS=(JULES_A JULES_B JULES_C JULES_D)

fail() {
  echo "VAEP_CHECKPOINT_ERROR=$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
usage: vaep-checkpoint.sh --checkpoint :00|:12|:24|:36|:48 [--worker JULES_A|JULES_B|JULES_C|JULES_D]
       vaep-checkpoint.sh --self-test
EOF
}

normalize_checkpoint() {
  local raw="${1:-}"
  case "$raw" in
    :00|:12|:24|:36|:48) printf '%s\n' "$raw" ;;
    '0 * * * *') printf ':00\n' ;;
    '12 * * * *') printf ':12\n' ;;
    '24 * * * *') printf ':24\n' ;;
    '36 * * * *') printf ':36\n' ;;
    '48 * * * *') printf ':48\n' ;;
    *) return 1 ;;
  esac
}

validate_checkpoint_contract() {
  local expected actual
  [[ -f "$MASTER_FILE" && -f "$PARSER" ]] || fail "master_or_parser_missing"
  actual="$(bash "$PARSER" --get VAEP_CHECKPOINTS "$MASTER_FILE")"
  expected=':00,:12,:24,:36,:48'
  [[ "$actual" == "$expected" ]] || fail "checkpoint_policy_mismatch expected=$expected actual=$actual"
}

api() {
  gh api "$@"
}

emit_preflight() {
  local checkpoint="$1" head catalog_parent admission
  head="$(api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha')"
  catalog_parent="$(jq -r '.currentParent // "MISSING"' "$CATALOG")"
  admission="$(api "repos/$GITHUB_REPOSITORY/contents/vaep/control/dispatch-admission.json?ref=$BRANCH" --jq '.content' | tr -d '\n' | base64 -d | jq -r '.newDispatchAdmission // "MISSING"')"
  printf 'VAEP_CHECKPOINT=%s\n' "$checkpoint"
  printf 'VAEP_CHECKPOINT_HEAD=%s\n' "$head"
  printf 'VAEP_CHECKPOINT_CURRENT_PARENT=%s\n' "$catalog_parent"
  printf 'VAEP_CHECKPOINT_ADMISSION=%s\n' "$admission"
  printf 'VAEP_CHECKPOINT_POLICY=%s\n' "$(bash "$PARSER" --hash "$MASTER_FILE")"
}

emit_role_observation() {
  local checkpoint="$1"
  case "$checkpoint" in
    :00) echo 'VAEP_CHECKPOINT_ROLE=PRIMARY action=LANE_REFILL_HARD_FIRST' ;;
    :12) echo 'VAEP_CHECKPOINT_ROLE=RECOVERY_CLOSURE action=RCA_GUARDED_REFILL' ;;
    :24) echo 'VAEP_CHECKPOINT_ROLE=REVIEW_CERT_CLOSE action=REVIEW_FIRST_QUEUE_OBSERVATION' ;;
    :36) echo 'VAEP_CHECKPOINT_ROLE=WATCHDOG_CLOSURE action=LIVE_RUN_GUARD_AND_REFILL' ;;
    :48) echo 'VAEP_CHECKPOINT_ROLE=DEBT_CORRECTOR action=MATERIAL_BACKLOG_FLOOR' ;;
  esac
}

run_lane_refill() {
  local worker="$1" checkpoint="$2" output rc
  echo "VAEP_CHECKPOINT_LANE_START worker=$worker checkpoint=$checkpoint"
  set +e
  output="$(WORKER_ID="$worker" VAEP_CHECKPOINT="$checkpoint" GITHUB_REPOSITORY="$GITHUB_REPOSITORY" GH_TOKEN="$GH_TOKEN" bash "$AUTOREFILL" 2>&1)"
  rc=$?
  set -e
  printf '%s\n' "$output"
  case "$rc" in
    0) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=RESERVED_OR_ALREADY_SAFE" ;;
    78) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=NO_SAFE_NEXT" ;;
    79) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=WAIT_DISPATCH_ADMISSION" ;;
    80|81) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=WAIT_RUNTIME_STATE rc=$rc" ;;
    *) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=ERROR rc=$rc" >&2; return "$rc" ;;
  esac
}

emit_review_observation() {
  local issues ready
  issues="$(api "repos/$GITHUB_REPOSITORY/issues?state=open&per_page=100&sort=updated&direction=desc")"
  ready="$(jq '[.[]? | (.body // "") | select(contains("READY_FOR_VAEP"))] | length' <<<"$issues")"
  echo "VAEP_CHECKPOINT_REVIEW_BACKLOG=$ready"
  echo 'VAEP_CHECKPOINT_REVIEW_AUTHORITY=VAEP_ONLY action=OBSERVE_NO_AUTOINTEGRATION'
}

emit_watchdog_observation() {
  local active
  active="$(api "repos/$GITHUB_REPOSITORY/actions/runs?branch=$BRANCH&per_page=100" | jq '[.workflow_runs[]? | select((.name | test("VAEP Jules [ABCD] Trusted Secondary Worker")) and (.status=="queued" or .status=="in_progress" or .status=="pending"))] | length')"
  echo "VAEP_CHECKPOINT_LIVE_JULES_RUNS=$active"
  echo 'VAEP_CHECKPOINT_WATCHDOG_AUTHORITY=LANE_RUNTIME action=NO_DUPLICATE_STOP'
}

run_self_test() {
  validate_checkpoint_contract
  [[ "$(normalize_checkpoint ':00')" == ':00' ]]
  [[ "$(normalize_checkpoint '48 * * * *')" == ':48' ]]
  if normalize_checkpoint ':15' >/dev/null 2>&1; then
    fail 'historical_checkpoint_accepted'
  fi
  echo 'VAEP_CHECKPOINT_SELF_TEST=PASS'
}

main() {
  local checkpoint_raw='' checkpoint='' worker='' worker_arg rc=0
  if [[ "${1:-}" == '--self-test' ]]; then
    run_self_test
    exit 0
  fi

  while (($#)); do
    case "$1" in
      --checkpoint) checkpoint_raw="${2:-}"; shift 2 ;;
      --worker) worker="${2:-}"; shift 2 ;;
      --help|-h) usage; exit 0 ;;
      *) usage >&2; fail "unknown_argument=$1" ;;
    esac
  done

  validate_checkpoint_contract
  checkpoint="$(normalize_checkpoint "$checkpoint_raw")" || fail "invalid_checkpoint=$checkpoint_raw"
  command -v gh >/dev/null 2>&1 || fail 'gh_missing'
  command -v jq >/dev/null 2>&1 || fail 'jq_missing'
  [[ -n "${GITHUB_REPOSITORY:-}" ]] || fail 'GITHUB_REPOSITORY_missing'
  [[ -n "${GH_TOKEN:-}" ]] || fail 'GH_TOKEN_missing'
  [[ -f "$CATALOG" ]] || fail 'catalog_missing'

  case "$worker" in
    '') workers=("${WORKERS[@]}" ) ;;
    JULES_A|JULES_B|JULES_C|JULES_D) workers=("$worker") ;;
    *) fail "invalid_worker=$worker" ;;
  esac

  emit_role_observation "$checkpoint"
  emit_preflight "$checkpoint"
  [[ "$checkpoint" != ':24' ]] || emit_review_observation
  [[ "$checkpoint" != ':36' ]] || emit_watchdog_observation

  for worker_arg in "${workers[@]}"; do
    if ! run_lane_refill "$worker_arg" "$checkpoint"; then
      rc=1
    fi
  done
  exit "$rc"
}

main "$@"
