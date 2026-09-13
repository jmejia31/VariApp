#!/usr/bin/env bash
set -euo pipefail

readonly REGISTRY="vaep/control/jules-workers.json"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"

# Historical compatibility entrypoint. The old Jules queue/backlog floor is no
# longer a production requirement. TASKS_FIRST uses direct execution first and
# permits Jules only through explicit, beneficial offload.

command -v jq >/dev/null 2>&1 || exit 2
[[ -f "$REGISTRY" ]] || { echo "JULES_DIAGNOSTIC_ERROR=registry_missing" >&2; exit 2; }
[[ -f "$CATALOG" ]] || { echo "JULES_DIAGNOSTIC_ERROR=catalog_missing" >&2; exit 2; }

model="$(jq -r '.executionModel // empty' "$REGISTRY")"
autorefill="$(jq -r '.automaticRefillEnabled // true' "$REGISTRY")"
queuefloor="$(jq -r '.automaticQueueFloorEnabled // true' "$REGISTRY")"

if [[ "$model" != "TASKS_FIRST_JULES_ON_DEMAND" || "$autorefill" != "false" || "$queuefloor" != "false" ]]; then
  echo "JULES_DIAGNOSTIC_ERROR=registry_contract_mismatch model=${model:-MISSING}" >&2
  exit 2
fi

worker="${WORKER_ID:-UNSPECIFIED}"
current_parent="$(jq -r '.currentParent // "UNKNOWN"' "$CATALOG")"

# No TARGET/FLOOR/ELIGIBLE_MIN checks exist here by design. An empty Jules lane
# is healthy unless a controller has explicitly approved an offload. This output
# is informational only and cannot trigger materialization or dispatch.
echo "JULES_QUEUE_DIAGNOSTIC worker=$worker current_parent=$current_parent state=OPTIONAL_ON_DEMAND automatic_refill=false queue_floor=false production_gate=false"
exit 0
