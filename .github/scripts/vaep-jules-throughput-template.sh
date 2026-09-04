#!/usr/bin/env bash
set -euo pipefail

# VAEP/Jules v3.26 throughput guard.
# Keeps the existing v3.25 worker semantics, but prevents one Jules session from
# monopolizing a lane long enough to break the parent-close throughput SLA.
readonly VAEP_JULES_GUARD_PROTOCOL="v3.26"
readonly PARENT_LISTO_TARGET_ROLLING_60=3
readonly PARENT_MAX_DWELL_MINUTES=20
readonly JULES_LANE_BUDGET_SECONDS="${VAEP_JULES_LANE_BUDGET_SECONDS:-1080}"
readonly INNER_WORKER=".github/scripts/vaep-jules-worker-v320.sh"
readonly DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT=true
readonly VERIFY_ATOMIC_DISPATCH_PARENT=true

if [[ "${1:-}" == "--static-self-test" ]]; then
  [[ "$PARENT_LISTO_TARGET_ROLLING_60" -eq 3 ]]
  [[ "$PARENT_MAX_DWELL_MINUTES" -eq 20 ]]
  [[ "$JULES_LANE_BUDGET_SECONDS" -le 1200 ]]
  [[ "$DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT" == true ]]
  [[ "$VERIFY_ATOMIC_DISPATCH_PARENT" == true ]]
  bash "$INNER_WORKER" --static-self-test >/dev/null
  printf '{"status":"ok","guardProtocol":"%s","parentListoTargetRolling60":%d,"parentMaxDwellMinutes":%d,"laneBudgetSeconds":%d,"innerWorker":"%s","deleteOrphanedRemoteSessionOnTimeout":true,"verifyAtomicDispatchParent":true}\n' \
    "$VAEP_JULES_GUARD_PROTOCOL" "$PARENT_LISTO_TARGET_ROLLING_60" "$PARENT_MAX_DWELL_MINUTES" "$JULES_LANE_BUDGET_SECONDS" "$INNER_WORKER"
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
: "${JULES_API_BASE:=https://jules.googleapis.com/v1alpha}"
: "${JULES_API_KEY:?Jules API key is required}"
: "${SESSION_TITLE_PREFIX:?SESSION_TITLE_PREFIX is required}"

mapfile -t manifests < <(git diff-tree --no-commit-id --name-only --diff-filter=A -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
original_manifest=""
transport_verified=false
if [[ ${#manifests[@]} -eq 1 ]]; then
  manifest="${manifests[0]}"
  dispatch_id="$(jq -r '.dispatchId // "UNKNOWN"' "$manifest")"
  task_id="$(jq -r '.taskId // "UNKNOWN"' "$manifest")"

  # The dispatch commit is transport, not product work. Prove that invariant on
  # the trusted GitHub runner before Jules is allowed to reason about its base.
  mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
  if [[ ${#changed_files[@]} -ne 1 || "${changed_files[0]}" != "$manifest" ]]; then
    printf 'VAEP transport invariant failed: dispatch commit must change exactly one file (%s).\n' "$manifest" >&2
    exit 26
  fi

  primary_base="$(jq -r '.primaryBaseHead // empty' "$manifest")"
  if [[ ! "$primary_base" =~ ^[0-9a-fA-F]{40}$ ]]; then
    printf 'VAEP transport invariant failed: invalid PRIMARY_BASE_HEAD in %s.\n' "$manifest" >&2
    exit 27
  fi

  dispatch_parent="$(git rev-parse "${GITHUB_SHA}^")"
  if [[ "${primary_base,,}" != "${dispatch_parent,,}" ]]; then
    printf 'VAEP transport invariant failed: PRIMARY_BASE_HEAD %s is not dispatch parent %s.\n' "$primary_base" "$dispatch_parent" >&2
    exit 28
  fi

  original_manifest="$RUNNER_TEMP/vaep-jules-original-dispatch.json"
  cp "$manifest" "$original_manifest"

  transport_note="VAEP_TRANSPORT_VERIFIED=true
The trusted GitHub VAEP guard verified that PRIMARY_BASE_HEAD=$primary_base is the exact parent of atomic dispatch commit GITHUB_SHA=$GITHUB_SHA and that this dispatch commit changes only the single Jules manifest $manifest. Your Jules cloud workspace HEAD/baseCommitId may therefore be the atomic dispatch commit itself. That one transport-only child commit is EXPECTED and MUST NOT be treated as product/scope divergence. Do not require PRIMARY_BASE_HEAD itself to be locally present or checked out in the Jules workspace. Fail closed only if product/scope-relevant content materially diverges beyond this verified manifest transport."
  injected_manifest="$RUNNER_TEMP/vaep-jules-transport-injected.json"
  jq --arg transport "$transport_note" '.prompt = ($transport + "\n\n" + .prompt)' "$manifest" > "$injected_manifest"
  cat "$injected_manifest" > "$manifest"
  rm -f "$injected_manifest"
  transport_verified=true
else
  manifest=""
  dispatch_id="UNKNOWN-$GITHUB_RUN_ID"
  task_id="UNKNOWN"
fi

set +e
timeout --foreground --signal=TERM --kill-after=30s "${JULES_LANE_BUDGET_SECONDS}s" bash "$INNER_WORKER"
rc=$?
set -e

if [[ "$rc" -ne 124 && "$rc" -ne 137 && "$rc" -ne 143 ]]; then
  exit "$rc"
fi

# A timed-out lane is not allowed to remain ambiguously ACTIVE. Materialize a
# fail-closed controller handoff artifact and issue so the next checkpoint can
# QA-takeover/rebind immediately instead of waiting for the former 110m budget.
result_dir="$RUNNER_TEMP/vaep-jules-throughput-guard"
mkdir -p "$result_dir"
if [[ -n "$original_manifest" && -f "$original_manifest" ]]; then
  cp "$original_manifest" "$result_dir/dispatch.json"
elif [[ -n "$manifest" ]]; then
  cp "$manifest" "$result_dir/dispatch.json"
else
  printf '{}\n' > "$result_dir/dispatch.json"
fi
: > "$result_dir/changes.patch"

# The outer timeout can terminate the local worker while the remote Jules
# session remains alive. Resolve the exact session by its deterministic title
# and delete only a non-terminal exact match. This prevents stale sessions from
# remaining in Jules as IN_PROGRESS / Needs clarification after the VAEP lane
# has already failed over. Completed/failed/paused sessions are preserved.
remote_session=""
remote_session_state=""
remote_cleanup="NOT_FOUND"
if [[ "$DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT" == true && "$dispatch_id" != UNKNOWN-* ]]; then
  expected_title="${SESSION_TITLE_PREFIX}${dispatch_id}"
  page_token=""
  declare -A seen_cleanup_tokens=()
  lookup_failed=false
  for page in $(seq 1 50); do
    args=(--fail-with-body --silent --show-error --get -H "x-goog-api-key: $JULES_API_KEY" --data-urlencode "pageSize=100")
    [[ -z "$page_token" ]] || args+=(--data-urlencode "pageToken=$page_token")
    response="$result_dir/session-cleanup-page-$page.json"
    if ! curl "${args[@]}" "$JULES_API_BASE/sessions" > "$response"; then
      lookup_failed=true
      remote_cleanup="LOOKUP_FAILED"
      rm -f "$response"
      break
    fi
    remote_session="$(jq -r --arg title "$expected_title" '[.sessions[]? | select(.title == $title)] | first | .name // empty' "$response")"
    if [[ -n "$remote_session" ]]; then
      remote_session_state="$(jq -r --arg title "$expected_title" '[.sessions[]? | select(.title == $title)] | first | .state // "UNKNOWN"' "$response")"
      break
    fi
    next_token="$(jq -r '.nextPageToken // empty' "$response")"
    [[ -n "$next_token" ]] || break
    if [[ -n "${seen_cleanup_tokens[$next_token]+x}" ]]; then
      lookup_failed=true
      remote_cleanup="LOOKUP_FAILED_REPEATED_PAGE_TOKEN"
      break
    fi
    seen_cleanup_tokens["$next_token"]=1
    page_token="$next_token"
  done

  if [[ "$lookup_failed" != true && -n "$remote_session" ]]; then
    case "$remote_session_state" in
      COMPLETED|FAILED|PAUSED)
        remote_cleanup="TERMINAL_PRESERVED"
        ;;
      *)
        if curl --fail-with-body --silent --show-error -X DELETE -H "x-goog-api-key: $JULES_API_KEY" "$JULES_API_BASE/$remote_session" >/dev/null; then
          remote_cleanup="DELETED"
        else
          remote_cleanup="DELETE_FAILED"
        fi
        ;;
    esac
  fi
fi

# Do not retain the full Jules sessions listing in the artifact. It is only a
# transient lookup surface and can contain unrelated session metadata.
rm -f "$result_dir"/session-cleanup-page-*.json

jq -n \
  --arg guardProtocol "$VAEP_JULES_GUARD_PROTOCOL" \
  --arg workerId "$WORKER_ID" \
  --arg dispatchId "$dispatch_id" \
  --arg taskId "$task_id" \
  --arg state "JULES_LANE_BUDGET_EXCEEDED" \
  --arg remoteSession "$remote_session" \
  --arg remoteSessionState "$remote_session_state" \
  --arg remoteSessionCleanup "$remote_cleanup" \
  --argjson transportVerified "$transport_verified" \
  --argjson laneBudgetSeconds "$JULES_LANE_BUDGET_SECONDS" \
  --argjson parentListoTargetRolling60 "$PARENT_LISTO_TARGET_ROLLING_60" \
  --argjson parentMaxDwellMinutes "$PARENT_MAX_DWELL_MINUTES" \
  '{guardProtocol:$guardProtocol,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,state:$state,laneBudgetSeconds:$laneBudgetSeconds,parentListoTargetRolling60:$parentListoTargetRolling60,parentMaxDwellMinutes:$parentMaxDwellMinutes,transportVerified:$transportVerified,remoteSession:$remoteSession,remoteSessionState:$remoteSessionState,remoteSessionCleanup:$remoteSessionCleanup,patchPresent:false,controllerHandoff:"QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY",falseListoProhibited:true}' \
  > "$result_dir/result.json"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n' \
  "VAEP $WORKER_LABEL throughput guard v3.26 — lane budget exceeded." \
  "- Worker: \`$WORKER_ID\`" \
  "- Dispatch: \`$dispatch_id\`" \
  "- Task: \`$task_id\`" \
  "- State: \`JULES_LANE_BUDGET_EXCEEDED\` after ${JULES_LANE_BUDGET_SECONDS}s" \
  "- Atomic transport verified: \`$transport_verified\`" \
  "- Remote Jules cleanup: \`$remote_cleanup\`; prior state: \`${remote_session_state:-UNKNOWN}\`" \
  "- Parent SLA: \`3 LISTO / rolling 60m\`; max dwell \`20m\`" \
  "- Controller handoff: \`QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY\`" \
  "- Workflow run: $run_url" \
  "Fail-closed: this timeout does NOT mean LISTO. Do not redispatch redundant same-parent evidence. ChatGPT/VAEP must either remove the concrete blocker or rebind the lane to NEXT_SAFE immediately; false LISTO remains prohibited."

gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id THROUGHPUT_STALL" --body "$body" >/dev/null || true

printf 'ARTIFACT_NAME=%s-%s-throughput-stall\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

printf 'VAEP/Jules lane budget exceeded for %s (%s). Transport verified=%s; remote cleanup=%s. Lane released for controller failover.\n' "$dispatch_id" "$task_id" "$transport_verified" "$remote_cleanup" >&2
exit 124
