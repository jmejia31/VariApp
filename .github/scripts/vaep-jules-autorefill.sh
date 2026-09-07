#!/usr/bin/env bash
set -euo pipefail

readonly UNIQUE_REGISTRY="vaep/control/jules-completed-semantic-facets.json"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly BRANCH="Desarrollo"

post_terminal=0
if [[ "${1:-}" == "--post-terminal" ]]; then
  post_terminal=1
  : "${RUNNER_TEMP:?RUNNER_TEMP required for post-terminal refill}"
  state_file="$RUNNER_TEMP/vaep-jules-runtime-state.json"
  [[ -f "$state_file" ]] || { echo "AUTOREFILL_POST_TERMINAL_REJECT reason=runtime_state_missing" >&2; exit 80; }
  phase="$(jq -r '.phase // empty' "$state_file")"
  case "$phase" in
    TERMINAL_*|STALL_NO_PROGRESS|LANE_BUDGET_EXCEEDED)
      echo "AUTOREFILL_WAIT=REVIEW_FIRST_REQUIRED phase=$phase action=NO_POST_TERMINAL_REFILL" >&2
      exit 81
      ;;
    *)
      echo "AUTOREFILL_POST_TERMINAL_REJECT phase=${phase:-MISSING} reason=session_not_terminal" >&2
      exit 80
      ;;
  esac
fi

# A terminal hook may be running from an older manifest checkout while the
# control-plane has already advanced. Logic changes remain fail-closed, but
# data-only catalog/registry changes are refreshed from current Desarrollo for
# non-terminal reservation paths. Post-terminal refill is intentionally held
# until REVIEW_FIRST/classification resolves the previous result.
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

review_first_debt_exists() {
  local current_parent issues commits integrated_json count
  [[ -f "$CATALOG" ]] || return 1
  current_parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  [[ -n "$current_parent" ]] || return 1
  [[ -n "${GITHUB_REPOSITORY:-}" && -n "${WORKER_ID:-}" && -n "${GH_TOKEN:-}" ]] || return 1

  issues="$(api "repos/$GITHUB_REPOSITORY/issues?state=open&per_page=100&sort=updated&direction=desc")"
  commits="$(api "repos/$GITHUB_REPOSITORY/commits?sha=$BRANCH&per_page=100")"
  integrated_json="$(jq -c '
    [.[]?
      | (.commit.message // "" | split("\n")) as $lines
      | select($lines | index("VAEP-Review: ACCEPTED"))
      | select($lines | index("VAEP-Integrated: true"))
      | ($lines[]? | select(startswith("VAEP-Dispatch: ")) | sub("^VAEP-Dispatch: "; ""))
    ] | unique
  ' <<<"$commits")"

  count="$(jq --arg worker "$WORKER_ID" --arg parent "$current_parent" --argjson integrated "$integrated_json" '
    [.[]?
      | (.body // "") as $b
      | ($b | split("\n")[]? | select(startswith("- Worker: `")) | sub("^- Worker: `"; "") | sub("`.*$"; "")) as $issue_worker
      | ($b | split("\n")[]? | select(startswith("- Task: `")) | sub("^- Task: `"; "") | sub("`.*$"; "")) as $task
      | ($b | split("\n")[]? | select(startswith("- Dispatch: `")) | sub("^- Dispatch: `"; "") | sub("`.*$"; "")) as $dispatch
      | select($issue_worker == $worker)
      | select($task | startswith($parent + "."))
      | select($b | contains("- Terminal state: `COMPLETED`"))
      | select($b | contains("- Patch present: `true`"))
      | select(($integrated | index($dispatch)) | not)
    ] | length
  ' <<<"$issues")"

  if (( count > 0 )); then
    echo "AUTOREFILL_WAIT=REVIEW_FIRST_REQUIRED worker=$WORKER_ID current_parent=$current_parent pending_review_results=$count"
    return 0
  fi
  return 1
}

# REVIEW_FIRST is a hard gate for every refill path, not just the terminal hook.
# A completed patch must be classified and integrated/rejected before a lane can
# consume or replace its pre-reserved NEXT_SAFE.
if review_first_debt_exists; then
  exit 81
fi

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

filter_dependency_safe_parent
filter_completed_facets
bash .github/scripts/vaep-jules-catalog-floor.sh
# catalog-floor may publish a refreshed remote catalog; the current checkout is
# intentionally not mutated by that commit. Re-apply guards before core
# selection so this run cannot reserve a future-parent or completed facet.
filter_dependency_safe_parent
filter_completed_facets
exec bash .github/scripts/vaep-jules-autorefill-core.sh
