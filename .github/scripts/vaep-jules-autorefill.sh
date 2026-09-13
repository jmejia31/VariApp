#!/usr/bin/env bash
set -euo pipefail

readonly REGISTRY="vaep/control/jules-workers.json"

# Compatibility shim for historical workflow steps. Autonomous Jules refill is
# retired under TASKS_FIRST_JULES_ON_DEMAND. This script NEVER creates a
# manifest, moves HEAD or dispatches a Jules run.
#
# A scheduled VAEP task/controller that decides JULES_OFFLOAD_APPROVED must
# materialize exactly one scope-specific manifest itself after fresh HEAD,
# dependency, dedupe and non-overlap checks. The trusted J1-J6 workflow then
# consumes that manifest. There is deliberately no generic queue selector here.

command -v jq >/dev/null 2>&1 || { echo "JULES_SHIM_ERROR=jq_missing" >&2; exit 2; }
[[ -f "$REGISTRY" ]] || { echo "JULES_SHIM_ERROR=registry_missing" >&2; exit 2; }

model="$(jq -r '.executionModel // empty' "$REGISTRY")"
autorefill="$(jq -r 'if has("automaticRefillEnabled") then .automaticRefillEnabled else true end' "$REGISTRY")"
required="$(jq -r 'if has("julesRequiredForProgress") then .julesRequiredForProgress else true end' "$REGISTRY")"

if [[ "$model" != "TASKS_FIRST_JULES_ON_DEMAND" || "$autorefill" != "false" || "$required" != "false" ]]; then
  echo "JULES_SHIM_ERROR=registry_contract_mismatch model=${model:-MISSING} automaticRefill=$autorefill required=$required" >&2
  exit 2
fi

case "${WORKER_ID:-}" in
  JULES_A|JULES_B|JULES_C|JULES_D)
    echo "JULES_SHIM_NOOP worker=${WORKER_ID} reason=LEGACY_WORKER_DISABLED"
    exit 0
    ;;
esac

if [[ "${1:-}" == "--post-terminal" ]]; then
  echo "JULES_POST_TERMINAL_NO_AUTOFILL worker=${WORKER_ID:-UNKNOWN} action=RETURN_TO_TASK_CONTROLLER"
  exit 0
fi

if [[ "${VAEP_JULES_OFFLOAD_APPROVED:-FALSE}" == "TRUE" ]]; then
  : "${WORKER_ID:?WORKER_ID required for an approved offload}"
  : "${VAEP_JULES_OFFLOAD_SCOPE:?VAEP_JULES_OFFLOAD_SCOPE required for an approved offload}"
  case "$WORKER_ID" in
    J1|J2|J3|J4|J5|J6) ;;
    *) echo "JULES_SHIM_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;
  esac
  echo "JULES_OFFLOAD_REQUIRES_EXACT_CONTROLLER_MANIFEST worker=$WORKER_ID scope=$VAEP_JULES_OFFLOAD_SCOPE action=NO_AUTOMATIC_MATERIALIZATION"
  exit 0
fi

echo "JULES_SHIM_NOOP worker=${WORKER_ID:-UNKNOWN} reason=TASKS_FIRST_DIRECT_EXECUTION"
exit 0
