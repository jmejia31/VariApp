#!/usr/bin/env bash
set -euo pipefail

# Internal worker invoked only by .github/scripts/vaep-jules-master.sh.
# Runtime semantics are governed by docs/VAEP_AUTHORITY.md (MASTER).
readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly RUNTIME_CONTRACT="scripts/vaep/jules_runtime_contract.py"

test -f "$MASTER_FILE"
test -f "$PARSER"
test -f "$RUNTIME_CONTRACT"

if ! policy_env="$(bash "$PARSER" --env "$MASTER_FILE")"; then
  printf 'VAEP Jules worker policy parser failed; refusing runtime startup.\n' >&2
  exit 68
fi
while IFS='=' read -r key val; do
  case "$key" in
    PARENT_STALL_NO_PROGRESS_MINUTES) PARENT_STALL_NO_PROGRESS_MINUTES="$val" ;;
    MAX_VOLUNTARY_IDLE) MAX_VOLUNTARY_IDLE="$val" ;;
    VAEP_CHECKPOINTS) VAEP_CHECKPOINTS="$val" ;;
    JULES_LANE_BUDGET_SECONDS) JULES_LANE_BUDGET_SECONDS="$val" ;;
    JULES_MAX_ATTEMPTS) JULES_MAX_ATTEMPTS_PER_TASK="$val" ;;
    JULES_REWORK_MAX) JULES_REWORK_MAX="$val" ;;
    PARENT_CLOSE_FIRST) PARENT_CLOSE_FIRST="${val,,}" ;;
    AUTOMATION_POLICY_HASH) AUTOMATION_POLICY_HASH="$val" ;;
    MASTER_COMMIT_SHA) MASTER_COMMIT_SHA="$val" ;;
  esac
done <<< "$policy_env"

: "${PARENT_STALL_NO_PROGRESS_MINUTES:?MASTER policy parser did not emit PARENT_STALL_NO_PROGRESS_MINUTES}"
: "${MAX_VOLUNTARY_IDLE:?MASTER policy parser did not emit MAX_VOLUNTARY_IDLE}"
: "${VAEP_CHECKPOINTS:?MASTER policy parser did not emit VAEP_CHECKPOINTS}"
: "${JULES_LANE_BUDGET_SECONDS:?MASTER policy parser did not emit JULES_LANE_BUDGET_SECONDS}"
: "${JULES_MAX_ATTEMPTS_PER_TASK:?MASTER policy parser did not emit JULES_MAX_ATTEMPTS}"
: "${JULES_REWORK_MAX:?MASTER policy parser did not emit JULES_REWORK_MAX}"
: "${PARENT_CLOSE_FIRST:?MASTER policy parser did not emit PARENT_CLOSE_FIRST}"
: "${AUTOMATION_POLICY_HASH:?MASTER policy parser did not emit AUTOMATION_POLICY_HASH}"
: "${MASTER_COMMIT_SHA:?MASTER policy parser did not emit MASTER_COMMIT_SHA}"

readonly VAEP_JULES_PROTOCOL="MASTER"
readonly PARENT_STALL_NO_PROGRESS_MINUTES
readonly MAX_VOLUNTARY_IDLE
readonly VAEP_CHECKPOINTS
readonly JULES_LANE_BUDGET_SECONDS
readonly JULES_MAX_ATTEMPTS_PER_TASK
readonly JULES_REWORK_MAX
readonly PARENT_CLOSE_FIRST
readonly AUTOMATION_POLICY_HASH
readonly MASTER_COMMIT_SHA

R3_PROHIBITED=false
if (( JULES_MAX_ATTEMPTS_PER_TASK < 3 )); then
  R3_PROHIBITED=true
fi
readonly R3_PROHIBITED

if [[ "${1:-}" == "--static-self-test" ]]; then
  [[ "$VAEP_JULES_PROTOCOL" == "MASTER" ]]
  [[ "$JULES_MAX_ATTEMPTS_PER_TASK" =~ ^[1-9][0-9]*$ ]]
  [[ "$JULES_REWORK_MAX" =~ ^(0|[1-9][0-9]*)$ ]]
  [[ "$PARENT_CLOSE_FIRST" == true || "$PARENT_CLOSE_FIRST" == false ]]
  [[ "$PARENT_STALL_NO_PROGRESS_MINUTES" =~ ^[1-9][0-9]*$ ]]
  [[ "$MAX_VOLUNTARY_IDLE" =~ ^(0|[1-9][0-9]*)$ ]]
  [[ "$VAEP_CHECKPOINTS" =~ ^:[0-5][0-9](,:[0-5][0-9])*$ ]]
  [[ ${#AUTOMATION_POLICY_HASH} -eq 64 ]]
  [[ "$MASTER_COMMIT_SHA" =~ ^[0-9a-fA-F]{40}$ ]]
  [[ "$R3_PROHIBITED" == true || "$R3_PROHIBITED" == false ]]
  command -v python3 >/dev/null 2>&1
  python3 "$RUNTIME_CONTRACT" --self-test >/dev/null
  printf '{"status":"ok","protocol":"%s","laneBudgetSeconds":%d,"maxAttempts":%d,"r3Prohibited":%s,"qaTakeoverOnRetryExhaustion":true,"parentCloseFirst":%s,"checkpoints":"%s","policyHash":"%s","networkUsed":false,"sessionCreated":false,"attemptConsumed":false}\n' \
    "$VAEP_JULES_PROTOCOL" "$JULES_LANE_BUDGET_SECONDS" "$JULES_MAX_ATTEMPTS_PER_TASK" "$R3_PROHIBITED" "$PARENT_CLOSE_FIRST" "$VAEP_CHECKPOINTS" "$AUTOMATION_POLICY_HASH"
  exit 0
fi

if [[ "${1:-}" == "--runtime-preflight" ]]; then
  if ! command -v python3 >/dev/null 2>&1; then
    printf 'VAEP Jules worker prerequisite missing: python3 is required for runtime contract validation.\n' >&2
    exit 69
  fi
  python3 "$RUNTIME_CONTRACT" --self-test >/dev/null
  if ! command -v jq >/dev/null 2>&1; then
    printf 'VAEP Jules worker prerequisite missing: jq is required for runtime JSON handling. Install jq and ensure it is on PATH.\n' >&2
    exit 69
  fi
  printf '{"status":"ok","runtime":"jq","worker":"Jules","networkUsed":false,"sessionCreated":false,"attemptConsumed":false}\n'
  exit 0
fi

: "${JULES_API_KEY:?Jules API key is required}"
if ! command -v jq >/dev/null 2>&1; then
  printf 'VAEP Jules worker prerequisite missing: jq is required for runtime JSON handling. Install jq and ensure it is on PATH.\n' >&2
  exit 69
fi
: "${JULES_API_BASE:=https://jules.googleapis.com/v1alpha}"
: "${EXPECTED_OWNER:=jmejia31}"
: "${EXPECTED_REPO:=VariApp}"
: "${EXPECTED_BRANCH:=Desarrollo}"
: "${DISPATCH_PATH:?DISPATCH_PATH is required}"
: "${SESSION_TITLE_PREFIX:?SESSION_TITLE_PREFIX is required}"
: "${ARTIFACT_PREFIX:?ARTIFACT_PREFIX is required}"
: "${ISSUE_PREFIX:?ISSUE_PREFIX is required}"
: "${WORKER_LABEL:=Jules}"
: "${WORKER_ID:=}"
: "${GH_TOKEN:?GH_TOKEN is required}"
: "${GITHUB_SHA:?GITHUB_SHA is required}"
readonly DISPATCH_SHA="${VAEP_DISPATCH_SHA:-$GITHUB_SHA}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
: "${GITHUB_RUN_ID:?GITHUB_RUN_ID is required}"
: "${GITHUB_SERVER_URL:=https://github.com}"
: "${RUNNER_TEMP:?RUNNER_TEMP is required}"
: "${VAEP_JULES_RUNTIME_STATE_FILE:=$RUNNER_TEMP/vaep-jules-runtime-state.json}"

work="$RUNNER_TEMP/vaep-jules-master"
result_dir="$work/result"
mkdir -p "$result_dir"

fail() { echo "$*" >&2; exit "${2:-1}"; }

readonly JULES_API_CONNECT_TIMEOUT_SECONDS="${JULES_API_CONNECT_TIMEOUT_SECONDS:-10}"
readonly JULES_API_MAX_TIME_SECONDS="${JULES_API_MAX_TIME_SECONDS:-45}"

jules_curl() {
  curl --connect-timeout "$JULES_API_CONNECT_TIMEOUT_SECONDS" --max-time "$JULES_API_MAX_TIME_SECONDS" "$@"
}

dispatch_is_superseded() {
  local target_dispatch="${1:?dispatch required}"
  local issues_json
  if ! issues_json="$(gh issue list --repo "$GITHUB_REPOSITORY" --state all --search "$target_dispatch in:title" --limit 100 --json title 2>/dev/null)"; then
    return 2
  fi
  jq -e --arg title "[VAEP-JULES-SUPERSEDED] $target_dispatch" 'any(.[]; .title == $title)' <<< "$issues_json" >/dev/null
}

write_runtime_state() {
  local phase="${1:?phase required}"
  local session_name="${2:-}"
  local attempt_consumed=false
  [[ -n "$session_name" ]] && attempt_consumed=true
  local tmp="${VAEP_JULES_RUNTIME_STATE_FILE}.tmp"
  jq -n \
    --arg authority "MASTER" \
    --arg masterCommitSha "$MASTER_COMMIT_SHA" \
    --arg policyHash "$AUTOMATION_POLICY_HASH" \
    --arg workerId "${WORKER_ID:-JULES_A}" \
    --arg dispatchId "$dispatch_id" \
    --arg taskId "$task_id" \
    --arg phase "$phase" \
    --arg sessionName "$session_name" \
    --arg primaryBaseHead "$primary_base" \
    --arg startedAt "$runtime_started_at" \
    --argjson taskAttempt "$task_attempt" \
    --argjson attemptConsumed "$attempt_consumed" \
    '{authority:$authority,masterCommitSha:$masterCommitSha,policyHash:$policyHash,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,taskAttempt:$taskAttempt,phase:$phase,sessionName:$sessionName,primaryBaseHead:$primaryBaseHead,attemptConsumed:$attemptConsumed,startedAt:$startedAt}' > "$tmp"
  mv "$tmp" "$VAEP_JULES_RUNTIME_STATE_FILE"
}

api_get() { jules_curl --fail-with-body --silent --show-error -H "x-goog-api-key: $JULES_API_KEY" "$1"; }
api_post_json() {
  local url="$1" body_file="$2"
  jules_curl --fail-with-body --silent --show-error -X POST -H "Content-Type: application/json" -H "x-goog-api-key: $JULES_API_KEY" --data-binary @"$body_file" "$url"
}
api_post_empty() {
  local url="$1"
  jules_curl --fail-with-body --silent --show-error -X POST -H "Content-Type: application/json" -H "x-goog-api-key: $JULES_API_KEY" "$url"
}

write_base_equivalence() {
  local output="$1" comparison
  if [[ "$actual_base" == "$primary_base" ]]; then
    jq -n --arg requested "$primary_base" --arg actual "$actual_base" \
      '{ok:true,requested:$requested,actual:$actual,controlPlaneOnly:false,relation:"EXACT"}' > "$output"
    return 0
  fi

  # Jules accepts a branch, not a commit pin. A lane can therefore report a
  # later base after another immutable manifest/control-plane commit lands on
  # Desarrollo. Accept that drift only when GitHub proves that every commit
  # between the manifest base and the patch base is control-plane-only.
  if ! comparison="$(gh api "repos/$GITHUB_REPOSITORY/compare/${primary_base}...${actual_base}" 2>/dev/null)"; then
    jq -n --arg requested "$primary_base" --arg actual "$actual_base" \
      '{ok:false,requested:$requested,actual:$actual,controlPlaneOnly:false,relation:"UNVERIFIED"}' > "$output"
    return 0
  fi

  if jq -e '
    def control_plane_path:
      startswith("vaep/") or
      startswith(".github/scripts/vaep-") or
      startswith(".github/workflows/vaep-") or
      . == "docs/VAEP_AUTHORITY.md" or
      . == "AGENTS.md" or
      . == "PLAN_EJECUCION_AUTONOMA.md" or
      . == "PROJECT_CONTEXT.md" or
      . == "TASKS.md";
    (.status == "ahead") and
    ((.ahead_by // 0) > 0) and
    ((.files // []) | length > 0) and
    all(.files[]?; (.filename | control_plane_path))
  ' <<<"$comparison" >/dev/null; then
    jq -n --arg requested "$primary_base" --arg actual "$actual_base" \
      --argjson commits "$(jq '(.commits // []) | length' <<<"$comparison")" \
      '{ok:true,requested:$requested,actual:$actual,controlPlaneOnly:true,relation:"CONTROL_PLANE_DESCENDANT",commits:$commits}' > "$output"
  else
    jq -n --arg requested "$primary_base" --arg actual "$actual_base" \
      '{ok:false,requested:$requested,actual:$actual,controlPlaneOnly:false,relation:"FUNCTIONAL_DRIFT_OR_UNVERIFIED"}' > "$output"
  fi
}

# Atomic dispatch invariant: exactly one new manifest for THIS worker.
# The same commit may batch one manifest for each Jules lane so all four lanes
# start from one HEAD movement. No non-dispatch files are allowed in the batch.
mapfile -t added_manifests < <(git diff-tree --no-commit-id --name-only --diff-filter=A -r "$DISPATCH_SHA" -- "$DISPATCH_PATH/*.json")
[[ ${#added_manifests[@]} -eq 1 ]] || fail "MASTER expected exactly one newly added dispatch manifest for this worker; found ${#added_manifests[@]}." 21
manifest="${added_manifests[0]}"
mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$DISPATCH_SHA")
[[ ${#changed_files[@]} -ge 1 && ${#changed_files[@]} -le 4 ]] || fail "MASTER atomic dispatch batch must contain 1..4 files." 22
for changed in "${changed_files[@]}"; do
  [[ "$changed" =~ ^vaep/jules(-b|-c|-d)?/dispatch/[^/]+\.json$ ]] || fail "MASTER atomic dispatch batch contains a non-dispatch file: $changed" 22
done

jq -e '
  type == "object" and
  (.dispatchId | type == "string" and test("^[A-Za-z0-9_.-]+$")) and
  (.taskId | type == "string" and test("^[A-Za-z0-9_.-]+$")) and
  (.expectedBranch == "Desarrollo") and
  (.primaryBaseHead | type == "string" and test("^[0-9a-fA-F]{40}$")) and
  (.prompt | type == "string" and length > 0) and
  (.fileScopeHint | type == "string") and
  ((.taskAttempt == null) or ((.taskAttempt | type) == "number"))
' "$manifest" >/dev/null || fail "Invalid MASTER dispatch manifest schema." 23
if [[ -n "$WORKER_ID" ]]; then
  jq -e --arg worker "$WORKER_ID" '.workerId == $worker' "$manifest" >/dev/null || fail "Unexpected workerId in dispatch manifest." 24
fi

dispatch_id="$(jq -r '.dispatchId' "$manifest")"
task_id="$(jq -r '.taskId' "$manifest")"
primary_base="$(jq -r '.primaryBaseHead' "$manifest")"
file_scope="$(jq -r '.fileScopeHint' "$manifest")"
user_prompt="$(jq -r '.prompt' "$manifest")"
expected_manifest_name="$dispatch_id.json"
[[ "$(basename "$manifest")" == "$expected_manifest_name" ]] || fail "INVALID_REDISPATCH: manifest filename must equal dispatchId.json and remain immutable." 28
printf 'VAEP_METRIC stage=MANIFEST_ACCEPTED value=true worker=%s dispatch=%s task=%s\n' "${WORKER_ID:-JULES_A}" "$dispatch_id" "$task_id"
manifest_accepted_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

# Hard retry cap. New manifests SHOULD carry taskAttempt; compatibility manifests
# without it infer ATTEMPT=2 only from an explicit R2 dispatch label, otherwise 1.
task_attempt="$(python3 - "$manifest" "$JULES_MAX_ATTEMPTS_PER_TASK" "$JULES_REWORK_MAX" <<'PY'
import json
import re
import sys
from pathlib import Path

p = Path(sys.argv[1])
max_attempts = int(sys.argv[2])
rework_max = int(sys.argv[3])
data = json.loads(p.read_text(encoding='utf-8'))
dispatch_id = str(data['dispatchId'])
explicit = data.get('taskAttempt')
m = re.search(r'(?:^|-)R(\d+)(?:-|$)', dispatch_id, flags=re.IGNORECASE)
label_attempt = int(m.group(1)) if m else None

if max_attempts < 1 or rework_max < 0:
    raise SystemExit('invalid retry policy emitted by MASTER')
if label_attempt is not None and (label_attempt < 1 or label_attempt > max_attempts):
    raise SystemExit(f'{dispatch_id}: retry label exceeds MASTER max attempts={max_attempts}')

if explicit is None:
    attempt = label_attempt if label_attempt is not None else 1
else:
    if isinstance(explicit, bool) or not isinstance(explicit, int):
        raise SystemExit('taskAttempt must be an integer')
    attempt = explicit

if attempt < 1 or attempt > max_attempts:
    raise SystemExit(f'taskAttempt={attempt} rejected; MASTER max attempts={max_attempts}')
if label_attempt is not None and attempt != label_attempt:
    raise SystemExit(f'retry label R{label_attempt} conflicts with taskAttempt={attempt}')
if attempt - 1 > rework_max:
    raise SystemExit(f'taskAttempt={attempt} exceeds MASTER rework max={rework_max}')
print(attempt)
PY
)" || fail "MASTER retry-cap validation failed." 25

runtime_started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
write_runtime_state "PRE_SESSION" ""

set +e
dispatch_is_superseded "$dispatch_id"
superseded_rc=$?
set -e
if [[ "$superseded_rc" -eq 0 ]]; then
  fail "LATE_RESULT_GUARD: dispatch $dispatch_id is SUPERSEDED and cannot resume or create a Jules session." 26
fi
if [[ "$superseded_rc" -eq 2 ]]; then
  fail "LATE_RESULT_GUARD: unable to verify supersession ledger for $dispatch_id; refusing runtime startup." 27
fi

page_token=""
source_name=""
declare -A seen_source_tokens=()
for page in $(seq 1 50); do
  args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
  [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
  response="$work/sources-$page.json"
  jules_curl "${args[@]}" "$JULES_API_BASE/sources" > "$response"
  source_name="$(jq -r --arg owner "$EXPECTED_OWNER" --arg repo "$EXPECTED_REPO" '[.sources[]? | select(.githubRepo.owner == $owner and .githubRepo.repo == $repo)] | first | .name // empty' "$response")"
  if [[ -n "$source_name" ]]; then
    jq -e --arg name "$source_name" --arg branch "$EXPECTED_BRANCH" '[.sources[]? | select(.name == $name)] | first | (.githubRepo.branches // []) | any(.displayName == $branch)' "$response" >/dev/null || fail "Desarrollo is not visible in Jules source." 31
    break
  fi
  next_token="$(jq -r '.nextPageToken // empty' "$response")"
  [[ -n "$next_token" ]] || break
  [[ -z "${seen_source_tokens[$next_token]+x}" ]] || fail "Repeated source page token." 32
  seen_source_tokens["$next_token"]=1
  page_token="$next_token"
  [[ "$page" -lt 50 ]] || fail "Source pagination exceeded 50 pages." 33
done
[[ -n "$source_name" ]] || fail "VariApp/Desarrollo is not connected to Jules." 30

title="${SESSION_TITLE_PREFIX}${dispatch_id}"
printf '{"sessions":[]}\n' > "$work/sessions-index.json"
page_token=""
pagination_complete=false
declare -A seen_session_tokens=()
for page in $(seq 1 50); do
  args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
  [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
  response="$work/sessions-$page.json"
  jules_curl "${args[@]}" "$JULES_API_BASE/sessions" > "$response"
  jq -s '{sessions: ((.[0].sessions // []) + (.[1].sessions // []))}' "$work/sessions-index.json" "$response" > "$work/sessions-index.next.json"
  mv "$work/sessions-index.next.json" "$work/sessions-index.json"
  next_token="$(jq -r '.nextPageToken // empty' "$response")"
  if [[ -z "$next_token" ]]; then pagination_complete=true; break; fi
  [[ -z "${seen_session_tokens[$next_token]+x}" ]] || fail "Repeated session page token." 41
  seen_session_tokens["$next_token"]=1
  page_token="$next_token"
  [[ "$page" -lt 50 ]] || fail "Session pagination exceeded 50 pages." 42
done
[[ "$pagination_complete" == true ]] || fail "Session pagination did not finish normally." 43
set +e
python3 "$RUNTIME_CONTRACT" --check-active-duplicate "$manifest" "$DISPATCH_PATH" "$work/sessions-index.json" "$SESSION_TITLE_PREFIX" > "$work/duplicate-session-guard.json"
duplicate_guard_rc=$?
set -e
if [[ "$duplicate_guard_rc" -ne 0 ]]; then
  cat "$work/duplicate-session-guard.json" >&2
  fail "ACTIVE_SESSION_GUARD: duplicate/equivalent Jules session blocks new dispatch." 45
fi
printf 'VAEP_METRIC stage=ACTIVE_SESSION_GUARD value=PASS worker=%s dispatch=%s task=%s base=%s attempt=%s\n' "${WORKER_ID:-JULES_A}" "$dispatch_id" "$task_id" "$primary_base" "$task_attempt"
session_name=""
prompt_file="$work/prompt.txt"
printf '%s\n' \
  "You are $WORKER_LABEL, an autonomous trusted implementer of the VariApp VAEP team." \
  "PROJECT_ID=VARIAPP" \
  "WORKER_ID=${WORKER_ID:-JULES_A}" \
  "REPOSITORY=jmejia31/VariApp" \
  "BRANCH=Desarrollo" \
  "VAEP_JULES_PROTOCOL=MASTER" \
  "GLOBAL_CONTROL_PLANE=VAEP_MASTER" \
  "PARENT_CLOSE_FIRST=$PARENT_CLOSE_FIRST" \
  "VAEP_CHECKPOINTS=$VAEP_CHECKPOINTS" \
  "VAEP_AUTHORITY_FILE=docs/VAEP_AUTHORITY.md" \
  "PRIMARY_BASE_HEAD=$primary_base" \
  "VAEP_TASK_ID=$task_id" \
  "TASK_ATTEMPT=$task_attempt" \
  "JULES_MAX_ATTEMPTS_PER_TASK=$JULES_MAX_ATTEMPTS_PER_TASK" \
  "JULES_REWORK_MAX=$JULES_REWORK_MAX" \
  "PARENT_STALL_NO_PROGRESS_MINUTES=$PARENT_STALL_NO_PROGRESS_MINUTES" \
  "MAX_VOLUNTARY_IDLE=$MAX_VOLUNTARY_IDLE" \
  "FILE_SCOPE_HINT=$file_scope" \
  "" \
  "Before changing anything, read docs/VAEP_AUTHORITY.md FIRST, then the dispatch, AGENTS.md, PLAN_EJECUCION_AUTONOMA.md and docs/VAEP_JULES.md. MASTER is the only operational Jules authority." \
  "HARD RETRY RULE: this logical task allows at most $JULES_MAX_ATTEMPTS_PER_TASK attempt(s) and $JULES_REWORK_MAX rework(s), exactly as defined by MASTER. Never exceed MASTER retry limits. If the final allowed attempt still contains a blocking defect, report it exactly for ChatGPT/VAEP/Vibe QA takeover and finish your evidence." \
  "PARENT CLOSE FIRST: stay inside the assigned exclusive scope of the current parent. Preparation never promotes N+1. Dispatch, activity or COMPLETED never equals LISTO without review, causal validation and evidence." \
  "CHECKPOINTS: use VAEP_CHECKPOINTS=$VAEP_CHECKPOINTS from MASTER. A declared schedule is not proof that a checkpoint ran; only executor evidence is." \
  "Work only inside your Jules cloud workspace. Never create branches, pull requests, pushes, merges, deployments, Production changes, secrets, or changes to main." \
  "Do not publish anything to GitHub. Return a reviewable ChangeSet/gitPatch with exact baseCommitId." \
  "Inspect only assigned scope and direct dependencies. If scope materially diverged from PRIMARY_BASE_HEAD and makes the task unsafe, make no changes and report the conflict." \
  "Run proportional tests and, before COMPLETED, emit the exact evidence marker TESTS_EXECUTED: <command and result>. Report observations, limitations, risks and recommendations; never claim false PASS." \
  "Before COMPLETED, perform two independent self-reviews and report SELF_REVIEW_PASS_1 and SELF_REVIEW_PASS_2. Review git status, full diff, scope, contracts, security/RBAC, audit/data, tests, temporary files and every unexecuted validation." \
  "" \
  "ASSIGNED VAEP TASK" \
  "$user_prompt" > "$prompt_file"
jq -n --arg prompt "$(cat "$prompt_file")" --arg title "$title" --arg source "$source_name" --arg branch "$EXPECTED_BRANCH" '{prompt:$prompt,title:$title,sourceContext:{source:$source,githubRepoContext:{startingBranch:$branch}},requirePlanApproval:false}' > "$work/create-session.json"
api_post_json "$JULES_API_BASE/sessions" "$work/create-session.json" > "$work/session-created.json"
session_name="$(jq -r '.name // empty' "$work/session-created.json")"
[[ -n "$session_name" ]] || fail "Jules did not return a session resource." 44
printf 'VAEP_METRIC stage=SESSION_CREATED value=true worker=%s dispatch=%s session=%s\n' "${WORKER_ID:-JULES_A}" "$dispatch_id" "$session_name"
session_created_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
session_id="${session_name#sessions/}"
write_runtime_state "SESSION_ACTIVE" "$session_name"

deadline=$((SECONDS + JULES_LANE_BUDGET_SECONDS))
stall_seconds=$((PARENT_STALL_NO_PROGRESS_MINUTES * 60))
last_progress_seconds=$SECONDS
last_progress_state=""
last_progress_signal=""
last_activity_signal=""
first_in_progress_at=""
last_activity_at=""
final_state_at=""
terminal_state=""
feedback_question=""
auto_feedback_count=0
max_followups=1
routine_prompt="VAEP Jules MASTER automated follow-up. Continue inside the assigned exclusive scope of the current parent. Authority: docs/VAEP_AUTHORITY.md -> dispatch -> AGENTS.md/PLAN_EJECUCION_AUTONOMA.md/docs/VAEP_JULES.md -> code/tests. TASK_ATTEMPT=$task_attempt; MASTER max attempts=$JULES_MAX_ATTEMPTS_PER_TASK; MASTER max reworks=$JULES_REWORK_MAX. Never exceed MASTER retry limits; exhaustion transfers correction to ChatGPT/VAEP/Vibe QA takeover. Do not expand scope or promote N+1. COMPLETED never equals LISTO. Before COMPLETED emit two independent reviews, SELF_REVIEW_PASS_1 and SELF_REVIEW_PASS_2; report observations, limitations, risks, recommendations and tests not executed; preserve a reviewable ChangeSet/gitPatch with baseCommitId. Never trade quality or causal evidence for throughput."

while (( SECONDS < deadline )); do
  api_get "$JULES_API_BASE/$session_name" > "$work/session-latest.json"
  state="$(jq -r '.state // "UNKNOWN"' "$work/session-latest.json")"
  if [[ "$state" == "IN_PROGRESS" && -z "$first_in_progress_at" ]]; then first_in_progress_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"; fi
  echo "Jules MASTER session $session_id state: $state (attempt $task_attempt/$JULES_MAX_ATTEMPTS_PER_TASK; auto followups $auto_feedback_count/$max_followups)"
  progress_signal="$(jq -r '[.updateTime // .update_time // .lastActivityTime // .lastUpdateTime // ""] | join("|")' "$work/session-latest.json")"
  activity_signal=""
  if api_get "$JULES_API_BASE/$session_name/activities?pageSize=20" > "$work/activity-progress.json" 2>/dev/null; then
    activity_signal="$(jq -r '[.activities[]?.createTime // empty] | sort | last // ""' "$work/activity-progress.json")"
  fi
  if [[ -n "$activity_signal" ]]; then last_activity_at="$activity_signal"; fi
  if [[ "$state" != "$last_progress_state" || ( -n "$progress_signal" && "$progress_signal" != "$last_progress_signal" ) || ( -n "$activity_signal" && "$activity_signal" != "$last_activity_signal" ) ]]; then
    last_progress_seconds=$SECONDS
    last_progress_state="$state"
    last_progress_signal="$progress_signal"
    last_activity_signal="$activity_signal"
    printf 'VAEP_METRIC stage=SESSION_PROGRESS value=true session=%s state=%s activity=%s\n' "$session_name" "$state" "${activity_signal:-NONE}"
  fi
  if [[ "$state" =~ ^(QUEUED|PLANNING|IN_PROGRESS)$ ]] && (( SECONDS - last_progress_seconds >= stall_seconds )); then
    printf 'VAEP_METRIC stage=STALL_DETECTED value=true session=%s state=%s no_progress_seconds=%d\n' "$session_name" "$state" "$((SECONDS-last_progress_seconds))" >&2
    write_runtime_state "STALL_NO_PROGRESS" "$session_name"
    exit 125
  fi
  case "$state" in
    COMPLETED|FAILED|PAUSED)
      terminal_state="$state"
      break
      ;;
    AWAITING_PLAN_APPROVAL)
      if (( auto_feedback_count >= max_followups )); then terminal_state="AUTO_FEEDBACK_EXHAUSTED"; break; fi
      api_post_empty "$JULES_API_BASE/$session_name:approvePlan" >/dev/null
      auto_feedback_count=$((auto_feedback_count + 1))
      sleep 10
      ;;
    AWAITING_USER_FEEDBACK)
      feedback_question="$(jq -r '[.. | objects | (.question? // .userQuestion? // .feedbackQuestion? // .clarificationQuestion? // empty) | select(type=="string" and length>0)] | last // ""' "$work/session-latest.json")"
      if [[ -z "$feedback_question" ]]; then
        feedback_question="QUESTION_NOT_EXPOSED_BY_JULES_SESSION_API"
        printf 'VAEP_METRIC stage=AWAITING_USER_FEEDBACK value=true session=%s question=%q action=QA_TAKEOVER reason=question_not_exposed\n' "$session_name" "$feedback_question" >&2
        terminal_state="AWAITING_USER_FEEDBACK_QA_TAKEOVER"
        break
      fi
      if (( auto_feedback_count < max_followups )); then
        feedback_answer="VAEP specific clarification for TASK_ID=$task_id DISPATCH_ID=$dispatch_id ATTEMPT=$task_attempt PRIMARY_BASE_HEAD=$primary_base FILE_SCOPE_HINT=$file_scope. Your exact question was: $feedback_question. Resolve it from repository evidence at PRIMARY_BASE_HEAD and direct dependencies only; do not invent entities, contracts, fields or behavior. If the repository still does not contain enough evidence, ask again with the exact unresolved fact and make no unsafe/out-of-scope change."
        jq -n --arg prompt "$feedback_answer" '{prompt:$prompt}' > "$work/feedback-answer.json"
        api_post_json "$JULES_API_BASE/$session_name:sendMessage" "$work/feedback-answer.json" >/dev/null
        auto_feedback_count=$((auto_feedback_count + 1))
        printf 'VAEP_METRIC stage=AWAITING_USER_FEEDBACK value=true session=%s question=%q action=SPECIFIC_RESPONSE response_count=%d\n' "$session_name" "$feedback_question" "$auto_feedback_count"
        sleep 20
      else
        printf 'VAEP_METRIC stage=AWAITING_USER_FEEDBACK value=true session=%s question=%q action=QA_TAKEOVER reason=question_persisted\n' "$session_name" "$feedback_question" >&2
        terminal_state="AWAITING_USER_FEEDBACK_QA_TAKEOVER"
        break
      fi
      ;;
    QUEUED|PLANNING|IN_PROGRESS)
      sleep 30
      ;;
    *)
      echo "Non-terminal Jules state: $state"
      sleep 30
      ;;
  esac
done
if [[ -z "$terminal_state" ]]; then
  write_runtime_state "LANE_BUDGET_EXCEEDED" "$session_name"
  exit 124
fi
final_state_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
write_runtime_state "TERMINAL_$terminal_state" "$session_name"

printf '{"activities":[]}\n' > "$result_dir/activities.json"
page_token=""
pagination_complete=false
declare -A seen_activity_tokens=()
for page in $(seq 1 100); do
  args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
  [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
  response="$work/activities-$page.json"
  jules_curl "${args[@]}" "$JULES_API_BASE/$session_name/activities" > "$response"
  jq -s '{activities: ((.[0].activities // []) + (.[1].activities // []))}' "$result_dir/activities.json" "$response" > "$result_dir/activities.next.json"
  mv "$result_dir/activities.next.json" "$result_dir/activities.json"
  next_token="$(jq -r '.nextPageToken // empty' "$response")"
  if [[ -z "$next_token" ]]; then pagination_complete=true; break; fi
  [[ -z "${seen_activity_tokens[$next_token]+x}" ]] || fail "Repeated activity page token." 60
  seen_activity_tokens["$next_token"]=1
  page_token="$next_token"
  [[ "$page" -lt 100 ]] || fail "Activity pagination exceeded 100 pages." 61
done
[[ "$pagination_complete" == true ]] || fail "Partial Jules activities collection refused." 62

if [[ -f "$work/session-latest.json" ]]; then cp "$work/session-latest.json" "$result_dir/session.json"; else api_get "$JULES_API_BASE/$session_name" > "$result_dir/session.json"; fi
cp "$manifest" "$result_dir/dispatch.json"
self_review_pass_1=false
self_review_pass_2=false
grep -q 'SELF_REVIEW_PASS_1' "$result_dir/activities.json" && self_review_pass_1=true
grep -q 'SELF_REVIEW_PASS_2' "$result_dir/activities.json" && self_review_pass_2=true

set +e
dispatch_is_superseded "$dispatch_id"
late_guard_rc=$?
set -e
if [[ "$late_guard_rc" -eq 0 ]]; then
  terminal_state="LATE_RESULT_SUPERSEDED"
elif [[ "$late_guard_rc" -eq 2 ]]; then
  fail "LATE_RESULT_GUARD: unable to verify supersession ledger before result publication; refusing publication." 63
fi
jq '[.activities[]? as $a | $a.artifacts[]? | .changeSet?.gitPatch? | select(. != null) | {createTime:($a.createTime // ""),patch:.}] | sort_by(.createTime) | last | .patch // null' "$result_dir/activities.json" > "$result_dir/gitpatch.json"
patch_present="$(jq -r 'type == "object"' "$result_dir/gitpatch.json")"
actual_base=""
suggested=""
base_equivalence="$work/base-equivalence.json"
if [[ "$patch_present" == true ]]; then
  actual_base="$(jq -r '.baseCommitId // ""' "$result_dir/gitpatch.json")"
  suggested="$(jq -r '.suggestedCommitMessage // ""' "$result_dir/gitpatch.json")"
  jq -r '.unidiffPatch // ""' "$result_dir/gitpatch.json" > "$result_dir/changes.patch"
  write_base_equivalence "$base_equivalence"
else
  : > "$result_dir/changes.patch"
  jq -n --arg requested "$primary_base" --arg actual "" \
    '{ok:false,requested:$requested,actual:$actual,controlPlaneOnly:false,relation:"PATCH_BASE_MISSING"}' > "$base_equivalence"
fi
terminal_contract_valid=false
ready_for_vaep=false
if [[ "$terminal_state" == "COMPLETED" ]]; then
  set +e
  python3 "$RUNTIME_CONTRACT" --validate-terminal "$manifest" "$result_dir/gitpatch.json" "$result_dir/activities.json" --base-equivalence "$base_equivalence" > "$result_dir/terminal-contract.json"
  terminal_contract_rc=$?
  set -e
  if [[ "$terminal_contract_rc" -eq 0 ]]; then
    terminal_contract_valid=true
    ready_for_vaep=true
  fi
else
  jq -n --arg state "$terminal_state" '{ok:false,skipped:true,reason:"terminal state is not COMPLETED",state:$state}' > "$result_dir/terminal-contract.json"
fi
jq -n \
  --arg manifestAcceptedAt "$manifest_accepted_at" \
  --arg sessionCreatedAt "$session_created_at" \
  --arg firstInProgressAt "$first_in_progress_at" \
  --arg lastActivityAt "$last_activity_at" \
  --arg finalStateAt "$final_state_at" \
  --arg finalState "$terminal_state" \
  --arg dispatchCommitSha "$DISPATCH_SHA" \
  --arg manifestPath "$manifest" \
  --arg workflowRunId "$GITHUB_RUN_ID" \
  --arg sessionName "$session_name" \
  --argjson baseEquivalence "$(cat "$base_equivalence")" \
  --argjson patchPresent "$patch_present" \
  --argjson readyForVaep "$ready_for_vaep" \
  '{dispatchCommitSha:$dispatchCommitSha,manifestPath:$manifestPath,workflowRunId:$workflowRunId,sessionName:$sessionName,manifestAcceptedAt:$manifestAcceptedAt,sessionCreatedAt:$sessionCreatedAt,firstInProgressAt:$firstInProgressAt,lastActivityAt:$lastActivityAt,finalStateAt:$finalStateAt,finalState:$finalState,patchPresent:$patchPresent,baseEquivalence:$baseEquivalence,readyForVaep:$readyForVaep,handoffState:(if $readyForVaep then "READY_FOR_VAEP" else "NOT_READY_FOR_VAEP" end),reviewResult:"PENDING_REVIEW_FIRST",integrationResult:"NOT_INTEGRATED",correlationComplete:true}' > "$result_dir/lifecycle.json"

jq -n \
  --arg protocol "$VAEP_JULES_PROTOCOL" \
  --arg policyHash "$AUTOMATION_POLICY_HASH" \
  --arg masterCommitSha "$MASTER_COMMIT_SHA" \
  --argjson r3Prohibited "$R3_PROHIBITED" \
  --arg workerId "${WORKER_ID:-JULES_A}" \
  --arg dispatchId "$dispatch_id" \
  --arg taskId "$task_id" \
  --arg session "$session_name" \
  --arg state "$terminal_state" \
  --arg requestedBase "$primary_base" \
  --arg actualBase "$actual_base" \
  --argjson baseEquivalence "$(cat "$base_equivalence")" \
  --arg suggestedCommitMessage "$suggested" \
  --arg feedbackQuestion "$feedback_question" \
  --arg dispatchCommitSha "$DISPATCH_SHA" \
  --arg manifestPath "$manifest" \
  --arg workflowRunId "$GITHUB_RUN_ID" \
  --argjson readyForVaep "$ready_for_vaep" \
  --argjson selfReviewPass1 "$self_review_pass_1" \
  --argjson selfReviewPass2 "$self_review_pass_2" \
  --argjson patchPresent "$patch_present" \
  --argjson autoFeedbackCount "$auto_feedback_count" \
  --argjson taskAttempt "$task_attempt" \
  --argjson maxAttempts "$JULES_MAX_ATTEMPTS_PER_TASK" \
  --argjson parentCloseFirst "$PARENT_CLOSE_FIRST" \
  --argjson laneBudgetSeconds "$JULES_LANE_BUDGET_SECONDS" \
  --arg checkpoints "$VAEP_CHECKPOINTS" \
  '{protocol:$protocol,globalControlPlane:"VAEP_MASTER",masterCommitSha:$masterCommitSha,policyHash:$policyHash,parentCloseFirst:$parentCloseFirst,checkpoints:$checkpoints,laneBudgetSeconds:$laneBudgetSeconds,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,taskAttempt:$taskAttempt,maxAttempts:$maxAttempts,r3Prohibited:$r3Prohibited,qaTakeoverOnRetryExhaustion:true,session:$session,state:$state,requestedBase:$requestedBase,actualPatchBase:$actualBase,baseEquivalence:$baseEquivalence,patchPresent:$patchPresent,selfReviewPass1:$selfReviewPass1,selfReviewPass2:$selfReviewPass2,feedbackQuestion:$feedbackQuestion,triggered:true,manifestAccepted:true,sessionCreated:true,sessionCompleted:($state=="COMPLETED"),terminalContractArtifact:"terminal-contract.json",lifecycleArtifact:"lifecycle.json",reviewAccepted:false,integrated:false,integrationReceiptRequired:true,integrationReceiptMode:"COMMIT_TRAILERS",productivityCountedStage:"INTEGRATED",superseded:($state=="LATE_RESULT_SUPERSEDED"),lateResultAutoIntegrationDenied:($state=="LATE_RESULT_SUPERSEDED"),suggestedCommitMessage:$suggestedCommitMessage,autoFeedbackCount:$autoFeedbackCount,dispatchCommitSha:$dispatchCommitSha,manifestPath:$manifestPath,workflowRunId:$workflowRunId,readyForVaep:$readyForVaep,handoffState:(if $readyForVaep then "READY_FOR_VAEP" else "NOT_READY_FOR_VAEP" end),correlationComplete:true,controllerHandoff:(if $state=="LATE_RESULT_SUPERSEDED" then "LATE_RESULT_EVIDENCE_ONLY" elif $state=="AWAITING_USER_FEEDBACK_QA_TAKEOVER" then "QA_TAKEOVER_FEEDBACK_REQUIRED" elif $readyForVaep then "READY_FOR_VAEP_REVIEW_FIRST_REQUIRED_NO_REFILL" else "QA_CLASSIFICATION_REQUIRED_NO_REFILL" end)}' \
  > "$result_dir/result.json"

controller_handoff="$(python3 scripts/vaep/terminal_handoff.py --annotate "$result_dir")"
run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v issue_body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n' \
  "VAEP $WORKER_LABEL MASTER result and controller handoff signal." \
  "- Protocol: \`MASTER\`; global control-plane: \`MASTER\`" \
  "- Worker: \`${WORKER_ID:-JULES_A}\`" \
  "- Dispatch: \`$dispatch_id\`" \
  "- Task: \`$task_id\`" \
  "- Task attempt: \`$task_attempt/$JULES_MAX_ATTEMPTS_PER_TASK\`; Jules R3+ is PROHIBITED" \
  "- Jules session: \`$session_name\`" \
  "- Terminal state: \`$terminal_state\`" \
  "- Inline auto-feedback count: \`$auto_feedback_count\`" \
  "- Patch present: \`$patch_present\`; patch base: \`$actual_base\`" \
  "- Ready for VAEP: \`$ready_for_vaep\`; review status: \`PENDING_REVIEW_FIRST\`" \
  "- Dispatch commit: \`$DISPATCH_SHA\`; manifest: \`$manifest\`" \
  "- Workflow run ID: \`$GITHUB_RUN_ID\`; correlation complete: \`true\`" \
  "- Controller handoff: \`$controller_handoff\`; correction owner: CHATGPT_VAEP; takeover executed: false" \
  "- Parent-close-first: \`$PARENT_CLOSE_FIRST\`; checkpoints: \`$VAEP_CHECKPOINTS\`" \
  "- Workflow run: $run_url" \
  'Artifact only. Nothing was applied to Desarrollo, pushed, merged or deployed. VAEP/ChatGPT review is mandatory. When MASTER retry capacity is exhausted, ChatGPT/VAEP/Vibe takes over; do not exceed MASTER retry limits.'
gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id result" --body "$issue_body" >/dev/null

printf 'ARTIFACT_NAME=%s-%s\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

if [[ "$terminal_state" == "AWAITING_USER_FEEDBACK_QA_TAKEOVER" ]]; then
  fail "Jules requested user feedback; question captured and ownership transferred to QA_TAKEOVER: $feedback_question" 52
fi
[[ "$terminal_state" == COMPLETED ]] || fail "Jules did not complete successfully: $terminal_state" 50
printf 'VAEP_METRIC stage=SESSION_COMPLETED value=true session=%s\n' "$session_name"
[[ "$patch_present" == true ]] || fail "Jules completed without ChangeSet/gitPatch." 51
printf 'VAEP_METRIC stage=PATCH_PRESENT value=true session=%s\n' "$session_name"
if [[ "$actual_base" != "$primary_base" ]]; then
  jq -e --arg requested "$primary_base" --arg actual "$actual_base" \
    '.ok == true and .requested == $requested and .actual == $actual and .controlPlaneOnly == true' \
    "$base_equivalence" >/dev/null || fail "Jules patch rejected: baseCommitId is not primaryBaseHead and the intervening history is not proven control-plane-only." 54
  printf 'VAEP_METRIC stage=BASE_EQUIVALENCE value=CONTROL_PLANE_DESCENDANT requested=%s actual=%s\n' "$primary_base" "$actual_base"
fi
[[ "$self_review_pass_1" == true && "$self_review_pass_2" == true ]] || fail "Jules patch rejected before REVIEW_FIRST: missing SELF_REVIEW_PASS_1/SELF_REVIEW_PASS_2 evidence." 53
printf 'VAEP_METRIC stage=SELF_REVIEW_COMPLETE value=true session=%s\n' "$session_name"
if [[ "$terminal_contract_valid" != true ]]; then
  cat "$result_dir/terminal-contract.json" >&2
  fail "Jules COMPLETED rejected by terminal contract: scope/tests/base/artifact evidence incomplete." 55
fi
printf 'VAEP_METRIC stage=TERMINAL_CONTRACT_VALID value=true session=%s base=%s\n' "$session_name" "$actual_base"
printf 'VAEP_METRIC stage=READY_FOR_VAEP value=true session=%s dispatch_commit=%s workflow_run_id=%s\n' "$session_name" "$DISPATCH_SHA" "$GITHUB_RUN_ID"
