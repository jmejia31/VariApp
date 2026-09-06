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
test -f "$CATALOG" || { echo "CATALOG_FLOOR_ERROR=catalog_missing" >&2; exit 2; }

case "$WORKER_ID" in
  JULES_A) DISPATCH_PATH="vaep/jules/dispatch" ;;
  JULES_B) DISPATCH_PATH="vaep/jules-b/dispatch" ;;
  JULES_C) DISPATCH_PATH="vaep/jules-c/dispatch" ;;
  JULES_D) DISPATCH_PATH="vaep/jules-d/dispatch" ;;
  *) echo "CATALOG_FLOOR_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;
esac

api(){ gh api "$@"; }

listing="$(api "repos/$GITHUB_REPOSITORY/contents/$DISPATCH_PATH?ref=$BRANCH" 2>/dev/null || printf '[]')"
programmed_unused=0
eligible_unused=0

while IFS=$'\t' read -r dispatch eligible; do
  [[ -n "$dispatch" ]] || continue
  if ! jq -e --arg f "$dispatch.json" '.[]? | select(.name==$f)' <<<"$listing" >/dev/null; then
    programmed_unused=$((programmed_unused+1))
    if [[ "$eligible" == "true" ]]; then
      eligible_unused=$((eligible_unused+1))
    fi
  fi
done < <(jq -r --arg w "$WORKER_ID" '.lanes[$w][]? | [(.dispatchId // ""), ((.dispatchEligible // true)|tostring)] | @tsv' "$CATALOG")

if (( programmed_unused <= FLOOR )); then
  echo "CATALOG_PROGRAMMED_LOW worker=$WORKER_ID programmed_unused=$programmed_unused target=$TARGET floor=$FLOOR action=CONTROLLER_REPLENISH_TO_TARGET"
else
  echo "CATALOG_PROGRAMMED_OK worker=$WORKER_ID programmed_unused=$programmed_unused target=$TARGET floor=$FLOOR"
fi

if (( eligible_unused < ELIGIBLE_MIN )); then
  echo "CATALOG_ELIGIBLE_LOW worker=$WORKER_ID eligible_unused=$eligible_unused min=$ELIGIBLE_MIN action=ACTIVATE_SAFE_WORK_OR_CLOSE_PROMOTE"
else
  echo "CATALOG_ELIGIBLE_OK worker=$WORKER_ID eligible_unused=$eligible_unused min=$ELIGIBLE_MIN"
fi

exit 0
