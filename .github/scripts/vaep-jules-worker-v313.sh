#!/usr/bin/env bash
set -euo pipefail

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

work="$RUNNER_TEMP/vaep-jules-v313"
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

mapfile -t added_manifests < <(git diff-tree --no-commit-id --name-only --diff-filter=A -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
[[ ${#added_manifests[@]} -eq 1 ]] || fail "Expected exactly one newly added dispatch manifest; found ${#added_manifests[@]}." 21
manifest="${added_manifests[0]}"
mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
[[ ${#changed_files[@]} -eq 1 && "${changed_files[0]}" == "$manifest" ]] || fail "Dispatch commit must change exactly the single new manifest." 22

jq -e '
  type == "object" and
  (.dispatchId | type == "string" and test("^[A-Za-z0-9_.-]+$")) and
  (.taskId | type == "string" and test("^[A-Za-z0-9_.-]+$")) and
  (.expectedBranch == "Desarrollo") and
  (.primaryBaseHead | type == "string" and test("^[0-9a-fA-F]{40}$")) and
  (.prompt | type == "string" and length > 0) and
  (.fileScopeHint | type == "string")
' "$manifest" >/dev/null
if [[ -n "$WORKER_ID" ]]; then
  jq -e --arg worker "$WORKER_ID" '.workerId == $worker' "$manifest" >/dev/null || fail "Unexpected workerId in dispatch manifest." 23
fi

dispatch_id="$(jq -r '.dispatchId' "$manifest")"
task_id="$(jq -r '.taskId' "$manifest")"
primary_base="$(jq -r '.primaryBaseHead' "$manifest")"
file_scope="$(jq -r '.fileScopeHint' "$manifest")"
user_prompt="$(jq -r '.prompt' "$manifest")"

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
    "You are $WORKER_LABEL, a trusted secondary developer of the VariApp VAEP team." \
    "PROJECT_ID=VARIAPP" \
    "WORKER_ID=${WORKER_ID:-JULES_A}" \
    "REPOSITORY=jmejia31/VariApp" \
    "BRANCH=Desarrollo" \
    "PRIMARY_BASE_HEAD=$primary_base" \
    "VAEP_TASK_ID=$task_id" \
    "FILE_SCOPE_HINT=$file_scope" \
    "" \
    "Before changing anything, read AGENTS.md and docs/VAEP_JULES.md. Confirm repository, branch and assigned scope." \
    "Work only inside your Jules cloud workspace. Never create branches, pull requests, pushes, merges, deployments, Production changes, secrets, or changes to main." \
    "Do not publish anything to GitHub. Return a reviewable ChangeSet/gitPatch with exact baseCommitId." \
    "Inspect only assigned scope and direct dependencies. If scope materially diverged from PRIMARY_BASE_HEAD and makes the task unsafe, make no changes and report the conflict." \
    "Run proportional tests. Report observations, limitations, risks, recommendations and tests not executed; never claim false PASS." \
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
routine_prompt='VAEP v3.13 automated follow-up. Continue autonomously inside the assigned task and FILE_SCOPE_HINT. Resolve routine ambiguity from AGENTS.md, docs/VAEP_JULES.md, the dispatch manifest, current code and tests. Do not expand scope. If implementation and validations are complete, perform final self-review, report every observation/limitation/risk/recommendation and every test not executed, preserve a reviewable ChangeSet/gitPatch with baseCommitId, and finish as COMPLETED. If and only if a genuine human-only business or authorization decision remains, state the exact blocking question.'

while (( SECONDS < deadline )); do
  api_get "$JULES_API_BASE/$session_name" > "$work/session-latest.json"
  state="$(jq -r '.state // "UNKNOWN"' "$work/session-latest.json")"
  echo "Jules session $session_id state: $state (auto followups $auto_feedback_count/$max_followups)"
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

jq -n --arg workerId "${WORKER_ID:-JULES_A}" --arg dispatchId "$dispatch_id" --arg taskId "$task_id" --arg session "$session_name" --arg state "$terminal_state" --arg requestedBase "$primary_base" --arg actualBase "$actual_base" --arg suggestedCommitMessage "$suggested" --argjson patchPresent "$patch_present" --argjson autoFeedbackCount "$auto_feedback_count" '{workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,session:$session,state:$state,requestedBase:$requestedBase,actualPatchBase:$actualBase,patchPresent:$patchPresent,suggestedCommitMessage:$suggestedCommitMessage,autoFeedbackCount:$autoFeedbackCount}' > "$result_dir/result.json"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v issue_body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n' \
  "VAEP $WORKER_LABEL trusted-secondary-worker result." \
  "- Worker: \`${WORKER_ID:-JULES_A}\`" \
  "- Dispatch: \`$dispatch_id\`" \
  "- Task: \`$task_id\`" \
  "- Jules session: \`$session_name\`" \
  "- Terminal state: \`$terminal_state\`" \
  "- Inline auto-feedback count: \`$auto_feedback_count\`" \
  "- Patch present: \`$patch_present\`; patch base: \`$actual_base\`" \
  "- Workflow run: $run_url" \
  'Artifact only. Nothing was applied to Desarrollo, pushed, merged or deployed. VAEP review is mandatory.'
gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id result" --body "$issue_body" >/dev/null

printf 'ARTIFACT_NAME=%s-%s\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

[[ "$terminal_state" == COMPLETED ]] || fail "Jules did not complete successfully: $terminal_state" 50
[[ "$patch_present" == true ]] || fail "Jules completed without ChangeSet/gitPatch." 51
