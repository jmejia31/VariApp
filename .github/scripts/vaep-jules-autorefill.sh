#!/usr/bin/env bash
set -euo pipefail

readonly UNIQUE_REGISTRY="vaep/control/jules-completed-semantic-facets.json"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"

# A terminal hook may be running from an older manifest checkout while the
# control-plane has already advanced. Never let stale autorefill logic create a
# semantically duplicated task under a new numeric id. Compare the local
# controller inputs with the current Desarrollo versions; if any differ, leave
# refill to a fresh run/watchdog instead of publishing from stale logic.
if git remote get-url origin >/dev/null 2>&1; then
  git fetch --quiet origin Desarrollo || true
  if git rev-parse --verify origin/Desarrollo >/dev/null 2>&1; then
    stale=0
    for path in \
      .github/scripts/vaep-jules-autorefill-core.sh \
      .github/scripts/vaep-jules-catalog-floor.sh \
      vaep/control/jules-autorefill-catalog.json \
      vaep/control/jules-completed-semantic-facets.json; do
      if ! git diff --quiet HEAD origin/Desarrollo -- "$path"; then
        stale=1
        echo "AUTOREFILL_WAIT=STALE_CONTROL_PLANE path=$path action=FRESH_WATCHDOG_REFILL"
      fi
    done
    if (( stale != 0 )); then
      exit 0
    fi
  fi
fi

# Durable semantic dedupe guard. A task number/dispatch/session is not identity:
# CURRENT_PARENT + semantic facet is. Remove already-completed facets from the
# local catalog view before any selector can reserve them again. This prevents
# regenerated numeric IDs from becoming DUPLICATE_EVIDENCE_ONLY work.
filter_completed_facets() {
  [[ -f "$UNIQUE_REGISTRY" && -f "$CATALOG" ]] || return 0
  local tmp before after
  before="$(jq '[.lanes[][]?] | length' "$CATALOG")"
  tmp="$(mktemp)"
  jq --slurpfile done "$UNIQUE_REGISTRY" '
    def facet:
      (.taskId // "")
      | try capture("^N4\\.10\\.A\\.[0-9]+\\.(?<f>.*)_TESTS$").f catch "";
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
  echo "AUTOREFILL_UNIQUE_GUARD removed=$((before-after)) registry=$UNIQUE_REGISTRY"
}

filter_completed_facets
bash .github/scripts/vaep-jules-catalog-floor.sh
# catalog-floor may publish a refreshed remote catalog; the current checkout is
# intentionally not mutated by that commit. Re-apply the local semantic guard
# before core selection so this run cannot reserve a completed facet.
filter_completed_facets
exec bash .github/scripts/vaep-jules-autorefill-core.sh
