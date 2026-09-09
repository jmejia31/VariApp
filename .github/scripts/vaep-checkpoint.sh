#!/usr/bin/env bash
set -euo pipefail

readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly AUTOREFILL=".github/scripts/vaep-jules-autorefill.sh"
readonly PARENT_CLOSE=".github/scripts/vaep-parent-close.sh"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly ADMISSION="vaep/control/dispatch-admission.json"
readonly BRANCH="Desarrollo"
readonly WORKERS=(J1 J2 J3 J4 J5 J6)

fail() {
  echo "VAEP_CHECKPOINT_ERROR=$1" >&2
  exit 2
}

usage() {
  cat <<'EOF'
usage: vaep-checkpoint.sh --checkpoint :00|:12|:24|:36|:48 [--worker J1|J2|J3|J4|J5|J6]
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

is_watchdog_freeze_reason() {
  [[ "${1:-}" == WATCHDOG* ]]
}

jules_api_key_for_worker() {
  case "${1:-}" in
    J1) printf '%s\n' "${J1_API_KEY:-}" ;;
    J2) printf '%s\n' "${J2_API_KEY:-}" ;;
    J3) printf '%s\n' "${J3_API_KEY:-}" ;;
    J4) printf '%s\n' "${J4_API_KEY:-}" ;;
    J5) printf '%s\n' "${J5_API_KEY:-}" ;;
    J6) printf '%s\n' "${J6_API_KEY:-}" ;;
    *) printf '\n' ;;
  esac
}

remote_watchdog_sessions_terminal() {
  local parent="$1" issues rows worker session key payload state session_count=0
  issues="$(api --paginate --slurp "repos/$GITHUB_REPOSITORY/issues?state=all&per_page=100&sort=updated&direction=desc" 2>/dev/null | jq -c 'add' 2>/dev/null)" || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=issues_unavailable' >&2
    return 1
  }
  [[ -n "$issues" ]] || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=issues_empty' >&2
    return 1
  }

  rows="$(jq -r --arg parent "$parent" '
    .[]?
    | (.body // "") as $body
    | select(($body | contains("CURRENT_PARENT=" + $parent)) or ($body | contains("- Task: `" + $parent + ".")))
    | (try ($body | capture("- Worker: `(?<worker>J[1-6]|JULES_[ABCD])`").worker) catch "") as $worker
    | ([$body | scan("sessions/[0-9]+")] | last // "") as $session
    | select($worker != "" and $session != "")
    | [$worker, $session]
    | @tsv
  ' <<<"$issues" | sort -u)"

  while IFS=$'\t' read -r worker session; do
    [[ -n "$worker" && -n "$session" ]] || continue
    session_count=$((session_count + 1))
    key="$(jules_api_key_for_worker "$worker")"
    if [[ -z "$key" ]]; then
      echo "VAEP_ADMISSION_RECONCILE=WAIT reason=remote_key_unavailable worker=$worker session=$session" >&2
      return 1
    fi
    payload="$(curl --connect-timeout 10 --max-time 20 --fail-with-body --silent --show-error \
      -H "x-goog-api-key: $key" \
      "${JULES_API_BASE:-https://jules.googleapis.com/v1alpha}/$session" 2>/dev/null)" || {
        echo "VAEP_ADMISSION_RECONCILE=WAIT reason=remote_session_unavailable worker=$worker session=$session" >&2
        return 1
      }
    state="$(jq -r '.state // "UNKNOWN"' <<<"$payload")"
    echo "VAEP_ADMISSION_REMOTE_SESSION worker=$worker session=$session state=$state"
    case "$state" in
      QUEUED|PLANNING|IN_PROGRESS|AWAITING_USER_FEEDBACK|AWAITING_PLAN_APPROVAL)
        echo "VAEP_ADMISSION_RECONCILE=WAIT reason=remote_session_active worker=$worker session=$session state=$state" >&2
        return 1
        ;;
      COMPLETED|FAILED|PAUSED) ;;
      *)
        echo "VAEP_ADMISSION_RECONCILE=WAIT reason=remote_session_state_unknown worker=$worker session=$session state=$state" >&2
        return 1
        ;;
    esac
  done <<<"$rows"

  if (( session_count == 0 )); then
    echo "VAEP_ADMISSION_RECONCILE=WAIT reason=current_parent_sessions_not_proven parent=$parent" >&2
    return 1
  fi
  return 0
}

hardening_gate_success() {
  local workflow="$1" head="$2" runs
  runs="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/$workflow/runs?branch=$BRANCH&head_sha=$head&per_page=20" 2>/dev/null)" || {
    echo "VAEP_ADMISSION_RECONCILE=WAIT reason=hardening_gate_unavailable workflow=$workflow" >&2
    return 1
  }
  if ! jq -e --arg head "$head" \
    '[.workflow_runs[]? | select(.head_sha == $head and .status == "completed" and .conclusion == "success")] | length > 0' \
    <<<"$runs" >/dev/null; then
    echo "VAEP_ADMISSION_RECONCILE=WAIT reason=hardening_gate_not_success workflow=$workflow head=$head" >&2
    return 1
  fi
  return 0
}

hardening_gates_ok() {
  local head="$1" pr
  hardening_gate_success "vaep-engine-ci.yml" "$head" || return 1
  hardening_gate_success "vaep-jules-diagnostic.yml" "$head" || return 1
  pr="$(api "repos/$GITHUB_REPOSITORY/pulls/2" 2>/dev/null)" || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=hardening_pr_unavailable' >&2
    return 1
  }
  if ! jq -e '
    .state == "open"
    and .draft == true
    and .merged == false
    and .head.ref == "Desarrollo"
    and .base.ref == "main"
  ' <<<"$pr" >/dev/null; then
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=hardening_pr_contract_not_satisfied' >&2
    return 1
  fi
  echo "VAEP_ADMISSION_HARDENING_GATES=PASS head=$head"
  return 0
}

publish_reconciled_admission() {
  local expected_head="$1" parent="$2" prior_reason="$3" expected_state="${4:-FROZEN}" reconciliation_reason="${5:-}" commit_message="${6:-chore(vaep): reopen admission after watchdog reconciliation}" payload decoded current_head current_state now open_json
  local admission_blob base_commit base_tree tree commit rc

  payload="$(api "repos/$GITHUB_REPOSITORY/contents/$ADMISSION?ref=$BRANCH" 2>/dev/null)" || return 1
  decoded="$(jq -r '.content' <<<"$payload" | tr -d '\n' | base64 -d 2>/dev/null)" || return 1
  current_state="$(jq -r '.newDispatchAdmission // empty' <<<"$decoded")"
  if [[ "$current_state" == "OPEN" ]]; then
    printf '%s\n' "$decoded"
    return 0
  fi
  [[ "$current_state" == "$expected_state" ]] || return 2
  current_head="$(api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha' 2>/dev/null)" || return 1
  [[ "$current_head" == "$expected_head" ]] || return 2
  now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  [[ -n "$reconciliation_reason" ]] || reconciliation_reason="WATCHDOG_RECONCILED: no live GitHub Jules run and all current-parent remote sessions terminal; parent=$parent; prior=$prior_reason"
  open_json="$(jq -n \
    --arg now "$now" \
    --arg reason "$reconciliation_reason" \
    '{newDispatchAdmission:"OPEN",allowExistingActiveSessions:true,reason:$reason,updatedAtUtc:$now}')"
  admission_blob="$(jq -n --arg content "$open_json" '{content:$content,encoding:"utf-8"}' | api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - --jq '.sha')" || return 1
  base_commit="$(api "repos/$GITHUB_REPOSITORY/git/commits/$expected_head" 2>/dev/null)" || return 1
  base_tree="$(jq -r '.tree.sha' <<<"$base_commit")"
  tree="$(jq -n --arg base "$base_tree" --arg admission "$admission_blob" \
    '{base_tree:$base,tree:[{path:"vaep/control/dispatch-admission.json",mode:"100644",type:"blob",sha:$admission}]}' \
    | api "repos/$GITHUB_REPOSITORY/git/trees" --method POST --input - --jq '.sha')" || return 1
  commit="$(jq -n --arg message "$commit_message" --arg tree "$tree" --arg parent "$expected_head" \
    '{message:$message,tree:$tree,parents:[$parent]}' \
    | api "repos/$GITHUB_REPOSITORY/git/commits" --method POST --input - --jq '.sha')" || return 1
  set +e
  jq -n --arg sha "$commit" '{sha:$sha,force:false}' \
    | api "repos/$GITHUB_REPOSITORY/git/refs/heads/$BRANCH" --method PATCH --input - >/dev/null 2>&1
  rc=$?
  set -e
  if (( rc == 0 )); then
    printf '%s\n' "$open_json"
    return 0
  fi
  return 2
}

reconcile_watchdog_admission() {
  local payload decoded state reason parent head live local_admission rc refreshed
  payload="$(api "repos/$GITHUB_REPOSITORY/contents/$ADMISSION?ref=$BRANCH" 2>/dev/null)" || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=admission_unavailable' >&2
    return 0
  }
  decoded="$(jq -r '.content' <<<"$payload" | tr -d '\n' | base64 -d 2>/dev/null)" || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=admission_decode_failed' >&2
    return 0
  }
  state="$(jq -r '.newDispatchAdmission // "INVALID"' <<<"$decoded")"
  if [[ "$state" == "OPEN" ]]; then
    printf '%s\n' "$decoded" > "$ADMISSION"
    echo 'VAEP_ADMISSION_RECONCILE=OPEN reason=already_open'
    return 0
  fi
  reason="$(jq -r '.reason // empty' <<<"$decoded")"
  if [[ "$state" == "CLOSED" && "$reason" == REVIEW_FIRST_DEBT_FAIL_CLOSED* ]]; then
    parent="$(jq -r '.currentParent // empty' "$CATALOG")"
    [[ -n "$parent" ]] || {
      echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=current_parent_missing' >&2
      return 0
    }
    head="$(api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha' 2>/dev/null)" || {
      echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=head_unavailable' >&2
      return 0
    }
    if ! hardening_gates_ok "$head"; then
      return 0
    fi
    if local_admission="$(publish_reconciled_admission \
      "$head" "$parent" "$reason" "CLOSED" \
      "REVIEW_FIRST_DEBT_RECONCILED: pending REVIEW_FIRST/QA_TAKEOVER remains recorded and non-blocking for independent NEXT_SAFE; parent=$parent; prior=$reason" \
      "fix(vaep): normalize review debt admission state")"; then
      printf '%s\n' "$local_admission" > "$ADMISSION"
      echo "VAEP_ADMISSION_RECONCILE=OPEN reason=review_first_debt_non_blocking parent=$parent head=$head"
    else
      rc=$?
      if (( rc == 2 )); then
        refreshed="$(api "repos/$GITHUB_REPOSITORY/contents/$ADMISSION?ref=$BRANCH" --jq '.content' 2>/dev/null | tr -d '\n' | base64 -d 2>/dev/null || true)"
        if [[ "$(jq -r '.newDispatchAdmission // empty' <<<"$refreshed")" == "OPEN" ]]; then
          printf '%s\n' "$refreshed" > "$ADMISSION"
          echo 'VAEP_ADMISSION_RECONCILE=OPEN reason=concurrent_review_debt_reconciler'
        else
          echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=review_debt_publish_race' >&2
        fi
      else
        echo "VAEP_ADMISSION_RECONCILE=WAIT reason=review_debt_publish_failed rc=$rc" >&2
      fi
    fi
    return 0
  fi
  if [[ "$state" != "FROZEN" ]] || ! is_watchdog_freeze_reason "$reason"; then
    echo "VAEP_ADMISSION_RECONCILE=WAIT reason=non_watchdog_freeze state=$state" >&2
    return 0
  fi
  parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  [[ -n "$parent" ]] || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=current_parent_missing' >&2
    return 0
  }
  live="$(api "repos/$GITHUB_REPOSITORY/actions/runs?branch=$BRANCH&per_page=100" 2>/dev/null | jq '[.workflow_runs[]? | select(.name | test("^VAEP J[1-6] Trusted Worker$")) | select(.status=="queued" or .status=="in_progress" or .status=="pending")] | length' 2>/dev/null)" || live=-1
  if (( live < 0 )); then
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=live_runs_unavailable' >&2
    return 0
  fi
  if (( live > 0 )); then
    echo "VAEP_ADMISSION_RECONCILE=WAIT reason=live_jules_runs count=$live" >&2
    return 0
  fi
  if ! remote_watchdog_sessions_terminal "$parent"; then
    return 0
  fi
  head="$(api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha' 2>/dev/null)" || {
    echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=head_unavailable' >&2
    return 0
  }
  if ! hardening_gates_ok "$head"; then
    return 0
  fi
  if local_admission="$(publish_reconciled_admission "$head" "$parent" "$reason")"; then
    printf '%s\n' "$local_admission" > "$ADMISSION"
    echo "VAEP_ADMISSION_RECONCILE=OPEN parent=$parent head=$head"
  else
    rc=$?
    if (( rc == 2 )); then
      refreshed="$(api "repos/$GITHUB_REPOSITORY/contents/$ADMISSION?ref=$BRANCH" --jq '.content' 2>/dev/null | tr -d '\n' | base64 -d 2>/dev/null || true)"
      if [[ "$(jq -r '.newDispatchAdmission // empty' <<<"$refreshed")" == "OPEN" ]]; then
        printf '%s\n' "$refreshed" > "$ADMISSION"
        echo 'VAEP_ADMISSION_RECONCILE=OPEN reason=concurrent_reconciler'
      else
        echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=publish_race' >&2
      fi
    else
      echo 'VAEP_ADMISSION_RECONCILE=WAIT reason=publish_failed' >&2
    fi
  fi
  return 0
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
    78) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=UNIQUE_WORK_EXHAUSTED" ;;
    79) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=WAIT_DISPATCH_ADMISSION" ;;
    80|81) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=WAIT_RUNTIME_STATE rc=$rc" ;;
    82) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=WAIT_CAUSAL_CI" ;;
    *) echo "VAEP_CHECKPOINT_LANE_RESULT worker=$worker result=ERROR rc=$rc" >&2; return "$rc" ;;
  esac
}

emit_review_observation() {
  local issues current_parent ready terminal_patch terminal_blocked
  issues="$(api "repos/$GITHUB_REPOSITORY/issues?state=all&per_page=100&sort=updated&direction=desc")"
  current_parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  ready="$(jq --arg parent "$current_parent" '[.[]? | (.body // "") as $b | select($b | contains("READY_FOR_VAEP")) | select($b | test("- Task: `" + $parent + "\\."))] | length' <<<"$issues")"
  terminal_patch="$(jq --arg parent "$current_parent" '[.[]? | (.body // "") as $b | select($b | contains("Terminal state: `COMPLETED`")) | select($b | contains("Patch present: `true`")) | select($b | test("- Task: `" + $parent + "\\."))] | length' <<<"$issues")"
  terminal_blocked="$(jq --arg parent "$current_parent" '[.[]? | (.body // "") as $b | select($b | test("QA_CLASSIFICATION_REQUIRED_NO_REFILL|READY_FOR_VAEP_REVIEW_FIRST_REQUIRED_NO_REFILL")) | select($b | test("- Task: `" + $parent + "\\."))] | length' <<<"$issues")"
  echo "VAEP_CHECKPOINT_REVIEW_BACKLOG=$ready"
  echo "VAEP_CHECKPOINT_TERMINAL_PATCH_RESULTS=$terminal_patch"
  echo "VAEP_CHECKPOINT_TERMINAL_BLOCKED_RESULTS=$terminal_blocked"
  echo 'VAEP_CHECKPOINT_REVIEW_AUTHORITY=VAEP_ONLY action=REVIEW_FIRST_REQUIRED_NO_AUTOINTEGRATION'
}

emit_watchdog_observation() {
  local active
  active="$(api "repos/$GITHUB_REPOSITORY/actions/runs?branch=$BRANCH&per_page=100" | jq '[.workflow_runs[]? | select((.name | test("^VAEP J[1-6] Trusted Worker$")) and (.status=="queued" or .status=="in_progress" or .status=="pending"))] | length')"
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
  is_watchdog_freeze_reason 'WATCHDOG :36 containment'
  if is_watchdog_freeze_reason 'MANUAL FREEZE'; then
    fail 'non_watchdog_freeze_reconciled'
  fi
  grep -q 'hardening_gates_ok' "$0"
  grep -q 'vaep-engine-ci.yml' "$0"
  grep -q 'vaep-jules-diagnostic.yml' "$0"
  grep -q 'REVIEW_FIRST_DEBT_RECONCILED' "$0"
  grep -q 'expected_state' "$0"
  grep -q 'J1|J2|J3|J4|J5|J6' "$0"
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
  [[ -f "$ADMISSION" ]] || fail 'admission_missing'
  [[ -f "$PARENT_CLOSE" ]] || fail 'parent_close_governor_missing'

  case "$worker" in
    '') workers=("${WORKERS[@]}" ) ;;
    J1|J2|J3|J4|J5|J6) workers=("$worker") ;;
    *) fail "invalid_worker=$worker" ;;
  esac

  reconcile_watchdog_admission
  emit_role_observation "$checkpoint"
  emit_preflight "$checkpoint"

  for worker_arg in "${workers[@]}"; do
    if ! run_lane_refill "$worker_arg" "$checkpoint"; then
      rc=1
    fi
  done
  local closure_output closure_rc closure_promoted=0
  set +e
  closure_output="$(GITHUB_REPOSITORY="$GITHUB_REPOSITORY" GH_TOKEN="$GH_TOKEN" bash "$PARENT_CLOSE" 2>&1)"
  closure_rc=$?
  set -e
  printf '%s\n' "$closure_output"
  if (( closure_rc != 0 )); then
    echo "VAEP_CHECKPOINT_CLOSURE_RESULT=ERROR rc=$closure_rc" >&2
    rc=1
  elif grep -Eq 'VAEP_PARENT_(PROMOTED|PROMOTION_ALREADY_PRESENT|PROMOTION_RACE)=true' <<<"$closure_output"; then
    closure_promoted=1
    echo "VAEP_CHECKPOINT_CLOSURE_RESULT=ADVANCE_DETECTED checkpoint=$checkpoint"
  else
    echo "VAEP_CHECKPOINT_CLOSURE_RESULT=NO_ADVANCE checkpoint=$checkpoint"
  fi

  if (( closure_promoted != 0 )); then
    echo "VAEP_CHECKPOINT_CONTINUITY_RETRY=AFTER_PARENT_ADVANCE checkpoint=$checkpoint"
    for worker_arg in "${workers[@]}"; do
      if ! run_lane_refill "$worker_arg" "$checkpoint"; then
        rc=1
      fi
    done
  fi

  echo "VAEP_CHECKPOINT_EXECUTION=COMPLETE checkpoint=$checkpoint worker_count=${#workers[@]} closure_promoted=$closure_promoted"
  [[ "$checkpoint" != ':24' ]] || emit_review_observation
  [[ "$checkpoint" != ':36' ]] || emit_watchdog_observation
  exit "$rc"
}

main "$@"
