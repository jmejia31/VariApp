#!/usr/bin/env bash
set -euo pipefail

readonly UNIQUE_REGISTRY="vaep/control/jules-completed-semantic-facets.json"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly BRANCH="Desarrollo"

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
      # REVIEW_FIRST owns the delivered scope, not the lane. The MASTER
      # requires review/QA to run in parallel while the terminal workflow
      # releases ownership and replenishes CURRENT + NEXT_SAFE material.
      # The core still enforces admission, active-run depth, immutable
      # manifests, semantic dedupe and the R2/R3 limits.
      echo "AUTOREFILL_TERMINAL_HANDOFF phase=$phase action=CONTINUE_LANE_REFILL_REVIEW_FIRST_IN_PARALLEL" >&2
      ;;
    *)
      echo "AUTOREFILL_POST_TERMINAL_REJECT phase=${phase:-MISSING} reason=session_not_terminal" >&2
      exit 80
      ;;
  esac
fi

# A terminal hook may be running from an older manifest checkout while the
# control-plane has already advanced. Logic changes remain fail-closed. For
# non-terminal reservation paths, data-only catalog/registry changes may be
# refreshed from current Desarrollo. Terminal hooks already exited above and
# can never reserve or publish replacement work before REVIEW_FIRST.
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

# Dependency-safe parent guard. dispatchEligible alone is insufficient: a
# stale/misprogrammed catalog must never dispatch work belonging to a future
# parent while currentParent is still open. This guard is local/fail-closed;
# it does not mutate the canonical catalog and therefore cannot create a
# control-plane race from a terminal worker checkout.
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
# CURRENT_PARENT + semantic facet is. The parent comes from the live catalog;
# never hard-code a previous parent in this guard.
filter_completed_facets() {
  [[ -f "$UNIQUE_REGISTRY" && -f "$CATALOG" ]] || return 0
  local tmp before after parent registry_parent
  parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  registry_parent="$(jq -r '.currentParent // empty' "$UNIQUE_REGISTRY")"
  [[ -n "$parent" ]] || return 0

  # A registry for a different parent is historical evidence only and must not
  # suppress unique work in the newly promoted parent.
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

# REVIEW_FIRST debt is never a content retry. Fail closed before the core sees
# a candidate whose attempt-1 terminal result is explicitly classified as an
# evidence gap. The worker output uses backticked `1/2`; match structurally
# instead of relying on the old literal "attempt: 1/" substring. This guard is
# deliberately duplicated outside the recovery core so stale or malformed
# recovery classification cannot manufacture R2 work from review-only debt.
filter_evidence_gap_review_debt() {
  [[ -f "$CATALOG" ]] || return 0
  local issues tmp before after blocked
  issues="$(api "repos/$GITHUB_REPOSITORY/issues?state=all&per_page=100&sort=updated&direction=desc" 2>/dev/null || printf '[]')"
  blocked="$(jq -r '
    .[]?
    | (.body // "") as $b
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
filter_evidence_gap_review_debt
bash .github/scripts/vaep-jules-catalog-floor.sh
# catalog-floor may publish a refreshed remote catalog; the current checkout is
# intentionally not mutated by that commit. Re-apply guards before core
# selection so this run cannot reserve a future-parent, completed facet or
# REVIEW_FIRST-only debt.
filter_dependency_safe_parent
filter_completed_facets
filter_evidence_gap_review_debt
exec bash .github/scripts/vaep-jules-autorefill-core.sh
