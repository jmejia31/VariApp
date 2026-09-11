#!/usr/bin/env bash
set -euo pipefail

readonly BRANCH="Desarrollo"
readonly REGISTRY="vaep/control/jules-workers.json"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"

# Compatibility entrypoint retained so historical workflow references do not
# break. Under TASKS_FIRST_JULES_ON_DEMAND this script is NOT an autonomous
# refill loop. Scheduled VAEP tasks execute material work directly by default.
# Jules may be invoked only after an explicit, scope-specific offload decision.

command -v jq >/dev/null 2>&1 || { echo "JULES_OFFLOAD_ERROR=jq_missing" >&2; exit 2; }
[[ -f "$REGISTRY" ]] || { echo "JULES_OFFLOAD_ERROR=registry_missing" >&2; exit 2; }

model="$(jq -r '.executionModel // empty' "$REGISTRY")"
autorefill="$(jq -r '.automaticRefillEnabled // true' "$REGISTRY")"
required="$(jq -r '.julesRequiredForProgress // true' "$REGISTRY")"

if [[ "$model" != "TASKS_FIRST_JULES_ON_DEMAND" || "$autorefill" != "false" || "$required" != "false" ]]; then
  echo "JULES_OFFLOAD_ERROR=registry_contract_mismatch model=${model:-MISSING} automaticRefill=$autorefill required=$required" >&2
  exit 2
fi

case "${WORKER_ID:-}" in
  JULES_A|JULES_B|JULES_C|JULES_D)
    echo "JULES_OFFLOAD_NOOP worker=${WORKER_ID} reason=LEGACY_WORKER_DISABLED"
    exit 0
    ;;
esac

# A lane terminal must never chain another Jules run automatically. The
# scheduled task/controller owns review, recovery, closure and the next direct
# execution decision. This prevents Jules from becoming the critical path.
if [[ "${1:-}" == "--post-terminal" ]]; then
  echo "JULES_POST_TERMINAL_NO_AUTOFILL worker=${WORKER_ID:-UNKNOWN} action=RETURN_TO_TASK_CONTROLLER"
  exit 0
fi

# Calls from the historical "Reserve NEXT" workflow step intentionally no-op.
# A real offload call must carry both an explicit approval bit and the exact
# material scope. Approval is per invocation and is never inferred from an idle
# worker, queue depth, backlog target, schedule, or prior Jules success.
if [[ "${VAEP_JULES_OFFLOAD_APPROVED:-FALSE}" != "TRUE" ]]; then
  echo "JULES_OFFLOAD_NOOP worker=${WORKER_ID:-UNKNOWN} reason=EXPLICIT_APPROVAL_REQUIRED execution_model=$model"
  exit 0
fi

: "${VAEP_JULES_OFFLOAD_SCOPE:?VAEP_JULES_OFFLOAD_SCOPE required when offload is approved}"
: "${WORKER_ID:?WORKER_ID required when offload is approved}"

case "$WORKER_ID" in
  J1|J2|J3|J4|J5|J6) ;;
  *) echo "JULES_OFFLOAD_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;
esac

[[ -f "$CATALOG" ]] || { echo "JULES_OFFLOAD_ERROR=catalog_missing" >&2; exit 2; }

# The lower-level core retains the exact-head, dependency, dedupe, attempt and
# transport safety checks. Reaching it now means a controller has already made
# an explicit offload decision; it does NOT imply LISTO_REAL or ACTIVE_REAL.
echo "JULES_OFFLOAD_APPROVED worker=$WORKER_ID scope=$VAEP_JULES_OFFLOAD_SCOPE branch=$BRANCH"
exec bash .github/scripts/vaep-jules-autorefill-core.sh
