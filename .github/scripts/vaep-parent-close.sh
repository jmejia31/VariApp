#!/usr/bin/env bash
set -euo pipefail

readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly ADMISSION="vaep/control/dispatch-admission.json"
readonly BRANCH="Desarrollo"

require_runtime() {
  : "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY required}"
  : "${GH_TOKEN:?GH_TOKEN required}"
  command -v gh >/dev/null 2>&1 || { echo 'VAEP_CLOSE_ERROR=gh_missing' >&2; exit 2; }
  command -v jq >/dev/null 2>&1 || { echo 'VAEP_CLOSE_ERROR=jq_missing' >&2; exit 2; }
}

api() {
  gh api "$@"
}

current_head() {
  api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha'
}

is_control_plane_path() {
  case "$1" in
    vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|vaep/control/*|vaep/evidence/*|docs/VAEP_AUTHORITY.md|.github/scripts/vaep-*|.github/workflows/vaep-*)
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

latest_valid_fragment() {
  local parent="$1" file
  while IFS= read -r file; do
    [[ -f "$file" ]] || continue
    if jq -e --arg parent "$parent" '
      type == "object" and
      .authority == "docs/VAEP_AUTHORITY.md" and
      .parent == $parent and
      .decision == "LISTO_REAL" and
      (.functionalHead | type == "string" and test("^[0-9a-fA-F]{40}$")) and
      .review == "PASS" and
      .combinedStatus == "SUCCESS" and
      .causalGates == "TERMINAL_SUCCESS_APPLICABLE" and
      .p0Open == 0 and .p1Open == 0 and
      .productionTouched == false and .mergePerformed == false
    ' "$file" >/dev/null; then
      printf '%s\n' "$file"
      return 0
    fi
  done < <(find vaep/evidence/fragments -maxdepth 1 -type f -name "${parent}_LISTO_REAL_*.json" -printf '%T@ %p\n' 2>/dev/null | sort -rn | cut -d' ' -f2-)
  return 1
}

critical_gates_ok() {
  local functional="$1" build acceptance recovery
  # The repository-wide Actions listing is capped at the newest 100 runs and
  # can omit an otherwise valid causal gate. Query each canonical workflow by
  # exact functional HEAD instead of relying on that truncated aggregate.
  build="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/desarrollo-ci.yml/runs?branch=$BRANCH&head_sha=$functional&per_page=10")"
  acceptance="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/catalogos-aceptacion.yml/runs?branch=$BRANCH&head_sha=$functional&per_page=10")"
  recovery="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/migration-recovery-desarrollo.yml/runs?branch=$BRANCH&head_sha=$functional&per_page=10")"
  jq -e '
    ([.workflow_runs[]? | select(.conclusion == "success")] | length > 0) and
    ([.workflow_runs[]? | select(.status == "queued" or .status == "in_progress" or .status == "pending")] | length == 0)
  ' <<<"$build" >/dev/null &&
    jq -e '
      ([.workflow_runs[]? | select(.conclusion == "success")] | length > 0) and
      ([.workflow_runs[]? | select(.status == "queued" or .status == "in_progress" or .status == "pending")] | length == 0)
    ' <<<"$acceptance" >/dev/null &&
    jq -e '
      ([.workflow_runs[]? | select(.conclusion == "success")] | length > 0) and
      ([.workflow_runs[]? | select(.status == "queued" or .status == "in_progress" or .status == "pending")] | length == 0)
    ' <<<"$recovery" >/dev/null
}

live_jules_runs() {
  local runs
  runs="$(api "repos/$GITHUB_REPOSITORY/actions/runs?branch=$BRANCH&per_page=100")"
  jq '[.workflow_runs[]? | select(.name | test("^VAEP Jules [ABCD] Trusted Secondary Worker$")) | select(.status == "queued" or .status == "in_progress" or .status == "pending")] | length' <<<"$runs"
}

remote_catalog() {
  api "repos/$GITHUB_REPOSITORY/contents/$CATALOG?ref=$BRANCH" | jq -r '.content' | tr -d '\n' | base64 -d
}

remote_admission() {
  api "repos/$GITHUB_REPOSITORY/contents/$ADMISSION?ref=$BRANCH" | jq -r '.content' | tr -d '\n' | base64 -d
}

next_parent() {
  local current candidate
  current="$(jq -r '.currentParent' "$CATALOG")"
  while IFS= read -r candidate; do
    [[ "$candidate" > "$current" ]] || continue
    printf '%s\n' "$candidate"
    return 0
  done < <(jq -r '.lanes[][]? | .plannedParent // empty' "$CATALOG" | sort -u | sort -V)
  return 1
}

publish_promotion() {
  local expected_head="$1" catalog_file="$2" admission_file="$3" catalog_blob admission_blob base_commit base_tree tree commit
  catalog_blob="$(jq -n --arg content "$(<"$catalog_file")" '{content:$content,encoding:"utf-8"}')"
  admission_blob="$(jq -n --arg content "$(<"$admission_file")" '{content:$content,encoding:"utf-8"}')"
  catalog_sha="$(api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - <<<"$catalog_blob" --jq '.sha')"
  admission_sha="$(api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - <<<"$admission_blob" --jq '.sha')"
  base_commit="$(api "repos/$GITHUB_REPOSITORY/git/commits/$expected_head")"
  base_tree="$(jq -r '.tree.sha' <<<"$base_commit")"
  tree="$(jq -n --arg base "$base_tree" --arg c "$catalog_sha" --arg a "$admission_sha" '{base_tree:$base,tree:[{path:"vaep/control/jules-autorefill-catalog.json",mode:"100644",type:"blob",sha:$c},{path:"vaep/control/dispatch-admission.json",mode:"100644",type:"blob",sha:$a}]}' | api "repos/$GITHUB_REPOSITORY/git/trees" --method POST --input - --jq '.sha')"
  commit="$(jq -n --arg tree "$tree" --arg parent "$expected_head" '{message:"chore(vaep): close current parent and advance automation",tree:$tree,parents:[$parent]}' | api "repos/$GITHUB_REPOSITORY/git/commits" --method POST --input - --jq '.sha')"
  if jq -n --arg sha "$commit" '{sha:$sha,force:false}' | api "repos/$GITHUB_REPOSITORY/git/refs/heads/$BRANCH" --method PATCH --input - >/dev/null 2>&1; then
    echo "VAEP_PARENT_PROMOTED=true commit=$commit"
    return 0
  fi
  echo "VAEP_PARENT_PROMOTION_RACE=true commit=$commit action=REFRESH_AND_CONTINUE"
  return 0
}

main() {
  local parent next fragment head functional remote_parent now tmp_catalog tmp_admission live
  require_runtime
  [[ -f "$MASTER_FILE" && -f "$PARSER" && -f "$CATALOG" && -f "$ADMISSION" ]] || { echo 'VAEP_CLOSE=BLOCKED reason=control_files_missing'; return 0; }
  [[ "$(bash "$PARSER" --get PARENT_CLOSE_FIRST "$MASTER_FILE")" == "TRUE" ]] || { echo 'VAEP_CLOSE=BLOCKED reason=master_parent_close_policy'; return 0; }
  parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  [[ -n "$parent" ]] || { echo 'VAEP_CLOSE=BLOCKED reason=current_parent_missing'; return 0; }
  fragment="$(latest_valid_fragment "$parent" || true)"
  if [[ -z "$fragment" ]]; then
    echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=LISTO_REAL_EVIDENCE_MISSING"
    return 0
  fi
  head="$(current_head)"
  functional="$(functional_head "$head")"
  if [[ "$(jq -r '.functionalHead' "$fragment")" != "$functional" ]]; then
    echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=FUNCTIONAL_HEAD_MISMATCH fragment=$(jq -r '.functionalHead' "$fragment") actual=$functional"
    return 0
  fi
  if ! critical_gates_ok "$functional"; then
    echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=CAUSAL_GATES_NOT_TERMINAL_OR_SUCCESS functional_head=$functional"
    return 0
  fi
  live="$(live_jules_runs)"
  if (( live > 0 )); then
    echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=LIVE_JULES_RUNS count=$live"
    return 0
  fi
  next="$(next_parent || true)"
  if [[ -z "$next" ]]; then
    echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=NEXT_PARENT_MISSING"
    return 0
  fi
  remote_parent="$(remote_catalog | jq -r '.currentParent // empty')"
  if [[ "$remote_parent" != "$parent" ]]; then
    echo "VAEP_PARENT_PROMOTION_ALREADY_PRESENT=true current_parent=$remote_parent expected_parent=$parent"
    return 0
  fi
  now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  tmp_catalog="$(mktemp)"
  tmp_admission="$(mktemp)"
  trap 'rm -f "$tmp_catalog" "$tmp_admission"' EXIT
  jq --arg next "$next" --arg now "$now" --arg parent "$parent" '
    .currentParent=$next |
    .generatedAt=$now |
    .regenerationReason=("AUTOMATIC_PARENT_CLOSE__" + $parent + "_LISTO_REAL__" + $next + "_CURRENT") |
    .lanes |= with_entries(.value |= map(if (.plannedParent // "") == $next then .dispatchEligible=true else .dispatchEligible=false end))
  ' "$CATALOG" > "$tmp_catalog"
  jq --arg now "$now" --arg parent "$parent" --arg next "$next" '.reason=("VAEP automatic verified closure: " + $parent + " LISTO_REAL; " + $next + " is the only dependency-valid CURRENT_PARENT. Exact gates and P0/P1 were validated by the closure governor.") | .updatedAtUtc=$now' "$ADMISSION" > "$tmp_admission"
  publish_promotion "$head" "$tmp_catalog" "$tmp_admission"
}

if [[ "${1:-}" == "--self-test" ]]; then
  [[ -f "$MASTER_FILE" && -f "$PARSER" ]] || exit 2
  echo 'VAEP_PARENT_CLOSE_SELF_TEST=PASS'
  exit 0
fi

main "$@"
