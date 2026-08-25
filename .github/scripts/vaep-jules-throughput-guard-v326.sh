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

if [[ "${1:-}" == "--static-self-test" ]]; then
  [[ "$PARENT_LISTO_TARGET_ROLLING_60" -eq 3 ]]
  [[ "$PARENT_MAX_DWELL_MINUTES" -eq 20 ]]
  [[ "$JULES_LANE_BUDGET_SECONDS" -le 1200 ]]
  bash "$INNER_WORKER" --static-self-test >/dev/null
  printf '{"status":"ok","guardProtocol":"%s","parentListoTargetRolling60":%d,"parentMaxDwellMinutes":%d,"laneBudgetSeconds":%d,"innerWorker":"%s"}\n' \
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

mapfile -t manifests < <(git diff-tree --no-commit-id --name-only --diff-filter=A -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
if [[ ${#manifests[@]} -eq 1 ]]; then
  manifest="${manifests[0]}"
  dispatch_id="$(jq -r '.dispatchId // "UNKNOWN"' "$manifest")"
  task_id="$(jq -r '.taskId // "UNKNOWN"' "$manifest")"
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
if [[ -n "$manifest" ]]; then
  cp "$manifest" "$result_dir/dispatch.json"
else
  printf '{}\n' > "$result_dir/dispatch.json"
fi
: > "$result_dir/changes.patch"

jq -n \
  --arg guardProtocol "$VAEP_JULES_GUARD_PROTOCOL" \
  --arg workerId "$WORKER_ID" \
  --arg dispatchId "$dispatch_id" \
  --arg taskId "$task_id" \
  --arg state "JULES_LANE_BUDGET_EXCEEDED" \
  --argjson laneBudgetSeconds "$JULES_LANE_BUDGET_SECONDS" \
  --argjson parentListoTargetRolling60 "$PARENT_LISTO_TARGET_ROLLING_60" \
  --argjson parentMaxDwellMinutes "$PARENT_MAX_DWELL_MINUTES" \
  '{guardProtocol:$guardProtocol,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,state:$state,laneBudgetSeconds:$laneBudgetSeconds,parentListoTargetRolling60:$parentListoTargetRolling60,parentMaxDwellMinutes:$parentMaxDwellMinutes,patchPresent:false,controllerHandoff:"QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY",falseListoProhibited:true}' \
  > "$result_dir/result.json"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n' \
  "VAEP $WORKER_LABEL throughput guard v3.26 — lane budget exceeded." \
  "- Worker: \`$WORKER_ID\`" \
  "- Dispatch: \`$dispatch_id\`" \
  "- Task: \`$task_id\`" \
  "- State: \`JULES_LANE_BUDGET_EXCEEDED\` after ${JULES_LANE_BUDGET_SECONDS}s" \
  "- Parent SLA: \`3 LISTO / rolling 60m\`; max dwell \`20m\`" \
  "- Controller handoff: \`QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY\`" \
  "- Workflow run: $run_url" \
  "Fail-closed: this timeout does NOT mean LISTO. Do not redispatch redundant same-parent evidence. ChatGPT/VAEP must either remove the concrete blocker or rebind the lane to NEXT_SAFE immediately; false LISTO remains prohibited."

gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id THROUGHPUT_STALL" --body "$body" >/dev/null || true

printf 'ARTIFACT_NAME=%s-%s-throughput-stall\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

printf 'VAEP/Jules lane budget exceeded for %s (%s). Lane released for controller failover.\n' "$dispatch_id" "$task_id" >&2
exit 124
