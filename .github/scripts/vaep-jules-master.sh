#!/usr/bin/env bash
set -euo pipefail

# Single operational Jules entrypoint.
# ALL operational rules come from docs/VAEP_AUTHORITY.md.
readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly WORKER=".github/scripts/vaep-jules-worker.sh"
readonly CONTROL_STATE_FILE="vaep/control/dispatch-admission.json"

test -f "$MASTER_FILE"
test -f "$PARSER"
test -f "$WORKER"

manifest_count_action() {
  local count="${1:?manifest count required}"
  case "$count" in
    0) printf 'NO_OP\n'; return 0 ;;
    1) printf 'ADMIT\n'; return 0 ;;
    *) printf 'FAIL_CLOSED\n'; return 21 ;;
  esac
}

admission_state_action() {
  local state_file="${1:?admission state file required}"
  if [[ ! -f "$state_file" ]]; then
    printf 'INVALID\n'
    return 72
  fi
  if ! jq -e '
    type == "object" and
    ((keys | sort) == ["allowExistingActiveSessions","newDispatchAdmission","reason","updatedAtUtc"]) and
    (.newDispatchAdmission == "FROZEN" or .newDispatchAdmission == "OPEN") and
    (.allowExistingActiveSessions == true) and
    (.reason | type == "string" and length > 0) and
    (.updatedAtUtc | type == "string" and length > 0)
  ' "$state_file" >/dev/null 2>&1; then
    printf 'INVALID\n'
    return 72
  fi

  case "$(jq -r '.newDispatchAdmission' "$state_file")" in
    OPEN) printf 'OPEN\n'; return 0 ;;
    FROZEN) printf 'FROZEN\n'; return 75 ;;
    *) printf 'INVALID\n'; return 72 ;;
  esac
}

is_active_jules_state() {
  case "${1:-UNKNOWN}" in
    QUEUED|PLANNING|IN_PROGRESS|AWAITING_USER_FEEDBACK|AWAITING_PLAN_APPROVAL) return 0 ;;
    *) return 1 ;;
  esac
}

write_timeout_result() {
  local output="${1:?output required}"
  local dispatch_id="${2:?dispatch required}"
  local task_id="${3:?task required}"
  local task_attempt="${4:-0}"
  local session_name="${5:-}"
  local started_at="${6:-}"
  local timed_out_at="${7:?timeout timestamp required}"
  local stop_action="${8:-NOT_ATTEMPTED}"
  local before_state="${9:-UNKNOWN}"
  local after_state="${10:-UNKNOWN}"
  local attempt_consumed=false
  [[ -n "$session_name" ]] && attempt_consumed=true

  jq -n \
    --arg authority "MASTER" \
    --arg masterFile "$MASTER_FILE" \
    --arg masterCommitSha "$MASTER_COMMIT_SHA" \
    --arg policyHash "$AUTOMATION_POLICY_HASH" \
    --arg workerId "${WORKER_ID:-JULES_A}" \
    --arg dispatchId "$dispatch_id" \
    --arg taskId "$task_id" \
    --arg session "$session_name" \
    --arg startedAt "$started_at" \
    --arg timedOutAt "$timed_out_at" \
    --arg safeRemoteStopAction "$stop_action" \
    --arg beforeState "$before_state" \
    --arg afterState "$after_state" \
    --argjson taskAttempt "$task_attempt" \
    --argjson attemptConsumed "$attempt_consumed" \
    --argjson laneBudgetSeconds "$JULES_LANE_BUDGET_SECONDS" \
    --argjson parentListoTargetRolling60 "$PARENT_LISTO_TARGET_ROLLING_60" \
    --argjson parentMaxDwellMinutes "$PARENT_MAX_DWELL_MINUTES" \
    '{authority:$authority,masterFile:$masterFile,masterCommitSha:$masterCommitSha,policyHash:$policyHash,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,taskAttempt:$taskAttempt,session:$session,state:"JULES_LANE_BUDGET_EXCEEDED",attemptConsumed:$attemptConsumed,laneBudgetSeconds:$laneBudgetSeconds,parentListoTargetRolling60:$parentListoTargetRolling60,parentMaxDwellMinutes:$parentMaxDwellMinutes,safeRemoteStopAttempted:($session!=""),safeRemoteStopAction:$safeRemoteStopAction,remoteStateBefore:$beforeState,remoteStateAfter:$afterState,ownershipRevoked:true,superseded:true,lateResultAutoIntegrationDenied:true,laneReleased:true,startedAt:$startedAt,timedOutAt:$timedOutAt,patchPresent:false,controllerHandoff:"QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY",falseListoProhibited:true,numericProtocolLabelsProhibited:true}' > "$output"
}

# Safe parsing without source or eval. Capture parser status explicitly so
# process-substitution cannot mask a fail-closed parser error.
if ! policy_env="$(bash "$PARSER" --env "$MASTER_FILE")"; then
  printf 'VAEP MASTER policy parser failed; refusing runtime startup.\n' >&2
  exit 68
fi
while IFS='=' read -r key val; do
  case "$key" in
    PARENT_CLOSE_SLA_ROLLING_60M) PARENT_LISTO_TARGET_ROLLING_60="$val" ;;
    PARENT_MAX_DWELL_MINUTES) PARENT_MAX_DWELL_MINUTES="$val" ;;
    PARENT_STALL_NO_PROGRESS_MINUTES) PARENT_STALL_NO_PROGRESS_MINUTES="$val" ;;
    MAX_VOLUNTARY_IDLE) MAX_VOLUNTARY_IDLE="$val" ;;
    VAEP_CHECKPOINTS) VAEP_CHECKPOINTS="$val" ;;
    JULES_LANE_BUDGET_SECONDS) JULES_LANE_BUDGET_SECONDS="$val" ;;
    JULES_MAX_ATTEMPTS) JULES_MAX_ATTEMPTS="$val" ;;
    JULES_REWORK_MAX) JULES_REWORK_MAX="$val" ;;
    PARENT_CLOSE_FIRST) PARENT_CLOSE_FIRST="$val" ;;
    AUTOMATION_POLICY_HASH) AUTOMATION_POLICY_HASH="$val" ;;
    MASTER_COMMIT_SHA) MASTER_COMMIT_SHA="$val" ;;
  esac
done <<< "$policy_env"

: "${PARENT_LISTO_TARGET_ROLLING_60:?MASTER policy parser did not emit PARENT_CLOSE_SLA_ROLLING_60M}"
: "${PARENT_MAX_DWELL_MINUTES:?MASTER policy parser did not emit PARENT_MAX_DWELL_MINUTES}"
: "${PARENT_STALL_NO_PROGRESS_MINUTES:?MASTER policy parser did not emit PARENT_STALL_NO_PROGRESS_MINUTES}"
: "${MAX_VOLUNTARY_IDLE:?MASTER policy parser did not emit MAX_VOLUNTARY_IDLE}"
: "${VAEP_CHECKPOINTS:?MASTER policy parser did not emit VAEP_CHECKPOINTS}"
: "${JULES_LANE_BUDGET_SECONDS:?MASTER policy parser did not emit JULES_LANE_BUDGET_SECONDS}"
: "${JULES_MAX_ATTEMPTS:?MASTER policy parser did not emit JULES_MAX_ATTEMPTS}"
: "${JULES_REWORK_MAX:?MASTER policy parser did not emit JULES_REWORK_MAX}"
: "${PARENT_CLOSE_FIRST:?MASTER policy parser did not emit PARENT_CLOSE_FIRST}"
: "${AUTOMATION_POLICY_HASH:?MASTER policy parser did not emit AUTOMATION_POLICY_HASH}"
: "${MASTER_COMMIT_SHA:?MASTER policy parser did not emit MASTER_COMMIT_SHA}"

readonly PARENT_LISTO_TARGET_ROLLING_60
readonly PARENT_MAX_DWELL_MINUTES
readonly PARENT_STALL_NO_PROGRESS_MINUTES
readonly MAX_VOLUNTARY_IDLE
readonly VAEP_CHECKPOINTS
readonly JULES_LANE_BUDGET_SECONDS
readonly JULES_MAX_ATTEMPTS
readonly JULES_REWORK_MAX
readonly PARENT_CLOSE_FIRST
readonly AUTOMATION_POLICY_HASH
readonly MASTER_COMMIT_SHA

if [[ "${1:-}" == "--static-self-test" ]]; then
  bash "$PARSER" --self-test >/dev/null
  [[ "$(manifest_count_action 0)" == "NO_OP" ]]
  [[ "$(manifest_count_action 1)" == "ADMIT" ]]
  set +e
  multi_action="$(manifest_count_action 2)"
  multi_rc=$?
  set -e
  [[ "$multi_action" == "FAIL_CLOSED" && "$multi_rc" -eq 21 ]]

  admission_tmp="$(mktemp -d)"
  jq -n '{newDispatchAdmission:"FROZEN",allowExistingActiveSessions:true,reason:"SELFTEST",updatedAtUtc:"2026-01-01T00:00:00Z"}' > "$admission_tmp/frozen.json"
  jq -n '{newDispatchAdmission:"OPEN",allowExistingActiveSessions:true,reason:"SELFTEST",updatedAtUtc:"2026-01-01T00:00:00Z"}' > "$admission_tmp/open.json"
  jq -n '{newDispatchAdmission:"BROKEN",allowExistingActiveSessions:true,reason:"SELFTEST",updatedAtUtc:"2026-01-01T00:00:00Z"}' > "$admission_tmp/invalid.json"
  jq -n '{newDispatchAdmission:"OPEN",allowExistingActiveSessions:true,reason:"SELFTEST",updatedAtUtc:"2026-01-01T00:00:00Z",extra:"NO"}' > "$admission_tmp/unknown-key.json"

  set +e
  frozen_action="$(admission_state_action "$admission_tmp/frozen.json")"; frozen_rc=$?
  open_action="$(admission_state_action "$admission_tmp/open.json")"; open_rc=$?
  invalid_action="$(admission_state_action "$admission_tmp/invalid.json")"; invalid_rc=$?
  unknown_action="$(admission_state_action "$admission_tmp/unknown-key.json")"; unknown_rc=$?
  missing_action="$(admission_state_action "$admission_tmp/missing.json")"; missing_rc=$?
  set -e

  [[ "$frozen_action" == "FROZEN" && "$frozen_rc" -eq 75 ]]
  [[ "$open_action" == "OPEN" && "$open_rc" -eq 0 ]]
  [[ "$invalid_action" == "INVALID" && "$invalid_rc" -eq 72 ]]
  [[ "$unknown_action" == "INVALID" && "$unknown_rc" -eq 72 ]]
  [[ "$missing_action" == "INVALID" && "$missing_rc" -eq 72 ]]
  rm -rf "$admission_tmp"

  is_active_jules_state IN_PROGRESS
  ! is_active_jules_state COMPLETED
  phase3_tmp="$(mktemp)"
  write_timeout_result "$phase3_tmp" "SELFTEST-DISPATCH" "N0.0.H.SELFTEST" 1 "sessions/123" "2026-01-01T00:00:00Z" "2026-01-01T00:18:00Z" "STOP_SIGNAL_SENT" "IN_PROGRESS" "IN_PROGRESS"
  jq -e '.state=="JULES_LANE_BUDGET_EXCEEDED" and .attemptConsumed==true and .ownershipRevoked==true and .superseded==true and .lateResultAutoIntegrationDenied==true and .laneReleased==true' "$phase3_tmp" >/dev/null
  rm -f "$phase3_tmp"
  [[ "$PARENT_LISTO_TARGET_ROLLING_60" =~ ^[1-9][0-9]*$ ]]
  [[ "$PARENT_MAX_DWELL_MINUTES" =~ ^[1-9][0-9]*$ ]]
  [[ "$PARENT_STALL_NO_PROGRESS_MINUTES" =~ ^[1-9][0-9]*$ ]]
  [[ "$MAX_VOLUNTARY_IDLE" =~ ^(0|[1-9][0-9]*)$ ]]
  [[ "$VAEP_CHECKPOINTS" =~ ^:[0-5][0-9](,:[0-5][0-9])*$ ]]
  [[ "$JULES_LANE_BUDGET_SECONDS" =~ ^[1-9][0-9]*$ ]]
  [[ "$JULES_MAX_ATTEMPTS" =~ ^[1-9][0-9]*$ ]]
  [[ "$JULES_REWORK_MAX" =~ ^(0|[1-9][0-9]*)$ ]]
  [[ "$PARENT_CLOSE_FIRST" == "TRUE" || "$PARENT_CLOSE_FIRST" == "FALSE" ]]
  [[ ${#AUTOMATION_POLICY_HASH} -eq 64 ]]
  [[ "$MASTER_COMMIT_SHA" =~ ^[0-9a-fA-F]{40}$ ]]
  grep -q 'AUTOMATION_AUTHORITY=MASTER' "$MASTER_FILE"
  grep -q 'NUMERIC_PROTOCOL_LABELS=PROHIBITED' "$MASTER_FILE"

  active_files=(
    AGENTS.md
    docs/VAEP_AUTHORITY.md
    docs/VAEP_JULES.md
    PLAN_EJECUCION_AUTONOMA.md
    PROJECT_CONTEXT.md
    docs/CONTEXTO_CHATGPT_VAEP.md
    TASKS.md
    .github/scripts/vaep-policy-parser.sh
    .github/scripts/vaep-jules-master.sh
    .github/scripts/vaep-jules-worker.sh
  )
  while IFS= read -r f; do active_files+=("$f"); done < <(find .github/workflows -maxdepth 1 -type f -name 'vaep-*.yml' -print | sort)

  for f in "${active_files[@]}"; do
    [[ -f "$f" ]] || continue
    if grep -Eq '[vV][0-9]+(\.[0-9]+)+' "$f"; then
      printf 'VAEP MASTER invariant failed: numeric protocol label found in active source %s.\n' "$f" >&2
      exit 70
    fi
  done

  bash "$WORKER" --static-self-test >/dev/null
  printf '{"status":"ok","authority":"MASTER","masterFile":"%s","parentListoTargetRolling60":%d,"parentMaxDwellMinutes":%d,"parentStallNoProgressMinutes":%d,"maxVoluntaryIdle":%d,"checkpoints":"%s","laneBudgetSeconds":%d,"policyHash":"%s","numericProtocolLabelsProhibited":true}\n' \
    "$MASTER_FILE" "$PARENT_LISTO_TARGET_ROLLING_60" "$PARENT_MAX_DWELL_MINUTES" "$PARENT_STALL_NO_PROGRESS_MINUTES" "$MAX_VOLUNTARY_IDLE" "$VAEP_CHECKPOINTS" "$JULES_LANE_BUDGET_SECONDS" "$AUTOMATION_POLICY_HASH"
  exit 0
fi

: "${DISPATCH_PATH:?DISPATCH_PATH is required}"
: "${GITHUB_SHA:?GITHUB_SHA is required}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
: "${GITHUB_RUN_ID:?GITHUB_RUN_ID is required}"
: "${GITHUB_SERVER_URL:=https://github.com}"
: "${RUNNER_TEMP:?RUNNER_TEMP is required}"
: "${GITHUB_ENV:?GITHUB_ENV is required}"
: "${ISSUE_PREFIX:=[VAEP-JULES]}"
: "${ARTIFACT_PREFIX:=vaep-jules}"
: "${WORKER_ID:=JULES_A}"
: "${WORKER_LABEL:=Jules}"

mapfile -t manifests < <(git diff-tree --no-commit-id --name-only --diff-filter=A -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
manifest_action_rc=0
manifest_action="$(manifest_count_action "${#manifests[@]}")" || manifest_action_rc=$?
if [[ "$manifest_action" == "NO_OP" ]]; then
  printf 'VAEP MASTER NO_OP: no new dispatch manifest was added by %s for %s. No session, attempt, ownership or recovery was created.\n' "$GITHUB_SHA" "$DISPATCH_PATH"
  exit 0
fi
if [[ "$manifest_action" != "ADMIT" ]]; then
  printf 'VAEP MASTER transport invariant failed closed: expected at most one new dispatch manifest; found %d.\n' "${#manifests[@]}" >&2
  exit "$manifest_action_rc"
fi

admission_rc=0
admission_action="$(admission_state_action "$CONTROL_STATE_FILE")" || admission_rc=$?
if [[ "$admission_action" == "FROZEN" ]]; then
  printf 'VAEP MASTER admission FROZEN: rejecting new dispatch before session, attempt, ownership or recovery. Existing ACTIVE_REAL sessions remain unaffected.\n' >&2
  exit "$admission_rc"
fi
if [[ "$admission_action" != "OPEN" ]]; then
  printf 'VAEP MASTER admission state invalid or unavailable; refusing new dispatch fail-closed.\n' >&2
  exit "$admission_rc"
fi

manifest="${manifests[0]}"
dispatch_id="$(jq -r '.dispatchId // "UNKNOWN"' "$manifest")"
task_id="$(jq -r '.taskId // "UNKNOWN"' "$manifest")"
primary_base="$(jq -r '.primaryBaseHead // empty' "$manifest")"

mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
if [[ ${#changed_files[@]} -ne 1 || "${changed_files[0]}" != "$manifest" ]]; then
  printf 'VAEP MASTER transport invariant failed: dispatch commit must change exactly one file (%s).\n' "$manifest" >&2
  exit 22
fi

if [[ ! "$primary_base" =~ ^[0-9a-fA-F]{40}$ ]]; then
  printf 'VAEP MASTER transport invariant failed: invalid PRIMARY_BASE_HEAD in %s.\n' "$manifest" >&2
  exit 23
fi

dispatch_parent="$(git rev-parse "${GITHUB_SHA}^")"
if [[ "${primary_base,,}" != "${dispatch_parent,,}" ]]; then
  printf 'VAEP MASTER transport invariant failed: PRIMARY_BASE_HEAD %s is not dispatch parent %s.\n' "$primary_base" "$dispatch_parent" >&2
  exit 24
fi

original_manifest="$RUNNER_TEMP/vaep-jules-original-dispatch.json"
cp "$manifest" "$original_manifest"

transport_note="VAEP_MASTER_TRANSPORT_VERIFIED=true
MASTER_FILE=docs/VAEP_AUTHORITY.md
The trusted GitHub VAEP MASTER verified PRIMARY_BASE_HEAD=$primary_base as the exact parent of atomic dispatch commit GITHUB_SHA=$GITHUB_SHA and verified that the dispatch commit changes only $manifest. The transport-only child commit is expected and is not product-scope divergence. Read docs/VAEP_AUTHORITY.md as the only operational rule source. Ignore numeric protocol labels from historical evidence."

injected="$RUNNER_TEMP/vaep-jules-master-dispatch.json"
jq --arg transport "$transport_note" '.prompt = ($transport + "\n\n" + .prompt)' "$manifest" > "$injected"
cat "$injected" > "$manifest"
rm -f "$injected"

runtime_state="$RUNNER_TEMP/vaep-jules-runtime-state.json"
export VAEP_JULES_RUNTIME_STATE_FILE="$runtime_state"

set +e
timeout --foreground --signal=TERM --kill-after=30s "${JULES_LANE_BUDGET_SECONDS}s" bash "$WORKER"
rc=$?
set -e

if [[ "$rc" -ne 124 && "$rc" -ne 137 && "$rc" -ne 143 ]]; then
  exit "$rc"
fi

result_dir="$RUNNER_TEMP/vaep-jules-master-timeout"
mkdir -p "$result_dir"
cp "$original_manifest" "$result_dir/dispatch.json"
: > "$result_dir/changes.patch"

session_name=""
task_attempt=0
started_at=""
if [[ -f "$runtime_state" ]] && jq -e 'type == "object"' "$runtime_state" >/dev/null 2>&1; then
  cp "$runtime_state" "$result_dir/runtime-state.json"
  session_name="$(jq -r '.sessionName // empty' "$runtime_state")"
  task_attempt="$(jq -r '.taskAttempt // 0' "$runtime_state")"
  started_at="$(jq -r '.startedAt // empty' "$runtime_state")"
fi

safe_stop_action="NO_SESSION"
before_state="NO_SESSION"
after_state="NO_SESSION"
if [[ "$session_name" =~ ^sessions/[0-9]+$ ]]; then
  safe_stop_action="INSPECTION_FAILED"
  before_state="UNKNOWN"
  after_state="UNKNOWN"
  if [[ -n "${JULES_API_KEY:-}" ]]; then
    if curl --fail-with-body --silent --show-error --max-time 15 -H "x-goog-api-key: $JULES_API_KEY" "$JULES_API_BASE/$session_name" > "$result_dir/session-before-stop.json"; then
      before_state="$(jq -r '.state // "UNKNOWN"' "$result_dir/session-before-stop.json")"
      after_state="$before_state"
      if is_active_jules_state "$before_state"; then
        stop_prompt="VAEP MASTER: esta sesión excedió JULES_LANE_BUDGET_SECONDS y queda SUPERSEDED. Detén trabajo nuevo inmediatamente. No produzcas cambios adicionales, rama, PR, push, merge ni deploy. Cualquier resultado posterior queda como evidencia histórica y NO es integrable automáticamente."
        jq -n --arg prompt "$stop_prompt" '{prompt:$prompt}' > "$result_dir/stop-message.json"
        if curl --fail-with-body --silent --show-error --max-time 15 -X POST -H "Content-Type: application/json" -H "x-goog-api-key: $JULES_API_KEY" --data-binary @"$result_dir/stop-message.json" "$JULES_API_BASE/$session_name:sendMessage" >/dev/null; then
          safe_stop_action="STOP_SIGNAL_SENT"
          sleep 2
          if curl --fail-with-body --silent --show-error --max-time 15 -H "x-goog-api-key: $JULES_API_KEY" "$JULES_API_BASE/$session_name" > "$result_dir/session-after-stop.json"; then
            after_state="$(jq -r '.state // "UNKNOWN"' "$result_dir/session-after-stop.json")"
          else
            after_state="POLL_FAILED"
          fi
        else
          safe_stop_action="STOP_SIGNAL_FAILED"
        fi
      else
        safe_stop_action="ALREADY_NONACTIVE"
      fi
    fi
  else
    safe_stop_action="API_KEY_UNAVAILABLE"
  fi
fi

timed_out_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
write_timeout_result "$result_dir/result.json" "$dispatch_id" "$task_id" "$task_attempt" "$session_name" "$started_at" "$timed_out_at" "$safe_stop_action" "$before_state" "$after_state"
cp "$result_dir/result.json" "$result_dir/supersession.json"

printf 'ARTIFACT_NAME=%s-%s-throughput-stall\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
issue_json="$(jq -c '.' "$result_dir/result.json")"
printf -v body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n' \
  "VAEP $WORKER_LABEL MASTER — lane budget exceeded and session superseded." \
  "- Authority: `docs/VAEP_AUTHORITY.md`" \
  "- MASTER commit: `$MASTER_COMMIT_SHA`" \
  "- Policy hash: `$AUTOMATION_POLICY_HASH`" \
  "- Worker: `$WORKER_ID`" \
  "- Dispatch: `$dispatch_id`" \
  "- Task: `$task_id`; attempt: `$task_attempt/$JULES_MAX_ATTEMPTS`" \
  "- Session: `${session_name:-NO_SESSION}`" \
  "- Stop action: `$safe_stop_action`; before: `$before_state`; after: `$after_state`" \
  "- Workflow run: $run_url" \
  "SUPERSEDED=true; OWNERSHIP_REVOKED=true; LANE_RELEASED=true; LATE_RESULT_AUTO_INTEGRATION_DENIED=true. Structured evidence: `$issue_json`"

gh issue create --repo "$GITHUB_REPOSITORY" --title "[VAEP-JULES-SUPERSEDED] $dispatch_id" --body "$body" >/dev/null

printf 'VAEP MASTER lane budget exceeded for %s (%s). Ownership revoked, session superseded and lane released for controller failover.\n' "$dispatch_id" "$task_id" >&2
exit 124
