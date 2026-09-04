#!/usr/bin/env bash
set -euo pipefail

# Single operational Jules entrypoint.
# ALL operational rules come from docs/VAEP_AUTHORITY.md.
readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly WORKER=".github/scripts/vaep-jules-worker.sh"

test -f "$MASTER_FILE"
test -f "$PARSER"
test -f "$WORKER"

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
: "${JULES_LANE_BUDGET_SECONDS:?MASTER policy parser did not emit JULES_LANE_BUDGET_SECONDS}"
: "${JULES_MAX_ATTEMPTS:?MASTER policy parser did not emit JULES_MAX_ATTEMPTS}"
: "${JULES_REWORK_MAX:?MASTER policy parser did not emit JULES_REWORK_MAX}"
: "${PARENT_CLOSE_FIRST:?MASTER policy parser did not emit PARENT_CLOSE_FIRST}"
: "${AUTOMATION_POLICY_HASH:?MASTER policy parser did not emit AUTOMATION_POLICY_HASH}"
: "${MASTER_COMMIT_SHA:?MASTER policy parser did not emit MASTER_COMMIT_SHA}"

readonly PARENT_LISTO_TARGET_ROLLING_60
readonly PARENT_MAX_DWELL_MINUTES
readonly JULES_LANE_BUDGET_SECONDS
readonly JULES_MAX_ATTEMPTS
readonly JULES_REWORK_MAX
readonly PARENT_CLOSE_FIRST
readonly AUTOMATION_POLICY_HASH
readonly MASTER_COMMIT_SHA

if [[ "${1:-}" == "--static-self-test" ]]; then
  bash "$PARSER" --self-test >/dev/null
  [[ "$PARENT_LISTO_TARGET_ROLLING_60" =~ ^[1-9][0-9]*$ ]]
  [[ "$PARENT_MAX_DWELL_MINUTES" =~ ^[1-9][0-9]*$ ]]
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
  printf '{"status":"ok","authority":"MASTER","masterFile":"%s","parentListoTargetRolling60":%d,"parentMaxDwellMinutes":%d,"laneBudgetSeconds":%d,"policyHash":"%s","numericProtocolLabelsProhibited":true}\n' \
    "$MASTER_FILE" "$PARENT_LISTO_TARGET_ROLLING_60" "$PARENT_MAX_DWELL_MINUTES" "$JULES_LANE_BUDGET_SECONDS" "$AUTOMATION_POLICY_HASH"
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
if [[ ${#manifests[@]} -ne 1 ]]; then
  printf 'VAEP MASTER transport invariant failed: expected exactly one new dispatch manifest; found %d.\n' "${#manifests[@]}" >&2
  exit 21
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

jq -n \
  --arg authority "MASTER" \
  --arg masterFile "$MASTER_FILE" \
  --arg policyHash "$AUTOMATION_POLICY_HASH" \
  --arg masterCommitSha "$MASTER_COMMIT_SHA" \
  --arg workerId "$WORKER_ID" \
  --arg dispatchId "$dispatch_id" \
  --arg taskId "$task_id" \
  --arg state "JULES_LANE_BUDGET_EXCEEDED" \
  --argjson laneBudgetSeconds "$JULES_LANE_BUDGET_SECONDS" \
  --argjson parentListoTargetRolling60 "$PARENT_LISTO_TARGET_ROLLING_60" \
  --argjson parentMaxDwellMinutes "$PARENT_MAX_DWELL_MINUTES" \
  '{authority:$authority,masterFile:$masterFile,masterCommitSha:$masterCommitSha,policyHash:$policyHash,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,state:$state,laneBudgetSeconds:$laneBudgetSeconds,parentListoTargetRolling60:$parentListoTargetRolling60,parentMaxDwellMinutes:$parentMaxDwellMinutes,patchPresent:false,controllerHandoff:"QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY",falseListoProhibited:true,numericProtocolLabelsProhibited:true}' \
  > "$result_dir/result.json"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n'   "VAEP $WORKER_LABEL MASTER — lane budget exceeded."   "- Authority: `docs/VAEP_AUTHORITY.md`"   "- Worker: `$WORKER_ID`"   "- Dispatch: `$dispatch_id`"   "- Task: `$task_id`"   "- State: `JULES_LANE_BUDGET_EXCEEDED` after ${JULES_LANE_BUDGET_SECONDS}s"   "- Controller handoff: `QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY`"   "- Workflow run: $run_url"   "Fail-closed: timeout does NOT mean LISTO. VAEP must review/recover/rebind according to the MAESTRO. Numeric protocol labels are not authority."

gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id THROUGHPUT_STALL" --body "$body" >/dev/null || true

printf 'ARTIFACT_NAME=%s-%s-throughput-stall\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

printf 'VAEP MASTER lane budget exceeded for %s (%s). Lane released for controller failover.\n' "$dispatch_id" "$task_id" >&2
exit 124
