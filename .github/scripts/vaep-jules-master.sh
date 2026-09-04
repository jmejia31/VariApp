#!/usr/bin/env bash
set -euo pipefail

# Single operational Jules entrypoint.
# ALL operational rules come from docs/VAEP_AUTHORITY.md.
readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly WORKER=".github/scripts/vaep-jules-worker.sh"
readonly CONTROL_STATE_FILE="vaep/control/dispatch-admission.json"
readonly PARENT_LISTO_TARGET_ROLLING_60=3
readonly PARENT_MAX_DWELL_MINUTES=20
readonly JULES_LANE_BUDGET_SECONDS="${VAEP_JULES_LANE_BUDGET_SECONDS:-1080}"

test -f "$MASTER_FILE"
test -f "$WORKER"

if [[ "${1:-}" == "--static-self-test" ]]; then
  [[ "$PARENT_LISTO_TARGET_ROLLING_60" -eq 3 ]]
  [[ "$PARENT_MAX_DWELL_MINUTES" -eq 20 ]]
  [[ "$JULES_LANE_BUDGET_SECONDS" -le 1200 ]]
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
  printf '{"status":"ok","authority":"MASTER","masterFile":"%s","parentListoTargetRolling60":%d,"parentMaxDwellMinutes":%d,"laneBudgetSeconds":%d,"numericProtocolLabelsProhibited":true}\n'     "$MASTER_FILE" "$PARENT_LISTO_TARGET_ROLLING_60" "$PARENT_MAX_DWELL_MINUTES" "$JULES_LANE_BUDGET_SECONDS"
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
if [[ ${#manifests[@]} -eq 0 ]]; then
  printf 'VAEP MASTER NO_OP: no new dispatch manifest was added by %s for %s.\n' "$GITHUB_SHA" "$DISPATCH_PATH"
  exit 0
fi
if [[ ${#manifests[@]} -ne 1 ]]; then
  printf 'VAEP MASTER transport invariant failed: expected exactly one new dispatch manifest; found %d.\n' "${#manifests[@]}" >&2
  exit 21
fi

if [[ ! -f "$CONTROL_STATE_FILE" ]]; then
  printf 'VAEP MASTER admission invariant failed: missing control state file %s.\n' "$CONTROL_STATE_FILE" >&2
  exit 25
fi

if ! jq -e '
  type == "object" and
  has("newDispatchAdmission") and
  (.newDispatchAdmission | type == "string") and
  (.newDispatchAdmission == "OPEN" or .newDispatchAdmission == "FROZEN") and
  has("allowExistingActiveSessions") and
  (.allowExistingActiveSessions == true)
' "$CONTROL_STATE_FILE" >/dev/null; then
  printf 'VAEP MASTER admission invariant failed: invalid control state file %s.\n' "$CONTROL_STATE_FILE" >&2
  exit 25
fi

dispatch_admission="$(jq -r '.newDispatchAdmission' "$CONTROL_STATE_FILE")"
if [[ "$dispatch_admission" == "FROZEN" ]]; then
  printf 'VAEP MASTER rejected new dispatch: NEW_DISPATCH_ADMISSION=FROZEN; existing ACTIVE_REAL sessions remain unaffected.\n' >&2
  exit 26
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

jq -n   --arg authority "MASTER"   --arg masterFile "$MASTER_FILE"   --arg workerId "$WORKER_ID"   --arg dispatchId "$dispatch_id"   --arg taskId "$task_id"   --arg state "JULES_LANE_BUDGET_EXCEEDED"   --argjson laneBudgetSeconds "$JULES_LANE_BUDGET_SECONDS"   --argjson parentListoTargetRolling60 "$PARENT_LISTO_TARGET_ROLLING_60"   --argjson parentMaxDwellMinutes "$PARENT_MAX_DWELL_MINUTES"   '{authority:$authority,masterFile:$masterFile,workerId:$workerId,dispatchId:$dispatchId,taskId:$taskId,state:$state,laneBudgetSeconds:$laneBudgetSeconds,parentListoTargetRolling60:$parentListoTargetRolling60,parentMaxDwellMinutes:$parentMaxDwellMinutes,patchPresent:false,controllerHandoff:"QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY",falseListoProhibited:true,numericProtocolLabelsProhibited:true}'   > "$result_dir/result.json"

run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
printf -v body '%s\n\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n\n%s\n'   "VAEP $WORKER_LABEL MASTER — lane budget exceeded."   "- Authority: `docs/VAEP_AUTHORITY.md`"   "- Worker: `$WORKER_ID`"   "- Dispatch: `$dispatch_id`"   "- Task: `$task_id`"   "- State: `JULES_LANE_BUDGET_EXCEEDED` after ${JULES_LANE_BUDGET_SECONDS}s"   "- Controller handoff: `QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY`"   "- Workflow run: $run_url"   "Fail-closed: timeout does NOT mean LISTO. VAEP must review/recover/rebind according to the MAESTRO. Numeric protocol labels are not authority."

gh issue create --repo "$GITHUB_REPOSITORY" --title "$ISSUE_PREFIX $dispatch_id THROUGHPUT_STALL" --body "$body" >/dev/null || true

printf 'ARTIFACT_NAME=%s-%s-throughput-stall\n' "$ARTIFACT_PREFIX" "$dispatch_id" >> "$GITHUB_ENV"
printf 'RESULT_DIR=%s\n' "$result_dir" >> "$GITHUB_ENV"

printf 'VAEP MASTER lane budget exceeded for %s (%s). Lane released for controller failover.\n' "$dispatch_id" "$task_id" >&2
exit 124
