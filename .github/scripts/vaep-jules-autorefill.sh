#!/usr/bin/env bash
set -euo pipefail

readonly BRANCH="Desarrollo"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly ADMISSION_PATH="vaep/control/dispatch-admission.json"

: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY required}"
: "${GH_TOKEN:?GH_TOKEN required}"
: "${WORKER_ID:?WORKER_ID required}"

command -v gh >/dev/null 2>&1 || { echo "AUTOREFILL_ERROR=gh_missing" >&2; exit 2; }
command -v jq >/dev/null 2>&1 || { echo "AUTOREFILL_ERROR=jq_missing" >&2; exit 2; }
test -f "$CATALOG" || { echo "AUTOREFILL_ERROR=catalog_missing" >&2; exit 2; }

case "$WORKER_ID" in
  JULES_A) DISPATCH_PATH="vaep/jules/dispatch" ;;
  JULES_B) DISPATCH_PATH="vaep/jules-b/dispatch" ;;
  JULES_C) DISPATCH_PATH="vaep/jules-c/dispatch" ;;
  JULES_D) DISPATCH_PATH="vaep/jules-d/dispatch" ;;
  *) echo "AUTOREFILL_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;
esac
readonly DISPATCH_PATH

api() {
  gh api "$@"
}

current_head() {
  api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha'
}

lane_workflow_name() {
  case "$WORKER_ID" in
    JULES_A) printf '%s\n' "VAEP Jules A Trusted Secondary Worker" ;;
    JULES_B) printf '%s\n' "VAEP Jules B Trusted Secondary Worker" ;;
    JULES_C) printf '%s\n' "VAEP Jules C Trusted Secondary Worker" ;;
    JULES_D) printf '%s\n' "VAEP Jules D Trusted Secondary Worker" ;;
  esac
}

lane_workflow_file() {
  case "$WORKER_ID" in
    JULES_A) printf '%s\n' "vaep-jules-secondary.yml" ;;
    JULES_B) printf '%s\n' "vaep-jules-secondary-b.yml" ;;
    JULES_C) printf '%s\n' "vaep-jules-secondary-c.yml" ;;
    JULES_D) printf '%s\n' "vaep-jules-secondary-d.yml" ;;
  esac
}

other_live_lane_run_exists() {
  local name runs count current_id
  name="$(lane_workflow_name)"
  current_id="${GITHUB_RUN_ID:-0}"
  runs="$(api "repos/$GITHUB_REPOSITORY/actions/runs?branch=$BRANCH&per_page=100")"
  count="$(jq --arg name "$name" --argjson current "$current_id" '
    [.workflow_runs[]?
      | select(.name==$name)
      | select(.id != $current)
      | select(.status=="queued" or .status=="in_progress" or .status=="pending")
    ] | length' <<<"$runs")"
  (( count > 0 ))
}

admission_open() {
  local payload
  if ! payload="$(api "repos/$GITHUB_REPOSITORY/contents/$ADMISSION_PATH?ref=$BRANCH" 2>/dev/null)"; then
    echo "AUTOREFILL_WAIT=admission_unavailable"
    return 1
  fi
  local decoded
  decoded="$(jq -r '.content' <<<"$payload" | tr -d '\n' | base64 -d)"
  jq -e '.newDispatchAdmission=="OPEN" and .allowExistingActiveSessions==true' <<<"$decoded" >/dev/null
}

is_control_plane_path() {
  case "$1" in
    vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|vaep/control/*|docs/VAEP_AUTHORITY.md|.github/scripts/vaep-*|.github/workflows/vaep-*)
      return 0 ;;
    *) return 1 ;;
  esac
}

functional_head() {
  local sha="$1" commit parent all_control path
  for _ in $(seq 1 60); do
    commit="$(api "repos/$GITHUB_REPOSITORY/commits/$sha")"
    all_control=true
    while IFS= read -r path; do
      [[ -n "$path" ]] || continue
      if ! is_control_plane_path "$path"; then
        all_control=false
        break
      fi
    done < <(jq -r '.files[]?.filename' <<<"$commit")

    if [[ "$all_control" == false ]]; then
      printf '%s\n' "$sha"
      return 0
    fi
    parent="$(jq -r '.parents[0].sha // empty' <<<"$commit")"
    [[ -n "$parent" ]] || break
    sha="$parent"
  done
  printf '%s\n' "$1"
}

causal_freeze_active() {
  local head="$1" functional runs count
  functional="$(functional_head "$head")"
  runs="$(api "repos/$GITHUB_REPOSITORY/actions/runs?branch=$BRANCH&per_page=100")"
  count="$(jq --arg sha "$functional" '[.workflow_runs[]? | select(.head_sha==$sha) | select(.status=="queued" or .status=="in_progress") | select(.name | test("Development|Acceptance|Fase ?8|M13|Recovery";"i"))] | length' <<<"$runs")"
  if (( count > 0 )); then
    echo "AUTOREFILL_WAIT=HEAD_FREEZE_CAUSAL functional_head=$functional active_critical=$count"
    return 0
  fi
  return 1
}

path_exists_on_branch() {
  local path="$1"
  api "repos/$GITHUB_REPOSITORY/contents/$path?ref=$BRANCH" >/dev/null 2>&1
}

select_next_entry() {
  local entry dispatch path
  while IFS= read -r entry; do
    dispatch="$(jq -r '.dispatchId' <<<"$entry")"
    path="$DISPATCH_PATH/$dispatch.json"
    if ! path_exists_on_branch "$path"; then
      printf '%s\n' "$entry"
      return 0
    fi
  done < <(jq -c --arg w "$WORKER_ID" '.lanes[$w][]?' "$CATALOG")
  return 1
}

dispatch_lane_workflow() {
  local workflow started runs run_id status
  workflow="$(lane_workflow_file)"
  started="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

  api "repos/$GITHUB_REPOSITORY/actions/workflows/$workflow/dispatches" \
    --method POST \
    -f ref="$BRANCH" >/dev/null

  for _ in $(seq 1 12); do
    runs="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/$workflow/runs?branch=$BRANCH&event=workflow_dispatch&per_page=10")"
    run_id="$(jq -r --arg started "$started" '[.workflow_runs[]? | select(.created_at >= $started)] | sort_by(.created_at) | reverse | .[0].id // empty' <<<"$runs")"
    if [[ -n "$run_id" ]]; then
      status="$(jq -r --argjson id "$run_id" '.workflow_runs[]? | select(.id==$id) | .status' <<<"$runs")"
      echo "AUTOREFILL_WORKFLOW_DISPATCHED worker=$WORKER_ID workflow=$workflow run_id=$run_id status=$status"
      return 0
    fi
    sleep 2
  done

  echo "AUTOREFILL_ERROR=workflow_dispatch_not_observed worker=$WORKER_ID workflow=$workflow" >&2
  return 4
}

create_atomic_manifest_commit() {
  local entry="$1" dispatch task scope prompt path head base_commit base_tree manifest blob tree commit payload rc
  dispatch="$(jq -r '.dispatchId' <<<"$entry")"
  task="$(jq -r '.taskId' <<<"$entry")"
  scope="$(jq -r '.fileScopeHint' <<<"$entry")"
  prompt="$(jq -r '.prompt' <<<"$entry")"
  path="$DISPATCH_PATH/$dispatch.json"

  for attempt in $(seq 1 8); do
    if path_exists_on_branch "$path"; then
      echo "AUTOREFILL_ALREADY_RESERVED worker=$WORKER_ID dispatch=$dispatch"
      return 0
    fi

    head="$(current_head)"
    if causal_freeze_active "$head"; then
      return 0
    fi

    manifest="$(jq -n \
      --arg dispatchId "$dispatch" \
      --arg taskId "$task" \
      --arg workerId "$WORKER_ID" \
      --arg expectedBranch "$BRANCH" \
      --arg fileScopeHint "$scope" \
      --arg prompt "$prompt" \
      --arg primaryBaseHead "$head" \
      '{dispatchId:$dispatchId,taskId:$taskId,workerId:$workerId,expectedBranch:$expectedBranch,taskAttempt:1,fileScopeHint:$fileScopeHint,prompt:$prompt,primaryBaseHead:$primaryBaseHead}')"

    blob="$(jq -n --arg content "$manifest" '{content:$content,encoding:"utf-8"}' | \
      api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - --jq '.sha')"
    base_commit="$(api "repos/$GITHUB_REPOSITORY/git/commits/$head")"
    base_tree="$(jq -r '.tree.sha' <<<"$base_commit")"
    tree="$(jq -n --arg base "$base_tree" --arg path "$path" --arg sha "$blob" \
      '{base_tree:$base,tree:[{path:$path,mode:"100644",type:"blob",sha:$sha}]}' | \
      api "repos/$GITHUB_REPOSITORY/git/trees" --method POST --input - --jq '.sha')"
    commit="$(jq -n --arg message "chore(vaep): autorefill $WORKER_ID $task" --arg tree "$tree" --arg parent "$head" \
      '{message:$message,tree:$tree,parents:[$parent]}' | \
      api "repos/$GITHUB_REPOSITORY/git/commits" --method POST --input - --jq '.sha')"

    payload="$(jq -n --arg sha "$commit" '{sha:$sha,force:false}')"
    set +e
    jq -n --arg sha "$commit" '{sha:$sha,force:false}' | \
      api "repos/$GITHUB_REPOSITORY/git/refs/heads/$BRANCH" --method PATCH --input - >/dev/null 2>&1
    rc=$?
    set -e

    if [[ "$rc" -eq 0 ]]; then
      echo "AUTOREFILL_RESERVED worker=$WORKER_ID dispatch=$dispatch task=$task commit=$commit base=$head"
      dispatch_lane_workflow
      return 0
    fi
    echo "AUTOREFILL_RETRY worker=$WORKER_ID dispatch=$dispatch attempt=$attempt reason=head_race"
    sleep 1
  done

  echo "AUTOREFILL_ERROR=head_race_exhausted worker=$WORKER_ID dispatch=$dispatch" >&2
  return 3
}

main() {
  if ! admission_open; then
    echo "AUTOREFILL_WAIT=dispatch_admission_not_open worker=$WORKER_ID"
    exit 0
  fi

  if other_live_lane_run_exists; then
    echo "AUTOREFILL_RESERVE_EXISTS worker=$WORKER_ID action=no_new_manifest"
    exit 0
  fi

  local head entry
  head="$(current_head)"
  if causal_freeze_active "$head"; then
    exit 0
  fi

  if ! entry="$(select_next_entry)"; then
    echo "AUTOREFILL_NO_SAFE_NEXT worker=$WORKER_ID catalog_exhausted=true"
    exit 0
  fi

  create_atomic_manifest_commit "$entry"
}

main "$@"
