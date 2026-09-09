#!/usr/bin/env bash
set -euo pipefail

readonly BRANCH="Desarrollo"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly TARGET=12
readonly FLOOR=4
readonly ELIGIBLE_MIN=2

: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY required}"
: "${GH_TOKEN:?GH_TOKEN required}"
: "${WORKER_ID:?WORKER_ID required}"
command -v gh >/dev/null 2>&1 || exit 2
command -v jq >/dev/null 2>&1 || exit 2
command -v python3 >/dev/null 2>&1 || exit 2
test -f "$CATALOG" || { echo "CATALOG_FLOOR_ERROR=catalog_missing" >&2; exit 2; }
test -f scripts/vaep/alex_backlog_planner.py || { echo "CATALOG_FLOOR_ERROR=alex_planner_missing" >&2; exit 2; }

case "$WORKER_ID" in
  J1) DISPATCH_PATH="vaep/jules/dispatch" ;;
  J2) DISPATCH_PATH="vaep/jules-b/dispatch" ;;
  J3) DISPATCH_PATH="vaep/jules-c/dispatch" ;;
  J4) DISPATCH_PATH="vaep/jules-d/dispatch" ;;
  J5) DISPATCH_PATH="vaep/j5/dispatch" ;;
  J6) DISPATCH_PATH="vaep/j6/dispatch" ;;
  *) echo "CATALOG_FLOOR_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;
esac

api(){ gh api "$@"; }

listing_raw="$(api "repos/$GITHUB_REPOSITORY/contents/$DISPATCH_PATH?ref=$BRANCH" 2>/dev/null || true)"
if jq -e 'type == "array"' <<<"${listing_raw:-null}" >/dev/null 2>&1; then
  listing="$listing_raw"
else
  # An unmaterialized dispatch directory is a valid empty queue, not malformed
  # JSON. This is expected for a canonical worker that has never received work.
  listing='[]'
fi
programmed_unused=0
eligible_unused=0

while IFS=$'\t' read -r dispatch eligible; do
  [[ -n "$dispatch" ]] || continue
  if ! jq -e --arg f "$dispatch.json" '.[]? | select(type == "object" and .name==$f)' <<<"$listing" >/dev/null; then
    programmed_unused=$((programmed_unused+1))
    if [[ "$eligible" == "true" ]]; then
      eligible_unused=$((eligible_unused+1))
    fi
  fi
done < <(jq -r --arg w "$WORKER_ID" '.lanes[$w][]? | [(.dispatchId // ""), ((.dispatchEligible != false)|tostring)] | @tsv' "$CATALOG")

request_alex_generation() {
  local runtime_file request deficit eligible_deficit parents expansion_required
  runtime_file="${RUNNER_TEMP:-/tmp}/alex-floor-${WORKER_ID}-${GITHUB_RUN_ID:-local}.json"
  python3 scripts/vaep/alex_backlog_planner.py --output "$runtime_file" >/dev/null
  request="$(jq -c --arg w "$WORKER_ID" '.generationRequests[]? | select(.workerId==$w)' "$runtime_file" | head -n 1)"
  if [[ -z "$request" ]]; then
    echo "CATALOG_FLOOR_ERROR=alex_generation_request_missing worker=$WORKER_ID programmed_unused=$programmed_unused eligible_unused=$eligible_unused" >&2
    return 2
  fi
  deficit="$(jq -r '.programmedDeficit' <<<"$request")"
  eligible_deficit="$(jq -r '.eligibleDeficit' <<<"$request")"
  parents="$(jq -r '[.candidateParents[].parentId] | join(",")' <<<"$request")"
  expansion_required="$(jq -r '.requiresRoadmapExpansion' <<<"$request")"
  echo "ALEX_MATERIAL_GENERATION_REQUEST worker=$WORKER_ID programmed_deficit=$deficit eligible_deficit=$eligible_deficit candidate_parents=${parents:-NONE} roadmap_expansion_required=$expansion_required action=MATERIALIZE_ROADMAP_DERIVED_SCOPES_DEPENDENCY_GATED"
}

low=0
if (( programmed_unused <= FLOOR )); then
  low=1
  echo "CATALOG_PROGRAMMED_LOW worker=$WORKER_ID programmed_unused=$programmed_unused target=$TARGET floor=$FLOOR action=ALEX_ROADMAP_GENERATION_REQUEST"
else
  echo "CATALOG_PROGRAMMED_OK worker=$WORKER_ID programmed_unused=$programmed_unused target=$TARGET floor=$FLOOR"
fi

if (( eligible_unused < ELIGIBLE_MIN )); then
  low=1
  echo "CATALOG_ELIGIBLE_LOW worker=$WORKER_ID eligible_unused=$eligible_unused min=$ELIGIBLE_MIN action=ALEX_DEPENDENCY_SAFE_SCOPE_REQUEST"
else
  echo "CATALOG_ELIGIBLE_OK worker=$WORKER_ID eligible_unused=$eligible_unused min=$ELIGIBLE_MIN"
fi

if (( low != 0 )); then
  request_alex_generation
fi

exit 0
