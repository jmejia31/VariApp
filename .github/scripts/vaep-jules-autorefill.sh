#!/usr/bin/env bash
set -euo pipefail

readonly UNIQUE_REGISTRY="vaep/control/jules-completed-semantic-facets.json"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly BRANCH="Desarrollo"
readonly ISSUES_CACHE="${RUNNER_TEMP:-/tmp}/vaep-issues-${GITHUB_RUN_ID:-local}.json"

# J1-J6 cutover guard: a legacy lane may finish an already-created session,
# but it must never publish/refill new legacy work after cutover.
if [[ -f vaep/control/jules-workers.json ]] && jq -e '.cutoverEnabled == true' vaep/control/jules-workers.json >/dev/null 2>&1; then
  case "${WORKER_ID:-}" in
    JULES_A|JULES_B|JULES_C|JULES_D)
      echo "AUTOREFILL_LEGACY_NOOP worker=${WORKER_ID} reason=J1_J6_CUTOVER_ACTIVE"
      exit 0
      ;;
  esac
fi

if [[ "${1:-}" == "--post-terminal" ]]; then
  : "${RUNNER_TEMP:?RUNNER_TEMP required for post-terminal refill}"
  state_file="$RUNNER_TEMP/vaep-jules-runtime-state.json"
  [[ -f "$state_file" ]] || { echo "AUTOREFILL_POST_TERMINAL_REJECT reason=runtime_state_missing" >&2; exit 80; }
  phase="$(jq -r '.phase // empty' "$state_file")"
  case "$phase" in
    TERMINAL_*|STALL_NO_PROGRESS|LANE_BUDGET_EXCEEDED)
      echo "AUTOREFILL_TERMINAL_HANDOFF phase=$phase action=CONTINUE_LANE_REFILL_REVIEW_FIRST_IN_PARALLEL" >&2
      ;;
    *)
      echo "AUTOREFILL_POST_TERMINAL_REJECT phase=${phase:-MISSING} reason=session_not_terminal" >&2
      exit 80
      ;;
  esac
fi

# Refresh control data from current Desarrollo, but never run stale logic.
if git remote get-url origin >/dev/null 2>&1; then
  git fetch --quiet origin Desarrollo || true
  if git rev-parse --verify origin/Desarrollo >/dev/null 2>&1; then
    stale_logic=0
    for path in \
      .github/scripts/vaep-jules-autorefill.sh \
      .github/scripts/vaep-jules-autorefill-core.sh \
      .github/scripts/vaep-jules-catalog-floor.sh; do
      if ! git diff --quiet HEAD origin/Desarrollo -- "$path"; then
        stale_logic=1
        echo "AUTOREFILL_WAIT=STALE_LOGIC path=$path action=FRESH_WATCHDOG_REFILL"
      fi
    done
    if (( stale_logic != 0 )); then
      exit 0
    fi

    for path in \
      vaep/control/jules-autorefill-catalog.json \
      vaep/control/jules-completed-semantic-facets.json; do
      if ! git diff --quiet HEAD origin/Desarrollo -- "$path"; then
        git checkout --quiet origin/Desarrollo -- "$path"
        echo "AUTOREFILL_REFRESHED_CONTROL_DATA path=$path source=origin/Desarrollo"
      fi
    done
  fi
fi

api() {
  gh api "$@"
}

worker_has_current_material() {
  [[ -f "$CATALOG" ]] || return 1
  jq -e --arg w "${WORKER_ID:-}" '
    (.currentParent // "") as $parent
    | ($parent | length) > 0 and
      any(.lanes[$w][]?;
        (.dispatchEligible != false) and
        ((.plannedParent // "") == $parent) and
        ((.taskId // "") | startswith($parent + ".")))
  ' "$CATALOG" >/dev/null 2>&1
}

# When PARENT_CLOSE_FIRST makes future material non-dispatchable, idle lanes
# still precompute one distinct roadmap-derived NEXT candidate without moving
# HEAD, creating a manifest, starting a Jules session, or consuming an attempt.
# This is preparation only: it can reduce handoff latency after the dependency
# clears, but it can never satisfy ACTIVE_REAL/LISTO_REAL by itself.
emit_read_only_prearm() {
  [[ -f scripts/vaep/alex_backlog_planner.py ]] || return 0
  local runtime_file candidate packet
  runtime_file="${RUNNER_TEMP:-/tmp}/alex-prearm-${WORKER_ID:-UNKNOWN}-${GITHUB_RUN_ID:-local}.json"
  if ! python3 scripts/vaep/alex_backlog_planner.py --output "$runtime_file" >/dev/null 2>&1; then
    echo "AUTOREFILL_PREARM_UNAVAILABLE worker=${WORKER_ID:-MISSING} reason=PLANNER_FAILED" >&2
    return 0
  fi
  candidate="$(jq -c --arg w "${WORKER_ID:-}" '
    [.generationRequests[]? | select(.workerId==$w) | .candidateParents[]?]
    | sort_by((if .dependencyState=="READY" then 0 else 1 end), (.order // 999999999))
    | .[0] // empty
  ' "$runtime_file")"
  if [[ -z "$candidate" ]]; then
    echo "AUTOREFILL_NO_SAFE_NEXT worker=${WORKER_ID:-MISSING} mode=READ_ONLY_PREARM reason=NO_DISTINCT_ROADMAP_CANDIDATE" >&2
    return 0
  fi
  packet="$(jq -cn \
    --arg worker "${WORKER_ID:-UNKNOWN}" \
    --arg branch "$BRANCH" \
    --arg currentParent "$(jq -r '.currentParent // ""' "$CATALOG")" \
    --argjson candidate "$candidate" \
    '{mode:"READ_ONLY_PREARM",workerId:$worker,branch:$branch,currentParent:$currentParent,candidate:$candidate,allowProductCodeWrite:false,allowManifestPublish:false,allowWorkflowDispatch:false,allowIntegration:false,allowPromotion:false,countsAsActiveReal:false,countsAsListoReal:false,consumesAttempt:false,revalidateDependenciesBeforeMaterialization:true,headMoving:false}')"
  echo "AUTOREFILL_READ_ONLY_PREARM_READY packet=$packet"
}

# Read the live issue window at most once per controller run. All six lanes
# share RUNNER_TEMP in the single checkpoint job, so this cache eliminates the
# previous repeated issue-feed scans without weakening the fail-closed guard.
fetch_issues_array() {
  local endpoint payload attempt
  if [[ -s "$ISSUES_CACHE" ]] && jq -e 'type == "array"' "$ISSUES_CACHE" >/dev/null 2>&1; then
    cat "$ISSUES_CACHE"
    return 0
  fi
  endpoint="repos/$GITHUB_REPOSITORY/issues?state=all&per_page=100&sort=updated&direction=desc"
  for attempt in 1 2 3; do
    if payload="$(api "$endpoint" 2>/dev/null)" && jq -e 'type == "array"' <<<"$payload" >/dev/null 2>&1; then
      printf '%s\n' "$payload" > "$ISSUES_CACHE"
      printf '%s\n' "$payload"
      return 0
    fi
    sleep 1
  done
  return 1
}

# Dependency-safe parent guard. dispatchEligible alone is insufficient: a
# stale/misprogrammed catalog must never dispatch work belonging to a future
# parent while currentParent is still open.
filter_dependency_safe_parent() {
  [[ -f "$CATALOG" ]] || return 0
  local parent tmp before after
  parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  [[ -n "$parent" ]] || return 0
  before="$(jq '[.lanes[][]? | select((.dispatchEligible != false))] | length' "$CATALOG")"
  tmp="$(mktemp)"
  jq --arg parent "$parent" '
    .lanes |= with_entries(
      .value |= map(
        if ((.dispatchEligible != false)) and ((.plannedParent // $parent) != $parent)
        then .dispatchEligible = false
        else .
        end
      )
    )
  ' "$CATALOG" > "$tmp"
  mv "$tmp" "$CATALOG"
  after="$(jq '[.lanes[][]? | select((.dispatchEligible != false))] | length' "$CATALOG")"
  if (( after < before )); then
    echo "AUTOREFILL_PARENT_GUARD current_parent=$parent blocked_future_parent_entries=$((before-after))"
  fi
}

# Durable semantic dedupe guard. Task number/dispatch/session is not identity:
# CURRENT_PARENT + semantic facet is.
filter_completed_facets() {
  [[ -f "$UNIQUE_REGISTRY" && -f "$CATALOG" ]] || return 0
  local tmp before after parent registry_parent
  parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  registry_parent="$(jq -r '.currentParent // empty' "$UNIQUE_REGISTRY")"
  [[ -n "$parent" ]] || return 0

  if [[ -n "$registry_parent" && "$registry_parent" != "$parent" ]]; then
    echo "AUTOREFILL_UNIQUE_GUARD registry_parent=$registry_parent current_parent=$parent action=IGNORE_HISTORICAL_REGISTRY"
    return 0
  fi

  before="$(jq '[.lanes[][]?] | length' "$CATALOG")"
  tmp="$(mktemp)"
  jq --arg parent "$parent" --slurpfile done "$UNIQUE_REGISTRY" '
    def facet:
      (.taskId // "") as $task
      | if ($task | startswith($parent + ".")) then
          ($task | ltrimstr($parent + ".") | split(".") | .[1:] | join(".") | sub("_TESTS$"; ""))
        else "" end;
    .lanes |= with_entries(
      .key as $lane
      | .value |= map(
          select(
            facet as $f
            | $f == "" or (($done[0].lanes[$lane] // []) | index($f) | not)
          )
        )
    )
  ' "$CATALOG" > "$tmp"
  mv "$tmp" "$CATALOG"
  after="$(jq '[.lanes[][]?] | length' "$CATALOG")"
  echo "AUTOREFILL_UNIQUE_GUARD removed=$((before-after)) parent=$parent registry=$UNIQUE_REGISTRY"
}

# REVIEW_FIRST debt is never a content retry.
filter_evidence_gap_review_debt() {
  [[ -f "$CATALOG" ]] || return 0
  local issues tmp before after blocked
  if ! worker_has_current_material; then
    return 0
  fi
  if ! issues="$(fetch_issues_array)"; then
    echo "AUTOREFILL_WAIT=ISSUES_FEED_UNAVAILABLE_OR_MALFORMED action=FAIL_CLOSED_NO_DISPATCH" >&2
    exit 0
  fi
  blocked="$(jq -r '
    .[]?
    | select(type == "object")
    | (.body? // "") as $b
    | select($b | contains("Terminal contract classification: `EVIDENCE_GAP_REVIEW_REQUIRED`"))
    | select($b | test("(?m)^- Task attempt: `1/[0-9]+`$|attempt: `?1/[0-9]+`?"))
    | ($b | split("\n")[]? | select(startswith("- Dispatch: `")) | sub("^- Dispatch: `"; "") | sub("`.*$"; ""))
  ' <<<"$issues" | sort -u)"
  [[ -n "$blocked" ]] || return 0
  before="$(jq '[.lanes[][]? | select(.dispatchEligible != false)] | length' "$CATALOG")"
  tmp="$(mktemp)"
  jq --arg blocked "$blocked" '
    ($blocked | split("\n") | map(select(length > 0))) as $ids
    | .lanes |= with_entries(.value |= map(if (.dispatchId as $d | $ids | index($d)) then .dispatchEligible=false else . end))
  ' "$CATALOG" > "$tmp"
  mv "$tmp" "$CATALOG"
  after="$(jq '[.lanes[][]? | select(.dispatchEligible != false)] | length' "$CATALOG")"
  echo "AUTOREFILL_REVIEW_DEBT_GUARD blocked=$((before-after)) action=NO_R2_REVIEW_OR_QA"
}

filter_dependency_safe_parent
filter_completed_facets

# Most checkpoint lanes are intentionally idle when the CURRENT_PARENT has a
# single material facet. Prearm one distinct future roadmap candidate locally,
# then stop before REST/API dispatch work. The prearm packet is non-authoritative
# and must be dependency-revalidated after CURRENT_PARENT closes.
if ! worker_has_current_material; then
  emit_read_only_prearm
  echo "AUTOREFILL_UNIQUE_WORK_EXHAUSTED worker=${WORKER_ID:-MISSING} current_parent=$(jq -r '.currentParent // "MISSING"' "$CATALOG") action=LOCAL_NO_API_IDLE_LANE" >&2
  exit 78
fi

filter_evidence_gap_review_debt
if ! worker_has_current_material; then
  emit_read_only_prearm
  echo "AUTOREFILL_NO_SAFE_NEXT worker=${WORKER_ID:-MISSING} reason=REVIEW_FIRST_DEBT_GUARD" >&2
  exit 78
fi

bash .github/scripts/vaep-jules-catalog-floor.sh
# catalog-floor may publish a refreshed remote catalog; re-apply guards before
# core selection so no future-parent/completed/review-only facet can run.
filter_dependency_safe_parent
filter_completed_facets
filter_evidence_gap_review_debt

if ! worker_has_current_material; then
  emit_read_only_prearm
  echo "AUTOREFILL_UNIQUE_WORK_EXHAUSTED worker=${WORKER_ID:-MISSING} current_parent=$(jq -r '.currentParent // "MISSING"' "$CATALOG") action=POST_GUARD_NO_API_IDLE_LANE" >&2
  exit 78
fi

exec bash .github/scripts/vaep-jules-autorefill-core.sh
