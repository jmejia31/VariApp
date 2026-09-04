#!/usr/bin/env bash
set -euo pipefail

# Internal worker invoked only by .github/scripts/vaep-jules-master.sh.
# Runtime semantics are governed by docs/VAEP_AUTHORITY.md (MASTER).
readonly VAEP_JULES_PROTOCOL="MASTER"
readonly JULES_MAX_ATTEMPTS_PER_TASK=2
readonly JULES_REWORK_MAX=1
readonly PARENT_CLOSE_FIRST=true
readonly VAEP_CHECKPOINTS=":00,:15,:30,:45,:55"

if [[ "${1:-}" == "--static-self-test" ]]; then
  [[ "$VAEP_JULES_PROTOCOL" == "MASTER" ]]
  [[ "$JULES_MAX_ATTEMPTS_PER_TASK" -eq 2 ]]
  [[ "$JULES_REWORK_MAX" -eq 1 ]]
  [[ "$PARENT_CLOSE_FIRST" == true ]]
  [[ "$VAEP_CHECKPOINTS" == ":00,:15,:30,:45,:55" ]]
  printf '{"status":"ok","protocol":"%s","maxAttempts":%d,"r3Prohibited":true,"qaTakeoverOnR2Failure":true,"parentCloseFirst":%s,"checkpoints":"%s","networkUsed":false,"sessionCreated":false,"attemptConsumed":false}\n' \
    "$VAEP_JULES_PROTOCOL" "$JULES_MAX_ATTEMPTS_PER_TASK" "$PARENT_CLOSE_FIRST" "$VAEP_CHECKPOINTS"
  exit 0
fi

: "${JULES_API_KEY:?Jules API key is required}"
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
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
: "${GITHUB_RUN_ID:?GITHUB_RUN_ID is required}"
: "${GITHUB_SERVER_URL:=https://github.com}"
: "${RUNNER_TEMP:?RUNNER_TEMP is required}"

work="$RUNNER_TEMP/vaep-jules-master"
result_dir="$work/result"
mkdir -p "$result_dir"

fail() { echo "$*" >&2; exit "${2:-1}"; }
api_get() { curl --fail-with-body --silent --show-error -H "x-goog-api-key: $JULES_API_KEY" "$1"; }
api_post_json() {
  local url="$1" body_file="$2"
  curl --fail-with-body --silent --show-error -X POST -H "Content-Type: application/json" -H "x-goog-api-key: $JULES_API_KEY" --data-binary @"$body_file" "$url"
}
api_post_empty() {
  local url="$1"
  curl --fail-with-body --silent --show-error -X POST -H "Content-Type: application/json" -H "x-goog-api-key: $JULES_API_KEY" "$url"
}

# Atomic dispatch invariant: one new manifest, one changed file, one worker.
mapfile -t added_manifests < <(git diff-tree --no-commit-id --name-only --diff-filter=A -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
[[ ${#added_manifests[@]} -eq 1 ]] || fail "MASTER expected exactly one newly added dispatch manifest for this worker; found ${#added_manifests[@]}." 21
manifest="${added_manifests[0]}"
mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
[[ ${#changed_files[@]} -eq 1 && "${changed_files[0]}" == "$manifest" ]] || fail "MASTER dispatch commit must change exactly the single new worker manifest." 22

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

# Hard retry cap. New manifests SHOULD carry taskAttempt; compatibility manifests
# without it infer ATTEMPT=2 only from an explicit R2 dispatch label, otherwise 1.
task_attempt="$(python3 - "$manifest" <<'PY'
import json
import re
import sys
from pathlib import Path

p = Path(sys.argv[1])
data = json.loads(p.read_text(encoding='utf-8'))
dispatch_id = str(data['dispatchId'])
explicit = data.get('taskAttempt')
m = re.search(r'(?:^|-)R(\d+)(?:-|$)', dispatch_id, flags=re.IGNORECASE)
label_attempt = int(m.group(1)) if m else None

if label_attempt is not None and label_attempt >= 3:
    raise SystemExit(f'R3+ prohibited by VAEP Jules MASTER: {dispatch_id}')

if explicit is None:
    attempt = 2 if label_attempt == 2 else 1
else:
    if isinstance(explicit, bool):
        raise SystemExit('taskAttempt must be integer 1 or 2')
    attempt = int(explicit)

if attempt not in (1, 2):
    raise SystemExit(f'taskAttempt={attempt} rejected; MASTER allows only 1 or 2')
if label_attempt == 2 and attempt != 2:
    raise SystemExit('R2 dispatch must be ATTEMPT=2')
print(attempt)
PY
)" || fail "MASTER retry-cap validation failed." 25

page_token=""
source_name=""
declare -A seen_source_tokens=()
for page in $(seq 1 50); do
  args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
  [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
  response="$work/sources-$page.json"
  curl "${args[@]}" "$JULES_API_BASE/sources" > "$response"
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
page_token=""
session_name=""
pagination_complete=false
declare -A seen_session_tokens=()
for page in $(seq 1 50); do
  args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
  [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
  response="$work/sessions-$page.json"
  curl "${args[@]}" "$JULES_API_BASE/sessions" > "$response"
  session_name="$(jq -r --arg title "$title" '[.sessions[]? | select(.title == $title)] | first | .name // empty' "$response")"
  [[ -z "$session_name" ]] || break
  next_token="$(jq -r '.nextPageToken // empty' "$response")"
  if [[ -z "$next_token" ]]; then pagination_complete=true; break; fi
  [[ -z "${seen_session_tokens[$next_token]+x}" ]] || fail "Repeated session page token." 41
  seen_session_tokens["$next_token"]=1
  page_token="$next_token"
  [[ "$page" -lt 50 ]] || fail "Session pagination exceeded 50 pages." 42
done

if [[ -z "$session_name" ]]; then
  [[ "$pagination_complete" == true ]] || fail "Session pagination did not finish normally." 43
  prompt_file="$work/prompt.txt"
  printf '%s\n' \
    "You are $WORKER_LABEL, an autonomous trusted implementer of the VariApp VAEP team." \
    "PROJECT_ID=VARIAPP" \
    "WORKER_ID=${WORKER_ID:-JULES_A}" \
    "REPOSITORY=jmejia31/VariApp" \
    "BRANCH=Desarrollo" \
    "VAEP_JULES_PROTOCOL=MASTER" \
    "GLOBAL_CONTROL_PLANE=VAEP_MASTER" \
    "PARENT_CLOSE_FIRST=true" \
    "VAEP_CHECKPOINTS=$VAEP_CHECKPOINTS" \
    "VAEP_AUTHORITY_FILE=docs/VAEP_AUTHORITY.md" \
    "PRIMARY_BASE_HEAD=$primary_base" \
    "VAEP_TASK_ID=$task_id" \
    "TASK_ATTEMPT=$task_attempt" \
    "JULES_MAX_ATTEMPTS_PER_TASK=2" \
    "JULES_REWORK_MAX=1" \
    "FILE_SCOPE_HINT=$file_scope" \
    "" \
    "Before changing anything, read docs/VAEP_AUTHORITY.md FIRST, then the dispatch, AGENTS.md, PLAN_EJECUCION_AUTONOMA.md and docs/VAEP_JULES.md. MASTER is the only operational Jules authority." \
    "HARD RETRY RULE: this logical task allows only ATTEMPT=1 plus one final correction ATTEMPT=2/R2. Never request, propose, create or perform Jules R3+. If ATTEMPT=2 still contains a blocking defect, report it exactly for ChatGPT/VAEP/Vibe QA takeover and finish your evidence." \
    "PARENT CLOSE FIRST: stay inside the assigned exclusive scope of the current parent. Preparation never promotes N+1. Dispatch, activity or COMPLETED never equals LISTO without review, causal validation and evidence." \
    "CHECKPOINTS: the task system declares :00/:15/:30/:45 with backup :55. A declared schedule is not proof that a checkpoint ran; only executor evidence is." \
    "Work only inside your Jules cloud workspace. Never create branches, pull requests, pushes, merges, deployments, Production changes, secrets, or changes to main." \
    "Do not publish anything to GitHub. Return a reviewable ChangeSet/gitPatch with exact baseCommitId." \
    "Inspect only assigned scope and direct dependencies. If scope materially diverged from PRIMARY_BASE_HEAD and makes the task unsafe, make no changes and report the conflict." \
    "Run proportional tests. Report observations, limitations, risks, recommendations and tests not executed; never claim false PASS." \
    "Before COMPLETED, perform two independent self-reviews and report SELF_REVIEW_PASS_1 and SELF_REVIEW_PASS_2. Review git status, full diff, scope, contracts, security/RBAC, audit/data, tests, temporary files and every unexecuted validation." \
    "" \
    "ASSIGNED VAEP MICROTASK" \
    "$user_prompt" > "$prompt_file"

  jq -n --arg prompt "$(cat "$prompt_file")" --arg title "$title" --arg source "$source_name" --arg branch "$EXPECTED_BRANCH" '{prompt:$prompt,title:$title,sourceContext:{source:$source,githubRepoContext:{startingBranch:$branch}},requirePlanApproval:false}' > "$work/create-session.json"
  api_post_json "$JULES_API_BASE/sessions" "$work/create-session.json" > "$work/session-created.json"
  session_name="$(jq -r '.name // empty' "$work/session-created.json")"
fi
[[ -n "$session_name" ]] || fail "Jules did not return a session resource." 44
session_id="${session_name#sessions/}"

deadline=$((SECONDS + 6600))
terminal_state=""
auto_feedback_count=0
max_followups=3
routine_prompt="VAEP Jules MASTER automated follow-up. Continue inside the assigned exclusive scope of the current parent. Authority: docs/VAEP_AUTHORITY.md -> dispatch -> AGENTS.md/PLAN_EJECUCION_AUTONOMA.md/docs/VAEP_JULES.md -> code/tests. TASK_ATTEMPT=$task_attempt; maximum attempts=2; R2 is final; R3+ is prohibited and transfers correction to ChatGPT/VAEP/Vibe QA takeover. Do not expand scope or promote N+1. COMPLETED never equals LISTO. Before COMPLETED emit two independent reviews, SELF_REVIEW_PASS_1 and SELF_REVIEW_PASS_2; report observations, limitations, risks, recommendations and tests not executed; preserve a reviewable ChangeSet/gitPatch with baseCommitId. Never trade quality or causal evidence for throughput."

while (( SECONDS < deadline )); do
  api_get "$JULES_API_BASE/$session_name" > "$work/session-latest.json"
  state="$(jq -r '.state // "UNKNOWN"' "$work/session-latest.json")"
  echo "Jules MASTER session $session_id state: $state (attempt $task_attempt/2; auto followups $auto_feedback_count/$max_followups)"
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
      if (( auto_feedback_count >= max_followups )); then terminal_state="AUTO_FEEDBACK_EXHAUSTED"; break; fi
      jq -n --arg prompt "$routine_prompt" '{prompt:$prompt}' > "$work/inline-feedback.json"
      api_post_json "$JULES_API_BASE/$session_name:sendMessage" "$work/inline-feedback.json" >/dev/null
      auto_feedback_count=$((auto_feedback_count + 1))
      sleep 10
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
[[ -n "$terminal_state" ]] || terminal_state="WORKFLOW_TIMEOUT"

printf '{"activities":[]}\n' > "$result_dir/activities.json"
page_token=""
pagination_complete=false
declare -A seen_activity_tokens=()
for page in $(seq 1 100); do
  args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
  [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
  response="$work/activities-$page.json"
  curl "${args[@]}" "$JULES_API_BASE/$session_name/activities" > "$response"
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
jq '[.activities[]? as $a | $a.artifacts[]? | .changeSet?.gitPatch? | select(. != null) | {createTime:($a.createTime // ""),patch:.}] | sort_by(.createTime) | last | .patch // null' "$result_dir/activities.json" > "$result_dir/gitpatch.json"
patch_present="$(jq -r 'type == "object"' "$result_dir/gitpatch.json")"
actual_base=""
suggested=""
if [[ "$patch_present" == true ]]; then
  actual_base="$(jq -r '.baseCommitId // ""' "$result_dir/gitpatch.json")"
  suggested="$(jq -r '.suggestedCommitMessage // ""' "$result_dir/gitpatch.json")"
  jq -r '.unidiffPatch // ""' "$result_dir/gitpatch.json" > "$result_dir/changes.patch"
else
  : > "$result_dir/changes.patch"
fi

jq -n \
  --arg protocol "$VAEP_JULES_PROTOCOL" \
  --arg workerId "${WORKER_ID:-JULES_A}" \
  --arg dispatchId "$dispatch_id" \
  --arg taskId "$task_id" \
  --arg session "$session_name" \
  --arg state "$terminal_state" \
  --arg requestedBase "$primary_base" \
  --arg actualBase "$actual_base" \
  --arg suggestedCommitMessage "$suggested" \
  --argjson patchPresent "$patch_present" \
  --argjson autoFeedbackCount "$auto_feedback_count" \
  --argjson taskAttempt "$task_attempt" \
  --argjson maxAttempts "$JULES_MAX_ATTEMPTS_PER_TASK" \
  --argjson parentCloseFirst "$PARENT_CLOSE_FIRST" \
  --arg checkpoints "$VAEP_CHECKPOINTS" \
  '{protocol:$protocol,globalControlPlane:"VAEP_MASTER",parentCloseFirst:$parentCloseFirst,checkpoints:$checkpoints,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,taskAttempt:$taskAttempt,maxAttempts:$maxAttempts,r3Prohibited:true,qaTakeoverOnR2Failure:true,session:$session,state:$state,requestedBase:$requestedBase,actualPatchBase:$actualBase,patchPresent:$patchPresent,suggestedCommitMessage:$suggestedCommitMessage,autoFeedbackCount:$autoFeedbackCount,controllerHandoff:"REVIEW_IMMEDIATELY_AND_ASSIGN_NEXT_SAFE"}' \
  > "$result_dir/result.json"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v issue_body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n' \
  "VAEP $WORKER_LABEL MASTER result and controller handoff signal." \
  "- Protocol: \`MASTER\`; global control-plane: \`MASTER\`" \
  "- Worker: \`${WORKER_ID:-JULES_A}\`" \
  "- Dispatch: \`$dispatch_id\`" \
  "- Task: \`$task_id\`" \
  "- Task attempt: \`$task_attempt/2\`; Jules R3+ is PROHIBITED" \
  "- Jules session: \`$session_name\`" \
  "- Terminal state: \`$terminal_state\`" \
  "- Inline auto-feedback count: \`$auto_feedback_count\`" \
  "- Patch present: \`$patch_present\`; patch base: \`$actual_base\`" \
  "- Controller handoff: \`REVIEW_IMMEDIATELY_AND_ASSIGN_NEXT_SAFE\`" \
  "- Parent-close-first: \`true\`; checkpoints: \`$VAEP_CHECKPOINTS\`" \
  "- Workflow run: $run_url" \
  'Artifact only. Nothing was applied to Desarrollo, pushed, merged or deployed. VAEP/ChatGPT review is mandatory. If ATTEMPT=2 fails review, ChatGPT/VAEP/Vibe takes over; do not create a Jules R3.'
gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id result" --body "$issue_body" >/dev/null

printf 'ARTIFACT_NAME=%s-%s\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

[[ "$terminal_state" == COMPLETED ]] || fail "Jules did not complete successfully: $terminal_state" 50
[[ "$patch_present" == true ]] || fail "Jules completed without ChangeSet/gitPatch." 51
