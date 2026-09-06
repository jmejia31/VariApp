#!/usr/bin/env bash
set -euo pipefail

readonly BRANCH="Desarrollo"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly TARGET=2
readonly FLOOR=1

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
unused=0
while IFS= read -r dispatch; do
  [[ -n "$dispatch" ]] || continue
  if ! jq -e --arg f "$dispatch.json" '.[]? | select(.name==$f)' <<<"$listing" >/dev/null; then
    unused=$((unused+1))
  fi
done < <(jq -r --arg w "$WORKER_ID" '.lanes[$w][]?.dispatchId // empty' "$CATALOG")

if (( unused <= FLOOR )); then
  echo "CATALOG_FLOOR_LOW worker=$WORKER_ID unused=$unused target=$TARGET action=CONTROLLER_REPLENISH_OR_CLOSE_PARENT"
else
  echo "CATALOG_FLOOR_OK worker=$WORKER_ID unused=$unused target=$TARGET"
fi

# Deliberately no automatic catalog regeneration here.
# Generic facet recycling caused duplicate evidence and parent-close starvation.
# The controller replenishes only material work for the CURRENT_PARENT or closes/promotes it.
exit 0
